# Progression Coordinator Architecture

## Overview

The Progression Coordinator is the **orchestration layer** that manages interactions between Quest System, Lead System, Dialogue System, NPC System, and Time System. It ensures these systems work together cohesively without creating tight coupling.

**Key Responsibilities:**

- Coordinate Quest ↔ Lead interactions
- Sync dialogue outcomes with game state
- Trigger content generation based on time
- Update NPC schedules and device locations
- Handle cascading events without circular dependencies

---

## Design Philosophy

### Why a Coordinator?

**Problem:** Systems need to react to each other's changes:
- Lead created → Check if unlocks quests
- Quest unlocked → Update related leads
- Dialogue ended → Create leads, unlock quests, modify relationships
- Hour changed → Update NPC schedules → Move devices → Update network topology

**Without Coordinator (Bad):**
```csharp
// Tight coupling - systems know about each other
public class LeadManager
{
    private IQuestManager questManager; // ❌ LeadManager knows about QuestManager
    
    public void CreateLead(Lead lead)
    {
        // ...
        questManager.CheckIfLeadUnlocksQuests(lead); // ❌ Direct call
    }
}
```

**With Coordinator (Good):**
```csharp
// Loose coupling - coordinator mediates
public class LeadManager
{
    public event Action<Lead> OnLeadCreated; // ✅ Just emit event
    
    public void CreateLead(Lead lead)
    {
        // ...
        OnLeadCreated?.Invoke(lead); // ✅ Coordinator will handle
    }
}

public class ProgressionCoordinator
{
    public void Initialize()
    {
        leadManager.OnLeadCreated += (lead) => 
        {
            questManager.CheckIfLeadUnlocksQuests(lead);
        };
    }
}
```

---

## Architecture

### Component Hierarchy

```
ProgressionCoordinator (IProgressionCoordinator) - Service
  ├── Mediates between systems
  ├── Subscribes to system events
  ├── Orchestrates multi-system operations
  ├── Prevents circular dependencies
  └── Handles event priority and ordering

Managed Systems:
  ├── QuestManager
  ├── LeadManager
  ├── DialogueService
  ├── TimeManager
  ├── NPCScheduler
  └── NPCContentGenerator
```

---

## Service Interface

```csharp
public interface IProgressionCoordinator : IGameService
{
    // === Initialization ===
    void Initialize();
    
    // === Manual Coordination ===
    void CoordinateDialogueOutcome(string npcId, List<string> flagsSet);
    void CoordinateQuestCompletion(Quest quest);
    void CoordinateDeviceCompromise(Device device);
    void CoordinateNPCMovement(string npcId, PhysicalLocation newLocation);
    
    // === State Queries ===
    bool IsCoordinationInProgress { get; }
    int PendingCoordinationTasks { get; }
    
    // === Events ===
    event Action<string> OnCoordinationStarted;
    event Action<string> OnCoordinationCompleted;
}
```

---

## Implementation

### Core Coordinator

