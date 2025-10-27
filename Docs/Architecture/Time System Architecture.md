# Time System Architecture

## Overview

The Time System manages in-game time progression, time scaling for different contexts, and time-based event broadcasting. It's designed as a **service** that persists across scenes and integrates with NPC schedules, quest conditions, and world state changes.

**Key Design Principles:**

- **Service-based**: Persistent singleton, survives scene transitions
- **Event-driven**: Broadcasts time events, other systems subscribe
- **Context-aware**: Different time scales for different activities (walking, hacking, dialogue)
- **Deterministic**: Time only advances when game is running, no real-world time
- **Pausable**: Can pause time for specific gameplay contexts

---

## Core Architecture

### Component Hierarchy

```
TimeManager (ITimeManager) - Service
  ├── Tracks current game DateTime
  ├── Manages time scale per context
  ├── Broadcasts minute/hour/day events
  ├── Handles time consumption (actions that take time)
  └── Integrates with save/load system

NPCScheduler (INPCScheduler) - Subscribes to time events
  ├── Updates NPC locations on hour change
  ├── Triggers device movement
  └── Updates network topology

QuestManager (IQuestManager) - Subscribes to time events
  ├── Checks time-based quest conditions
  └── Handles timed quest failures

Other Systems
  ├── ContentGenerator (daily file generation)
  ├── WorldStateManager (day/night cycle)
  └── UIManager (clock display)
```

---

## Service Interface

```csharp
public interface ITimeManager : IGameService
{
    // === Current Time ===
    DateTime CurrentGameTime { get; }
    DateTime StartTime { get; }
    TimeSpan TotalTimePlayed { get; }
    
    // === Time Control ===
    void SetTimeContext(TimeContext context);
    TimeContext CurrentContext { get; }
    bool IsPaused { get; }
    
    void ConsumeTime(TimeSpan duration, string reason);
    void SkipTime(TimeSpan duration, bool triggerEvents = true);
    
    // === Configuration ===
    float RealSecondsPerGameMinute { get; set; }
    void SetTimeScale(TimeContext context, float scale);
    
    // === Events ===
    event Action<int> OnMinutePassed;          // Passes current minute (0-59)
    event Action<int> OnHourChanged;           // Passes current hour (0-23)
    event Action<DayOfWeek> OnDayChanged;      // Passes new day of week
    event Action<DateTime> OnTimeConsumed;     // Passes new time after consumption
    event Action<TimeContext> OnContextChanged; // Passes new context
    
    // === Validation ===
    bool CanChangeContext(TimeContext newContext);
    void RegisterContextValidator(Func<TimeContext, bool> validator);
}

public enum TimeContext
{
    Walking,        // Normal time (1x)
    DeviceUse,      // Paused (0x)
    Conversation,   // Paused (0x)
    JobMinigame,    // Slower (0.5x)
    Travel,         // Faster (3x)
    Sleeping,       // Very fast (10x)
    Custom          // Set manually
}
```

---

## Implementation

### TimeManager Service

