# Quest System Architecture

## Overview

The Quest System manages story progression, side quests, job opportunities, and emergent objectives using a **hybrid event/poll system**. It's designed to handle complex dependencies, branching paths, hidden objectives, and failure states while remaining designer-friendly.

**Key Design Principles:**

- **Hybrid triggering**: Events for discrete moments, polling for continuous states
- **Flexible conditions**: Time, location, relationships, hacks all can unlock quests
- **Dependency management**: Quest chains with validation to prevent circular dependencies
- **ScriptableObject + Code hybrid**: Visual editing for data, code for complex logic
- **Coordinated with Lead System**: Quests and leads reference each other

---

## Core Architecture

### Component Hierarchy

```
QuestManager (IQuestManager) - Service
  ├── Manages all quest data (locked, active, completed, failed)
  ├── Listens to GameEvents (NPC interactions, hacks, discoveries)
  ├── Polls GameState (relationships, stats, time)
  ├── Evaluates quest conditions
  └── Coordinates with LeadManager

QuestConditionRegistry
  ├── Stores reusable condition instances
  ├── Maps condition IDs to condition objects
  └── Used by both ScriptableObjects and code

ProgressionCoordinator
  ├── Orchestrates Quest ↔ Lead interaction
  ├── Updates leads when quests unlock
  └── Checks if leads unlock new quests

Quest Data (ScriptableObjects)
  ├── High-level quest metadata
  ├── References condition IDs (not condition logic)
  └── Designer-editable in Unity Inspector
```

---

## Service Interface

```csharp
public interface IQuestManager : IGameService
{
    // === Quest State ===
    Quest GetQuest(string questId);
    List<Quest> GetActiveQuests();
    List<Quest> GetCompletedQuests();
    List<Quest> GetFailedQuests();
    List<Quest> GetAvailableQuests(); // Unlocked but not started
    
    // === Quest Progression ===
    void UnlockQuest(string questId);
    void StartQuest(string questId);
    void CompleteObjective(string questId, string objectiveId);
    void CompleteQuest(string questId);
    void FailQuest(string questId, string reason);
    
    // === Condition Checking ===
    bool CheckQuestUnlockConditions(Quest quest);
    bool IsQuestComplete(Quest quest);
    bool HasQuestFailed(Quest quest);
    
    // === Lead Integration ===
    void CheckIfLeadUnlocksQuests(Lead lead);
    List<Quest> GetQuestsForLead(string leadId);
    
    // === Events ===
    event Action<Quest> OnQuestUnlocked;
    event Action<Quest> OnQuestStarted;
    event Action<Quest, QuestObjective> OnObjectiveCompleted;
    event Action<Quest> OnQuestCompleted;
    event Action<Quest, string> OnQuestFailed;
}
```

---

## Data Structures

### Quest

```csharp
[System.Serializable]
public class Quest
{
    public string QuestId;
    public string QuestName;
    public string Description;
    public QuestType Type; // Main, Side, Job, Emergent
    public QuestCategory Category; // Hacking, Social, Investigation, Job
    
    // === Prerequisites ===
    public List<string> UnlockConditionIds;  // Reference to QuestConditionRegistry
    public List<string> RequiredCompletedQuests;   // Quest dependencies
    
    // === Objectives ===
    public List<QuestObjective> Objectives;
    public bool RequireAllObjectives = true;       // AND vs OR logic
    
    // === Progression Tracking ===
    public QuestState State = QuestState.Locked;
    public DateTime UnlockedAt;
    public DateTime StartedAt;
    public DateTime CompletedAt;
    
    // === Story/Metadata ===
    public string QuestGiverNpcId;      // NPC who gave this
    public Sprite Icon;
    public Color CategoryColor;
    
    // === Rewards ===
    public QuestRewards Rewards;
    
    // === Failure Conditions (Optional) ===
    public List<string> FailureConditionIds;
    public bool CanFail = false;
    public DateTime FailureTime; // If failed
    public string FailureReason;
    
    // === Lead Integration ===
    public List<string> RelatedLeadIds; // Leads that reference this quest
}

public enum QuestType
{
    Main,       // Main story progression
    Side,       // Optional side content
    Job,        // Career path quests
    Emergent    // Dynamically created from player actions
}

public enum QuestCategory
{
    Hacking,
    Social,
    Investigation,
    Job,
    Combat,     // Future: if combat is added
    Exploration
}

public enum QuestState
{
    Locked,     // Prerequisites not met
    Available,  // Unlocked but not started
    Active,     // Currently in progress
    Completed,  // Successfully finished
    Failed      // Failed (if CanFail = true)
}
```

### Quest Objective

```csharp
[System.Serializable]
public class QuestObjective
{
    public string ObjectiveId;
    public string Description;
    public ObjectiveType Type;
    
    // === Condition ===
    public string CompletionConditionId; // Reference to QuestConditionRegistry
    
    // === Tracking ===
    public bool IsComplete;
    public bool IsHidden; // For twist objectives (revealed mid-quest)
    public DateTime CompletedAt;
    
    // === Progress Tracking (Optional) ===
    public int CurrentProgress; // e.g., 3/5 devices hacked
    public int RequiredProgress; // e.g., 5
    
    // === Lead Integration ===
    public List<string> RelatedLeadIds; // Leads that help complete this objective
}

public enum ObjectiveType
{
    HackDevice,           // "Compromise the coffee shop POS"
    TalkToNPC,            // "Ask Sarah about the tech company"
    ObtainItem,           // "Get the leaked password database"
    ReachLocation,        // "Find the underground hacker meetup"
    AchieveReputation,    // "Reach 50 reputation with Anonymous group"
    AchieveRelationship,  // "Reach 70 trust with Sarah"
    CompleteMinigame,     // "Successfully complete your first day at BigTechCorp"
    TimeElapsed,          // "Wait until Friday"
    DiscoverLead,         // "Discover who Phoenix is"
    Custom                // For complex logic
}
```

