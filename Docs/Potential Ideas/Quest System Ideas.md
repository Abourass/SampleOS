My thoughts are that we use a hybrid event/poll system
- **Events** handle discrete moments: "Player talked to NPC," "Player hacked device X," "Player discovered network Y"
- **Polling** handles continuous states: "Player has relationship >50 with NPC," "It's Friday," "Player has whitehat karma >80"

The hope is that we can handle:
- **Flexible triggers**: A quests can unlock from anything - time, location, relationships, hacks
- **Complex dependencies**: "Quest B only unlocks if you completed Quest A AND have high blackhat rep"
- **Branching paths**: Job quests can be mutually exclusive if needed
- **Hidden objectives**: The "twist" objective can be revealed mid-quest
- **Failure states**: Time-sensitive quests can fail if conditions aren't met

We may need a quest dependency graph to prevent circular dependencies

**Potential Quest Data Structure:**
```csharp
[System.Serializable]
public class Quest
{
    public string QuestId;
    public string QuestName;
    public QuestType Type; // Main, Side, Job, Emergent
    
    // Prerequisites
    public List<QuestCondition> UnlockConditions;  // What makes this quest appear?
    public List<string> RequiredCompletedQuests;   // Quest dependencies
    
    // Objectives
    public List<QuestObjective> Objectives;
    public bool RequireAllObjectives = true;       // AND vs OR logic
    
    // Progression tracking
    public QuestState State = QuestState.Locked;
    public DateTime UnlockedAt;
    public DateTime CompletedAt;
    
    // Story/metadata
    public string Description;
    public string QuestGiver;      // NPC who gave this
    public QuestCategory Category; // Hacking, Social, Job, etc.
    
    // Rewards
    public QuestRewards Rewards;
    
    // Failure conditions (optional)
    public List<QuestCondition> FailureConditions;
    public bool CanFail = false;
}

public class QuestObjective
{
    public string ObjectiveId;
    public string Description;
    public ObjectiveType Type;
    
    // Condition to complete
    public QuestCondition CompletionCondition;
    
    // Optional: hints/leads that help complete this
    public List<string> RelatedLeadIds;
    
    // Tracking
    public bool IsComplete;
    public bool IsHidden; // For twist objectives
}

public enum ObjectiveType
{
    HackDevice,           // "Compromise the coffee shop POS"
    TalkToNPC,            // "Ask Sarah about the tech company"
    ObtainItem,           // "Get the leaked password database"
    ReachLocation,        // "Find the underground hacker meetup"
    AchieveReputation,    // "Reach 50 reputation with Anonymous group"
    CompleteMinigame,     // "Successfully complete your first day at BigTechCorp"
    TimeElapsed,          // "Wait until Friday"
    Custom                // For complex logic
}
```

**Potential Condition System:**
```csharp
public abstract class QuestCondition
{
    public abstract bool Evaluate(GameState state);
}

// Examples:
public class DeviceCompromisedCondition : QuestCondition
{
    public string DeviceId;
    public override bool Evaluate(GameState state) 
        => state.Player.CompromisedDevices.Contains(DeviceId);
}

public class RelationshipCondition : QuestCondition
{
    public string NpcId;
    public int MinimumValue;
    public override bool Evaluate(GameState state)
        => state.GetRelationship(NpcId) >= MinimumValue;
}

public class TimeCondition : QuestCondition
{
    public DayOfWeek Day;
    public TimeSpan Time;
    public override bool Evaluate(GameState state)
        => state.CurrentTime.DayOfWeek == Day && 
           state.CurrentTime.TimeOfDay >= Time;
}

public class KarmaCondition : QuestCondition
{
    public int MinKarma;
    public int MaxKarma;
    public override bool Evaluate(GameState state)
        => state.Player.Karma >= MinKarma && 
           state.Player.Karma <= MaxKarma;
}
```

**Potential QuestManager Implementation:**
```csharp
public class QuestManager : MonoBehaviour
{
    private Dictionary<string, Quest> allQuests;
    private List<Quest> activeQuests;
    private List<Quest> completedQuests;
    
    private void Start()
    {
        // Subscribe to relevant game events
        GameEvents.OnDeviceCompromised += CheckQuestProgress;
        GameEvents.OnNPCInteraction += CheckQuestProgress;
        GameEvents.OnItemObtained += CheckQuestProgress;
        GameEvents.OnLocationDiscovered += CheckQuestProgress;
        TimeManager.Instance.OnHourChanged += CheckTimeBasedQuests;
        
        // Load quest database
        LoadQuestDatabase();
    }
    
    private void Update()
    {
        // Poll for state-based conditions (relationships, stats, etc.)
        // Do this less frequently than every frame
        if (Time.frameCount % 60 == 0) // Once per second
        {
            CheckPollingConditions();
        }
    }
    
    private void CheckQuestProgress(object eventData)
    {
        // When an event fires, check if it completes any objectives
        foreach (var quest in activeQuests)
        {
            foreach (var objective in quest.Objectives.Where(o => !o.IsComplete))
            {
                if (objective.CompletionCondition.Evaluate(GameState.Instance))
                {
                    CompleteObjective(quest, objective);
                }
            }
            
            // Check if quest is now complete
            if (IsQuestComplete(quest))
            {
                CompleteQuest(quest);
            }
        }
        
        // Check if new quests should unlock
        CheckForNewQuests();
    }
    
    private void CheckForNewQuests()
    {
        foreach (var quest in allQuests.Values.Where(q => q.State == QuestState.Locked))
        {
            bool canUnlock = quest.UnlockConditions.All(c => c.Evaluate(GameState.Instance));
            bool dependenciesMet = quest.RequiredCompletedQuests.All(id => 
                completedQuests.Any(q => q.QuestId == id));
                
            if (canUnlock && dependenciesMet)
            {
                UnlockQuest(quest);
            }
        }
    }
}
```

Now I think that Quest Tracking and Lead Tracking should likely remain two separate systems, but have relationship too one another. See [[Lead System Ideas]]