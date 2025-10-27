# Save System Architecture

## Overview

The Save System manages game state persistence, autosaving, manual saves, and data migration. It's designed to handle **full-detail world state** including player progression, NPC positions, device file systems, quest states, and conversation history while remaining performant and resilient to corruption.

**Key Design Principles:**

- **Soft save scumming prevention**: Autosaves + limited manual saves at safe locations
- **Full detail preservation**: Save everything - files, emails, IM logs, NPC exact positions
- **Dual autosave strategy**: Named event saves (permanent) + rolling saves (crash recovery)
- **Forward migration**: Version numbers per system, graceful degradation for missing data
- **Development-friendly**: JSON for debugging, binary for production

---

## Design Decisions Summary

### Save Strategy
- **Autosave Frequency**: Every 5 minutes (background) + event-based (quest/hack/dialogue)
- **Manual Saves**: Limited to safe locations (player's apartment, safe houses, bed)
- **Save Slots**: One save per playthrough/character
- **Autosave History**: Last 3-5 rolling autosaves + named event saves

### Data Granularity
- **Full Detail**: Device file systems (all files), email/IM conversations (full logs), NPC positions (exact locations)
- **Reconstructable**: Network topology (rebuilt from device states)
- **Event-Based**: Quest progress, conversation history, lead investigation states

### Technical Details
- **Format**: JSON (development) → Binary with checksum (production)
- **Versioning**: Forward migration with version numbers per system
- **Storage**: Local files (development/initial), Steam Cloud (future feature)
- **Time Handling**: Exact state restoration, no catch-up mechanics

---

## Core Architecture

### Component Hierarchy

```
SaveSystem (ISaveSystem) - Service
  ├── Coordinates save/load operations
  ├── Manages autosave scheduling
  ├── Handles save file I/O
  └── Orchestrates data migration

SaveFileManager
  ├── File path resolution
  ├── Checksum validation
  ├── Compression/decompression
  └── Format conversion (JSON ↔ Binary)

AutosaveScheduler
  ├── Background autosave timer (5 min)
  ├── Event-based autosave triggers
  ├── Rolling autosave management
  └── Named event save management

ISaveable (Interface)
  └── Implemented by all systems that need persistence
      ├── QuestManager
      ├── LeadManager
      ├── NPCManager
      ├── TimeManager
      ├── DeviceRegistry
      ├── PlayerManager
      └── etc.

SaveDataMigrator
  ├── Version detection
  ├── Migration path execution
  └── Fallback value generation
```

---

## Service Interface

```csharp
public interface ISaveSystem : IGameService
{
    // === Save Operations ===
    void SaveGame(string saveName, SaveType saveType);
    void SaveGameAsync(string saveName, SaveType saveType, Action<SaveResult> onComplete);
    bool CanSaveAtCurrentLocation(); // Check if player is at safe location
    
    // === Load Operations ===
    SaveGameData LoadGame(string saveName);
    void LoadGameAsync(string saveName, Action<SaveGameData> onComplete);
    List<SaveFileInfo> GetAvailableSaves();
    
    // === Autosave Management ===
    void EnableAutosave();
    void DisableAutosave();
    void TriggerEventBasedAutosave(string eventName, string description);
    List<SaveFileInfo> GetAutosaveHistory();
    
    // === Save File Management ===
    bool DeleteSave(string saveName);
    bool RenameSave(string oldName, string newName);
    SaveFileInfo GetSaveInfo(string saveName);
    bool IsSaveCorrupted(string saveName);
    
    // === Events ===
    event Action<SaveType> OnSaveStarted;
    event Action<SaveResult> OnSaveCompleted;
    event Action<string> OnLoadStarted;
    event Action<SaveGameData> OnLoadCompleted;
    event Action<string> OnAutosaveTriggered;
}

public enum SaveType
{
    Manual,              // Player manually saved at safe location
    AutosaveRolling,     // Background autosave (5 min timer)
    AutosaveEvent,       // Event-based autosave (quest/hack/dialogue)
    Quicksave            // Future: Quick save feature (not implemented yet)
}

public class SaveResult
{
    public bool Success;
    public string SaveName;
    public SaveType SaveType;
    public DateTime Timestamp;
    public long FileSizeBytes;
    public string ErrorMessage;
}

public class SaveFileInfo
{
    public string SaveName;
    public string DisplayName;        // Human-readable name
    public SaveType SaveType;
    public DateTime SaveTimestamp;
    public TimeSpan PlayTime;
    public int SaveVersion;
    public long FileSizeBytes;
    public bool IsCorrupted;
    
    // Preview data (for load screen)
    public string PlayerName;
    public string CurrentLocation;
    public DateTime GameTime;
    public int PlayerLevel;
    public string LastQuestCompleted;
}
```

---

## Data Structures

### Master Save File

```csharp
[System.Serializable]
public class SaveGameData
{
    public const int CURRENT_VERSION = 1;
    
    // === Metadata ===
    public int SaveVersion = CURRENT_VERSION;
    public string SaveName;
    public SaveType SaveType;
    public DateTime SaveTimestamp;
    public TimeSpan TotalPlayTime;
    public string GameVersion;           // e.g., "0.1.5-alpha"
    
    // === Preview Data (for load menu) ===
    public string PlayerName;
    public string CurrentLocation;
    public int PlayerLevel;
    public string LastQuestCompleted;
    
    // === System Save Data ===
    public PlayerSaveData PlayerData;
    public TimeSaveData TimeData;
    public QuestSaveData QuestData;
    public LeadSaveData LeadData;
    public NPCSaveData NPCData;
    public DeviceSaveData DeviceData;
    public WorldSaveData WorldData;
    public ProgressionSaveData ProgressionData;
    
    // === Checksum (for corruption detection) ===
    public string Checksum;              // SHA256 hash of serialized data
}
```

### Player Save Data

```csharp
[System.Serializable]
public class PlayerSaveData
{
    public const int CURRENT_VERSION = 1;
    public int SaveVersion = CURRENT_VERSION;
    
    // === Identity ===
    public string PlayerName;
    public string PlayerId;
    
    // === Stats ===
    public Dictionary<StatType, int> PrimaryStats;   // Job-related stats
    public Dictionary<StatType, int> HackingStats;   // Hacking-related stats
    
    // === Progression ===
    public int PlayerLevel;
    public int Experience;
    public int Money;
    public int Karma;                    // Black hat vs white hat (-100 to +100)
    
    // === Inventory ===
    public List<string> InventoryItemIds;
    public Dictionary<string, int> ItemQuantities;
    
    // === Compromised Devices ===
    public List<string> CompromisedDeviceIds;
    public Dictionary<string, DateTime> CompromiseTimestamps;
    
    // === Location ===
    public string CurrentLocationId;
    public Vector3 PlayerPosition;
    public Vector3 PlayerRotation;
    
    // === Reputation & Heat ===
    public Dictionary<string, int> FactionReputation;  // { "Anonymous": 75, "FBI": -20 }
    public Dictionary<string, float> HeatLevels;       // { "LocalPD": 0.3, "FBI": 0.8 }
    
    // === Job System ===
    public string CurrentJobId;
    public Dictionary<string, JobProgressData> JobProgress;
}

public enum StatType
{
    // Primary Stats (Job-related)
    Intelligence,
    Charisma,
    Technical,
    Creativity,
    Attention,
    
    // Hacking Stats
    Exploitation,
    SocialEngineering,
    Cryptography,
    NetworkAnalysis,
    Stealth
}

[System.Serializable]
public class JobProgressData
{
    public string JobId;
    public int DaysWorked;
    public int PerformanceRating;
    public bool IsCurrentJob;
    public DateTime HireDate;
    public DateTime? LastWorkDate;
}
```

### Device Save Data

```csharp
[System.Serializable]
public class DeviceSaveData
{
    public const int CURRENT_VERSION = 1;
    public int SaveVersion = CURRENT_VERSION;
    
    // === All Devices ===
    public List<DeviceState> DeviceStates;
    
    // === Network Topology (reconstructable, but cached for performance) ===
    public List<NetworkConnection> NetworkConnections;
}

[System.Serializable]
public class DeviceState
{
    public string DeviceId;
    public string Hostname;
    public string OperatingSystem;
    public bool IsDiscovered;
    public bool IsCompromised;
    public DateTime? CompromiseDate;
    
    // === Location ===
    public string CurrentLocationId;      // Where is this device physically?
    public string OwnerId;                // NPC who owns this device (if portable)
    
    // === File System (FULL DETAIL) ===
    public VirtualFileSystemSnapshot FileSystem;
    
    // === Software State ===
    public List<string> InstalledSoftwareIds;
    public Dictionary<string, SoftwareState> SoftwareStates;
    
    // === Network State ===
    public List<string> ConnectedNetworkIds;
    public string CurrentNetworkId;       // Primary network connection
    public List<PortState> OpenPorts;
    
    // === Access State ===
    public List<UserAccountState> UserAccounts;
    public List<string> PlayerKnownCredentials;  // Credentials player has discovered
}

[System.Serializable]
public class VirtualFileSystemSnapshot
{
    public List<VirtualFileState> Files;
    public List<VirtualDirectoryState> Directories;
}

[System.Serializable]
public class VirtualFileState
{
    public string FileId;
    public string FileName;
    public string FilePath;
    public string FileType;              // ".txt", ".exe", ".pdf", etc.
    public long FileSizeBytes;
    public DateTime CreatedDate;
    public DateTime ModifiedDate;
    public string OwnerId;               // NPC who created this file
    
    // === Content ===
    public string ContentType;           // "Email", "Document", "Script", "Binary"
    public string Content;               // Full file content (for text files)
    public bool IsPlayerCreated;
    public bool IsPlayerViewed;
    public DateTime? PlayerViewedDate;
    
    // === Email-Specific ===
    public EmailData EmailData;          // If file is an email
}

[System.Serializable]
public class EmailData
{
    public string From;
    public string To;
    public string Subject;
    public string Body;
    public DateTime SentDate;
    public bool IsRead;
    public List<string> AttachmentFileIds;
}

[System.Serializable]
public class SoftwareState
{
    public string SoftwareId;
    public string Version;
    public bool IsRunning;
    public List<string> ActiveVulnerabilityIds;  // Exploits that work on this software
}

[System.Serializable]
public class PortState
{
    public int PortNumber;
    public string ServiceName;
    public bool IsOpen;
    public bool IsPlayerKnown;
}
```

### NPC Save Data

```csharp
[System.Serializable]
public class NPCSaveData
{
    public const int CURRENT_VERSION = 1;
    public int SaveVersion = CURRENT_VERSION;
    
    // === All NPCs (FULL DETAIL) ===
    public List<NPCState> NPCStates;
    
    // === Relationships ===
    public List<NPCRelationshipState> Relationships;
    
    // === Conversation History (FULL DETAIL) ===
    public List<ConversationHistoryState> ConversationHistories;
}

[System.Serializable]
public class NPCState
{
    public string NpcId;
    public string Name;
    
    // === Location (EXACT POSITION) ===
    public string CurrentLocationId;
    public Vector3 Position;
    public Vector3 Rotation;
    
    // === Schedule ===
    public ScheduleState CurrentSchedule;
    public bool HasScheduleOverride;
    public ScheduleOverrideState ScheduleOverride;
    
    // === Owned Devices ===
    public List<string> OwnedDeviceIds;
    public string CurrentDeviceInHand;    // Phone/tablet they're currently using
    
    // === State ===
    public NPCActivity CurrentActivity;
    public bool IsAvailableForInteraction;
}

[System.Serializable]
public class ConversationHistoryState
{
    public string NpcId;
    public List<string> SeenDialogueNodes;
    public List<ConversationChoiceState> ChoiceHistory;
    public Dictionary<string, bool> ConversationFlags;
    public DateTime FirstInteraction;
    public DateTime LastInteraction;
    public int TotalInteractions;
}

[System.Serializable]
public class ConversationChoiceState
{
    public string NodeId;
    public int ChoiceIndex;
    public string ChoiceText;
    public DateTime Timestamp;
}

[System.Serializable]
public class NPCRelationshipState
{
    public string NpcId;
    public int TrustLevel;               // 0-100
    public int RomanceLevel;             // 0-100 (if romanceable)
    public RelationshipType Type;
    public List<string> Milestones;      // "high_trust", "romance_started", etc.
    public DateTime LastInteraction;
}

public enum RelationshipType
{
    Stranger,
    Acquaintance,
    Friend,
    CloseFriend,
    Rival,
    Enemy,
    RomanticInterest,
    Partner
}

public enum NPCActivity
{
    Idle,
    Working,
    Eating,
    Sleeping,
    Traveling,
    UsingDevice,
    InConversation
}
```

### World Save Data

```csharp
[System.Serializable]
public class WorldSaveData
{
    public const int CURRENT_VERSION = 1;
    public int SaveVersion = CURRENT_VERSION;
    
    // === Location States ===
    public List<LocationState> LocationStates;
    
    // === Dynamic World Events ===
    public List<WorldEventState> ActiveEvents;
    public List<string> CompletedEventIds;
    
    // === Environmental State ===
    public Dictionary<string, bool> GlobalFlags;
}

[System.Serializable]
public class LocationState
{
    public string LocationId;
    public bool IsDiscovered;
    public bool IsAccessible;
    public DateTime? FirstVisitDate;
    public int VisitCount;
}
```

---

## Autosave Strategy

### Two-Tier Autosave System

**Tier 1: Rolling Autosaves (Crash Recovery)**
- Saved every 5 minutes (background)
- Keeps last 3-5 saves
- Oldest is overwritten by newest
- File naming: `Autosave_Rolling_1.json`, `Autosave_Rolling_2.json`, etc.

**Tier 2: Named Event Autosaves (Milestone Recovery)**
- Saved on major events (quest complete, major hack, important dialogue)
- Kept permanently (or last N events, e.g., last 10)
- File naming: `Autosave_Event_{EventType}_{Timestamp}.json`
- Examples:
  - `Autosave_Event_Quest_Main_01_20251025_143022.json`
  - `Autosave_Event_Device_BigTech_Server_20251025_150315.json`
  - `Autosave_Event_Dialogue_Sarah_Trust_Milestone_20251025_162045.json`

### Implementation

```csharp
public class AutosaveScheduler : MonoBehaviour
{
    private ISaveSystem saveSystem;
    private ITimeManager timeManager;
    
    [Header("Configuration")]
    [SerializeField] private float rollingAutosaveInterval = 300f; // 5 minutes
    [SerializeField] private int maxRollingAutosaves = 5;
    [SerializeField] private int maxEventAutosaves = 10;
    
    private float timeSinceLastAutosave = 0f;
    private Queue<string> rollingAutosaveQueue = new Queue<string>();
    private Queue<string> eventAutosaveQueue = new Queue<string>();
    
    private void Update()
    {
        if (!saveSystem.IsAutosaveEnabled)
            return;
        
        timeSinceLastAutosave += Time.deltaTime;
        
        if (timeSinceLastAutosave >= rollingAutosaveInterval)
        {
            TriggerRollingAutosave();
            timeSinceLastAutosave = 0f;
        }
    }
    
    private void TriggerRollingAutosave()
    {
        // Generate rolling autosave name
        string saveName = $"Autosave_Rolling_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // Save game
        saveSystem.SaveGameAsync(saveName, SaveType.AutosaveRolling, (result) =>
        {
            if (result.Success)
            {
                // Add to queue
                rollingAutosaveQueue.Enqueue(saveName);
                
                // Remove oldest if over limit
                if (rollingAutosaveQueue.Count > maxRollingAutosaves)
                {
                    string oldestSave = rollingAutosaveQueue.Dequeue();
                    saveSystem.DeleteSave(oldestSave);
                }
                
                Debug.Log($"[AutosaveScheduler] Rolling autosave created: {saveName}");
            }
        });
    }
    
    public void TriggerEventAutosave(string eventType, string description)
    {
        // Generate event-based autosave name
        string saveName = $"Autosave_Event_{eventType}_{DateTime.Now:yyyyMMdd_HHmmss}";
        
        // Save game
        saveSystem.SaveGameAsync(saveName, SaveType.AutosaveEvent, (result) =>
        {
            if (result.Success)
            {
                // Add to queue
                eventAutosaveQueue.Enqueue(saveName);
                
                // Remove oldest if over limit
                if (eventAutosaveQueue.Count > maxEventAutosaves)
                {
                    string oldestSave = eventAutosaveQueue.Dequeue();
                    saveSystem.DeleteSave(oldestSave);
                }
                
                Debug.Log($"[AutosaveScheduler] Event autosave created: {saveName} ({description})");
            }
        });
    }
    
    // === Subscribe to game events ===
    
    private void SubscribeToEvents()
    {
        var questManager = ServiceLocator.Get<IQuestManager>();
        questManager.OnQuestCompleted += (quest) =>
        {
            TriggerEventAutosave($"Quest_{quest.QuestId}", $"Completed: {quest.QuestName}");
        };
        
        var deviceRegistry = ServiceLocator.Get<IDeviceRegistry>();
        GameEvents.Subscribe(GameEventType.DeviceCompromised, (data) =>
        {
            var device = data as Device;
            TriggerEventAutosave($"Device_{device.DeviceId}", $"Compromised: {device.Hostname}");
        });
        
        var dialogueService = ServiceLocator.Get<IDialogueService>();
        dialogueService.OnDialogueEnded += (npcId) =>
        {
            var npc = ServiceLocator.Get<INPCManager>().GetNPC(npcId);
            var relationship = ServiceLocator.Get<INPCManager>().GetRelationship(npcId);
            
            // Only autosave on important dialogues (milestone reached)
            if (relationship.Milestones.Count > 0)
            {
                TriggerEventAutosave($"Dialogue_{npcId}", $"Milestone with {npc.Name}");
            }
        });
    }
}
```

---

## Save System Implementation

### Core Save System

```csharp
public class SaveSystem : ISaveSystem
{
    private SaveFileManager fileManager;
    private AutosaveScheduler autosaveScheduler;
    private SaveDataMigrator migrator;
    
    private bool autosaveEnabled = true;
    private bool isSaving = false;
    private bool isLoading = false;
    
    // === Events ===
    public event Action<SaveType> OnSaveStarted;
    public event Action<SaveResult> OnSaveCompleted;
    public event Action<string> OnLoadStarted;
    public event Action<SaveGameData> OnLoadCompleted;
    public event Action<string> OnAutosaveTriggered;
    
    public bool IsAutosaveEnabled => autosaveEnabled;
    
    // === Initialization ===
    
    public void Initialize()
    {
        fileManager = new SaveFileManager();
        migrator = new SaveDataMigrator();
        
        // Find or create autosave scheduler
        var schedulerGO = new GameObject("AutosaveScheduler");
        GameObject.DontDestroyOnLoad(schedulerGO);
        autosaveScheduler = schedulerGO.AddComponent<AutosaveScheduler>();
        
        Debug.Log("[SaveSystem] Initialized");
    }
    
    // === Save Operations ===
    
    public void SaveGame(string saveName, SaveType saveType)
    {
        if (isSaving)
        {
            Debug.LogWarning("[SaveSystem] Save already in progress");
            return;
        }
        
        isSaving = true;
        OnSaveStarted?.Invoke(saveType);
        
        try
        {
            // Gather save data from all systems
            var saveData = GatherSaveData(saveName, saveType);
            
            // Calculate checksum
            saveData.Checksum = fileManager.CalculateChecksum(saveData);
            
            // Write to disk
            fileManager.WriteSaveFile(saveName, saveData);
            
            // Success
            var result = new SaveResult
            {
                Success = true,
                SaveName = saveName,
                SaveType = saveType,
                Timestamp = DateTime.Now,
                FileSizeBytes = fileManager.GetFileSizeBytes(saveName)
            };
            
            OnSaveCompleted?.Invoke(result);
            Debug.Log($"[SaveSystem] Game saved: {saveName} ({result.FileSizeBytes / 1024} KB)");
        }
        catch (Exception e)
        {
            var result = new SaveResult
            {
                Success = false,
                SaveName = saveName,
                SaveType = saveType,
                ErrorMessage = e.Message
            };
            
            OnSaveCompleted?.Invoke(result);
            Debug.LogError($"[SaveSystem] Save failed: {e.Message}");
        }
        finally
        {
            isSaving = false;
        }
    }
    
    public void SaveGameAsync(string saveName, SaveType saveType, Action<SaveResult> onComplete)
    {
        // Async version using Task
        Task.Run(() =>
        {
            SaveGame(saveName, saveType);
        }).ContinueWith((task) =>
        {
            // Callback on main thread
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                // Result already sent via OnSaveCompleted event
            });
        });
    }
    
    public bool CanSaveAtCurrentLocation()
    {
        var playerManager = ServiceLocator.Get<IPlayerManager>();
        var currentLocation = playerManager.GetCurrentLocation();
        
        // Check if location allows manual saves
        return currentLocation.AllowsManualSave;
    }
    
    // === Load Operations ===
    
    public SaveGameData LoadGame(string saveName)
    {
        if (isLoading)
        {
            Debug.LogWarning("[SaveSystem] Load already in progress");
            return null;
        }
        
        isLoading = true;
        OnLoadStarted?.Invoke(saveName);
        
        try
        {
            // Read from disk
            var saveData = fileManager.ReadSaveFile(saveName);
            
            // Validate checksum
            if (!fileManager.ValidateChecksum(saveData))
            {
                throw new Exception("Save file corrupted (checksum mismatch)");
            }
            
            // Migrate if needed
            if (saveData.SaveVersion < SaveGameData.CURRENT_VERSION)
            {
                saveData = migrator.Migrate(saveData);
            }
            
            // Restore game state from save data
            RestoreGameState(saveData);
            
            OnLoadCompleted?.Invoke(saveData);
            Debug.Log($"[SaveSystem] Game loaded: {saveName}");
            
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Load failed: {e.Message}");
            return null;
        }
        finally
        {
            isLoading = false;
        }
    }
    
    public void LoadGameAsync(string saveName, Action<SaveGameData> onComplete)
    {
        Task.Run(() =>
        {
            return LoadGame(saveName);
        }).ContinueWith((task) =>
        {
            UnityMainThreadDispatcher.Enqueue(() =>
            {
                onComplete?.Invoke(task.Result);
            });
        });
    }
    
    public List<SaveFileInfo> GetAvailableSaves()
    {
        return fileManager.GetAllSaveFiles();
    }
    
    // === Autosave Management ===
    
    public void EnableAutosave()
    {
        autosaveEnabled = true;
        Debug.Log("[SaveSystem] Autosave enabled");
    }
    
    public void DisableAutosave()
    {
        autosaveEnabled = false;
        Debug.Log("[SaveSystem] Autosave disabled");
    }
    
    public void TriggerEventBasedAutosave(string eventName, string description)
    {
        if (!autosaveEnabled)
            return;
        
        OnAutosaveTriggered?.Invoke(eventName);
        autosaveScheduler.TriggerEventAutosave(eventName, description);
    }
    
    public List<SaveFileInfo> GetAutosaveHistory()
    {
        var allSaves = fileManager.GetAllSaveFiles();
        return allSaves.Where(s => s.SaveType == SaveType.AutosaveRolling || 
                                   s.SaveType == SaveType.AutosaveEvent)
                      .OrderByDescending(s => s.SaveTimestamp)
                      .ToList();
    }
    
    // === Save File Management ===
    
    public bool DeleteSave(string saveName)
    {
        return fileManager.DeleteSaveFile(saveName);
    }
    
    public bool RenameSave(string oldName, string newName)
    {
        return fileManager.RenameSaveFile(oldName, newName);
    }
    
    public SaveFileInfo GetSaveInfo(string saveName)
    {
        return fileManager.GetSaveFileInfo(saveName);
    }
    
    public bool IsSaveCorrupted(string saveName)
    {
        try
        {
            var saveData = fileManager.ReadSaveFile(saveName);
            return !fileManager.ValidateChecksum(saveData);
        }
        catch
        {
            return true;
        }
    }
    
    // === Internal Methods ===
    
    private SaveGameData GatherSaveData(string saveName, SaveType saveType)
    {
        var saveData = new SaveGameData
        {
            SaveName = saveName,
            SaveType = saveType,
            SaveTimestamp = DateTime.Now,
            GameVersion = Application.version
        };
        
        // Gather data from all systems
        saveData.PlayerData = ServiceLocator.Get<IPlayerManager>().GetSaveData();
        saveData.TimeData = ServiceLocator.Get<ITimeManager>().GetSaveData();
        saveData.QuestData = ServiceLocator.Get<IQuestManager>().GetSaveData();
        saveData.LeadData = ServiceLocator.Get<ILeadManager>().GetSaveData();
        saveData.NPCData = ServiceLocator.Get<INPCManager>().GetSaveData();
        saveData.DeviceData = ServiceLocator.Get<IDeviceRegistry>().GetSaveData();
        saveData.WorldData = ServiceLocator.Get<IWorldStateManager>().GetSaveData();
        saveData.ProgressionData = ServiceLocator.Get<IProgressionCoordinator>().GetSaveData();
        
        // Calculate total play time
        saveData.TotalPlayTime = ServiceLocator.Get<IPlayTimeTracker>().GetTotalPlayTime();
        
        // Populate preview data
        saveData.PlayerName = saveData.PlayerData.PlayerName;
        saveData.CurrentLocation = saveData.PlayerData.CurrentLocationId;
        saveData.PlayerLevel = saveData.PlayerData.PlayerLevel;
        saveData.LastQuestCompleted = GetLastCompletedQuestName(saveData.QuestData);
        
        return saveData;
    }
    
    private void RestoreGameState(SaveGameData saveData)
    {
        // Restore state to all systems
        ServiceLocator.Get<IPlayerManager>().LoadSaveData(saveData.PlayerData);
        ServiceLocator.Get<ITimeManager>().LoadSaveData(saveData.TimeData);
        ServiceLocator.Get<IQuestManager>().LoadSaveData(saveData.QuestData);
        ServiceLocator.Get<ILeadManager>().LoadSaveData(saveData.LeadData);
        ServiceLocator.Get<INPCManager>().LoadSaveData(saveData.NPCData);
        ServiceLocator.Get<IDeviceRegistry>().LoadSaveData(saveData.DeviceData);
        ServiceLocator.Get<IWorldStateManager>().LoadSaveData(saveData.WorldData);
        ServiceLocator.Get<IProgressionCoordinator>().LoadSaveData(saveData.ProgressionData);
        
        Debug.Log("[SaveSystem] All systems restored from save data");
    }
    
    private string GetLastCompletedQuestName(QuestSaveData questData)
    {
        var completedQuests = questData.QuestStates
            .Where(q => q.State == QuestState.Completed)
            .OrderByDescending(q => q.CompletedAt);
        
        return completedQuests.Any() ? completedQuests.First().QuestName : "None";
    }
}
```

---

## Save File Manager

### File I/O and Format Handling

```csharp
public class SaveFileManager
{
    private const string SAVE_FOLDER = "Saves";
    private string saveFolderPath;
    
    public SaveFileManager()
    {
        // Development: Use persistent data path
        saveFolderPath = Path.Combine(Application.persistentDataPath, SAVE_FOLDER);
        
        // Create save folder if it doesn't exist
        if (!Directory.Exists(saveFolderPath))
        {
            Directory.CreateDirectory(saveFolderPath);
        }
    }
    
    // === Write Operations ===
    
    public void WriteSaveFile(string saveName, SaveGameData saveData)
    {
        string filePath = GetSaveFilePath(saveName);
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Development: Use JSON
        string json = JsonUtility.ToJson(saveData, prettyPrint: true);
        File.WriteAllText(filePath, json);
        #else
        // Production: Use binary with compression
        byte[] jsonBytes = Encoding.UTF8.GetBytes(JsonUtility.ToJson(saveData));
        byte[] compressedBytes = Compress(jsonBytes);
        File.WriteAllBytes(filePath, compressedBytes);
        #endif
    }
    
    // === Read Operations ===
    
    public SaveGameData ReadSaveFile(string saveName)
    {
        string filePath = GetSaveFilePath(saveName);
        
        if (!File.Exists(filePath))
        {
            throw new FileNotFoundException($"Save file not found: {saveName}");
        }
        
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        // Development: Read JSON
        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<SaveGameData>(json);
        #else
        // Production: Read binary
        byte[] compressedBytes = File.ReadAllBytes(filePath);
        byte[] jsonBytes = Decompress(compressedBytes);
        string json = Encoding.UTF8.GetString(jsonBytes);
        return JsonUtility.FromJson<SaveGameData>(json);
        #endif
    }
    
    // === File Management ===
    
    public List<SaveFileInfo> GetAllSaveFiles()
    {
        var saveFiles = new List<SaveFileInfo>();
        
        if (!Directory.Exists(saveFolderPath))
            return saveFiles;
        
        string[] files = Directory.GetFiles(saveFolderPath, "*.json") // Dev
                                  .Concat(Directory.GetFiles(saveFolderPath, "*.sav")) // Production
                                  .ToArray();
        
        foreach (string filePath in files)
        {
            try
            {
                var saveData = ReadSaveFile(Path.GetFileNameWithoutExtension(filePath));
                saveFiles.Add(ConvertToSaveFileInfo(saveData, filePath));
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[SaveFileManager] Failed to read save file: {filePath}\n{e.Message}");
            }
        }
        
        return saveFiles.OrderByDescending(s => s.SaveTimestamp).ToList();
    }
    
    public SaveFileInfo GetSaveFileInfo(string saveName)
    {
        var saveData = ReadSaveFile(saveName);
        return ConvertToSaveFileInfo(saveData, GetSaveFilePath(saveName));
    }
    
    public bool DeleteSaveFile(string saveName)
    {
        string filePath = GetSaveFilePath(saveName);
        
        if (File.Exists(filePath))
        {
            File.Delete(filePath);
            return true;
        }
        
        return false;
    }
    
    public bool RenameSaveFile(string oldName, string newName)
    {
        string oldPath = GetSaveFilePath(oldName);
        string newPath = GetSaveFilePath(newName);
        
        if (File.Exists(oldPath))
        {
            File.Move(oldPath, newPath);
            return true;
        }
        
        return false;
    }
    
    public long GetFileSizeBytes(string saveName)
    {
        string filePath = GetSaveFilePath(saveName);
        return new FileInfo(filePath).Length;
    }
    
    // === Checksum Operations ===
    
    public string CalculateChecksum(SaveGameData saveData)
    {
        // Temporarily clear checksum field
        string originalChecksum = saveData.Checksum;
        saveData.Checksum = "";
        
        // Serialize
        string json = JsonUtility.ToJson(saveData);
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        
        // Calculate SHA256
        using (var sha256 = System.Security.Cryptography.SHA256.Create())
        {
            byte[] hashBytes = sha256.ComputeHash(bytes);
            string checksum = BitConverter.ToString(hashBytes).Replace("-", "");
            
            // Restore original checksum
            saveData.Checksum = originalChecksum;
            
            return checksum;
        }
    }
    
    public bool ValidateChecksum(SaveGameData saveData)
    {
        string savedChecksum = saveData.Checksum;
        string calculatedChecksum = CalculateChecksum(saveData);
        
        return savedChecksum == calculatedChecksum;
    }
    
    // === Helper Methods ===
    
    private string GetSaveFilePath(string saveName)
    {
        #if UNITY_EDITOR || DEVELOPMENT_BUILD
        return Path.Combine(saveFolderPath, $"{saveName}.json");
        #else
        return Path.Combine(saveFolderPath, $"{saveName}.sav");
        #endif
    }
    
    private SaveFileInfo ConvertToSaveFileInfo(SaveGameData saveData, string filePath)
    {
        return new SaveFileInfo
        {
            SaveName = saveData.SaveName,
            DisplayName = GetDisplayName(saveData),
            SaveType = saveData.SaveType,
            SaveTimestamp = saveData.SaveTimestamp,
            PlayTime = saveData.TotalPlayTime,
            SaveVersion = saveData.SaveVersion,
            FileSizeBytes = new FileInfo(filePath).Length,
            IsCorrupted = !ValidateChecksum(saveData),
            PlayerName = saveData.PlayerName,
            CurrentLocation = saveData.CurrentLocation,
            GameTime = saveData.TimeData.currentTime,
            PlayerLevel = saveData.PlayerLevel,
            LastQuestCompleted = saveData.LastQuestCompleted
        };
    }
    
    private string GetDisplayName(SaveGameData saveData)
    {
        switch (saveData.SaveType)
        {
            case SaveType.Manual:
                return $"{saveData.PlayerName} - {saveData.CurrentLocation}";
            
            case SaveType.AutosaveRolling:
                return $"Autosave ({saveData.SaveTimestamp:HH:mm})";
            
            case SaveType.AutosaveEvent:
                return $"Event: {ExtractEventType(saveData.SaveName)}";
            
            default:
                return saveData.SaveName;
        }
    }
    
    private string ExtractEventType(string saveName)
    {
        // Extract event type from "Autosave_Event_{EventType}_{Timestamp}.json"
        var parts = saveName.Split('_');
        if (parts.Length >= 3)
        {
            return parts[2].Replace("_", " ");
        }
        return "Unknown";
    }
    
    // === Compression ===
    
    private byte[] Compress(byte[] data)
    {
        using (var outputStream = new MemoryStream())
        {
            using (var gzipStream = new System.IO.Compression.GZipStream(outputStream, 
                System.IO.Compression.CompressionMode.Compress))
            {
                gzipStream.Write(data, 0, data.Length);
            }
            return outputStream.ToArray();
        }
    }
    
    private byte[] Decompress(byte[] data)
    {
        using (var inputStream = new MemoryStream(data))
        using (var gzipStream = new System.IO.Compression.GZipStream(inputStream, 
            System.IO.Compression.CompressionMode.Decompress))
        using (var outputStream = new MemoryStream())
        {
            gzipStream.CopyTo(outputStream);
            return outputStream.ToArray();
        }
    }
}
```

---

## Save Data Migration

### Version Migration System

```csharp
public class SaveDataMigrator
{
    public SaveGameData Migrate(SaveGameData saveData)
    {
        int currentVersion = saveData.SaveVersion;
        int targetVersion = SaveGameData.CURRENT_VERSION;
        
        Debug.Log($"[SaveDataMigrator] Migrating save from v{currentVersion} to v{targetVersion}");
        
        // Apply migrations sequentially
        for (int version = currentVersion; version < targetVersion; version++)
        {
            saveData = MigrateToNextVersion(saveData, version);
        }
        
        return saveData;
    }
    
    private SaveGameData MigrateToNextVersion(SaveGameData saveData, int fromVersion)
    {
        int toVersion = fromVersion + 1;
        Debug.Log($"[SaveDataMigrator] Migrating from v{fromVersion} to v{toVersion}");
        
        switch (toVersion)
        {
            case 1:
                // Initial version, no migration needed
                break;
            
            case 2:
                // Example: v1 → v2 migration
                // Added FactionReputation system
                if (saveData.PlayerData.FactionReputation == null)
                {
                    saveData.PlayerData.FactionReputation = new Dictionary<string, int>();
                    Debug.Log("[SaveDataMigrator] Added FactionReputation (default: empty)");
                }
                break;
            
            case 3:
                // Example: v2 → v3 migration
                // Added HeatLevels system
                if (saveData.PlayerData.HeatLevels == null)
                {
                    saveData.PlayerData.HeatLevels = new Dictionary<string, float>();
                    Debug.Log("[SaveDataMigrator] Added HeatLevels (default: empty)");
                }
                break;
            
            // Add more migrations as needed
        }
        
        saveData.SaveVersion = toVersion;
        return saveData;
    }
    
    // === Per-System Migrators ===
    
    public PlayerSaveData MigratePlayerData(PlayerSaveData oldData)
    {
        if (oldData.SaveVersion == PlayerSaveData.CURRENT_VERSION)
            return oldData;
        
        // Apply player-specific migrations
        var newData = oldData;
        
        // Example migrations
        if (oldData.SaveVersion < 1)
        {
            // v0 → v1: Added Karma system
            newData.Karma = 0;
        }
        
        newData.SaveVersion = PlayerSaveData.CURRENT_VERSION;
        return newData;
    }
    
    // Similar methods for other save data types...
}
```

---

## UI Integration

### Save/Load Menu

```csharp
public class SaveLoadMenuUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform saveListContainer;
    [SerializeField] private SaveFileSlotUI saveSlotPrefab;
    [SerializeField] private Button saveBut ton;
    [SerializeField] private Button loadButton;
    [SerializeField] private Button deleteButton;
    
    private ISaveSystem saveSystem;
    private List<SaveFileInfo> availableSaves;
    private SaveFileInfo selectedSave;
    
    private void Start()
    {
        saveSystem = ServiceLocator.Get<ISaveSystem>();
        
        RefreshSaveList();
        
        saveButton.onClick.AddListener(OnSaveClicked);
        loadButton.onClick.AddListener(OnLoadClicked);
        deleteButton.onClick.AddListener(OnDeleteClicked);
    }
    
    private void RefreshSaveList()
    {
        // Clear existing slots
        foreach (Transform child in saveListContainer)
        {
            Destroy(child.gameObject);
        }
        
        // Get all save files
        availableSaves = saveSystem.GetAvailableSaves();
        
        // Create UI slots
        foreach (var saveInfo in availableSaves)
        {
            var slot = Instantiate(saveSlotPrefab, saveListContainer);
            slot.Initialize(saveInfo, OnSaveSlotSelected);
        }
    }
    
    private void OnSaveSlotSelected(SaveFileInfo saveInfo)
    {
        selectedSave = saveInfo;
        loadButton.interactable = true;
        deleteButton.interactable = true;
    }
    
    private void OnSaveClicked()
    {
        // Check if player can save at current location
        if (!saveSystem.CanSaveAtCurrentLocation())
        {
            ShowMessage("You can only save at safe locations (your apartment, safe houses)");
            return;
        }
        
        // Prompt for save name
        ShowSaveNameDialog((saveName) =>
        {
            saveSystem.SaveGame(saveName, SaveType.Manual);
            RefreshSaveList();
        });
    }
    
    private void OnLoadClicked()
    {
        if (selectedSave == null)
            return;
        
        // Confirm load (will lose unsaved progress)
        ShowConfirmDialog("Load this save? Unsaved progress will be lost.", () =>
        {
            saveSystem.LoadGame(selectedSave.SaveName);
            CloseMenu();
        });
    }
    
    private void OnDeleteClicked()
    {
        if (selectedSave == null)
            return;
        
        // Confirm delete
        ShowConfirmDialog($"Delete save '{selectedSave.DisplayName}'?", () =>
        {
            saveSystem.DeleteSave(selectedSave.SaveName);
            RefreshSaveList();
        });
    }
}
```

### Save File Slot UI

```csharp
public class SaveFileSlotUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI displayNameText;
    [SerializeField] private TextMeshProUGUI timestampText;
    [SerializeField] private TextMeshProUGUI playTimeText;
    [SerializeField] private TextMeshProUGUI levelText;
    [SerializeField] private TextMeshProUGUI locationText;
    [SerializeField] private Image corruptedIndicator;
    [SerializeField] private Image saveTypeIcon;
    
    private SaveFileInfo saveInfo;
    private Action<SaveFileInfo> onSelected;
    
    public void Initialize(SaveFileInfo info, Action<SaveFileInfo> onSelectedCallback)
    {
        saveInfo = info;
        onSelected = onSelectedCallback;
        
        // Populate UI
        displayNameText.text = info.DisplayName;
        timestampText.text = info.SaveTimestamp.ToString("yyyy-MM-dd HH:mm");
        playTimeText.text = FormatPlayTime(info.PlayTime);
        levelText.text = $"Level {info.PlayerLevel}";
        locationText.text = info.CurrentLocation;
        
        // Show corrupted indicator
        corruptedIndicator.gameObject.SetActive(info.IsCorrupted);
        
        // Set save type icon
        saveTypeIcon.sprite = GetSaveTypeIcon(info.SaveType);
    }
    
    public void OnClicked()
    {
        onSelected?.Invoke(saveInfo);
    }
    
    private string FormatPlayTime(TimeSpan playTime)
    {
        if (playTime.TotalHours >= 1)
            return $"{(int)playTime.TotalHours}h {playTime.Minutes}m";
        else
            return $"{playTime.Minutes}m";
    }
    
    private Sprite GetSaveTypeIcon(SaveType saveType)
    {
        // Load appropriate icon from Resources
        switch (saveType)
        {
            case SaveType.Manual:
                return Resources.Load<Sprite>("Icons/SaveManual");
            case SaveType.AutosaveRolling:
                return Resources.Load<Sprite>("Icons/SaveAutosave");
            case SaveType.AutosaveEvent:
                return Resources.Load<Sprite>("Icons/SaveEvent");
            default:
                return null;
        }
    }
}
```

---

## Performance Considerations

### Save Time Optimization

**Problem:** Saving full world state can be slow

**Solutions:**

1. **Async Saving**: Use async/await to prevent frame drops
2. **Incremental Serialization**: Serialize each system separately, then combine
3. **Dirty Tracking**: Only save systems that have changed (future optimization)

```csharp
public class OptimizedSaveSystem : ISaveSystem
{
    public async Task SaveGameAsync(string saveName, SaveType saveType)
    {
        // Serialize systems in parallel
        var tasks = new List<Task<object>>
        {
            Task.Run(() => (object)ServiceLocator.Get<IPlayerManager>().GetSaveData()),
            Task.Run(() => (object)ServiceLocator.Get<IQuestManager>().GetSaveData()),
            Task.Run(() => (object)ServiceLocator.Get<ILeadManager>().GetSaveData()),
            Task.Run(() => (object)ServiceLocator.Get<INPCManager>().GetSaveData()),
            Task.Run(() => (object)ServiceLocator.Get<IDeviceRegistry>().GetSaveData())
        };
        
        await Task.WhenAll(tasks);
        
        // Combine results
        var saveData = new SaveGameData
        {
            PlayerData = (PlayerSaveData)tasks[0].Result,
            QuestData = (QuestSaveData)tasks[1].Result,
            LeadData = (LeadSaveData)tasks[2].Result,
            NPCData = (NPCSaveData)tasks[3].Result,
            DeviceData = (DeviceSaveData)tasks[4].Result
        };
        
        // Write to disk (off main thread)
        await Task.Run(() => fileManager.WriteSaveFile(saveName, saveData));
    }
}
```

### File Size Optimization

**Full detail saves can get large. Optimizations:**

1. **Compression**: GZip reduces JSON files by ~70%
2. **Binary Format**: Production builds use binary (smaller than JSON)
3. **Selective Detail**: Only save viewed emails/files, not all content
4. **Delta Compression**: Store only changes from baseline (future)

**Estimated Save File Sizes:**

- **JSON (uncompressed)**: ~5-10 MB
- **JSON (compressed)**: ~1.5-3 MB
- **Binary (compressed)**: ~1-2 MB

---

## Troubleshooting

### Issue: Save File Corrupted

**Symptom:** Load fails with checksum mismatch

**Causes:**
- Game crashed during save
- Disk full during write
- File manually edited

**Solution:**

```csharp
public SaveGameData LoadGameWithRecovery(string saveName)
{
    try
    {
        return LoadGame(saveName);
    }
    catch (Exception e)
    {
        Debug.LogError($"Primary save corrupted: {e.Message}");
        
        // Try autosave history
        var autosaves = GetAutosaveHistory();
        foreach (var autosave in autosaves)
        {
            try
            {
                Debug.Log($"Attempting recovery from: {autosave.SaveName}");
                return LoadGame(autosave.SaveName);
            }
            catch
            {
                continue;
            }
        }
        
        throw new Exception("All save files corrupted, cannot recover");
    }
}
```

### Issue: Save Takes Too Long

**Symptom:** Frame drops during autosave

**Solution:** Use async saving with progress indicator

```csharp
public async void SaveGameWithProgress(string saveName)
{
    // Show progress UI
    var progressUI = UIManager.ShowProgressBar("Saving game...");
    
    progressUI.SetProgress(0.1f, "Gathering data...");
    var saveData = await Task.Run(() => GatherSaveData(saveName, SaveType.Manual));
    
    progressUI.SetProgress(0.5f, "Writing to disk...");
    await Task.Run(() => fileManager.WriteSaveFile(saveName, saveData));
    
    progressUI.SetProgress(1.0f, "Save complete!");
    await Task.Delay(500);
    
    progressUI.Close();
}
```

### Issue: Migration Failed

**Symptom:** Old save won't load after update

**Solution:** Add fallback values for all new fields

```csharp
public PlayerSaveData MigratePlayerData(PlayerSaveData oldData)
{
    // Always provide defaults for new fields
    if (oldData.HeatLevels == null)
    {
        oldData.HeatLevels = new Dictionary<string, float>
        {
            { "LocalPD", 0f },
            { "FBI", 0f },
            { "NSA", 0f }
        };
    }
    
    return oldData;
}
```

---

## Future Features

### Steam Cloud Integration

```csharp
public class SteamCloudSaveSystem : ISaveSystem
{
    public void SyncToCloud(string saveName)
    {
        // Read local save file
        var saveData = fileManager.ReadSaveFile(saveName);
        
        // Upload to Steam Cloud
        if (SteamRemoteStorage.FileWrite(saveName, saveData))
        {
            Debug.Log($"[SteamCloud] Uploaded save: {saveName}");
        }
    }
    
    public void SyncFromCloud(string saveName)
    {
        // Download from Steam Cloud
        byte[] data = SteamRemoteStorage.FileRead(saveName);
        
        // Write to local storage
        fileManager.WriteSaveFile(saveName, data);
        
        Debug.Log($"[SteamCloud] Downloaded save: {saveName}");
    }
}
```

### Save Slots (Multiple Characters)

```csharp
public class MultiSlotSaveSystem : ISaveSystem
{
    private const int MAX_SAVE_SLOTS = 3;
    
    public List<SaveSlotInfo> GetSaveSlots()
    {
        var slots = new List<SaveSlotInfo>();
        
        for (int i = 0; i < MAX_SAVE_SLOTS; i++)
        {
            string slotName = $"Slot_{i}";
            var slotInfo = new SaveSlotInfo
            {
                SlotIndex = i,
                IsOccupied = fileManager.SaveFileExists(slotName),
                SaveInfo = fileManager.GetSaveFileInfo(slotName)
            };
            slots.Add(slotInfo);
        }
        
        return slots;
    }
}
```

---

## Quick Start Guide

### 1. Initialize Save System

```csharp
// In game initialization (ServiceLocator setup):
var saveSystem = new SaveSystem();
saveSystem.Initialize();
ServiceLocator.Register<ISaveSystem>(saveSystem);
```

### 2. Implement ISaveable in Your Systems

```csharp
public class MyGameSystem : IGameService, ISaveable
{
    public MySaveData GetSaveData()
    {
        return new MySaveData
        {
            // ... populate save data
        };
    }
    
    public void LoadSaveData(MySaveData saveData)
    {
        // ... restore state from save data
    }
}
```

### 3. Trigger Manual Save

```csharp
// In save menu:
if (saveSystem.CanSaveAtCurrentLocation())
{
    saveSystem.SaveGame("MyCharacter_Save", SaveType.Manual);
}
else
{
    ShowMessage("Cannot save here. Find a safe location.");
}
```

### 4. Load Game

```csharp
// In load menu:
var saveData = saveSystem.LoadGame("MyCharacter_Save");
```

### 5. Check Autosave History

```csharp
// Show autosave list:
var autosaves = saveSystem.GetAutosaveHistory();
foreach (var autosave in autosaves)
{
    Debug.Log($"{autosave.DisplayName} - {autosave.SaveTimestamp}");
}
```

---

## Summary

The Save System provides:

- ✅ **Soft save scumming prevention** via limited manual saves
- ✅ **Dual autosave tiers** (rolling + named event saves)
- ✅ **Full world state detail** (files, emails, NPC positions)
- ✅ **Forward migration** with version numbers
- ✅ **Corruption protection** via checksums
- ✅ **Development-friendly** JSON format
- ✅ **Production-optimized** binary + compression
- ✅ **Async operations** to prevent frame drops
- ✅ **Steam Cloud ready** (future feature)