```csharp
public class ProgressionCoordinator : IProgressionCoordinator
{
    // === System References ===
    private IQuestManager questManager;
    private ILeadManager leadManager;
    private IDialogueService dialogueService;
    private ITimeManager timeManager;
    private INPCScheduler npcScheduler;
    private INPCContentGenerator contentGenerator;
    private INPCManager npcManager;
    private IDeviceRegistry deviceRegistry;
    private INetworkTopology networkTopology;
    
    // === Coordination State ===
    private bool isCoordinationInProgress;
    private int pendingCoordinationTasks;
    private Stack<string> coordinationStack;
    
    // === Events ===
    public event Action<string> OnCoordinationStarted;
    public event Action<string> OnCoordinationCompleted;
    
    public bool IsCoordinationInProgress => isCoordinationInProgress;
    public int PendingCoordinationTasks => pendingCoordinationTasks;
    
    // === Initialization ===
    
    public void Initialize()
    {
        // Get system references
        questManager = ServiceLocator.Get<IQuestManager>();
        leadManager = ServiceLocator.Get<ILeadManager>();
        dialogueService = ServiceLocator.Get<IDialogueService>();
        timeManager = ServiceLocator.Get<ITimeManager>();
        npcScheduler = ServiceLocator.Get<INPCScheduler>();
        contentGenerator = ServiceLocator.Get<INPCContentGenerator>();
        npcManager = ServiceLocator.Get<INPCManager>();
        deviceRegistry = ServiceLocator.Get<IDeviceRegistry>();
        networkTopology = ServiceLocator.Get<INetworkTopology>();
        
        coordinationStack = new Stack<string>();
        
        // Wire up event handlers
        SetupLeadQuestCoordination();
        SetupDialogueCoordination();
        SetupTimeCoordination();
        SetupNPCCoordination();
        
        Debug.Log("[ProgressionCoordinator] Initialized and wired all system events");
    }
    
    // === Lead ↔ Quest Coordination ===
    
    private void SetupLeadQuestCoordination()
    {
        // Lead created → Check if unlocks quests
        leadManager.OnLeadCreated += (lead) =>
        {
            BeginCoordination("LeadCreated");
            
            try
            {
                questManager.CheckIfLeadUnlocksQuests(lead);
            }
            finally
            {
                EndCoordination("LeadCreated");
            }
        };
        
        // Quest unlocked → Update related leads
        questManager.OnQuestUnlocked += (quest) =>
        {
            BeginCoordination("QuestUnlocked");
            
            try
            {
                UpdateLeadsForQuest(quest);
            }
            finally
            {
                EndCoordination("QuestUnlocked");
            }
        };
        
        // Quest completed → Resolve related leads
        questManager.OnQuestCompleted += (quest) =>
        {
            BeginCoordination("QuestCompleted");
            
            try
            {
                ResolveLeadsForQuest(quest);
                CoordinateQuestCompletion(quest);
            }
            finally
            {
                EndCoordination("QuestCompleted");
            }
        };
        
        // Objective completed → Update lead progress
        questManager.OnObjectiveCompleted += (quest, objective) =>
        {
            BeginCoordination("ObjectiveCompleted");
            
            try
            {
                UpdateLeadsForObjective(quest, objective);
            }
            finally
            {
                EndCoordination("ObjectiveCompleted");
            }
        };
    }
    
    private void UpdateLeadsForQuest(Quest quest)
    {
        foreach (var leadId in quest.RelatedLeadIds)
        {
            var lead = leadManager.GetLead(leadId);
            if (lead != null)
            {
                // Add quest to lead's related quests
                leadManager.LinkLeadToQuest(leadId, quest.QuestId);
                
                // Update lead description
                if (!lead.Description.Contains(quest.QuestName))
                {
                    lead.Description += $"\n\n📋 Related Quest: {quest.QuestName}";
                }
            }
        }
    }
    
    private void ResolveLeadsForQuest(Quest quest)
    {
        foreach (var leadId in quest.RelatedLeadIds)
        {
            var lead = leadManager.GetLead(leadId);
            if (lead != null && lead.State == LeadState.Active)
            {
                leadManager.ResolveLead(leadId);
            }
        }
    }
    
    private void UpdateLeadsForObjective(Quest quest, QuestObjective objective)
    {
        foreach (var leadId in objective.RelatedLeadIds)
        {
            var lead = leadManager.GetLead(leadId);
            if (lead != null)
            {
                // Calculate progress
                float progress = CalculateLeadProgress(lead, quest);
                leadManager.UpdateLeadProgress(leadId, progress);
            }
        }
    }
    
    private float CalculateLeadProgress(Lead lead, Quest quest)
    {
        var relevantObjectives = quest.Objectives
            .Where(o => o.RelatedLeadIds.Any(id => 
                lead.RelatedDeviceIds.Contains(id) || 
                lead.RelatedNPCIds.Contains(id)));
        
        if (!relevantObjectives.Any())
            return lead.InvestigationProgress;
        
        int total = relevantObjectives.Count();
        int completed = relevantObjectives.Count(o => o.IsComplete);
        
        return (float)completed / total;
    }
    
    // === Dialogue Coordination ===
    
    private void SetupDialogueCoordination()
    {
        // Dialogue ended → Sync all changes
        dialogueService.OnDialogueEnded += (npcId) =>
        {
            BeginCoordination("DialogueEnded");
            
            try
            {
                CoordinateDialogueOutcome(npcId, null);
            }
            finally
            {
                EndCoordination("DialogueEnded");
            }
        };
        
        // Node reached → Check for auto-lead generation
        dialogueService.OnNodeReached += (npcId, nodeId) =>
        {
            CheckDialogueForLeads(npcId, nodeId);
        };
    }
    
    public void CoordinateDialogueOutcome(string npcId, List<string> flagsSet)
    {
        // After dialogue:
        // 1. Relationship changes were already applied (via Ink external functions)
        // 2. Conversation flags were set
        // 3. Now check if anything unlocks
        
        // Check for new quest unlocks
        questManager.CheckForNewQuests();
        
        // Check if relationship milestones trigger events
        var relationship = npcManager.GetRelationship(npcId);
        if (relationship != null)
        {
            CheckRelationshipMilestones(npcId, relationship);
        }
    }
    
    private void CheckDialogueForLeads(string npcId, string nodeId)
    {
        // Check if this dialogue node should generate leads
        // Example: If NPC mentions another NPC, create a "Person" lead
        
        // This would be configured in a DialogueLeadMap
        // For now, simplified:
        if (nodeId.Contains("mention_phoenix"))
        {
            leadManager.CreateLeadFromNPC("phoenix", "Mysterious hacker mentioned by Sarah");
        }
    }
    
    private void CheckRelationshipMilestones(string npcId, NPCRelationship relationship)
    {
        var npc = npcManager.GetNPC(npcId);
        if (npc == null) return;
        
        // Check for milestone unlocks
        if (relationship.TrustLevel >= 70 && !relationship.Milestones.Contains("high_trust"))
        {
            relationship.Milestones.Add("high_trust");
            Debug.Log($"[ProgressionCoordinator] {npc.Name} reached high trust milestone");
            
            // This might unlock quests
            questManager.CheckForNewQuests();
        }
        
        if (relationship.RomanceLevel >= 50 && !relationship.Milestones.Contains("romance_started"))
        {
            relationship.Milestones.Add("romance_started");
            Debug.Log($"[ProgressionCoordinator] Romance started with {npc.Name}");
        }
    }
    
    // === Time Coordination ===
    
    private void SetupTimeCoordination()
    {
        // Hour changed → NPC schedule updates → Device movement
        timeManager.OnHourChanged += (hour) =>
        {
            BeginCoordination("HourChanged");
            
            try
            {
                // NPCScheduler already subscribed directly (high priority)
                // We don't need to call it here
                
                // But we can do additional coordination
                CheckTimeBasedEvents(hour);
            }
            finally
            {
                EndCoordination("HourChanged");
            }
        };
        
        // Day changed → Content generation → Quest condition checks
        timeManager.OnDayChanged += (day) =>
        {
            BeginCoordination("DayChanged");
            
            try
            {
                CoordinateDayChange(day);
            }
            finally
            {
                EndCoordination("DayChanged");
            }
        };
        
        // Time consumed → Check quest time limits
        timeManager.OnTimeConsumed += (newTime) =>
        {
            questManager.CheckQuestFailures();
        };
    }
    
    private void CheckTimeBasedEvents(int hour)
    {
        // Check for timed quest unlocks
        questManager.CheckForNewQuests();
        
        // Check for timed NPC events
        // e.g., "Sarah sends email at 9 AM on Mondays"
        if (hour == 9 && timeManager.CurrentGameTime.DayOfWeek == DayOfWeek.Monday)
        {
            // Trigger event
        }
    }
    
    private void CoordinateDayChange(DayOfWeek day)
    {
        Debug.Log($"[ProgressionCoordinator] Coordinating day change to {day}");
        
        // 1. Generate daily content (already handled by ContentGenerator subscription)
        // contentGenerator.GenerateDailyContent(); // Already subscribed
        
        // 2. Check for daily quest unlocks
        questManager.CheckForNewQuests();
        
        // 3. Reset daily NPC states (if any)
        // e.g., shopkeepers restock, daily quests reset
        
        // 4. Generate news articles, forum posts, etc.
        // (handled by ContentGenerator)
    }
    
    // === NPC Coordination ===
    
    private void SetupNPCCoordination()
    {
        // NPC moved → Device moved → Network updated
        npcManager.OnNPCMoved += (npcId, newLocation) =>
        {
            BeginCoordination("NPCMoved");
            
            try
            {
                CoordinateNPCMovement(npcId, newLocation);
            }
            finally
            {
                EndCoordination("NPCMoved");
            }
        };
    }
    
    public void CoordinateNPCMovement(string npcId, PhysicalLocation newLocation)
    {
        var npc = npcManager.GetNPC(npcId);
        if (npc == null) return;
        
        Debug.Log($"[ProgressionCoordinator] Coordinating movement of {npc.Name} to {newLocation.Name}");
        
        // Move devices is already handled by NPCScheduler
        // But we can do additional coordination:
        
        // 1. Check if player is following this NPC (for quests)
        var followQuests = questManager.GetActiveQuests()
            .Where(q => q.Objectives.Any(o => 
                o.Type == ObjectiveType.ReachLocation && 
                o.RelatedLeadIds.Contains(npcId)));
        
        foreach (var quest in followQuests)
        {
            // Update objective progress
            // (will be checked automatically by QuestManager)
        }
        
        // 2. If NPC enters player's location, trigger encounter
        var playerLocation = ServiceLocator.Get<IPlayerManager>().GetCurrentLocation();
        if (playerLocation == newLocation)
        {
            GameEvents.Publish(GameEventType.NPCEncountered, npcId);
        }
    }
    
    // === Device Coordination ===
    
    public void CoordinateDeviceCompromise(Device device)
    {
        BeginCoordination("DeviceCompromised");
        
        try
        {
            Debug.Log($"[ProgressionCoordinator] Coordinating compromise of {device.Hostname}");
            
            // 1. Update related leads
            var relatedLeads = leadManager.GetActiveLeads()
                .Where(l => l.RelatedDeviceIds.Contains(device.DeviceId));
            
            foreach (var lead in relatedLeads)
            {
                lead.Description += "\n\n✓ Device compromised!";
                leadManager.UpdateLeadProgress(lead.LeadId, 1.0f); // Mark complete
            }
            
            // 2. Check quest objectives
            questManager.CheckQuestProgressAfterEvent();
            
            // 3. Trigger content generation (reactive layer)
            // e.g., target NPC notices and sends concerned email
            TriggerReactiveContent(device);
        }
        finally
        {
            EndCoordination("DeviceCompromised");
        }
    }
    
    private void TriggerReactiveContent(Device device)
    {
        // Find NPC who owns this device
        var owner = npcManager.GetAllNPCs()
            .FirstOrDefault(npc => npc.OwnedDeviceIds.Contains(device.DeviceId));
        
        if (owner != null)
        {
            contentGenerator.GenerateReactiveFile(
                owner.NpcId,
                $"security_alert_{device.DeviceId}.txt",
                $"Security Alert: Unauthorized access detected on {device.Hostname}"
            );
        }
    }
    
    // === Quest Completion Coordination ===
    
    public void CoordinateQuestCompletion(Quest quest)
    {
        Debug.Log($"[ProgressionCoordinator] Coordinating completion of {quest.QuestName}");
        
        // 1. Resolve related leads (already done in SetupLeadQuestCoordination)
        
        // 2. Unlock new locations
        foreach (var locationId in quest.Rewards.UnlockedLocationIds)
        {
            GameEvents.Publish(GameEventType.LocationUnlocked, locationId);
        }
        
        // 3. Unlock new devices
        foreach (var deviceId in quest.Rewards.UnlockedDeviceIds)
        {
            var device = deviceRegistry.GetDevice(deviceId);
            if (device != null)
            {
                // Make device accessible
                device.IsDiscovered = true;
                
                // Create lead for newly unlocked device
                leadManager.CreateLeadFromDevice(device);
            }
        }
        
        // 4. Unlock new exploits
        foreach (var exploitId in quest.Rewards.UnlockedExploitIds)
        {
            ServiceLocator.Get<IExploitManager>()?.UnlockExploit(exploitId);
        }
        
        // 5. Check for new quest unlocks (quest chain progression)
        questManager.CheckForNewQuests();
        
        // 6. Generate celebration content (emails from quest giver, etc.)
        if (!string.IsNullOrEmpty(quest.QuestGiverNpcId))
        {
            contentGenerator.GenerateQuestCompletionContent(quest);
        }
    }
    
    // === Coordination State Management ===
    
    private void BeginCoordination(string coordinationType)
    {
        coordinationStack.Push(coordinationType);
        pendingCoordinationTasks++;
        
        if (!isCoordinationInProgress)
        {
            isCoordinationInProgress = true;
            OnCoordinationStarted?.Invoke(coordinationType);
        }
    }
    
    private void EndCoordination(string coordinationType)
    {
        if (coordinationStack.Count > 0 && coordinationStack.Peek() == coordinationType)
        {
            coordinationStack.Pop();
        }
        
        pendingCoordinationTasks--;
        
        if (pendingCoordinationTasks <= 0)
        {
            pendingCoordinationTasks = 0;
            isCoordinationInProgress = false;
            OnCoordinationCompleted?.Invoke(coordinationType);
        }
    }
}
```

