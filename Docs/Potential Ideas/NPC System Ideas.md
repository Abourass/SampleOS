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

```csharp
public interface IRelationshipService
{
    void ModifyTrust(string npcId, int delta);
    void ModifyRomance(string npcId, int delta);
    int GetTrust(string npcId);
    int GetRomance(string npcId);
    List<string> GetNPCsByTrust(int minTrust);
    void MarkDialogueSeen(string npcId, string dialogueNode);
    bool HasSeenDialogue(string npcId, string dialogueNode);
}

public class RelationshipService : IRelationshipService
{
    // Internal storage
    private Dictionary<string, NPCRelationship> relationships = new();
    
    public void ModifyTrust(string npcId, int delta)
    {
        var rel = GetOrCreateRelationship(npcId);
        rel.TrustLevel = Mathf.Clamp(rel.TrustLevel + delta, 0, 100);
        
        // Trigger events
        GameEvents.Instance.Trigger(GameEventType.RelationshipChanged, rel);
        
        // Check for milestones
        CheckRelationshipMilestones(rel);
    }
    
    private void CheckRelationshipMilestones(NPCRelationship rel)
    {
        if (rel.TrustLevel >= 70 && !rel.HasReachedMilestone("high_trust"))
        {
            rel.MarkMilestone("high_trust");
            GameEvents.Instance.Trigger(GameEventType.RelationshipMilestone, rel);
            
            // This could unlock new dialogue, quests, etc.
        }
    }
}
```

**Pros:**
- ✅ **Single responsibility**: One service handles all relationship logic
- ✅ **Centralized events**: All trust changes go through one place
- ✅ **Easy to extend**: Add reputation with factions, group standings, etc.
- ✅ **Business logic encapsulation**: Milestone checking, decay over time, etc.
- ✅ **Easy to test**: Mock the service in unit tests
- ✅ **Flexible storage**: Could swap backend (database, cloud save) easily

**Cons:**
- ❌ **Another service**: Adds to service count
- ❌ **Indirection**: Can't do `npc.TrustLevel`, must query service

---

### **My Recommendation: Option C (Relationship Service)**

This fits your service architecture perfectly. You'll have:
- `INPCManager` - Core data storage (NPCs, relationships, dialogue state)
- `IRelationshipService` - Relationship business logic (trust milestones, romance progression)
- `IDialogueService` - Dialogue flow (Ink integration, node tracking)
- `INPCScheduler` - Schedule updates (time-based location changes)
- `INPCContentGenerator` - File generation (emails, documents)

**Key insight**: Relationships are *player progression*, not NPC properties. They belong in the same architectural tier as `PlayerProgress`, `QuestManager`, etc.

**Why 5 services is okay**:

- They're thin facades (50-150 lines each, not 500+ monolithic)
- Each has a clear, single purpose (easy for new devs to understand)
- They delegate storage to NPCManager (no sync issues)
- They can evolve independently (add romance system without touching dialogue)

---

## NPC Schedule System Architecture

### **Option A: Simple Time-Based Schedule**

```csharp
[System.Serializable]
public class NPCSchedule
{
    public List<ScheduleEntry> Entries;
}

[System.Serializable]
public class ScheduleEntry
{
    public DayOfWeek Day;
    public int Hour; // 0-23
    public string LocationId;
    public string Activity; // "working", "sleeping", "eating"
}

// Usage
public PhysicalLocation GetLocationForTime(DayOfWeek day, int hour)
{
    var entry = Entries
        .Where(e => e.Day == day && e.Hour <= hour)
        .OrderByDescending(e => e.Hour)
        .FirstOrDefault();
        
    return LocationDatabase.GetLocation(entry?.LocationId);
}
```

**Pros:**
- ✅ **Simple**: Easy to understand and debug
- ✅ **Data-driven**: Can store in JSON/ScriptableObjects
- ✅ **Predictable**: Same schedule every week
- ✅ **Designer-friendly**: Non-programmers can edit schedules

**Cons:**
- ❌ **Static**: No dynamic behavior (e.g., "Sarah goes to gym only if stressed")
- ❌ **Memory**: Large schedules (7 days × 24 hours = 168 entries per NPC)
- ❌ **No exceptions**: Can't handle "Sarah works from home on rainy days"

---

### **Option B: State Machine Schedule**