```csharp
public class TimeManager : ITimeManager
{
    // === Configuration ===
    private float realSecondsPerGameMinute = 0.7f; // ~17 real minutes = 24 game hours
    private Dictionary<TimeContext, float> timeScales;
    
    // === State ===
    private DateTime currentGameTime;
    private DateTime startTime;
    private TimeSpan totalTimePlayed;
    private TimeContext currentContext = TimeContext.Walking;
    private bool isPaused;
    
    // === Context Validation ===
    private List<Func<TimeContext, bool>> contextValidators;
    
    // === Events ===
    public event Action<int> OnMinutePassed;
    public event Action<int> OnHourChanged;
    public event Action<DayOfWeek> OnDayChanged;
    public event Action<DateTime> OnTimeConsumed;
    public event Action<TimeContext> OnContextChanged;
    
    // === Properties ===
    public DateTime CurrentGameTime => currentGameTime;
    public DateTime StartTime => startTime;
    public TimeSpan TotalTimePlayed => totalTimePlayed;
    public TimeContext CurrentContext => currentContext;
    public bool IsPaused => isPaused || timeScales[currentContext] == 0f;
    
    public float RealSecondsPerGameMinute
    {
        get => realSecondsPerGameMinute;
        set => realSecondsPerGameMinute = Mathf.Max(0.1f, value);
    }
    
    // === Initialization ===
    
    public void Initialize()
    {
        // Default time scales
        timeScales = new Dictionary<TimeContext, float>
        {
            { TimeContext.Walking, 1f },
            { TimeContext.DeviceUse, 0f },      // Paused
            { TimeContext.Conversation, 0f },   // Paused
            { TimeContext.JobMinigame, 0.5f },
            { TimeContext.Travel, 3f },
            { TimeContext.Sleeping, 10f },
            { TimeContext.Custom, 1f }
        };
        
        contextValidators = new List<Func<TimeContext, bool>>();
        
        // Start time: Monday, 9:00 AM
        startTime = new DateTime(2025, 10, 27, 9, 0, 0); // A Monday
        currentGameTime = startTime;
        totalTimePlayed = TimeSpan.Zero;
        
        // Register update loop
        ServiceLocator.Get<IUpdateService>()?.RegisterUpdate(Update);
        
        Debug.Log($"[TimeManager] Initialized - Start time: {currentGameTime:dddd, MMMM dd, yyyy HH:mm}");
    }
    
    // === Time Progression ===
    
    public void Update(float deltaTime)
    {
        if (isPaused) return;
        
        float scale = timeScales[currentContext];
        if (scale == 0f) return; // Paused contexts (Device, Conversation)
        
        // Calculate game time progression
        float gameMinutes = (deltaTime / realSecondsPerGameMinute) * scale;
        AdvanceTime(TimeSpan.FromMinutes(gameMinutes));
        
        totalTimePlayed += TimeSpan.FromSeconds(deltaTime);
    }
    
    private void AdvanceTime(TimeSpan delta)
    {
        var oldTime = currentGameTime;
        currentGameTime += delta;
        
        // Broadcast events for time milestones
        if (oldTime.Minute != currentGameTime.Minute)
        {
            OnMinutePassed?.Invoke(currentGameTime.Minute);
        }
        
        if (oldTime.Hour != currentGameTime.Hour)
        {
            OnHourChanged?.Invoke(currentGameTime.Hour);
        }
        
        if (oldTime.DayOfWeek != currentGameTime.DayOfWeek)
        {
            OnDayChanged?.Invoke(currentGameTime.DayOfWeek);
        }
    }
    
    // === Time Control ===
    
    public void SetTimeContext(TimeContext context)
    {
        if (currentContext == context) return;
        
        // Validate context change
        if (!CanChangeContext(context))
        {
            Debug.LogWarning($"[TimeManager] Cannot change to context {context} - validation failed");
            return;
        }
        
        var oldContext = currentContext;
        currentContext = context;
        
        Debug.Log($"[TimeManager] Context changed: {oldContext} → {context} (scale: {timeScales[context]}x)");
        OnContextChanged?.Invoke(context);
    }
    
    public bool CanChangeContext(TimeContext newContext)
    {
        // Run all registered validators
        foreach (var validator in contextValidators)
        {
            if (!validator(newContext))
                return false;
        }
        
        return true;
    }
    
    public void RegisterContextValidator(Func<TimeContext, bool> validator)
    {
        contextValidators.Add(validator);
    }
    
    public void SetTimeScale(TimeContext context, float scale)
    {
        timeScales[context] = Mathf.Max(0f, scale);
        Debug.Log($"[TimeManager] Time scale for {context} set to {scale}x");
    }
    
    // === Time Consumption ===
    
    public void ConsumeTime(TimeSpan duration, string reason)
    {
        var oldTime = currentGameTime;
        currentGameTime += duration;
        
        Debug.Log($"[TimeManager] Consumed {duration.TotalMinutes:F1} minutes: {reason}");
        Debug.Log($"[TimeManager] Time advanced: {oldTime:HH:mm} → {currentGameTime:HH:mm}");
        
        // Broadcast events for any milestones crossed
        BroadcastMilestoneEvents(oldTime, currentGameTime);
        
        OnTimeConsumed?.Invoke(currentGameTime);
    }
    
    public void SkipTime(TimeSpan duration, bool triggerEvents = true)
    {
        var oldTime = currentGameTime;
        currentGameTime += duration;
        
        Debug.Log($"[TimeManager] Skipped {duration.TotalHours:F1} hours");
        Debug.Log($"[TimeManager] Time: {oldTime:dddd HH:mm} → {currentGameTime:dddd HH:mm}");
        
        if (triggerEvents)
        {
            BroadcastMilestoneEvents(oldTime, currentGameTime);
        }
    }
    
    private void BroadcastMilestoneEvents(DateTime from, DateTime to)
    {
        // Calculate how many hours/days passed
        int hoursPassed = (int)(to - from).TotalHours;
        
        // Broadcast hour changed events
        for (int i = 1; i <= hoursPassed; i++)
        {
            var intermediateTime = from.AddHours(i);
            OnHourChanged?.Invoke(intermediateTime.Hour);
            
            // Check for day change
            if (intermediateTime.DayOfWeek != from.DayOfWeek)
            {
                OnDayChanged?.Invoke(intermediateTime.DayOfWeek);
            }
        }
        
        // Final minute event
        OnMinutePassed?.Invoke(to.Minute);
    }
    
    // === Save/Load ===
    
    public TimeSaveData GetSaveData()
    {
        return new TimeSaveData
        {
            currentTime = currentGameTime,
            startTime = startTime,
            totalTimePlayed = totalTimePlayed,
            currentContext = currentContext
        };
    }
    
    public void LoadSaveData(TimeSaveData data)
    {
        currentGameTime = data.currentTime;
        startTime = data.startTime;
        totalTimePlayed = data.totalTimePlayed;
        currentContext = data.currentContext;
        
        Debug.Log($"[TimeManager] Loaded - Current time: {currentGameTime:dddd, MMMM dd, yyyy HH:mm}");
    }
}

[System.Serializable]
public class TimeSaveData
{
    public DateTime currentTime;
    public DateTime startTime;
    public TimeSpan totalTimePlayed;
    public TimeContext currentContext;
    public int saveVersion = 1;
}
```