---

## Event Priority Management

### Guaranteed Execution Order

The coordinator ensures events fire in the correct order:

```csharp
public class EventPriorityManager
{
    // Priority levels:
    // 100+: Core state changes (NPC movement, device location)
    // 50-99: Gameplay systems (quests, leads)
    // 0-49: UI updates, analytics
    
    public void SetupPriorities()
    {
        // === High Priority (100+) ===
        // NPCScheduler updates NPC locations
        timeManager.SubscribeToHourChanged(
            npcScheduler.OnHourChanged, 
            priority: 100
        );
        
        // === Normal Priority (50-99) ===
        // ProgressionCoordinator checks time-based events
        timeManager.SubscribeToHourChanged(
            CheckTimeBasedEvents, 
            priority: 50
        );
        
        // === Low Priority (0-49) ===
        // UIManager updates clock display
        timeManager.SubscribeToHourChanged(
            uiManager.UpdateClock, 
            priority: 10
        );
    }
}
```

### Preventing Circular Dependencies

```csharp
public class CircularDependencyDetector
{
    private HashSet<string> activeCoordinations = new HashSet<string>();
    
    public void BeginCoordination(string type)
    {
        if (activeCoordinations.Contains(type))
        {
            Debug.LogError($"[ProgressionCoordinator] CIRCULAR DEPENDENCY DETECTED: {type}");
            Debug.LogError($"Active coordinations: {string.Join(", ", activeCoordinations)}");
            return;
        }
        
        activeCoordinations.Add(type);
    }
    
    public void EndCoordination(string type)
    {
        activeCoordinations.Remove(type);
    }
}
```