```csharp
public class NPCSchedule
{
    private NPCState currentState;
    private Dictionary<NPCState, ScheduleStateData> states;
    
    public void Update(NPC npc, DateTime currentTime)
    {
        var stateData = states[currentState];
        
        // Check transitions
        foreach (var transition in stateData.Transitions)
        {
            if (transition.Condition.Evaluate(npc, currentTime))
            {
                TransitionTo(transition.TargetState, npc);
                break;
            }
        }
    }
}

public enum NPCState
{
    Sleeping,
    WorkingAtOffice,
    WorkingFromHome,
    Eating,
    Exercising,
    Socializing,
    Hacking // For certain NPCs!
}

public class ScheduleTransition
{
    public NPCState TargetState;
    public IScheduleCondition Condition;
}

// Example conditions
public class TimeRangeCondition : IScheduleCondition
{
    public int StartHour;
    public int EndHour;
    
    public bool Evaluate(NPC npc, DateTime time)
    {
        return time.Hour >= StartHour && time.Hour < EndHour;
    }
}

public class WeatherCondition : IScheduleCondition
{
    public WeatherType RequiredWeather;
    
    public bool Evaluate(NPC npc, DateTime time)
    {
        return WeatherSystem.Instance.CurrentWeather == RequiredWeather;
    }
}
```

**Pros:**
- ✅ **Dynamic**: Can react to game state (weather, quests, stress)
- ✅ **Flexible**: Complex schedules without massive data tables
- ✅ **Reusable states**: "Working" state shared across NPCs
- ✅ **Event-driven**: Can interrupt schedules (emergency calls NPC to work)

**Cons:**
- ❌ **Complex**: Harder to understand and debug
- ❌ **Programming required**: Can't easily hand off to designers
- ❌ **Hard to visualize**: What's Sarah's schedule? Need to trace state machine

---

### **Option C: Hybrid (Default Schedule + Overrides)**

```csharp
public class NPCSchedule
{
    // Base schedule (simple time table)
    public List<ScheduleEntry> DefaultSchedule;
    
    // Dynamic overrides
    public List<ScheduleOverride> Overrides;
    
    public PhysicalLocation GetLocationForTime(NPC npc, DateTime time)
    {
        // Check overrides first (quest-based, event-based)
        foreach (var override in Overrides)
        {
            if (override.IsActive(npc, time))
                return override.GetLocation(npc, time);
        }
        
        // Fall back to default schedule
        return GetDefaultLocation(time);
    }
}

public abstract class ScheduleOverride
{
    public int Priority; // Higher priority overrides win
    public abstract bool IsActive(NPC npc, DateTime time);
    public abstract PhysicalLocation GetLocation(NPC npc, DateTime time);
}

// Example: Quest overrides schedule
public class QuestScheduleOverride : ScheduleOverride
{
    public string QuestId;
    public string LocationId;
    
    public override bool IsActive(NPC npc, DateTime time)
    {
        var questManager = ServiceLocator.Instance.Get<IQuestManager>();
        var quest = questManager.GetQuest(QuestId);
        return quest != null && quest.State == QuestState.Active;
    }
    
    public override PhysicalLocation GetLocation(NPC npc, DateTime time)
    {
        return LocationDatabase.GetLocation(LocationId);
    }
}
```

**Pros:**
- ✅ **Best of both**: Simple default + dynamic overrides
- ✅ **Designer-friendly defaults**: Time tables for normal behavior
- ✅ **Programmer-friendly overrides**: Code for special cases
- ✅ **Quest integration**: Quests can temporarily change NPC locations
- ✅ **Debuggable**: Can see what's overriding the schedule

**Cons:**
- ❌ **Two systems to maintain**: Default schedule + override system
- ❌ **Potential conflicts**: Multiple overrides could fight for control

---

### **My Recommendation: Option C (Hybrid)**

Your game needs:
- **Simple schedules** for most NPCs (cafe owner always at cafe 9am-5pm)
- **Dynamic overrides** for quest-related changes (Sarah works late when project deadline approaches)
- **Easy authoring** for 50+ NPCs

Hybrid gives you the best balance.

---

## NPC File Generation System

### **Option A: Template-Based Generation**