### Quest Rewards

```csharp
[System.Serializable]
public class QuestRewards
{
    public int Money;
    public int Karma; // Can be positive (whitehat) or negative (blackhat)
    public Dictionary<string, int> ReputationChanges; // { "hackers_anonymous": +20 }
    public List<string> UnlockedExploitIds;
    public List<string> UnlockedDeviceIds; // Gain access to new devices
    public List<string> UnlockedLocationIds; // New areas unlock
    public Dictionary<StatType, int> StatIncreases; // { Charisma: +5 }
}
```

---

## Quest Condition System

### Abstract Base Class

```csharp
public abstract class QuestCondition
{
    public string ConditionId; // Unique ID for registry lookup
    public string Description; // Human-readable description
    
    public abstract bool Evaluate(IGameState gameState);
    
    // Optional: Display progress for UI
    public virtual string GetProgressText(IGameState gameState) => "";
}
```

### Concrete Condition Implementations

```csharp
// === Device Conditions ===
public class DeviceCompromisedCondition : QuestCondition
{
    public string DeviceId;
    
    public override bool Evaluate(IGameState gameState)
    {
        return gameState.Player.CompromisedDevices.Contains(DeviceId);
    }
    
    public override string GetProgressText(IGameState gameState)
    {
        return Evaluate(gameState) ? "✓ Device compromised" : "Device not compromised";
    }
}

public class DevicesCompromisedCountCondition : QuestCondition
{
    public int RequiredCount;
    public List<string> DeviceIds; // Optional: specific devices, or any devices if empty
    
    public override bool Evaluate(IGameState gameState)
    {
        if (DeviceIds.Count > 0)
        {
            int count = DeviceIds.Count(id => gameState.Player.CompromisedDevices.Contains(id));
            return count >= RequiredCount;
        }
        else
        {
            return gameState.Player.CompromisedDevices.Count >= RequiredCount;
        }
    }
    
    public override string GetProgressText(IGameState gameState)
    {
        int current = DeviceIds.Count > 0
            ? DeviceIds.Count(id => gameState.Player.CompromisedDevices.Contains(id))
            : gameState.Player.CompromisedDevices.Count;
        return $"{current}/{RequiredCount} devices compromised";
    }
}

// === Relationship Conditions ===
public class RelationshipCondition : QuestCondition
{
    public string NpcId;
    public int MinimumTrust;
    public int MaximumTrust = 100;
    
    public override bool Evaluate(IGameState gameState)
    {
        var relationship = gameState.NPCManager.GetRelationship(NpcId);
        if (relationship == null) return false;
        
        return relationship.TrustLevel >= MinimumTrust && 
               relationship.TrustLevel <= MaximumTrust;
    }
    
    public override string GetProgressText(IGameState gameState)
    {
        var relationship = gameState.NPCManager.GetRelationship(NpcId);
        int trust = relationship?.TrustLevel ?? 0;
        return $"Trust: {trust}/{MinimumTrust}";
    }
}

// === Time Conditions ===
public class TimeCondition : QuestCondition
{
    public DayOfWeek Day;
    public TimeSpan TimeOfDay;
    public ComparisonType Comparison = ComparisonType.GreaterOrEqual;
    
    public override bool Evaluate(IGameState gameState)
    {
        var currentTime = gameState.TimeManager.CurrentGameTime;
        
        bool dayMatches = Day == currentTime.DayOfWeek;
        if (!dayMatches) return false;
        
        return Comparison switch
        {
            ComparisonType.GreaterOrEqual => currentTime.TimeOfDay >= TimeOfDay,
            ComparisonType.LessOrEqual => currentTime.TimeOfDay <= TimeOfDay,
            ComparisonType.Equal => currentTime.TimeOfDay == TimeOfDay,
            _ => false
        };
    }
}

public class TimeElapsedCondition : QuestCondition
{
    public DateTime StartTime;
    public TimeSpan RequiredDuration;
    
    public override bool Evaluate(IGameState gameState)
    {
        var currentTime = gameState.TimeManager.CurrentGameTime;
        return currentTime >= StartTime + RequiredDuration;
    }
    
    public override string GetProgressText(IGameState gameState)
    {
        var currentTime = gameState.TimeManager.CurrentGameTime;
        var elapsed = currentTime - StartTime;
        return $"{elapsed.TotalHours:F1}/{RequiredDuration.TotalHours:F1} hours";
    }
}

// === Karma Conditions ===
public class KarmaCondition : QuestCondition
{
    public int MinKarma;
    public int MaxKarma;
    
    public override bool Evaluate(IGameState gameState)
    {
        var karma = gameState.PlayerManager.GetStats().Karma;
        return karma >= MinKarma && karma <= MaxKarma;
    }
    
    public override string GetProgressText(IGameState gameState)
    {
        var karma = gameState.PlayerManager.GetStats().Karma;
        return $"Karma: {karma} (need {MinKarma}-{MaxKarma})";
    }
}

// === Location Conditions ===
public class LocationCondition : QuestCondition
{
    public string LocationId;
    
    public override bool Evaluate(IGameState gameState)
    {
        return gameState.Player.CurrentLocationId == LocationId;
    }
}

// === Dialogue Conditions ===
public class DialogueNodeSeenCondition : QuestCondition
{
    public string NpcId;
    public string NodeId;
    
    public override bool Evaluate(IGameState gameState)
    {
        return gameState.DialogueService.HasSeenNode(NpcId, NodeId);
    }
}

// === Quest Conditions ===
public class QuestCompletedCondition : QuestCondition
{
    public string QuestId;
    
    public override bool Evaluate(IGameState gameState)
    {
        var quest = gameState.QuestManager.GetQuest(QuestId);
        return quest?.State == QuestState.Completed;
    }
}

// === Item Conditions ===
public class ItemObtainedCondition : QuestCondition
{
    public string ItemId;
    
    public override bool Evaluate(IGameState gameState)
    {
        return gameState.Player.Inventory.Contains(ItemId);
    }
}

// === Stat Conditions ===
public class StatCondition : QuestCondition
{
    public StatType Stat;
    public int MinValue;
    
    public override bool Evaluate(IGameState gameState)
    {
        var stats = gameState.PlayerManager.GetStats();
        return stats.GetStat(Stat) >= MinValue;
    }
}

// === Composite Conditions ===
public class AndCondition : QuestCondition
{
    public List<string> ConditionIds;
    
    public override bool Evaluate(IGameState gameState)
    {
        return ConditionIds.All(id => 
            QuestConditionRegistry.Get(id).Evaluate(gameState));
    }
}

public class OrCondition : QuestCondition
{
    public List<string> ConditionIds;
    
    public override bool Evaluate(IGameState gameState)
    {
        return ConditionIds.Any(id => 
            QuestConditionRegistry.Get(id).Evaluate(gameState));
    }
}

public enum ComparisonType
{
    Equal,
    GreaterOrEqual,
    LessOrEqual
}
```