---

## Async/Deferred Coordination

### When to Use Async

```csharp
public class ProgressionCoordinator
{
    // === Synchronous: Time-critical operations ===
    private void UpdateLeadsForQuest(Quest quest)
    {
        // Must happen immediately
        foreach (var leadId in quest.RelatedLeadIds)
        {
            var lead = leadManager.GetLead(leadId);
            leadManager.UpdateLeadProgress(leadId, 1.0f);
        }
    }
    
    // === Asynchronous: Heavy operations ===
    private async void CoordinateDayChange(DayOfWeek day)
    {
        // Content generation can happen in background
        await Task.Run(() =>
        {
            contentGenerator.GenerateDailyContent();
        });
        
        // Quest checks happen after content generation
        questManager.CheckForNewQuests();
    }
    
    // === Deferred: Batched operations ===
    private List<Action> deferredActions = new List<Action>();
    
    public void DeferCoordination(Action action)
    {
        deferredActions.Add(action);
    }
    
    public void ProcessDeferredCoordinations()
    {
        foreach (var action in deferredActions)
        {
            action();
        }
        deferredActions.Clear();
    }
}
```

---

## Cascading Event Example

### Example Flow: Player Hacks Device

```
1. Player executes exploit
   ↓
2. HackingService publishes DeviceCompromised event
   ↓
3. ProgressionCoordinator.CoordinateDeviceCompromise()
   ├── Updates related leads (mark as complete)
   ├── Checks quest objectives (may complete quest)
   └── Triggers reactive content (NPC sends alert email)
   ↓
4. Quest completed (if objective met)
   ↓
5. ProgressionCoordinator.CoordinateQuestCompletion()
   ├── Resolves related leads
   ├── Unlocks new devices (creates new leads)
   ├── Checks for new quest unlocks
   └── Generates celebration content
   ↓
6. New quest unlocked
   ↓
7. ProgressionCoordinator.UpdateLeadsForQuest()
   └── Links quest to existing leads
```

