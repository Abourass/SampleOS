# NPC System Architecture

## Overview

Our NPC system is designed to support a large number of NPCs (100+) across multiple cities, with dynamic schedules, relationships, dialogue, and content generation. The architecture prioritizes:

- **Performance**: Minimal overhead for NPCs not currently visible
- **Persistence**: Easy save/load of NPC state
- **Flexibility**: Quest-driven schedule changes, relationship progression
- **Service Integration**: Works seamlessly with our existing service architecture

---

## Core Architecture: Hybrid Data + Visual Approach

NPCs use a **hybrid approach** that separates logical state from visual representation:

```csharp
// Core NPC data (always in memory, lightweight)
[System.Serializable]
public class NPC
{
    public string NpcId;
    public string DisplayName;
    public NPCProfession Profession;
    public PhysicalLocation CurrentLocation;
    public List<string> OwnedDeviceIds;
    public NPCSchedule Schedule;
    
    // Visual representation (null when not visible)
    public NPCVisual Visual { get; set; }
}

// Lightweight visual component (only spawned when NPC is visible)
public class NPCVisual : MonoBehaviour
{
    private NPC npcData;
    private Animator animator;
    
    public void Initialize(NPC data)
    {
        npcData = data;
        animator = GetComponent<Animator>();
    }
    
    void Update()
    {
        // Only handles visuals: animation, facing direction, etc.
        // All logic stays in NPCManager
    }
}
```

### Why Hybrid?

- **Scalability**: Can have 1000 NPCs in memory, but only spawn visuals for the 20 the player can see
- **Performance**: One manager update handles all NPC logic instead of 100+ MonoBehaviour Update() calls
- **Persistence**: Plain C# objects serialize easily for save/load
- **Testability**: Can unit test NPC logic without Unity runtime
- **Unity Integration**: When needed, spawn full GameObjects with animation, audio, physics

---

## Service Architecture: Modular Facades

Our NPC system uses **modular service facades** with centralized data storage:

### Core Design

**Storage:** All NPC-related data is stored in `INPCManager` (single source of truth)

**APIs:** Thin facade services provide clean, domain-specific interfaces

```csharp
// === Core Manager (Stores ALL NPC data) ===
public interface INPCManager
{
    // Core data access
    NPC GetNPC(string npcId);
    List<NPC> GetAllNPCs();
    List<NPC> GetNPCsAtLocation(PhysicalLocation location);
    
    // Internal state access (used by facades)
    NPCRelationship GetRelationship(string npcId);
    NPCSchedule GetSchedule(string npcId);
    HashSet<string> GetSeenDialogueNodes(string npcId);
    
    // Events
    event Action<string, NPCRelationship> OnRelationshipChanged;
    event Action<string, PhysicalLocation> OnNPCMoved;
}

// === Relationship Facade (Clean API for relationship logic) ===
public interface IRelationshipService
{
    void ModifyTrust(string npcId, int delta);
    void ModifyRomance(string npcId, int delta);
    int GetTrust(string npcId);
    int GetRomance(string npcId);
    List<string> GetNPCsByTrust(int minTrust);
}

public class RelationshipService : IRelationshipService
{
    private INPCManager npcManager; // Delegates to manager for data
    
    public void ModifyTrust(string npcId, int delta)
    {
        // Get data from NPCManager
        var relationship = npcManager.GetRelationship(npcId);
        
        // Apply business logic HERE
        relationship.TrustLevel = Mathf.Clamp(relationship.TrustLevel + delta, 0, 100);
        CheckRelationshipMilestones(relationship);
        
        // Trigger events
        GameEvents.Instance.Trigger(GameEventType.TrustChanged, relationship);
    }
    
    private void CheckRelationshipMilestones(NPCRelationship rel)
    {
        // Complex business logic stays in RelationshipService
        if (rel.TrustLevel >= 70 && !rel.Milestones.Contains("high_trust"))
        {
            rel.Milestones.Add("high_trust");
            
            // Could unlock quests, dialogue, etc.
            var questManager = ServiceLocator.Instance.Get<IQuestManager>();
            questManager.CheckForNewQuests();
        }
    }
}

// === Dialogue Facade ===
public interface IDialogueService
{
    void StartDialogue(string npcId);
    bool HasSeenNode(string npcId, string nodeId);
    void MarkNodeSeen(string npcId, string nodeId);
}

// === Scheduler Facade ===
public interface INPCScheduler
{
    void UpdateSchedules(DateTime time);
    void AddScheduleOverride(string npcId, ScheduleOverride override);
}

// === Content Generator Facade ===
public interface INPCContentGenerator
{
    void GenerateDailyContent();
    void OnGameEvent(GameEventType eventType, object data);
    void AddFileToNPCDevice(string npcId, VirtualNode file);
}
```

### Why This Architecture?