---

## Quest Condition Registry

### Implementation

```csharp
public static class QuestConditionRegistry
{
    private static Dictionary<string, QuestCondition> conditions = new Dictionary<string, QuestCondition>();
    
    public static void Initialize()
    {
        conditions.Clear();
        
        // === Time Conditions ===
        Register(new TimeCondition 
        { 
            ConditionId = "time_monday_9am",
            Description = "Monday at 9:00 AM or later",
            Day = DayOfWeek.Monday,
            TimeOfDay = new TimeSpan(9, 0, 0)
        });
        
        Register(new TimeCondition 
        { 
            ConditionId = "time_friday_evening",
            Description = "Friday at 6:00 PM or later",
            Day = DayOfWeek.Friday,
            TimeOfDay = new TimeSpan(18, 0, 0)
        });
        
        // === Karma Conditions ===
        Register(new KarmaCondition 
        { 
            ConditionId = "karma_whitehat_50",
            Description = "Whitehat karma >= 50",
            MinKarma = 50,
            MaxKarma = 100
        });
        
        Register(new KarmaCondition 
        { 
            ConditionId = "karma_blackhat_50",
            Description = "Blackhat karma <= -50",
            MinKarma = -100,
            MaxKarma = -50
        });
        
        // === Relationship Conditions ===
        Register(new RelationshipCondition 
        { 
            ConditionId = "sarah_trust_70",
            Description = "Sarah trust >= 70",
            NpcId = "sarah",
            MinimumTrust = 70
        });
        
        Register(new RelationshipCondition 
        { 
            ConditionId = "marcus_trust_50",
            Description = "Marcus trust >= 50",
            NpcId = "marcus",
            MinimumTrust = 50
        });
        
        // === Device Conditions ===
        Register(new DeviceCompromisedCondition 
        { 
            ConditionId = "coffee_shop_pos_hacked",
            Description = "Coffee shop POS compromised",
            DeviceId = "coffee_shop_pos"
        });
        
        // More conditions registered here...
        
        Debug.Log($"[QuestConditionRegistry] Initialized with {conditions.Count} conditions");
    }
    
    public static void Register(QuestCondition condition)
    {
        if (conditions.ContainsKey(condition.ConditionId))
        {
            Debug.LogWarning($"[QuestConditionRegistry] Overwriting condition: {condition.ConditionId}");
        }
        
        conditions[condition.ConditionId] = condition;
    }
    
    public static QuestCondition Get(string conditionId)
    {
        if (conditions.TryGetValue(conditionId, out var condition))
        {
            return condition;
        }
        
        Debug.LogError($"[QuestConditionRegistry] Condition not found: {conditionId}");
        return null;
    }
    
    public static bool Exists(string conditionId)
    {
        return conditions.ContainsKey(conditionId);
    }
    
    public static List<string> GetAllConditionIds()
    {
        return conditions.Keys.ToList();
    }
}
```

---

## Quest Manager Implementation