---

## Integration with NPC Schedules

### Event-Driven Schedule Updates

```csharp
public class NPCScheduler : INPCScheduler
{
    private ITimeManager timeManager;
    private INPCManager npcManager;
    private IDeviceRegistry deviceRegistry;
    private INetworkTopology networkTopology;
    
    public void Initialize()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        npcManager = ServiceLocator.Get<INPCManager>();
        deviceRegistry = ServiceLocator.Get<IDeviceRegistry>();
        networkTopology = ServiceLocator.Get<INetworkTopology>();
        
        // Subscribe to time events
        timeManager.OnHourChanged += OnHourChanged;
        timeManager.OnDayChanged += OnDayChanged;
        
        Debug.Log("[NPCScheduler] Initialized and subscribed to time events");
    }
    
    private void OnHourChanged(int hour)
    {
        Debug.Log($"[NPCScheduler] Hour changed to {hour}:00 - updating NPC schedules");
        UpdateAllNPCSchedules(timeManager.CurrentGameTime);
    }
    
    private void OnDayChanged(DayOfWeek day)
    {
        Debug.Log($"[NPCScheduler] Day changed to {day} - triggering daily events");
        
        // Trigger daily content generation
        var contentGenerator = ServiceLocator.Get<INPCContentGenerator>();
        contentGenerator?.GenerateDailyContent();
    }
    
    private void UpdateAllNPCSchedules(DateTime currentTime)
    {
        var allNpcs = npcManager.GetAllNPCs();
        
        foreach (var npc in allNpcs)
        {
            var newLocation = GetLocationForTime(npc, currentTime);
            
            if (newLocation != npc.CurrentLocation)
            {
                MoveNPC(npc, newLocation);
            }
        }
    }
    
    private PhysicalLocation GetLocationForTime(NPC npc, DateTime time)
    {
        // 1. Check schedule overrides (highest priority first)
        var overrides = npc.ScheduleOverrides
            .OrderByDescending(o => o.Priority)
            .Where(o => o.IsActive && o.IsValidForTime(time));
        
        foreach (var override in overrides)
        {
            var location = override.GetLocation(npc, time);
            if (location != null)
                return location;
        }
        
        // 2. Fall back to default schedule
        return GetDefaultLocation(npc, time);
    }
    
    private PhysicalLocation GetDefaultLocation(NPC npc, DateTime time)
    {
        var schedule = npc.DefaultSchedule;
        var daySchedule = schedule.FirstOrDefault(s => s.Day == time.DayOfWeek);
        
        if (daySchedule == null)
            return npc.HomeLocation; // Fallback
        
        var entry = daySchedule.Entries
            .Where(e => time.TimeOfDay >= e.StartTime && time.TimeOfDay < e.EndTime)
            .FirstOrDefault();
        
        return entry?.Location ?? npc.HomeLocation;
    }
    
    private void MoveNPC(NPC npc, PhysicalLocation newLocation)
    {
        var oldLocation = npc.CurrentLocation;
        
        Debug.Log($"[NPCScheduler] Moving {npc.Name}: {oldLocation?.Name ?? "Unknown"} → {newLocation.Name}");
        
        // 1. Update NPC location
        npcManager.SetNPCLocation(npc.NpcId, newLocation);
        
        // 2. Move portable devices with the NPC
        MoveNPCDevices(npc, newLocation);
        
        // 3. Emit event for other systems
        GameEvents.Publish(GameEventType.NPCMoved, new NPCMovedEventData
        {
            NpcId = npc.NpcId,
            OldLocation = oldLocation,
            NewLocation = newLocation,
            Time = timeManager.CurrentGameTime
        });
    }
    
    private void MoveNPCDevices(NPC npc, PhysicalLocation newLocation)
    {
        foreach (var deviceId in npc.OwnedDeviceIds)
        {
            var device = deviceRegistry.GetDevice(deviceId);
            
            if (device == null || !device.IsPortable)
                continue;
            
            // Disconnect from old network
            if (device.CurrentNetworkId != null)
            {
                networkTopology.DisconnectDevice(deviceId);
                Debug.Log($"[NPCScheduler] Disconnected {device.Hostname} from network");
            }
            
            // Update device location
            deviceRegistry.UpdateDeviceLocation(deviceId, newLocation);
            
            // Connect to new network (if available and device supports auto-connect)
            var localNetwork = networkTopology.GetNetworkAtLocation(newLocation);
            
            if (localNetwork != null && device.CanAutoConnect(localNetwork))
            {
                networkTopology.ConnectDevice(deviceId, localNetwork.NetworkId);
                Debug.Log($"[NPCScheduler] Connected {device.Hostname} to {localNetwork.Name}");
            }
        }
    }
}
```