**Centralized Storage (NPCManager):**

- All NPC data lives in one place
- No synchronization issues
- Easy to save/load (one service to serialize)
- Atomic operations possible

**Modular APIs (Facades):**

- Clean, focused interfaces for each concern
- Business logic separated by domain
- Easy to test (facades are thin, can mock NPCManager)
- Multiple developers can work on different facades without conflicts

**Key Insight:** The facades don't **own** the data, they **manage** it. Think of them as "domain-specific controllers" over the central NPC state.

---

## Relationship & Dialogue Tracking

Relationships are **player progression data**, not NPC properties. They're stored in `NPCManager` but accessed through `IRelationshipService`.

```csharp
[System.Serializable]
public class NPCRelationship
{
    public string NpcId;
    public int TrustLevel; // 0-100
    public int RomanceLevel; // 0-100
    public RelationshipStatus Status; // Friendly, Romantic, Hostile, Neutral
    public List<string> Milestones; // "high_trust", "first_date", "confession", etc.
    public DateTime LastInteraction;
}
```

**Dialogue State** is also stored in `NPCManager`:

```csharp
// Per-NPC dialogue tracking
Dictionary<string, HashSet<string>> seenDialogueNodes;

// Access through DialogueService
bool hasSeenNode = dialogueService.HasSeenNode("sarah", "ask_about_phoenix");
```

### Conversation History (Detailed)

Conversation history is stored in `NPCManager` and accessed through `IDialogueService`. This centralized approach keeps all player-NPC interaction data together.

**Data Structure:**

```csharp
[System.Serializable]
public class ConversationHistory
{
    public string NpcId;
    
    // Seen dialogue nodes (Ink knot paths)
    public HashSet<string> SeenNodes = new HashSet<string>();
    
    // Choices made (for analytics/debugging)
    public List<ConversationChoice> ChoicesMade = new List<ConversationChoice>();
    
    // Conversation flags (persistent state)
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
    
    // Metadata
    public DateTime FirstInteraction;
    public DateTime LastInteraction;
    public int TotalInteractions;
}

[System.Serializable]
public class ConversationChoice
{
    public string NodeId;           // Where this choice was made
    public string ChoiceText;       // What the player chose
    public DateTime Timestamp;      // When they made this choice
}
```

**NPCManager Interface Extension:**

```csharp
public interface INPCManager
{
    // === Dialogue History ===
    void MarkDialogueNodeSeen(string npcId, string nodeId);
    bool HasSeenDialogueNode(string npcId, string nodeId);
    HashSet<string> GetSeenNodes(string npcId);
    
    void RecordDialogueChoice(string npcId, string choiceText);
    List<ConversationChoice> GetDialogueChoices(string npcId);
    
    void SetConversationFlag(string npcId, string flagName, bool value);
    bool HasConversationFlag(string npcId, string flagName);
    Dictionary<string, bool> GetConversationFlags(string npcId);
    
    void UpdateLastInteraction(string npcId, DateTime time);
    DateTime GetLastInteraction(string npcId);
    
    ConversationHistory GetConversationHistory(string npcId);
    void ClearConversationHistory(string npcId); // For debugging
}
```

**Save Strategy:**

- **Save completed nodes only** (not mid-conversation state)
- Store metadata (timestamps, choice history) for analytics
- Conversation flags persist between sessions
- No Ink story state saved (re-injected from game state on dialogue start)

**Benefits:**

- ✅ Single source of truth for all NPC-player interactions
- ✅ Easy to query: "Which NPCs has player talked to?" "What choices did they make?"
- ✅ Efficient serialization: One dictionary to save
- ✅ Works seamlessly with DialogueService (reads/writes through NPCManager)

### Why Separate from NPC Objects?

- **Player-centric:** All progression data in one conceptual space
- **Lazy initialization:** Only create relationship data when first interacting
- **Clean NPC data:** NPCs don't carry player-specific state
- **Easy comparisons:** "Top 5 trusted NPCs" is one query
- **Milestone logic:** Centralized in RelationshipService, not scattered
- **Conversation tracking:** All dialogue history accessible from one manager

---

## NPC Schedule System: Hybrid Approach

NPCs use a **hybrid schedule system**: simple default schedules with priority-based overrides.

### Default Schedule (Designer-Friendly)

Simple time-table stored in JSON or ScriptableObjects:

```csharp
[System.Serializable]
public class ScheduleEntry
{
    public DayOfWeek Day;
    public int Hour; // 0-23
    public string LocationId;
    public string Activity; // "working", "sleeping", "eating", etc.
}

// Example: Sarah's default week
var sarahSchedule = new List<ScheduleEntry>
{
    // Monday-Friday
    { Day = DayOfWeek.Monday, Hour = 7, LocationId = "sarah_apartment" },
    { Day = DayOfWeek.Monday, Hour = 8, LocationId = "bigtechcorp_sarah_desk" },
    { Day = DayOfWeek.Monday, Hour = 17, LocationId = "sarah_apartment" },
    { Day = DayOfWeek.Monday, Hour = 23, LocationId = "sarah_apartment_bedroom" },
    
    // Weekend
    { Day = DayOfWeek.Saturday, Hour = 9, LocationId = "sarah_apartment" },
    { Day = DayOfWeek.Saturday, Hour = 14, LocationId = "downtown_cafe" },
    // ...etc
};
```