```csharp
public class QuestManager : IQuestManager
{
    private Dictionary<string, Quest> allQuests;
    private List<Quest> activeQuests;
    private List<Quest> completedQuests;
    private List<Quest> failedQuests;
    
    private IGameState gameState;
    private ITimeManager timeManager;
    private ILeadManager leadManager;
    
    // === Events ===
    public event Action<Quest> OnQuestUnlocked;
    public event Action<Quest> OnQuestStarted;
    public event Action<Quest, QuestObjective> OnObjectiveCompleted;
    public event Action<Quest> OnQuestCompleted;
    public event Action<Quest, string> OnQuestFailed;
    
    // === Initialization ===
    
    public void Initialize()
    {
        gameState = ServiceLocator.Get<IGameState>();
        timeManager = ServiceLocator.Get<ITimeManager>();
        leadManager = ServiceLocator.Get<ILeadManager>();
        
        allQuests = new Dictionary<string, Quest>();
        activeQuests = new List<Quest>();
        completedQuests = new List<Quest>();
        failedQuests = new List<Quest>();
        
        // Initialize condition registry
        QuestConditionRegistry.Initialize();
        
        // Subscribe to game events (discrete moments)
        SubscribeToGameEvents();
        
        // Start polling coroutine (continuous states)
        ServiceLocator.Get<IUpdateService>()?.RegisterUpdate(CheckPollingConditions);
        
        // Load quest database
        LoadQuestDatabase();
        
        Debug.Log("[QuestManager] Initialized");
    }
    
    private void SubscribeToGameEvents()
    {
        GameEvents.Subscribe(GameEventType.DeviceCompromised, OnDeviceCompromised);
        GameEvents.Subscribe(GameEventType.NPCInteraction, OnNPCInteraction);
        GameEvents.Subscribe(GameEventType.ItemObtained, OnItemObtained);
        GameEvents.Subscribe(GameEventType.LocationDiscovered, OnLocationDiscovered);
        GameEvents.Subscribe(GameEventType.DialogueEnded, OnDialogueEnded);
        
        timeManager.OnHourChanged += OnHourChanged;
        timeManager.OnDayChanged += OnDayChanged;
    }
    
    private void LoadQuestDatabase()
    {
        // Load all QuestData ScriptableObjects from Resources
        var questAssets = Resources.LoadAll<QuestData>("Quests");
        
        foreach (var asset in questAssets)
        {
            var quest = ConvertToQuest(asset);
            allQuests[quest.QuestId] = quest;
        }
        
        Debug.Log($"[QuestManager] Loaded {allQuests.Count} quests");
    }
    
    private Quest ConvertToQuest(QuestData data)
    {
        // Convert ScriptableObject to runtime Quest instance
        return new Quest
        {
            QuestId = data.questId,
            QuestName = data.questName,
            Description = data.description,
            Type = data.type,
            Category = data.category,
            UnlockConditionIds = data.unlockConditionIds,
            RequiredCompletedQuests = data.requiredCompletedQuests,
            Objectives = data.objectives,
            RequireAllObjectives = data.requireAllObjectives,
            QuestGiverNpcId = data.questGiverNpcId,
            Rewards = data.rewards,
            FailureConditionIds = data.failureConditionIds,
            CanFail = data.canFail
        };
    }
    
    // === Event Handlers ===
    
    private void OnDeviceCompromised(object data)
    {
        var device = data as Device;
        Debug.Log($"[QuestManager] Device compromised: {device.Hostname}");
        CheckQuestProgressAfterEvent();
    }
    
    private void OnNPCInteraction(object data)
    {
        var npcId = data as string;
        Debug.Log($"[QuestManager] NPC interaction: {npcId}");
        CheckQuestProgressAfterEvent();
    }
    
    private void OnItemObtained(object data)
    {
        var itemId = data as string;
        Debug.Log($"[QuestManager] Item obtained: {itemId}");
        CheckQuestProgressAfterEvent();
    }
    
    private void OnLocationDiscovered(object data)
    {
        var locationId = data as string;
        Debug.Log($"[QuestManager] Location discovered: {locationId}");
        CheckQuestProgressAfterEvent();
    }
    
    private void OnDialogueEnded(object data)
    {
        var npcId = data as string;
        Debug.Log($"[QuestManager] Dialogue ended: {npcId}");
        CheckQuestProgressAfterEvent();
    }
    
    private void OnHourChanged(int hour)
    {
        // Check time-based conditions
        CheckTimeBasedQuests();
    }
    
    private void OnDayChanged(DayOfWeek day)
    {
        Debug.Log($"[QuestManager] Day changed: {day}");
        CheckTimeBasedQuests();
    }
    
    // === Quest Progress Checking ===
    
    private void CheckQuestProgressAfterEvent()
    {
        // Check active quest objectives
        foreach (var quest in activeQuests.ToList()) // ToList to allow modification
        {
            foreach (var objective in quest.Objectives.Where(o => !o.IsComplete))
            {
                if (CheckObjectiveCompletion(objective))
                {
                    CompleteObjective(quest.QuestId, objective.ObjectiveId);
                }
            }
            
            // Check if quest is now complete
            if (IsQuestComplete(quest))
            {
                CompleteQuest(quest.QuestId);
            }
            
            // Check failure conditions
            if (quest.CanFail && HasQuestFailed(quest))
            {
                FailQuest(quest.QuestId, "Failure condition met");
            }
        }
        
        // Check if new quests should unlock
        CheckForNewQuests();
    }
    
    private void CheckPollingConditions(float deltaTime)
    {
        // Poll once per second (not every frame)
        if (Time.frameCount % 60 != 0) return;
        
        // Check for new quest unlocks (relationship, stats, karma)
        CheckForNewQuests();
        
        // Check active quest failures (time limits, etc.)
        CheckQuestFailures();
    }
    
    private void CheckTimeBasedQuests()
    {
        CheckQuestProgressAfterEvent(); // Reuse event handler
    }
    
    private void CheckForNewQuests()
    {
        foreach (var quest in allQuests.Values.Where(q => q.State == QuestState.Locked))
        {
            if (CheckQuestUnlockConditions(quest))
            {
                UnlockQuest(quest.QuestId);
            }
        }
    }
    
    private void CheckQuestFailures()
    {
        foreach (var quest in activeQuests.ToList())
        {
            if (quest.CanFail && HasQuestFailed(quest))
            {
                FailQuest(quest.QuestId, "Time limit exceeded or failure condition met");
            }
        }
    }
    
    // === Condition Evaluation ===
    
    public bool CheckQuestUnlockConditions(Quest quest)
    {
        // Check unlock conditions
        bool conditionsMet = quest.UnlockConditionIds.All(id =>
        {
            var condition = QuestConditionRegistry.Get(id);
            return condition?.Evaluate(gameState) ?? false;
        });
        
        // Check quest dependencies
        bool dependenciesMet = quest.RequiredCompletedQuests.All(id =>
            completedQuests.Any(q => q.QuestId == id));
        
        return conditionsMet && dependenciesMet;
    }
    
    private bool CheckObjectiveCompletion(QuestObjective objective)
    {
        if (string.IsNullOrEmpty(objective.CompletionConditionId))
            return false;
        
        var condition = QuestConditionRegistry.Get(objective.CompletionConditionId);
        return condition?.Evaluate(gameState) ?? false;
    }
    
    public bool IsQuestComplete(Quest quest)
    {
        if (quest.RequireAllObjectives)
        {
            // AND logic: all objectives must be complete
            return quest.Objectives.All(o => o.IsComplete);
        }
        else
        {
            // OR logic: at least one objective must be complete
            return quest.Objectives.Any(o => o.IsComplete);
        }
    }
    
    public bool HasQuestFailed(Quest quest)
    {
        if (!quest.CanFail) return false;
        
        return quest.FailureConditionIds.Any(id =>
        {
            var condition = QuestConditionRegistry.Get(id);
            return condition?.Evaluate(gameState) ?? false;
        });
    }
    
    // === Quest State Management ===
    
    public void UnlockQuest(string questId)
    {
        var quest = GetQuest(questId);
        if (quest == null || quest.State != QuestState.Locked) return;
        
        quest.State = QuestState.Available;
        quest.UnlockedAt = timeManager.CurrentGameTime;
        
        Debug.Log($"[QuestManager] Quest unlocked: {quest.QuestName}");
        OnQuestUnlocked?.Invoke(quest);
        
        // Notify UI
        GameEvents.Publish(GameEventType.QuestUnlocked, quest);
    }
    
    public void StartQuest(string questId)
    {
        var quest = GetQuest(questId);
        if (quest == null || quest.State != QuestState.Available) return;
        
        quest.State = QuestState.Active;
        quest.StartedAt = timeManager.CurrentGameTime;
        activeQuests.Add(quest);
        
        Debug.Log($"[QuestManager] Quest started: {quest.QuestName}");
        OnQuestStarted?.Invoke(quest);
    }
    
    public void CompleteObjective(string questId, string objectiveId)
    {
        var quest = GetQuest(questId);
        if (quest == null) return;
        
        var objective = quest.Objectives.FirstOrDefault(o => o.ObjectiveId == objectiveId);
        if (objective == null || objective.IsComplete) return;
        
        objective.IsComplete = true;
        objective.CompletedAt = timeManager.CurrentGameTime;
        
        Debug.Log($"[QuestManager] Objective completed: {quest.QuestName} - {objective.Description}");
        OnObjectiveCompleted?.Invoke(quest, objective);
        
        // Check if quest is now complete
        if (IsQuestComplete(quest))
        {
            CompleteQuest(questId);
        }
    }
    
    public void CompleteQuest(string questId)
    {
        var quest = GetQuest(questId);
        if (quest == null || quest.State != QuestState.Active) return;
        
        quest.State = QuestState.Completed;
        quest.CompletedAt = timeManager.CurrentGameTime;
        
        activeQuests.Remove(quest);
        completedQuests.Add(quest);
        
        // Award rewards
        AwardQuestRewards(quest);
        
        Debug.Log($"[QuestManager] Quest completed: {quest.QuestName}");
        OnQuestCompleted?.Invoke(quest);
        
        // Check if this unlocks new quests
        CheckForNewQuests();
    }
    
    public void FailQuest(string questId, string reason)
    {
        var quest = GetQuest(questId);
        if (quest == null || quest.State != QuestState.Active) return;
        
        quest.State = QuestState.Failed;
        quest.FailureTime = timeManager.CurrentGameTime;
        quest.FailureReason = reason;
        
        activeQuests.Remove(quest);
        failedQuests.Add(quest);
        
        Debug.LogWarning($"[QuestManager] Quest failed: {quest.QuestName} - {reason}");
        OnQuestFailed?.Invoke(quest, reason);
    }
    
    private void AwardQuestRewards(Quest quest)
    {
        var rewards = quest.Rewards;
        var playerManager = ServiceLocator.Get<IPlayerManager>();
        
        // Money
        if (rewards.Money > 0)
        {
            playerManager.AddMoney(rewards.Money);
            Debug.Log($"[QuestManager] Awarded ${rewards.Money}");
        }
        
        // Karma
        if (rewards.Karma != 0)
        {
            playerManager.ModifyKarma(rewards.Karma);
            Debug.Log($"[QuestManager] Karma change: {rewards.Karma:+#;-#;0}");
        }
        
        // Stats
        foreach (var statChange in rewards.StatIncreases)
        {
            playerManager.ModifyStat(statChange.Key, statChange.Value);
            Debug.Log($"[QuestManager] {statChange.Key} +{statChange.Value}");
        }
        
        // Unlocked content
        // ... handle exploits, devices, locations
    }
    
    // === Lead Integration ===
    
    public void CheckIfLeadUnlocksQuests(Lead lead)
    {
        // Check if any locked quests reference this lead
        foreach (var quest in allQuests.Values.Where(q => q.State == QuestState.Locked))
        {
            if (quest.RelatedLeadIds.Contains(lead.LeadId))
            {
                // This quest is related to the lead, check if it unlocks
                if (CheckQuestUnlockConditions(quest))
                {
                    UnlockQuest(quest.QuestId);
                }
            }
        }
    }
    
    public List<Quest> GetQuestsForLead(string leadId)
    {
        return allQuests.Values
            .Where(q => q.RelatedLeadIds.Contains(leadId))
            .ToList();
    }
    
    // === Queries ===
    
    public Quest GetQuest(string questId)
    {
        allQuests.TryGetValue(questId, out var quest);
        return quest;
    }
    
    public List<Quest> GetActiveQuests() => activeQuests.ToList();
    public List<Quest> GetCompletedQuests() => completedQuests.ToList();
    public List<Quest> GetFailedQuests() => failedQuests.ToList();
    public List<Quest> GetAvailableQuests() => allQuests.Values
        .Where(q => q.State == QuestState.Available)
        .ToList();
}
```