```csharp
public class NPCFileGenerator
{
    public void GenerateFilesForNPC(NPC npc, DateTime currentTime)
    {
        var device = GetNPCDevice(npc);
        if (device == null) return;
        
        // Generate emails
        if (ShouldGenerateEmail(npc, currentTime))
        {
            var template = GetEmailTemplate(npc.Profession, npc.CurrentQuestState);
            var email = PopulateTemplate(template, npc);
            AddEmailToDevice(device, email);
        }
        
        // Generate documents
        if (ShouldGenerateDocument(npc, currentTime))
        {
            var docTemplate = GetDocumentTemplate(npc);
            var document = PopulateTemplate(docTemplate, npc);
            AddFileToDevice(device, document);
        }
    }
    
    private VirtualNode PopulateTemplate(FileTemplate template, NPC npc)
    {
        var content = template.Content;
        
        // Replace placeholders
        content = content.Replace("{NPC_NAME}", npc.DisplayName);
        content = content.Replace("{DATE}", TimeManager.Instance.CurrentGameTime.ToString("d"));
        content = content.Replace("{RANDOM_PROJECT}", GetRandomProjectName());
        
        return new VirtualNode(template.Filename, content, NodeType.File);
    }
}

// Template example
public class FileTemplate
{
    public string Filename;
    public string Content;
    public NPCProfession ApplicableTo;
    public float GenerationChance; // 0-1
}

// Stored in Resources or ScriptableObjects
/*
Templates/sarah_work_email.txt:
---
From: boss@bigtechcorp.com
To: {NPC_NAME}@bigtechcorp.com
Subject: Project {RANDOM_PROJECT} Update

Hey {NPC_NAME},

How's the {RANDOM_PROJECT} coming along? We need this done by {DEADLINE_DATE}.

Thanks,
Marcus
---
*/
```

**Pros:**
- ✅ **Content-driven**: Writers can create templates without coding
- ✅ **Variety**: Multiple templates = varied content
- ✅ **Easy to localize**: Templates can be translated
- ✅ **Modular**: Templates in separate files, easy to add more

**Cons:**
- ❌ **Static feel**: Templates can feel repetitive
- ❌ **Limited logic**: Hard to do complex generation ("email thread about recent hack")
- ❌ **Manual work**: Need to create lots of templates

---

### **Option B: Procedural Generation**

```csharp
public class NPCFileGenerator
{
    private MarkovChainGenerator emailGenerator;
    private NameGenerator projectNameGenerator;
    
    public VirtualNode GenerateEmail(NPC npc, EmailContext context)
    {
        var email = new StringBuilder();
        
        // Generate header
        email.AppendLine($"From: {GetRandomCoworker(npc)}@{npc.Company}.com");
        email.AppendLine($"To: {npc.EmailAddress}");
        email.AppendLine($"Subject: {GenerateEmailSubject(context)}");
        email.AppendLine();
        
        // Generate body using Markov chains or GPT-style generation
        var body = emailGenerator.GenerateText(
            seed: npc.Profession,
            context: context,
            minLength: 50,
            maxLength: 200
        );
        email.AppendLine(body);
        
        // Add signature
        email.AppendLine();
        email.AppendLine("Thanks,");
        email.AppendLine(GetRandomCoworker(npc));
        
        return new VirtualNode($"email_{Guid.NewGuid()}.eml", email.ToString(), NodeType.File);
    }
}
```

**Pros:**
- ✅ **Infinite variety**: Never runs out of content
- ✅ **Dynamic**: Can react to game state (emails about recent player hacks!)
- ✅ **Less authoring**: Don't need hundreds of templates

**Cons:**
- ❌ **Quality concerns**: Generated text might not make sense
- ❌ **Hard to control**: Can generate inappropriate content
- ❌ **Complex implementation**: Markov chains, grammar systems, etc.
- ❌ **No story control**: Can't ensure specific plot-relevant emails appear

---

### **Option C: Event-Driven File Creation**