---

## Time Consumption System

### Use Cases

**Hacking Actions:**

```csharp
public class HackingService : IHackingService
{
    private ITimeManager timeManager;
    
    public void ExecuteExploit(string deviceId, Exploit exploit)
    {
        // Exploit execution takes time
        int timeRequired = CalculateExploitTime(exploit);
        timeManager.ConsumeTime(TimeSpan.FromMinutes(timeRequired), 
            $"Executing {exploit.Name} against {deviceId}");
        
        // ... perform exploit
    }
    
    private int CalculateExploitTime(Exploit exploit)
    {
        // More complex exploits take longer
        return exploit.Complexity switch
        {
            ExploitComplexity.Simple => 1,   // 1 minute
            ExploitComplexity.Medium => 5,   // 5 minutes
            ExploitComplexity.Complex => 15, // 15 minutes
            ExploitComplexity.Expert => 30,  // 30 minutes
            _ => 5
        };
    }
}
```

**Social Actions:**

```csharp
public class SocialActionService : ISocialActionService
{
    private ITimeManager timeManager;
    
    public void SendPhishingEmail(string targetNpcId, string emailContent)
    {
        // Writing and sending takes time
        timeManager.ConsumeTime(TimeSpan.FromMinutes(5), 
            "Crafting and sending phishing email");
        
        // ... send email
    }
    
    public void MakeFakePhoneCall(string targetNpcId)
    {
        // Phone calls take time
        timeManager.ConsumeTime(TimeSpan.FromMinutes(10), 
            $"Phone call with {targetNpcId}");
        
        // ... handle call
    }
}
```

**Job Minigames:**

```csharp
public class JobService : IJobService
{
    private ITimeManager timeManager;
    
    public void CompleteWorkShift(Job job)
    {
        // Work shifts consume time
        int shiftHours = job.ShiftDuration;
        timeManager.ConsumeTime(TimeSpan.FromHours(shiftHours), 
            $"Working at {job.CompanyName}");
        
        // Award pay, update stats, etc.
    }
}
```

---

## Skip Time Feature

### Implementation

```csharp
public class SkipTimeController : MonoBehaviour
{
    [SerializeField] private CanvasGroup skipTimePanel;
    [SerializeField] private Slider timeSlider;
    [SerializeField] private TextMeshProUGUI targetTimeText;
    [SerializeField] private Button skipButton;
    
    private ITimeManager timeManager;
    private TimeSpan selectedDuration;
    
    // === TBD: Access restrictions ===
    // TODO: Decide if skip time is accessible anywhere or only at safe locations
    // private bool IsAtSafeLocation() { ... }
    
    private void Start()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        skipButton.onClick.AddListener(OnSkipButtonClicked);
        timeSlider.onValueChanged.AddListener(OnSliderChanged);
    }
    
    public void ShowSkipTimeUI()
    {
        // TBD: Check if player is at safe location
        // if (!IsAtSafeLocation()) { ShowError("Must be at home to skip time"); return; }
        
        skipTimePanel.gameObject.SetActive(true);
        timeSlider.value = 0;
        UpdateTargetTimeText();
    }
    
    private void OnSliderChanged(float value)
    {
        // Slider: 0 = +1 hour, 1 = +24 hours
        selectedDuration = TimeSpan.FromHours(Mathf.Lerp(1, 24, value));
        UpdateTargetTimeText();
    }
    
    private void UpdateTargetTimeText()
    {
        var targetTime = timeManager.CurrentGameTime + selectedDuration;
        targetTimeText.text = $"Skip to: {targetTime:dddd, HH:mm}";
    }
    
    private void OnSkipButtonClicked()
    {
        // Skip time (with events)
        timeManager.SkipTime(selectedDuration, triggerEvents: true);
        
        // TBD: Show "what happened during skip" UI
        // ShowSkipSummary();
        
        skipTimePanel.gameObject.SetActive(false);
    }
    
    // === TBD: Skip Summary ===
    // TODO: Decide if we want to show what happened during skip
    /*
    private void ShowSkipSummary()
    {
        var summary = new SkipTimeSummary
        {
            EmailsReceived = emailService.GetEmailsReceivedDuring(selectedDuration),
            NPCMovements = npcScheduler.GetMovementsDuring(selectedDuration),
            QuestsUpdated = questManager.GetQuestsAffectedDuring(selectedDuration)
        };
        
        skipSummaryUI.Show(summary);
    }
    */
}
```

### Event Handling During Skip

When `SkipTime()` is called with `triggerEvents: true`, all intermediate hour/day events are fired:

```csharp
// Example: Skip from Monday 9:00 AM to Tuesday 2:00 PM (29 hours)
timeManager.SkipTime(TimeSpan.FromHours(29), triggerEvents: true);

// This will trigger:
// - OnHourChanged (29 times) - Every hour from 9 AM Monday to 2 PM Tuesday
// - OnDayChanged (1 time) - When crossing midnight
// - NPCScheduler updates NPC locations 29 times
// - ContentGenerator generates daily content once (on day change)
// - QuestManager checks time-based conditions 29 times
```

**Performance Note:** For very long skips (e.g., 7 days), this could be expensive. Consider optimizing:

```csharp
public void SkipTime(TimeSpan duration, bool triggerEvents = true)
{
    var oldTime = currentGameTime;
    currentGameTime += duration;
    
    if (triggerEvents)
    {
        if (duration.TotalHours > 48)
        {
            // Long skip: Only trigger important events (day changes, weekly events)
            BroadcastCondensedEvents(oldTime, currentGameTime);
        }
        else
        {
            // Short skip: Trigger all events
            BroadcastMilestoneEvents(oldTime, currentGameTime);
        }
    }
}
```

---

## Context Validation

### Use Cases

**Prevent time pause during scripted events:**

```csharp
public class TutorialManager : MonoBehaviour
{
    private ITimeManager timeManager;
    
    private void Start()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        // During tutorial, don't allow pausing time
        timeManager.RegisterContextValidator(context =>
        {
            if (IsTutorialActive && context == TimeContext.DeviceUse)
            {
                ShowTutorialMessage("Complete the tutorial first!");
                return false; // Prevent context change
            }
            return true;
        });
    }
}
```

**Prevent context change during combat/chase:**

```csharp
public class CombatManager : MonoBehaviour
{
    private ITimeManager timeManager;
    
    private void OnCombatStarted()
    {
        timeManager.RegisterContextValidator(context =>
        {
            if (IsInCombat && context == TimeContext.DeviceUse)
            {
                ShowMessage("Can't use devices during combat!");
                return false;
            }
            return true;
        });
    }
}
```

---

## UI Integration

### Time Display Widget

```csharp
public class TimeDisplayWidget : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeText;
    [SerializeField] private TextMeshProUGUI dateText;
    [SerializeField] private Image pauseIndicator;
    
    private ITimeManager timeManager;
    
    private void Start()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        // Subscribe to time events
        timeManager.OnMinutePassed += OnTimeChanged;
        timeManager.OnContextChanged += OnContextChanged;
        
        UpdateDisplay();
    }
    
    private void OnTimeChanged(int minute)
    {
        UpdateDisplay();
    }
    
    private void OnContextChanged(TimeContext context)
    {
        // Show/hide pause indicator
        pauseIndicator.gameObject.SetActive(timeManager.IsPaused);
    }
    
    private void UpdateDisplay()
    {
        var time = timeManager.CurrentGameTime;
        
        timeText.text = time.ToString("HH:mm");
        dateText.text = time.ToString("dddd, MMM dd");
        
        // Animate if time is paused
        if (timeManager.IsPaused)
        {
            timeText.color = Color.yellow; // Visual indicator
        }
        else
        {
            timeText.color = Color.white;
        }
    }
}
```

---

## Save System Integration

### Save/Load Implementation

```csharp
public class SaveSystem
{
    public void SaveGame(string slotName)
    {
        var saveData = new GameSaveData
        {
            // ... other data ...
            timeData = ServiceLocator.Get<ITimeManager>().GetSaveData(),
            saveVersion = GameSaveData.CURRENT_VERSION,
            saveTimestamp = DateTime.Now
        };
        
        SaveToFile(slotName, saveData);
    }
    
    public GameSaveData LoadGame(string slotName)
    {
        var saveData = LoadFromFile(slotName);
        
        // Migrate if needed
        if (saveData.saveVersion < GameSaveData.CURRENT_VERSION)
        {
            saveData = MigrateSaveData(saveData);
        }
        
        // Load time state
        ServiceLocator.Get<ITimeManager>().LoadSaveData(saveData.timeData);
        
        return saveData;
    }
}
```

### Save Versioning & Migration

```csharp
[System.Serializable]
public class TimeSaveData
{
    public const int CURRENT_VERSION = 2;
    
    public int version = CURRENT_VERSION;
    public DateTime currentTime;
    public DateTime startTime;
    public TimeSpan totalTimePlayed;
    public TimeContext currentContext;
    
    // Version 2: Added time scale customization
    public Dictionary<TimeContext, float> customTimeScales;
}

public class TimeSaveDataMigrator
{
    public static TimeSaveData Migrate(TimeSaveData oldData)
    {
        if (oldData.version == TimeSaveData.CURRENT_VERSION)
            return oldData;
        
        var migratedData = oldData;
        
        if (oldData.version < 2)
            migratedData = MigrateV1ToV2(migratedData);
        
        migratedData.version = TimeSaveData.CURRENT_VERSION;
        return migratedData;
    }
    
    private static TimeSaveData MigrateV1ToV2(TimeSaveData oldData)
    {
        // Version 2 added custom time scales
        var newData = new TimeSaveData
        {
            version = 2,
            currentTime = oldData.currentTime,
            startTime = oldData.startTime,
            totalTimePlayed = oldData.totalTimePlayed,
            currentContext = oldData.currentContext,
            customTimeScales = null // Use defaults
        };
        
        return newData;
    }
}
```