---

## ScriptableObject Definition

### QuestData ScriptableObject

```csharp
[CreateAssetMenu(fileName = "New Quest", menuName = "Game/Quest")]
public class QuestData : ScriptableObject
{
    [Header("Basic Info")]
    public string questId;
    public string questName;
    [TextArea(3, 5)]
    public string description;
    public QuestType type;
    public QuestCategory category;
    
    [Header("Prerequisites")]
    public List<string> unlockConditionIds; // References to QuestConditionRegistry
    public List<string> requiredCompletedQuests; // Quest IDs
    
    [Header("Objectives")]
    public List<ObjectiveData> objectives;
    public bool requireAllObjectives = true;
    
    [Header("Story")]
    public string questGiverNpcId;
    public Sprite icon;
    public Color categoryColor = Color.white;
    
    [Header("Rewards")]
    public QuestRewards rewards;
    
    [Header("Failure (Optional)")]
    public bool canFail = false;
    public List<string> failureConditionIds;
}

[System.Serializable]
public class ObjectiveData
{
    public string objectiveId;
    [TextArea(2, 3)]
    public string description;
    public ObjectiveType type;
    public string completionConditionId; // Reference to QuestConditionRegistry
    public bool isHidden = false;
    public int requiredProgress = 1;
}
```

### Custom Inspector (Optional)