### Dynamic Overrides (Programmer-Friendly)

Quests, relationships, and events can temporarily override the default schedule:

```csharp
public abstract class ScheduleOverride
{
    public int Priority; // Higher priority wins
    public abstract bool IsActive(NPC npc, DateTime time);
    public abstract PhysicalLocation GetLocation(NPC npc, DateTime time);
}

// Example: Quest-based override
public class QuestScheduleOverride : ScheduleOverride
{
    public string QuestId;
    public Dictionary<int, string> HourToLocationMap;
    
    public override int Priority => 50;
    
    public override bool IsActive(NPC npc, DateTime time)
    {
        var questManager = ServiceLocator.Instance.Get<IQuestManager>();
        var quest = questManager.GetQuest(QuestId);
        return quest != null && quest.State == QuestState.Active;
    }
    
    public override PhysicalLocation GetLocation(NPC npc, DateTime time)
    {
        if (HourToLocationMap.TryGetValue(time.Hour, out string locationId))
        {
            return LocationDatabase.GetLocation(locationId);
        }
        return null; // Fall through to default or lower-priority override
    }
}
```

### Priority Resolution

```csharp
public PhysicalLocation GetLocationForTime(NPC npc, DateTime time)
{
    // 1. Check overrides (highest priority wins)
    var activeOverrides = ActiveOverrides
        .Where(o => o.IsActive(npc, time))
        .OrderByDescending(o => o.Priority)
        .ToList();
        
    if (activeOverrides.Count > 0)
    {
        var topOverride = activeOverrides.First();
        var location = topOverride.GetLocation(npc, time);
        if (location != null)
            return location;
    }
    
    // 2. Fall back to default schedule
    return GetDefaultLocation(time);
}
```

### Override Examples

**Quest Override:** Sarah works late during "Project Phoenix Deadline"

```csharp
var phoenixDeadlineOverride = new QuestScheduleOverride
{
    QuestId = "project_phoenix_deadline",
    HourToLocationMap = new Dictionary<int, string>
    {
        [17] = "bigtechcorp_sarah_desk", // Still at work at 5pm
        [18] = "bigtechcorp_sarah_desk", // Still at work at 6pm
        [19] = "bigtechcorp_sarah_desk", // Still at work at 7pm
        [20] = "sarah_apartment"         // Finally goes home at 8pm
    }
};
```

**Relationship Override:** Sarah goes to bar on Friday nights if trust >= 70

```csharp
var fridayBarOverride = new RelationshipScheduleOverride
{
    MinTrust = 70,
    Day = DayOfWeek.Friday,
    HourToLocationMap = new Dictionary<int, string>
    {
        [18] = "downtown_bar",
        [19] = "downtown_bar",
        [20] = "downtown_bar",
        [21] = "downtown_bar",
        [22] = "sarah_apartment"
    }
};
```

**Event Override:** Sarah follows player when hacking together

```csharp
var hackingTogetherOverride = new EventScheduleOverride
{
    Priority = 100, // Highest priority
    LocationProvider = (npc, time) =>
    {
        var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
        return playerState.CurrentLocation;
    }
};
```

### Why Hybrid?

- **Easy to author:** Designers create default schedules in JSON
- **Dynamic behavior:** Quests and relationships change schedules
- **Debuggable:** Can see why NPC is at location (check priority stack)
- **Quest integration:** Add/remove overrides without touching default schedules
- **Flexible:** Can handle simple NPCs (cafe owner) and complex NPCs (Sarah)

---

## NPC File Generation: Three-Layer Hybrid

NPCs generate files on their devices using a **three-layer approach**:

### Layer 1: Ambient Content (Templates)

Random, routine files that make NPCs feel alive:

```csharp
// Template stored in Resources or ScriptableObjects
public class FileTemplate
{
    public string Filename;
    public string Content;
    public NPCProfession ApplicableTo;
    public float GenerationChance; // 0-1 per day
}

// Example template: work_email_generic.txt
/*
From: {COWORKER}@{COMPANY}.com
To: {NPC_NAME}@{COMPANY}.com
Subject: {RANDOM_PROJECT} Update

Hey {NPC_NAME},

Quick update on {RANDOM_PROJECT}. Can you review the latest changes?

Thanks,
{COWORKER}
*/

// Generated daily
private void GenerateAmbientFiles()
{
    foreach (var npc in npcManager.GetAllNPCs())
    {
        if (Random.value < 0.3f) // 30% chance per day
        {
            GenerateRandomEmailFromTemplate(npc);
        }
    }
}
```