### No Real-World Time Progression

**Design Decision:** Time does NOT advance when the game is closed.

**Why?**

- ✅ No pressure to play daily (anti-FOMO design)
- ✅ No "missed events" while offline
- ✅ Respects player's schedule
- ✅ Simpler save/load logic

**Implementation:**

```csharp
public void LoadSaveData(TimeSaveData data)
{
    currentGameTime = data.currentTime;
    // ... rest of state
    
    // Do NOT apply real-world time delta:
    // var realTimePassed = DateTime.Now - data.saveTimestamp; // ❌ Don't do this
    // currentGameTime += realTimePassed; // ❌ Don't do this
    
    Debug.Log($"[TimeManager] Loaded save - Time: {currentGameTime:dddd HH:mm}");
}
```

### Timed Events Only Expire In-Game

**Example: Time-sensitive quest**

```csharp
public class Quest
{
    public DateTime UnlockedAt;
    public TimeSpan TimeLimit; // e.g., 3 game days
    
    public bool HasExpired(DateTime currentGameTime)
    {
        return currentGameTime > UnlockedAt + TimeLimit;
    }
}

// Quest expires 3 days after unlocking (in game time)
// If player saves on Monday and loads on real-world Friday, quest is still active
// Quest only expires after 3 in-game days have passed
```

---

## Event Priority & Ordering

### Guaranteed Execution Order

When multiple systems subscribe to the same time event, execution order matters:

**Priority Levels:**

1. **High Priority (100+):** Core systems that others depend on
2. **Normal Priority (50-99):** Gameplay systems
3. **Low Priority (0-49):** UI updates, analytics

**Implementation:**

```csharp
public interface ITimeManager
{
    void SubscribeToHourChanged(Action<int> callback, int priority = 50);
    void SubscribeToHourChanged(Action<int> callback, int priority = 50);
    void SubscribeToDayChanged(Action<DayOfWeek> callback, int priority = 50);
}

public class TimeManager : ITimeManager
{
    private SortedDictionary<int, List<Action<int>>> hourChangedSubscribers;
    
    private void BroadcastHourChanged(int hour)
    {
        // Execute in priority order (high to low)
        foreach (var priority in hourChangedSubscribers.Keys.OrderByDescending(k => k))
        {
            foreach (var callback in hourChangedSubscribers[priority])
            {
                try
                {
                    callback(hour);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[TimeManager] Error in hour changed callback: {e.Message}");
                    // Continue executing other callbacks
                }
            }
        }
    }
}
```

**Example Usage:**

```csharp
// NPCScheduler: High priority (must run first)
timeManager.SubscribeToHourChanged(OnHourChanged, priority: 100);

// QuestManager: Normal priority (depends on NPC locations)
timeManager.SubscribeToHourChanged(OnHourChanged, priority: 50);

// UIManager: Low priority (just updates display)
timeManager.SubscribeToHourChanged(OnHourChanged, priority: 10);
```

### Async/Deferred Event Handling

**When to use async:**

- ✅ Heavy computations (pathfinding, complex AI)
- ✅ Operations that can tolerate delay (content generation)
- ✅ Non-critical updates (analytics)

**When NOT to use async:**

- ❌ Time-critical operations (NPC schedule updates)
- ❌ Operations others depend on (device location updates)
- ❌ Player-visible state changes

**Implementation:**

```csharp
public class NPCContentGenerator : INPCContentGenerator
{
    private ITimeManager timeManager;
    
    public void Initialize()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        // Use async for daily content generation (not time-critical)
        timeManager.SubscribeToDayChanged(async (day) =>
        {
            await GenerateDailyContentAsync();
        }, priority: 50);
    }
    
    private async Task GenerateDailyContentAsync()
    {
        Debug.Log("[ContentGenerator] Starting daily content generation...");
        
        // Run on background thread
        await Task.Run(() =>
        {
            // Heavy computation here
            GenerateEmailsForAllNPCs();
            GenerateNewsArticles();
            GenerateForumPosts();
        });
        
        Debug.Log("[ContentGenerator] Daily content generation complete");
    }
}
```

### Cascading Events

**Problem:** One event triggers another, which triggers another...

**Example:**

```
TimeManager: OnDayChanged
  → NPCScheduler: Updates schedules
    → DeviceRegistry: Moves devices
      → NetworkTopology: Reconnects devices
        → LeadManager: Creates "Device moved" lead
          → QuestManager: Checks quest conditions
```

**Solution: Event batching & debouncing**