```csharp
[CustomEditor(typeof(QuestData))]
public class QuestDataEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        EditorGUILayout.Space();
        
        var questData = (QuestData)target;
        
        // Validate condition IDs
        EditorGUILayout.LabelField("Validation", EditorStyles.boldLabel);
        
        foreach (var conditionId in questData.unlockConditionIds)
        {
            if (!QuestConditionRegistry.Exists(conditionId))
            {
                EditorGUILayout.HelpBox($"Condition not found: {conditionId}", MessageType.Error);
            }
            else
            {
                EditorGUILayout.LabelField($"✓ {conditionId}");
            }
        }
        
        // Show all available conditions
        if (GUILayout.Button("Show Available Conditions"))
        {
            ConditionBrowserWindow.Show();
        }
    }
}
```

---

## Progression Coordinator

### Implementation

```csharp
public class ProgressionCoordinator : IGameService
{
    private IQuestManager questManager;
    private ILeadManager leadManager;
    
    public void Initialize()
    {
        questManager = ServiceLocator.Get<IQuestManager>();
        leadManager = ServiceLocator.Get<ILeadManager>();
        
        // Lead created → Check if unlocks quests
        leadManager.OnLeadCreated += (lead) =>
        {
            questManager.CheckIfLeadUnlocksQuests(lead);
        };
        
        // Quest unlocked → Update related leads
        questManager.OnQuestUnlocked += (quest) =>
        {
            UpdateLeadsForQuest(quest);
        };
        
        // Quest completed → Update related leads
        questManager.OnQuestCompleted += (quest) =>
        {
            ResolveLeadsForQuest(quest);
        };
        
        // Objective completed → Update related leads
        questManager.OnObjectiveCompleted += (quest, objective) =>
        {
            UpdateLeadsForObjective(quest, objective);
        };
        
        Debug.Log("[ProgressionCoordinator] Initialized");
    }
    
    private void UpdateLeadsForQuest(Quest quest)
    {
        foreach (var leadId in quest.RelatedLeadIds)
        {
            var lead = leadManager.GetLead(leadId);
            if (lead != null)
            {
                // Add quest to lead's related quests
                if (!lead.RelatedQuestIds.Contains(quest.QuestId))
                {
                    lead.RelatedQuestIds.Add(quest.QuestId);
                }
                
                // Update lead description with quest info
                lead.Description += $"\n\nRelated Quest: {quest.QuestName}";
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
                // Mark lead as resolved when quest completes
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
                // Update lead progress
                lead.InvestigationProgress = CalculateLeadProgress(lead, quest);
            }
        }
    }
    
    private float CalculateLeadProgress(Lead lead, Quest quest)
    {
        // Count completed objectives for this lead
        int totalObjectives = quest.Objectives
            .Count(o => o.RelatedLeadIds.Contains(lead.LeadId));
        
        int completedObjectives = quest.Objectives
            .Count(o => o.RelatedLeadIds.Contains(lead.LeadId) && o.IsComplete);
        
        return totalObjectives > 0 ? (float)completedObjectives / totalObjectives : 0f;
    }
}
```

---

## Quest Dependency Validation

### Dependency Graph Builder