```csharp
public class NPCFileGenerator
{
    public void Initialize()
    {
        // Subscribe to game events
        GameEvents.Instance.Subscribe(GameEventType.QuestProgressed, OnQuestProgressed);
        GameEvents.Instance.Subscribe(GameEventType.DeviceCompromised, OnDeviceCompromised);
        GameEvents.Instance.Subscribe(GameEventType.DayChanged, OnDayChanged);
        GameEvents.Instance.Subscribe(GameEventType.RelationshipChanged, OnRelationshipChanged);
    }
    
    private void OnQuestProgressed(object data)
    {
        var quest = data as Quest;
        
        // Quest progressed - add relevant files to NPC devices
        if (quest.QuestId == "corporate_espionage")
        {
            var sarah = npcManager.GetNPC("sarah");
            var sarahLaptop = GetNPCDevice(sarah, "laptop");
            
            // Add email from boss about project
            var email = CreateEmail(
                from: "boss@bigtechcorp.com",
                to: sarah.EmailAddress,
                subject: "Project Phoenix - Classified",
                body: "Sarah, I need you to keep this project under wraps..."
            );
            
            AddFileToDevice(sarahLaptop, email);
            
            // Create lead for player when they read this
            GameEvents.Instance.Subscribe(GameEventType.FileRead, (fileData) =>
            {
                if (fileData == email)
                {
                    var leadManager = ServiceLocator.Instance.Get<ILeadManager>();
                    leadManager.CreateLead(
                        LeadType.Mystery,
                        "Project Phoenix",
                        "Sarah's boss mentioned a classified project...",
                        new LeadSource { Type = LeadSourceType.EmailRead, SourceId = sarah.NpcId }
                    );
                }
            });
        }
    }
    
    private void OnDayChanged(object data)
    {
        var day = (DayOfWeek)data;
        
        // Every Monday, working NPCs get weekly meeting emails
        if (day == DayOfWeek.Monday)
        {
            var workingNPCs = npcManager.GetNPCsByProfession(NPCProfession.TechWorker);
            foreach (var npc in workingNPCs)
            {
                GenerateWeeklyMeetingEmail(npc);
            }
        }
    }
    
    private void OnDeviceCompromised(object data)
    {
        var device = data as Device;
        
        // Player hacked a device - generate "incident response" emails
        var deviceOwner = npcManager.GetNPCByDeviceId(device.DeviceId);
        if (deviceOwner != null)
        {
            // IT admin sends "unusual activity" email
            var itAdmin = npcManager.GetNPCByRole(NPCRole.ITAdmin);
            if (itAdmin != null)
            {
                GenerateSecurityAlertEmail(itAdmin, deviceOwner, device);
            }
        }
    }
}
```

**Pros:**
- ✅ **Reactive world**: Files appear in response to player actions
- ✅ **Story integration**: Quest progress generates relevant content
- ✅ **Feels dynamic**: "The world reacts to me!"
- ✅ **Controlled**: You decide exactly what files appear and when
- ✅ **Plot-relevant**: Can ensure clues appear at the right time

**Cons:**
- ❌ **Requires planning**: Need to script file creation for each event
- ❌ **Can feel scripted**: Players might notice patterns
- ❌ **Manual work**: Each quest/event needs file generation logic

---

### **Option D: Hybrid (My Recommendation)**

```csharp
public class NPCFileGenerator
{
    // Layer 1: Scheduled "ambient" file generation (templates)
    private void GenerateAmbientFiles()
    {
        // Every day, NPCs get routine emails from templates
        foreach (var npc in npcManager.GetAllNPCs())
        {
            if (Random.value < 0.3f) // 30% chance per day
            {
                GenerateRandomEmailFromTemplate(npc);
            }
        }
    }
    
    // Layer 2: Event-driven "story" file generation (scripted)
    private void OnQuestProgressed(object data)
    {
        // Important plot files are scripted
        CreateSpecificStoryFile(quest);
    }
    
    // Layer 3: Dynamic "reactive" file generation (procedural)
    private void OnDeviceCompromised(object data)
    {
        // System reacts to player actions with generated content
        GenerateDynamicSecurityResponse(device);
    }
}
```

**Why Hybrid:**
1. **Ambient templates** make NPCs feel alive (random work emails, personal notes)
2. **Story-driven events** ensure plot clues appear when needed
3. **Reactive generation** makes the world respond to player actions

This gives you:
- Variety (templates)
- Control (event-driven)
- Dynamism (procedural reactions)

---

## Complete NPC Architecture Recommendation

```csharp
// Core NPC data (pure C# class)
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

// Manager service
public interface INPCManager
{
    void Initialize();
    NPC GetNPC(string npcId);
    List<NPC> GetNPCsAtLocation(PhysicalLocation location);
    List<NPC> GetNPCsByProfession(NPCProfession profession);
    void UpdateNPCSchedules(DateTime currentTime);
}

// Separate relationship service
public interface IRelationshipService
{
    void ModifyTrust(string npcId, int delta);
    int GetTrust(string npcId);
    List<string> GetTrustedNPCs(int minTrust);
}

// File generation service
public interface INPCContentGenerator
{
    void GenerateDailyContent();
    void GenerateEventDrivenContent(GameEventType eventType, object eventData);
    void AddFileToNPCDevice(string npcId, string deviceType, VirtualNode file);
}
```

**Service dependency order:**
1. `TimeManager` (needed by schedules)
2. `DeviceRegistry` (needed to track NPC devices)
3. `NPCManager` (core NPC data)
4. `RelationshipService` (queries NPCManager)
5. `NPCContentGenerator` (queries NPCManager, DeviceRegistry)

**Does this NPC architecture align with your vision? Any concerns or adjustments needed?**