```csharp
public class EventBatcher
{
    private Dictionary<GameEventType, List<object>> batchedEvents;
    private float batchWindow = 0.1f; // 100ms
    private Coroutine flushCoroutine;
    
    public void PublishBatched(GameEventType eventType, object data)
    {
        if (!batchedEvents.ContainsKey(eventType))
            batchedEvents[eventType] = new List<object>();
        
        batchedEvents[eventType].Add(data);
        
        // Start/restart flush timer
        if (flushCoroutine != null)
            StopCoroutine(flushCoroutine);
        
        flushCoroutine = StartCoroutine(FlushAfterDelay());
    }
    
    private IEnumerator FlushAfterDelay()
    {
        yield return new WaitForSeconds(batchWindow);
        
        // Flush all batched events
        foreach (var kvp in batchedEvents)
        {
            GameEvents.PublishBatch(kvp.Key, kvp.Value);
        }
        
        batchedEvents.Clear();
    }
}
```

---

## Performance Considerations

### Time Event Frequency

**Problem:** Broadcasting events every minute/hour can be expensive with many subscribers

**Solutions:**

1. **Priority-based subscriptions** (already implemented)
2. **Event filtering** (only notify when relevant)

```csharp
public class SmartTimeSubscription
{
    // Only notify on specific hours
    public void SubscribeToSpecificHours(Action<int> callback, params int[] hours)
    {
        timeManager.OnHourChanged += (hour) =>
        {
            if (hours.Contains(hour))
                callback(hour);
        };
    }
    
    // Only notify during business hours
    public void SubscribeToBusinessHours(Action<int> callback)
    {
        SubscribeToSpecificHours(callback, 9, 10, 11, 12, 13, 14, 15, 16, 17);
    }
}
```

3. **Debouncing** (for rapid time consumption)

```csharp
private Coroutine debounceCoroutine;
private List<DateTime> pendingTimeUpdates = new List<DateTime>();

public void ConsumeTime(TimeSpan duration, string reason)
{
    currentGameTime += duration;
    pendingTimeUpdates.Add(currentGameTime);
    
    // Debounce event broadcasting
    if (debounceCoroutine != null)
        StopCoroutine(debounceCoroutine);
    
    debounceCoroutine = StartCoroutine(BroadcastAfterDelay());
}

private IEnumerator BroadcastAfterDelay()
{
    yield return new WaitForSeconds(0.1f);
    
    // Broadcast only the final time
    var finalTime = pendingTimeUpdates.Last();
    OnTimeConsumed?.Invoke(finalTime);
    pendingTimeUpdates.Clear();
}
```

### Memory Management

**Best Practices:**

- ✅ Use events (delegates) for time subscriptions, not polling
- ✅ Unsubscribe when systems are destroyed
- ✅ Limit time history storage (e.g., last 100 time consumption events)

```csharp
public class TimeHistoryTracker
{
    private const int MAX_HISTORY_SIZE = 100;
    private Queue<TimeConsumptionEvent> history = new Queue<TimeConsumptionEvent>();
    
    public void RecordTimeConsumption(TimeSpan duration, string reason)
    {
        history.Enqueue(new TimeConsumptionEvent
        {
            Duration = duration,
            Reason = reason,
            Timestamp = timeManager.CurrentGameTime
        });
        
        // Prune old history
        while (history.Count > MAX_HISTORY_SIZE)
        {
            history.Dequeue();
        }
    }
}
```

---

## Unity Editor Tools

### Time Control Window

```csharp
public class TimeControlWindow : EditorWindow
{
    [MenuItem("Tools/Time Control")]
    public static void ShowWindow()
    {
        GetWindow<TimeControlWindow>("Time Control");
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to control time", MessageType.Info);
            return;
        }
        
        var timeManager = ServiceLocator.Get<ITimeManager>();
        
        EditorGUILayout.LabelField("Current Time", EditorStyles.boldLabel);
        EditorGUILayout.LabelField(timeManager.CurrentGameTime.ToString("dddd, MMMM dd, yyyy HH:mm"));
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Context", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Current: {timeManager.CurrentContext}");
        EditorGUILayout.LabelField($"Paused: {timeManager.IsPaused}");
        
        EditorGUILayout.Space();
        
        EditorGUILayout.LabelField("Quick Actions", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Skip 1 Hour"))
        {
            timeManager.SkipTime(TimeSpan.FromHours(1));
        }
        
        if (GUILayout.Button("Skip 1 Day"))
        {
            timeManager.SkipTime(TimeSpan.FromDays(1));
        }
        
        if (GUILayout.Button("Skip 1 Week"))
        {
            timeManager.SkipTime(TimeSpan.FromDays(7));
        }
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button(timeManager.IsPaused ? "Resume Time" : "Pause Time"))
        {
            timeManager.SetTimeContext(
                timeManager.IsPaused ? TimeContext.Walking : TimeContext.DeviceUse
            );
        }
    }
}
```