**Key:** Each step emits events that trigger the next, but coordinator prevents infinite loops and ensures correct ordering.

---

## Performance Considerations

### Batching Coordinations

```csharp
public class CoordinationBatcher
{
    private Dictionary<string, List<object>> batchedEvents = new Dictionary<string, List<object>>();
    private float batchWindow = 0.1f; // 100ms
    private Coroutine flushCoroutine;
    
    public void BatchCoordination(string type, object data)
    {
        if (!batchedEvents.ContainsKey(type))
            batchedEvents[type] = new List<object>();
        
        batchedEvents[type].Add(data);
        
        // Reset flush timer
        if (flushCoroutine != null)
            StopCoroutine(flushCoroutine);
        
        flushCoroutine = StartCoroutine(FlushAfterDelay());
    }
    
    private IEnumerator FlushAfterDelay()
    {
        yield return new WaitForSeconds(batchWindow);
        
        // Process all batched coordinations at once
        foreach (var kvp in batchedEvents)
        {
            ProcessBatchedCoordination(kvp.Key, kvp.Value);
        }
        
        batchedEvents.Clear();
    }
    
    private void ProcessBatchedCoordination(string type, List<object> dataList)
    {
        // Example: Multiple devices compromised in quick succession
        if (type == "DeviceCompromised")
        {
            // Process all at once instead of individually
            var devices = dataList.Cast<Device>().ToList();
            CoordinateMultipleDeviceCompromises(devices);
        }
    }
}
```