```csharp
public class QuestDependencyGraph
{
    private Dictionary<string, List<string>> dependencies; // questId -> required quest IDs
    
    public void BuildGraph(Dictionary<string, Quest> allQuests)
    {
        dependencies = new Dictionary<string, List<string>>();
        
        foreach (var quest in allQuests.Values)
        {
            dependencies[quest.QuestId] = quest.RequiredCompletedQuests;
        }
    }
    
    public bool HasCircularDependency()
    {
        var visited = new HashSet<string>();
        var recursionStack = new HashSet<string>();
        
        foreach (var questId in dependencies.Keys)
        {
            if (DetectCycleRecursive(questId, visited, recursionStack))
            {
                Debug.LogError($"[QuestDependencyGraph] Circular dependency detected involving: {questId}");
                return true;
            }
        }
        
        return false;
    }
    
    private bool DetectCycleRecursive(string questId, HashSet<string> visited, HashSet<string> recursionStack)
    {
        if (recursionStack.Contains(questId))
            return true; // Cycle detected
        
        if (visited.Contains(questId))
            return false; // Already processed
        
        visited.Add(questId);
        recursionStack.Add(questId);
        
        if (dependencies.TryGetValue(questId, out var deps))
        {
            foreach (var depId in deps)
            {
                if (DetectCycleRecursive(depId, visited, recursionStack))
                    return true;
            }
        }
        
        recursionStack.Remove(questId);
        return false;
    }
    
    public List<string> GetQuestPath(string questId)
    {
        // Returns the dependency chain for a quest
        var path = new List<string>();
        BuildPathRecursive(questId, path, new HashSet<string>());
        return path;
    }
    
    private void BuildPathRecursive(string questId, List<string> path, HashSet<string> visited)
    {
        if (visited.Contains(questId)) return;
        visited.Add(questId);
        
        if (dependencies.TryGetValue(questId, out var deps))
        {
            foreach (var depId in deps)
            {
                BuildPathRecursive(depId, path, visited);
            }
        }
        
        path.Add(questId);
    }
}
```

### Unity Editor Tool: Quest Dependency Visualizer

```csharp
public class QuestDependencyVisualizer : EditorWindow
{
    private QuestDependencyGraph graph;
    private Vector2 scrollPosition;
    
    [MenuItem("Tools/Quest Dependency Visualizer")]
    public static void ShowWindow()
    {
        GetWindow<QuestDependencyVisualizer>("Quest Dependencies");
    }
    
    private void OnEnable()
    {
        // Load all quests and build graph
        var questAssets = Resources.LoadAll<QuestData>("Quests");
        var quests = new Dictionary<string, Quest>();
        
        foreach (var asset in questAssets)
        {
            var quest = ConvertToQuest(asset);
            quests[quest.QuestId] = quest;
        }
        
        graph = new QuestDependencyGraph();
        graph.BuildGraph(quests);
    }
    
    private void OnGUI()
    {
        EditorGUILayout.LabelField("Quest Dependency Graph", EditorStyles.boldLabel);
        
        // Check for circular dependencies
        if (GUILayout.Button("Validate Dependencies"))
        {
            if (graph.HasCircularDependency())
            {
                EditorGUILayout.HelpBox("CIRCULAR DEPENDENCY DETECTED!", MessageType.Error);
            }
            else
            {
                EditorGUILayout.HelpBox("All dependencies valid ✓", MessageType.Info);
            }
        }
        
        EditorGUILayout.Space();
        
        // Show dependency tree
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        
        var questAssets = Resources.LoadAll<QuestData>("Quests");
        foreach (var asset in questAssets)
        {
            EditorGUILayout.LabelField(asset.questName, EditorStyles.boldLabel);
            
            var path = graph.GetQuestPath(asset.questId);
            if (path.Count > 1)
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField($"Requires: {string.Join(" → ", path.Take(path.Count - 1))}");
                EditorGUI.indentLevel--;
            }
            else
            {
                EditorGUI.indentLevel++;
                EditorGUILayout.LabelField("No dependencies");
                EditorGUI.indentLevel--;
            }
            
            EditorGUILayout.Space();
        }
        
        EditorGUILayout.EndScrollView();
    }
}
```

---

## Save System Integration

### Quest Save Data