### Time Debug Overlay

```csharp
public class TimeDebugOverlay : MonoBehaviour
{
    private ITimeManager timeManager;
    private bool showDebug = false;
    
    private void Start()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
    }
    
    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F3))
        {
            showDebug = !showDebug;
        }
    }
    
    private void OnGUI()
    {
        if (!showDebug) return;
        
        var style = new GUIStyle(GUI.skin.box);
        style.alignment = TextAnchor.UpperLeft;
        
        var info = $"Time: {timeManager.CurrentGameTime:dddd HH:mm:ss}\n" +
                   $"Context: {timeManager.CurrentContext}\n" +
                   $"Scale: {GetCurrentScale()}x\n" +
                   $"Paused: {timeManager.IsPaused}\n" +
                   $"Played: {timeManager.TotalTimePlayed:hh\\:mm\\:ss}";
        
        GUI.Box(new Rect(10, 10, 300, 120), info, style);
    }
    
    private float GetCurrentScale()
    {
        // Access private time scales (for debug only)
        return 1.0f; // TODO: Expose this in ITimeManager if needed
    }
}
```

---

## Troubleshooting & Common Pitfalls

### Issue: Time Events Not Firing

**Symptom:** `OnHourChanged` not being called

**Cause:** TimeManager not registered with update service

**Solution:**

```csharp
public void Initialize()
{
    // ...
    
    // MUST register with update service
    ServiceLocator.Get<IUpdateService>()?.RegisterUpdate(Update);
}
```

### Issue: Time Skipping When Paused

**Symptom:** Time advances even in `DeviceUse` context

**Cause:** Forgot to check time scale

**Solution:**

```csharp
public void Update(float deltaTime)
{
    if (isPaused) return;
    
    float scale = timeScales[currentContext];
    if (scale == 0f) return; // ← Must check this!
    
    // ...
}
```

### Issue: NPCs Not Moving

**Symptom:** NPCScheduler subscribed to time events but NPCs don't move

**Cause:** Event subscription after time started

**Solution:**

```csharp
public void Initialize()
{
    // Subscribe BEFORE any time events are fired
    timeManager.OnHourChanged += OnHourChanged; // ← Do this first
    
    // Then do other initialization
}
```

### Issue: Cascading Event Performance

**Symptom:** Game stutters when day changes

**Cause:** Too many synchronous operations in day change handler

**Solution:**

```csharp
private async void OnDayChanged(DayOfWeek day)
{
    // Move heavy operations to background thread
    await Task.Run(() =>
    {
        GenerateAllDailyContent();
    });
}
```

### Issue: Save/Load Time Corruption

**Symptom:** Time resets or jumps to wrong date after load

**Cause:** DateTime serialization issue

**Solution:**

```csharp
// Use Ticks for reliable serialization
[System.Serializable]
public class TimeSaveData
{
    public long currentTimeTicks; // ← Use ticks, not DateTime directly
    
    public DateTime GetCurrentTime() => new DateTime(currentTimeTicks);
    public void SetCurrentTime(DateTime time) => currentTimeTicks = time.Ticks;
}
```

---

## Quick Start Guide

### 1. Initialize TimeManager

```csharp
// In ServiceLocator initialization:
var timeManager = new TimeManager();
timeManager.Initialize();
ServiceLocator.Register<ITimeManager>(timeManager);
```

### 2. Subscribe to Time Events

```csharp
public class MySystem : MonoBehaviour
{
    private ITimeManager timeManager;
    
    private void Start()
    {
        timeManager = ServiceLocator.Get<ITimeManager>();
        timeManager.OnHourChanged += OnHourChanged;
    }
    
    private void OnDestroy()
    {
        // IMPORTANT: Unsubscribe!
        if (timeManager != null)
            timeManager.OnHourChanged -= OnHourChanged;
    }
    
    private void OnHourChanged(int hour)
    {
        Debug.Log($"Hour changed to {hour}:00");
    }
}
```

### 3. Control Time Context

```csharp
// When player opens device
timeManager.SetTimeContext(TimeContext.DeviceUse); // Pauses time

// When player closes device
timeManager.SetTimeContext(TimeContext.Walking); // Resumes time
```

### 4. Consume Time

```csharp
// After player executes an action
timeManager.ConsumeTime(TimeSpan.FromMinutes(15), "Executed SQL injection");
```

---

## Summary

The Time System achieves:

- ✅ **Service-based architecture**: Persistent across scenes, single source of truth
- ✅ **Event-driven integration**: Other systems subscribe to time events
- ✅ **Context-aware scaling**: Different activities have different time scales
- ✅ **Deterministic**: No real-world time, only in-game time
- ✅ **Coordinated NPC movement**: NPCScheduler updates locations on hour change
- ✅ **Save/load ready**: Version migration, no data loss
- ✅ **Performance optimized**: Priority-based events, batching, debouncing
- ✅ **Debug-friendly**: Editor tools, overlay, validators