---

## Debug Tools

### Coordination Visualizer

```csharp
public class CoordinationVisualizer : EditorWindow
{
    [MenuItem("Tools/Coordination Visualizer")]
    public static void ShowWindow()
    {
        GetWindow<CoordinationVisualizer>("Coordination Visualizer");
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to visualize coordination", MessageType.Info);
            return;
        }
        
        var coordinator = ServiceLocator.Get<IProgressionCoordinator>();
        
        EditorGUILayout.LabelField("Coordination State", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"In Progress: {coordinator.IsCoordinationInProgress}");
        EditorGUILayout.LabelField($"Pending Tasks: {coordinator.PendingCoordinationTasks}");
        
        EditorGUILayout.Space();
        
        // Show coordination stack
        EditorGUILayout.LabelField("Active Coordinations:", EditorStyles.boldLabel);
        var stack = coordinator.GetCoordinationStack();
        foreach (var item in stack)
        {
            EditorGUILayout.LabelField($"  • {item}");
        }
    }
}
```

### Coordination Logger

```csharp
public class CoordinationLogger
{
    private List<CoordinationEvent> eventLog = new List<CoordinationEvent>();
    private const int MAX_LOG_SIZE = 1000;
    
    public void LogCoordination(string type, string details)
    {
        eventLog.Add(new CoordinationEvent
        {
            Type = type,
            Details = details,
            Timestamp = DateTime.Now
        });
        
        // Prune old logs
        if (eventLog.Count > MAX_LOG_SIZE)
        {
            eventLog.RemoveAt(0);
        }
        
        Debug.Log($"[Coordination] {type}: {details}");
    }
    
    public void ExportLog(string filePath)
    {
        var json = JsonUtility.ToJson(new { events = eventLog }, prettyPrint: true);
        File.WriteAllText(filePath, json);
    }
}
```