```csharp
[System.Serializable]
public class QuestSaveData
{
    public const int CURRENT_VERSION = 1;
    
    public int version = CURRENT_VERSION;
    public List<QuestProgress> questProgress;
    
    [System.Serializable]
    public class QuestProgress
    {
        public string questId;
        public QuestState state;
        public DateTime unlockedAt;
        public DateTime startedAt;
        public DateTime completedAt;
        public List<string> completedObjectiveIds;
        public DateTime failureTime;
        public string failureReason;
    }
}

public class QuestManager : IQuestManager, ISaveable
{
    public SaveData GetSaveData()
    {
        var saveData = new QuestSaveData
        {
            questProgress = new List<QuestSaveData.QuestProgress>()
        };
        
        foreach (var quest in allQuests.Values)
        {
            if (quest.State == QuestState.Locked)
                continue; // Don't save locked quests
            
            saveData.questProgress.Add(new QuestSaveData.QuestProgress
            {
                questId = quest.QuestId,
                state = quest.State,
                unlockedAt = quest.UnlockedAt,
                startedAt = quest.StartedAt,
                completedAt = quest.CompletedAt,
                completedObjectiveIds = quest.Objectives
                    .Where(o => o.IsComplete)
                    .Select(o => o.ObjectiveId)
                    .ToList(),
                failureTime = quest.FailureTime,
                failureReason = quest.FailureReason
            });
        }
        
        return saveData;
    }
    
    public void LoadSaveData(SaveData data)
    {
        var questData = data as QuestSaveData;
        
        // Clear current state
        activeQuests.Clear();
        completedQuests.Clear();
        failedQuests.Clear();
        
        // Restore quest state
        foreach (var progress in questData.questProgress)
        {
            var quest = GetQuest(progress.questId);
            if (quest == null) continue;
            
            quest.State = progress.state;
            quest.UnlockedAt = progress.unlockedAt;
            quest.StartedAt = progress.startedAt;
            quest.CompletedAt = progress.completedAt;
            quest.FailureTime = progress.failureTime;
            quest.FailureReason = progress.failureReason;
            
            // Restore objective completion
            foreach (var objectiveId in progress.completedObjectiveIds)
            {
                var objective = quest.Objectives.FirstOrDefault(o => o.ObjectiveId == objectiveId);
                if (objective != null)
                {
                    objective.IsComplete = true;
                }
            }
            
            // Add to appropriate lists
            switch (quest.State)
            {
                case QuestState.Active:
                    activeQuests.Add(quest);
                    break;
                case QuestState.Completed:
                    completedQuests.Add(quest);
                    break;
                case QuestState.Failed:
                    failedQuests.Add(quest);
                    break;
            }
        }
        
        Debug.Log($"[QuestManager] Loaded {activeQuests.Count} active quests");
    }
}
```

---

## Performance Considerations

### Polling Optimization

**Problem:** Checking quest conditions every frame is expensive

**Solution:** Check once per second, prioritize active quests

```csharp
private float timeSinceLastPoll = 0f;
private const float POLL_INTERVAL = 1f; // 1 second

public void Update(float deltaTime)
{
    timeSinceLastPoll += deltaTime;
    
    if (timeSinceLastPoll >= POLL_INTERVAL)
    {
        CheckPollingConditions();
        timeSinceLastPoll = 0f;
    }
}

private void CheckPollingConditions()
{
    // Only check active quests for failures
    CheckQuestFailures();
    
    // Check for new unlocks less frequently (every 5 seconds)
    if (Time.frameCount % 300 == 0)
    {
        CheckForNewQuests();
    }
}
```

### Condition Caching

**Problem:** Evaluating complex conditions repeatedly

**Solution:** Cache condition results with expiration

```csharp
public class CachedConditionEvaluator
{
    private Dictionary<string, (bool result, float expiration)> cache;
    private const float CACHE_DURATION = 1f; // 1 second
    
    public bool Evaluate(QuestCondition condition, IGameState gameState)
    {
        if (cache.TryGetValue(condition.ConditionId, out var cached))
        {
            if (Time.time < cached.expiration)
                return cached.result;
        }
        
        bool result = condition.Evaluate(gameState);
        cache[condition.ConditionId] = (result, Time.time + CACHE_DURATION);
        return result;
    }
}
```

---

## Troubleshooting & Common Pitfalls

### Issue: Quest Won't Unlock

**Symptom:** Quest stays locked despite meeting requirements

**Cause:** Condition ID typo or condition not registered

**Solution:**

```csharp
// Validate condition IDs on quest load
private Quest ConvertToQuest(QuestData data)
{
    foreach (var conditionId in data.unlockConditionIds)
    {
        if (!QuestConditionRegistry.Exists(conditionId))
        {
            Debug.LogError($"[QuestManager] Condition not found: {conditionId} for quest {data.questId}");
        }
    }
    // ...
}
```

### Issue: Circular Quest Dependencies

**Symptom:** Quests can never unlock

**Cause:** Quest A requires Quest B, which requires Quest A

**Solution:** Use QuestDependencyVisualizer tool to detect and fix

### Issue: Objectives Not Completing

**Symptom:** Player does action but objective doesn't complete

**Cause:** Event not firing or condition not evaluating correctly

**Solution:**

```csharp
// Add logging to condition evaluation
public override bool Evaluate(IGameState gameState)
{
    bool result = /* evaluation logic */;
    Debug.Log($"[Condition {ConditionId}] Evaluated: {result}");
    return result;
}
```

---

## Quick Start Guide

### 1. Create a Quest ScriptableObject

1. Right-click in Project → Create → Game → Quest
2. Fill in quest details
3. Add condition IDs from QuestConditionRegistry
4. Add objectives with their condition IDs

### 2. Register Custom Conditions

```csharp
// In QuestConditionRegistry.Initialize()
Register(new YourCustomCondition 
{ 
    ConditionId = "your_condition_id",
    Description = "Your condition description",
    // ... other properties
});
```

### 3. Hook Up Events

```csharp
// In your game system
GameEvents.Publish(GameEventType.DeviceCompromised, device);
```

### 4. Check Quest Progress

Quest Manager automatically checks progress on events. To manually check:

```csharp
questManager.CheckQuestProgressAfterEvent();
```

---

## Summary

The Quest System achieves:

- ✅ **Flexible triggering**: Hybrid event/poll handles all condition types
- ✅ **Designer-friendly**: ScriptableObjects for quest data
- ✅ **Programmer-friendly**: Code-based conditions with inheritance
- ✅ **Dependency validation**: Graph tool prevents circular dependencies
- ✅ **Lead integration**: Quests and leads reference each other bidirectionally
- ✅ **Performance optimized**: Caching, prioritized polling
- ✅ **Save/load ready**: Serializes quest progress cleanly
- ✅ **Debug-friendly**: Validation tools, dependency visualizer