### Layer 2: Story Content (Event-Driven)

Plot-relevant files that appear at specific moments:

```csharp
private void OnQuestProgressed(object data)
{
    var quest = data as Quest;
    
    if (quest.QuestId == "corporate_espionage")
    {
        var sarah = npcManager.GetNPC("sarah");
        var sarahLaptop = GetNPCDevice(sarah, "laptop");
        
        // Create specific story email
        var email = CreateEmail(
            from: "boss@bigtechcorp.com",
            to: sarah.EmailAddress,
            subject: "Project Phoenix - Classified",
            body: "Sarah, I need you to keep this project under wraps. " +
                  "No one outside the core team should know about Phoenix..."
        );
        
        AddFileToDevice(sarahLaptop, email);
        
        // Create lead when player reads this
        RegisterFileLeadTrigger(email, () =>
        {
            leadManager.CreateLead(
                LeadType.Mystery,
                "Project Phoenix",
                "Sarah's boss mentioned a classified project called Phoenix...",
                new LeadSource { Type = LeadSourceType.EmailRead, SourceId = "sarah" }
            );
        });
    }
}
```

### Layer 3: Reactive Content (Dynamic)

World reacts to player actions:

```csharp
private void OnDeviceCompromised(object data)
{
    var device = data as Device;
    var deviceOwner = npcManager.GetNPCByDeviceId(device.DeviceId);
    
    if (deviceOwner != null)
    {
        // IT admin sends security alert email
        var itAdmin = npcManager.GetNPCByRole(NPCRole.ITAdmin);
        if (itAdmin != null)
        {
            var alertEmail = CreateEmail(
                from: itAdmin.EmailAddress,
                to: deviceOwner.EmailAddress,
                subject: $"Unusual Activity Detected - {device.Hostname}",
                body: $"Hi {deviceOwner.DisplayName},\n\n" +
                      $"We detected unusual login activity on {device.Hostname} at {TimeManager.CurrentTime}. " +
                      $"Please verify this was you.\n\n" +
                      $"If you did not access this device, please contact IT immediately."
            );
            
            var itAdminDevice = GetNPCDevice(itAdmin, "laptop");
            AddFileToDevice(itAdminDevice, alertEmail);
        }
    }
}
```

### Why Three Layers?

- **Ambient:** Makes the world feel alive (variety)
- **Story:** Ensures plot clues appear when needed (control)
- **Reactive:** Makes the world respond to player actions (dynamism)

---

## Service Dependency Order

Services must be initialized in this order:

1. **TimeManager** (needed by schedules)
2. **DeviceRegistry** (needed to track NPC devices)
3. **NPCManager** (core NPC data storage)
4. **RelationshipService** (queries NPCManager)
5. **DialogueService** (queries NPCManager + RelationshipService)
6. **NPCScheduler** (updates NPCManager + DeviceRegistry)
7. **NPCContentGenerator** (queries NPCManager + DeviceRegistry)

```csharp
private void InitializeNPCSystems()
{
    // 1. Core NPC data
    var npcManager = new NPCManager();
    ServiceLocator.Instance.Register<INPCManager>(npcManager);
    npcManager.Initialize();
    
    // 2. Relationship facade
    var relationshipService = new RelationshipService(npcManager);
    ServiceLocator.Instance.Register<IRelationshipService>(relationshipService);
    relationshipService.Initialize();
    
    // 3. Dialogue facade
    var dialogueService = new DialogueService(npcManager, relationshipService);
    ServiceLocator.Instance.Register<IDialogueService>(dialogueService);
    dialogueService.Initialize();
    
    // 4. Scheduler
    var npcScheduler = new NPCScheduler(npcManager);
    ServiceLocator.Instance.Register<INPCScheduler>(npcScheduler);
    npcScheduler.Initialize();
    
    // 5. Content generator
    var contentGenerator = new NPCContentGenerator(npcManager);
    ServiceLocator.Instance.Register<INPCContentGenerator>(contentGenerator);
    contentGenerator.Initialize();
    
    Debug.Log("[NPC Systems] All NPC systems initialized");
}
```

---

## Summary

Our NPC system achieves:

- **Performance:** Hybrid data+visual approach, only render visible NPCs
- **Flexibility:** Schedule overrides allow quests/relationships to change behavior
- **Clean Architecture:** Modular facades with centralized storage
- **Dynamic World:** Three-layer file generation creates living, reactive NPCs
- **Service Integration:** Fits seamlessly with existing architecture

The system supports 100+ NPCs with minimal overhead, dynamic schedules, relationship progression, dialogue tracking, and emergent storytelling through file generation.