---

## Troubleshooting

### Issue: Infinite Loop Detected

**Symptom:** Game freezes, stack overflow

**Cause:** Circular coordination (A triggers B, B triggers A)

**Solution:**

```csharp
private void BeginCoordination(string type)
{
    if (coordinationStack.Contains(type))
    {
        Debug.LogError($"CIRCULAR COORDINATION: {type} already in stack!");
        Debug.LogError($"Stack: {string.Join(" → ", coordinationStack)}");
        return; // Prevent recursion
    }
    
    coordinationStack.Push(type);
}
```

### Issue: Events Firing in Wrong Order

**Symptom:** Quest completes before lead updates

**Cause:** Priority not set correctly

**Solution:** Use event priority subscription (see Event Priority Management)

### Issue: Coordination Never Completes

**Symptom:** `IsCoordinationInProgress` stuck at true

**Cause:** Exception thrown, `EndCoordination()` not called

**Solution:**

```csharp
private void UpdateLeadsForQuest(Quest quest)
{
    BeginCoordination("UpdateLeadsForQuest");
    
    try
    {
        // ... coordination logic
    }
    catch (Exception e)
    {
        Debug.LogError($"Coordination failed: {e.Message}");
    }
    finally
    {
        EndCoordination("UpdateLeadsForQuest"); // ← Always called
    }
}
```

---

## Quick Start Guide

### 1. Initialize Coordinator

```csharp
// In ServiceLocator initialization (after all other systems):
var progressionCoordinator = new ProgressionCoordinator();
progressionCoordinator.Initialize();
ServiceLocator.Register<IProgressionCoordinator>(progressionCoordinator);
```

### 2. Let Systems Emit Events

```csharp
// Systems just emit events, coordinator handles coordination
public class LeadManager
{
    public Lead CreateLead(...)
    {
        // ... create lead
        OnLeadCreated?.Invoke(lead); // Coordinator will handle
        return lead;
    }
}
```

### 3. Manual Coordination (Optional)

```csharp
// For complex multi-system operations:
var coordinator = ServiceLocator.Get<IProgressionCoordinator>();
coordinator.CoordinateDeviceCompromise(device);
```

---

## Summary

The Progression Coordinator achieves:

- ✅ **Loose coupling**: Systems don't directly call each other
- ✅ **Event orchestration**: Mediates between system events
- ✅ **Correct ordering**: Ensures events fire in proper sequence
- ✅ **Prevents circular deps**: Detects and blocks infinite loops
- ✅ **Handles cascading events**: Coordinates multi-step operations
- ✅ **Performance optimized**: Batching, async where appropriate
- ✅ **Debug-friendly**: Visualizer, logger, error detection
