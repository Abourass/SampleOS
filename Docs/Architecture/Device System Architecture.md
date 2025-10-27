# Device System Architecture

## Overview

The Device System is the foundation of the game's hacking mechanics and world simulation. Devices represent any networked entity that the player can discover, interact with, hack, or upgrade—from ATMs and POS terminals to servers, routers, and NPC smartphones. The architecture balances **rich device simulation** (file systems, software, security) with **performance** (lazy loading, smart indexing) to support 1000+ devices across multiple cities.

**Key Design Principles:**

- **Hybrid ownership model**: Devices store ownership, Player/NPCs track access methods
- **Lazy loading**: File systems load only when accessed, unload when idle
- **Story + procedural devices**: ~40% story-critical (pre-registered), ~60% procedural (generated on discovery)
- **Multiple access types**: Credentials, backdoors, physical access, authorized access
- **Event-driven coordination**: Device state changes cascade through game systems via ProgressionCoordinator

---

## Design Philosophy

### What is a Device?

In the game world, a **device** is any networked entity with:

- **Identity**: Hostname, IP address, device type (server, phone, ATM, etc.)
- **Network presence**: Connected to one or more networks, discoverable via scanning
- **Software stack**: Operating system, installed software (interactive apps + background services)
- **File system**: Virtual directory structure with emails, logs, scripts, credentials
- **Security profile**: Security level, credentials, vulnerabilities, detection state
- **Physical manifestation** (optional): Location in the game world, can be interacted with directly

Devices serve multiple gameplay purposes:

1. **Hacking targets**: Player exploits vulnerabilities to gain access
2. **Information sources**: Emails, files, logs reveal quest leads and story details
3. **Upgrade opportunities**: Hack workplace devices to make job minigames easier
4. **NPC characterization**: Sarah's messy desktop files tell us about her personality
5. **World building**: Network topology reflects organizational structure

### Devices vs NPCs vs Locations

It's important to understand how devices relate to other game entities:

| Entity | Purpose | Ownership | Examples |
|--------|---------|-----------|----------|
| **Device** | Networked system with hackable software | Owned by NPCs, organizations, or player | Sarah's phone, CoffeeShop POS, TechCorp server |
| **NPC** | Character with schedule, relationships, dialogue | References owned devices (by ID) | Sarah owns "phone_sarah" and "laptop_sarah" |
| **Location** | Physical place with devices and NPCs | Contains devices at specific positions | CoffeeShop (has POS, router, customers' phones) |

**Coordination Example:**
- Sarah (NPC) has `OwnedDeviceIds = ["phone_sarah", "laptop_sarah"]`
- Sarah's schedule says: 9:00 AM - 5:00 PM → TechCorp office
- When Sarah moves to TechCorp, her devices move too (`Device.MoveToLocation()`)
- This triggers network topology update (devices join TechCorp network)
- Player scanning TechCorp network now discovers Sarah's laptop

---

## Core Architecture

### Component Hierarchy

```
DeviceRegistry (IDeviceRegistry) - Service
  ├── Stores all device instances (authoritative source)
  ├── Indexes devices by: ID, hostname, IP, location, network, owner
  ├── Fires events on device registration/movement
  └── Integrates with ProgressionCoordinator for cascading updates

Device (Abstract Base Class)
  ├── Stores device properties (identity, security, ownership)
  ├── Lazy-loads file system on first access
  ├── Tracks compromise state and backdoor connections
  └── Manages credentials and installed software

DeviceFactory (Static Factory)
  ├── Creates devices from definitions (story devices)
  ├── Generates procedural devices from templates (filler devices)
  └── Sets initial ownership, software, file systems

NetworkService (INetworkService)
  ├── Queries devices by network
  ├── Updates network topology when devices move
  └── Manages inter-network connections

Related Systems:
  ├── NPCScheduler → Moves NPC-owned devices based on schedules
  ├── HackingService → Grants player access to devices
  ├── LeadManager → Generates leads from device discoveries
  ├── QuestManager → Tracks device-based quest objectives
  └── ProgressionCoordinator → Orchestrates device event cascades
```

---

## Device Lifecycle

### Phase 1: World Initialization (Story Devices)

When the game starts, **story-critical devices** are pre-registered. These are devices that:

- Are owned by key NPCs (Sarah's phone, Marcus's laptop)
- Are quest-related (CoffeeShop POS for tutorial hack)
- Have scripted content (emails, logs with story clues)
- Are referenced in dialogue or quest objectives

**Process:**

1. `WorldInitializer` loads device definitions from ScriptableObjects or JSON
2. `DeviceFactory` creates device instances with ownership set (`device.OwnerId = "sarah"`)
3. `DeviceRegistry` registers devices and builds indexes (by owner, location, network)
4. `NetworkService` adds devices to their initial networks
5. NPCs reference owned devices: `sarah.OwnedDeviceIds = ["phone_sarah", "laptop_sarah"]`

**Example Story Device Definition:**

```csharp
// ScriptableObject: Devices/Story/SarahPhone.asset
{
  "DeviceId": "phone_sarah",
  "Hostname": "sarah-iphone",
  "DeviceTypeId": "smartphone",
  "OwnerId": "sarah",
  "SecurityLevel": "Medium",
  "Location": { "CityId": "SmallTown", "BuildingId": "Cafe", "Position": "(5, 0, 3)" },
  "DefaultCredential": { "Username": "sarah", "Password": "summer2024!" },
  "InitialFileSystem": "FileSystems/SarahPhone.json",
  "SoftwareOverrides": ["ios", "messages", "email", "photos"]
}
```

**Memory Impact:** ~100 story devices × 1KB metadata = **100KB** (negligible)

---

### Phase 2: Discovery & Procedural Generation (Filler Devices)

As the player explores networks, **filler devices** are generated on-demand. These provide:

- Network density (realistic 20-50 devices per corporate network)
- Hacking practice targets (varying difficulty levels)
- Cover for story devices (harder to identify critical targets)
- Emergent gameplay (player finds unexpected valuable data)

**Process:**

1. Player runs network scan on CoffeeShop network
2. `NetworkScanService` finds existing story devices (POS terminal, router)
3. `NetworkScanService` generates filler devices based on network type:
   - CoffeeShop (Residential) → 5-10 devices (customer phones, tablets, IoT)
   - TechCorp (Corporate) → 30-50 devices (workstations, servers, printers)
4. `DeviceFactory.CreateFromTemplate()` generates devices with:
   - Procedural hostnames ("workstation-47", "print-server-3")
   - Network-appropriate IPs (192.168.1.x for small business)
   - Organization ownership (`device.OwnerId = "TechCorp"`)
   - Randomized security levels (weighted by network difficulty)
5. Devices registered in `DeviceRegistry` and added to network

**Template Example:**

```csharp
// Template: Generic corporate workstation
{
  "TemplateId": "corp_workstation",
  "DeviceTypeId": "desktop",
  "SecurityLevelRange": "Medium to High",
  "OwnershipType": "Organization", // Owned by network's organization
  "SoftwareCategories": ["office_suite", "email_client", "web_browser"],
  "FileSystemTemplate": "Generic/OfficeWorkstation",
  "CredentialStrategy": "WeakPassword" // 30% chance of weak password
}
```

**Memory Impact:** ~600 filler devices × 1KB metadata = **600KB** (acceptable, file systems load lazily)

---

### Phase 3: Runtime State Changes

Throughout gameplay, devices undergo state changes:

#### **Device Movement**

NPCs move between locations 6-8 times per day, carrying mobile devices:

1. `NPCScheduler` detects schedule change: Sarah moves from Home → TechCorp
2. `NPCScheduler` calls `device.MoveToLocation(techCorpOffice)` for each owned device
3. `Device.MoveToLocation()` updates location and triggers `GameEventType.DeviceLocationChanged`
4. `DeviceRegistry` receives event, rebuilds location indexes
5. `NetworkService` updates network topology (devices leave Home network, join TechCorp network)
6. `ProgressionCoordinator` checks if device movement triggers quest objectives

**Performance:** Event-driven, O(1) updates. Tested with 100 NPCs moving simultaneously: <1ms per frame.

#### **Device Compromise**

Player hacks a device, gaining access:

1. Player executes exploit against CoffeeShop POS terminal
2. `HackingService.ExecuteExploit()` determines access type (credential vs backdoor)
3. Device state updates: `device.IsCompromised = true`, `device.BackdoorConnections.Add()`
4. Player state updates: `player.CompromisedDeviceIds.Add()`, `player.DeviceAccessMethods[deviceId] = Backdoor`
5. `GameEventType.DeviceCompromised` triggered
6. `ProgressionCoordinator.CoordinateDeviceCompromise()` runs:
   - Update related leads (mark "Hack CoffeeShop POS" as complete)
   - Check quest objectives (did this complete a quest?)
   - Update karma (black hat or white hat hack?)
   - Roll for detection (did the hack trigger alarms?)

**Cascading Effects:** Quest completion → new quest unlocked → new leads generated → NPC schedule override (security guard investigates)

#### **Device Destruction** (Story Events Only)

Rare, but possible in scripted story moments:

1. Player hacks power station, shuts down power to a building
2. All devices at that location go offline: `device.IsOnline = false`
3. Network topology updates (devices temporarily unreachable)
4. Quest objective: "Restore power to regain access"
5. Devices come back online with rotated credentials (security response)

---

## Device Ownership Model (Hybrid Approach)

The ownership model answers: **"Who owns this device?"** and **"How can the player access it?"**

### Ownership (Device Property)

Devices store **primary ownership** to represent the game world's reality:

```csharp
public class Device
{
    public string OwnerId { get; set; }              // "sarah", "TechCorp", "player", null
    public List<string> AuthorizedUserIds { get; set; } // ["sarah", "marcus"] for shared devices
}
```

**Ownership Types:**

| Owner Type | Example | Meaning |
|------------|---------|---------|
| **Player** | `device.OwnerId = "player"` | Player's personal devices (phone, laptop, PC) |
| **NPC** | `device.OwnerId = "sarah"` | NPC-owned devices (Sarah's phone, laptop) |
| **Organization** | `device.OwnerId = "TechCorp"` | Company-owned devices (servers, workstations) |
| **Public** | `device.OwnerId = null` | Public devices (library computers, ATMs) |
| **Shared** | `device.AuthorizedUserIds = ["sarah", "alex"]` | Shared devices (conference room tablet) |

**Why Store on Device?**

- Single source of truth (no sync issues)
- Fast queries: `deviceRegistry.GetDevicesOwnedBy("sarah")` (indexed)
- Natural for world simulation (devices belong to someone)
- Easy to serialize (saves with device state)

### Access Methods (Player Progression)

Players track **how they can access each device** to represent hacking progression:

```csharp
public class Player
{
    public List<string> OwnedDeviceIds;              // Legitimately owned
    public List<string> CompromisedDeviceIds;        // Hacked devices
    public Dictionary<string, DeviceAccessType> DeviceAccessMethods; // How to access each
}

public enum DeviceAccessType
{
    Owner,          // Full access, no detection risk (player's own devices)
    Backdoor,       // Persistent remote access (installed rootkit/backdoor)
    Credentials,    // Stolen credentials (must re-authenticate)
    Physical,       // Physical access only (must be at device location)
    Authorized      // Legitimately authorized (work computer)
}
```

**Access Type Progression:**

```
Player scans network → Discovers device
  ↓
Executes weak exploit (phishing) → Gains Credentials
  ↓
Logs in with credentials → Downloads files, reads emails
  ↓
Executes strong exploit (rootkit) → Installs Backdoor
  ↓
Instant remote access from anywhere → No location requirement
  ↓
Detection system catches backdoor → Backdoor removed
  ↓
Player must re-hack or use physical access
```

**Why Separate Ownership from Access?**

- **Ownership** = world state (who it belongs to)
- **Access** = player progression (what you've hacked)
- A device can be owned by Sarah, but accessed by player via backdoor
- Player can lose access (detection), but ownership never changes
- Supports complex scenarios: "Hack your boss's laptop to delete job applications"

---

## Device Discovery System

### Story Devices: Pre-Registered

**When:** Game initialization  
**Why:** Required for quests, dialogue, NPC schedules  
**Count:** ~100-150 devices (40% of total)

Story devices are defined in ScriptableObjects or JSON configs, loaded at startup, and registered immediately. They have:

- Explicit ownership (tied to NPCs or organizations)
- Scripted file systems (emails with quest clues, logs with passwords)
- Guaranteed availability (must exist for story to work)

**Example:** Sarah's phone must exist because:
- Quest objective: "Read Sarah's text messages to find out about Phoenix"
- Dialogue reference: Sarah mentions "lost my phone at the cafe"
- Lead generation: Finding her phone creates lead "Mysterious hacker group: Phoenix"

### Filler Devices: Generated on Discovery

**When:** Player scans network for first time  
**Why:** Performance (don't create 1000 devices at startup)  
**Count:** ~250-300 devices (60% of total)

Filler devices are generated procedurally when player discovers a network:

1. **Network Scan Initiated:** Player scans TechCorp internal network
2. **Check Existing Devices:** Find story devices already on network (exec's laptop, secure server)
3. **Determine Filler Count:** Based on network type:
   - Residential: 5-10 devices
   - Small Business: 10-20 devices
   - Corporate: 30-50 devices
   - Government: 50-100 devices
4. **Generate Device Templates:** Select templates based on network (workstations, printers, IoT)
5. **Create Device Instances:** `DeviceFactory.CreateFromTemplate()`
6. **Register Devices:** Add to `DeviceRegistry` and network topology
7. **Return Results:** Player sees list of discovered devices

**Caching:** Once generated, filler devices persist for the playthrough (saved in save file).

**Gameplay Impact:**

- **Realism:** Corporate networks feel populated (not just 3 story devices)
- **Difficulty:** Harder to identify which devices are story-critical
- **Exploration:** Player might find unexpected valuable data on filler devices
- **Performance:** Lazy generation spreads load across gameplay (not all at startup)

---

## File System Architecture

### Lazy Loading Strategy

File systems are **expensive** (100KB per device with full email logs, files, directories). Loading 1000 file systems at startup = **100MB** wasted memory.

**Solution:** Lazy loading with dirty tracking.

```csharp
public class Device
{
    private VirtualFileSystem _fileSystem;
    private bool _fileSystemLoaded = false;
    public bool HasFileSystemChanges { get; private set; }
    
    public VirtualFileSystem FileSystem
    {
        get
        {
            if (!_fileSystemLoaded)
            {
                // Load file system from template or saved state
                _fileSystem = FileSystemFactory.CreateForDevice(DeviceType, Hostname);
                _fileSystemLoaded = true;
            }
            return _fileSystem;
        }
    }
    
    public void MarkFileSystemDirty()
    {
        HasFileSystemChanges = true;
    }
}
```

**Lifecycle:**

1. **Device Created:** File system NOT loaded yet (just metadata)
2. **Player Accesses Device:** First time `device.FileSystem` accessed → loads from template
3. **Player Modifies Files:** Calls `device.MarkFileSystemDirty()` to flag changes
4. **Game Saved:** Only devices with `HasFileSystemChanges = true` serialize file system
5. **Game Loaded:** Modified file systems loaded, unmodified devices use templates
6. **Device Inactive:** After 30 minutes of no access, file system can be unloaded (future optimization)

**Memory Savings:**

- At startup: 0KB (no file systems loaded)
- After 1 hour gameplay: ~5MB (50 devices accessed × 100KB each)
- After full playthrough: ~20MB (200 devices modified × 100KB each)

**Performance Cost:** ~10ms to load file system (only happens once per device, when first accessed)

### Template System

**Story Devices:** Explicit file system definitions (every file scripted)

```json
// FileSystems/SarahPhone.json
{
  "files": [
    {
      "path": "/Messages/Sarah/Phoenix.txt",
      "content": "Phoenix: The package is ready. Meet at the usual spot. -P",
      "timestamp": "2024-03-15T14:32:00Z"
    },
    {
      "path": "/Photos/IMG_1337.jpg",
      "fileType": "image",
      "metadata": { "location": "SmallTown_Warehouse", "questHint": "main_02" }
    }
  ]
}
```

**Filler Devices:** Procedural file system generation

```csharp
// Generate realistic office workstation files
FileSystemFactory.CreateFromTemplate("Generic/OfficeWorkstation")
  → Creates: /Documents/Quarterly_Report_Q3.docx
  → Creates: /Desktop/TODO.txt (with randomized tasks)
  → Creates: /Downloads/meeting_notes.pdf
  → Creates: /AppData/browser_history.db (with realistic sites for office worker)
```

**Content Variability:**

- **Names:** Randomized from name pools ("Sarah", "Marcus", "Jennifer")
- **Dates:** Realistic timestamps (workday hours, not 3 AM)
- **Relationships:** Files reference other NPCs/devices consistently
- **Tone:** Professional tone for work devices, casual for personal

---

## Device Compromise Mechanics

### Access Type Spectrum

Different exploits grant different levels of access:

```
┌─────────────────────────────────────────────────────────────────┐
│                    DEVICE ACCESS SPECTRUM                        │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  Physical Access                                                 │
│  ├─ Temporary shell (e.g., buffer overflow on local machine)    │
│  ├─ Must remain at location                                     │
│  └─ Lost when player leaves                                     │
│                                                                  │
│  Credentials                                                     │
│  ├─ Stolen username/password (e.g., phishing, keylogger)        │
│  ├─ Must re-authenticate each session                           │
│  ├─ Can be remotely accessed (SSH/RDP)                          │
│  └─ Low detection risk (looks like legitimate login)            │
│                                                                  │
│  Backdoor                                                        │
│  ├─ Persistent remote access (e.g., rootkit, firmware exploit)  │
│  ├─ Instant access, no authentication                           │
│  ├─ Works from anywhere                                         │
│  └─ High detection risk (security scans find backdoors)         │
│                                                                  │
│  Owner/Authorized                                                │
│  ├─ Legitimate access (your own device, or work computer)       │
│  └─ Zero detection risk                                         │
│                                                                  │
└─────────────────────────────────────────────────────────────────┘
```

### Exploit → Access Type Mapping

Different exploits grant different access types:

| Exploit Type | Access Granted | Duration | Detection Risk | Remote Access |
|--------------|----------------|----------|----------------|---------------|
| **Buffer Overflow** | Physical | Temporary | Low | No |
| **Phishing Attack** | Credentials | Permanent* | Very Low | Yes |
| **Password Brute Force** | Credentials | Permanent* | Medium | Yes |
| **Keylogger** | Credentials | Until removed | Medium | Yes |
| **Rootkit Install** | Backdoor | Permanent* | High | Yes |
| **Firmware Exploit** | Backdoor | Permanent* | Very High | Yes |
| **USB Rubber Ducky** | Physical | One-time | Low | No |
| **Social Engineering** | Authorized | Varies | Very Low | Depends |

*Permanent until detected or credentials rotated

### Detection System

The detection system tracks **suspicion levels** that escalate based on player actions:

#### **Suspicion Accumulation**

Every hacking action on a device increases suspicion:

```
Suspicion Level = Sum of (Action Detection Risk × Device Security Multiplier)

Device Security Multiplier:
- VeryLow: 0.2x  (abandoned systems, rarely monitored)
- Low: 0.5x      (poorly maintained, basic monitoring)
- Medium: 1.0x   (typical security, regular checks)
- High: 1.5x     (well-maintained, frequent audits)
- VeryHigh: 2.0x (hardened systems, real-time monitoring)
```

#### **Detection Thresholds**

When suspicion exceeds threshold, detection triggers:

| Security Level | Detection Threshold | Example Timeline |
|----------------|---------------------|------------------|
| VeryLow | 100 points | ~20 hacking actions before detection |
| Low | 75 points | ~15 actions |
| Medium | 50 points | ~10 actions |
| High | 30 points | ~6 actions |
| VeryHigh | 15 points | ~3 actions (nearly instant) |

#### **Detection Consequences**

When detection occurs:

1. **Backdoors Removed:** All backdoors on device deleted
2. **Credentials Rotated:** Passwords changed, stolen credentials invalid
3. **Security Hardened:** Device security level increases one tier
4. **Authorities Notified:** Karma penalty, law enforcement attention increases
5. **Network Alert:** Other devices on network increase monitoring (suspicion decay slower)

#### **Suspicion Decay**

If player stops targeting a device, suspicion slowly decreases:

```
Decay Rate = 5 points per in-game day (if no new actions)
```

This allows patient players to hack slowly, staying below detection threshold.

---

## Device Upgrade System

### Upgrade Scope: Physical Access Required

Players can upgrade devices to enhance performance, but **only with physical access**:

**Why Physical Access?**

- **Risk/reward gameplay:** Must infiltrate location to upgrade
- **Story consistency:** Can't remotely improve hardware
- **Detection opportunity:** Time spent at device increases risk
- **Job integration:** Upgrade workplace devices to make job minigames easier

### Upgradeable Components

```csharp
public class Device
{
    public int CPULevel;      // 1-5: Affects minigame processing speed
    public int RAMLevel;      // 1-5: Max concurrent app instances
    public int StorageLevel;  // 1-5: Max files/apps storable
    public int NetworkLevel;  // 1-5: Connection quality/speed
}
```

### Upgrade Rules

```csharp
public class DeviceUpgradeService : IDeviceUpgradeService
{
    public bool CanUpgrade(Device device, Player player)
    {
        // Must be physically present at device
        if (player.Location != device.Location)
            return false;
        
        // Check ownership/access
        if (device.OwnerId == player.PlayerId)
            return true; // Always can upgrade own devices
        
        if (device.OwnerId == null)
            return true; // Public devices (with detection risk)
        
        // Can upgrade compromised devices (with detection risk)
        if (player.CompromisedDeviceIds.Contains(device.DeviceId))
            return true;
        
        return false; // Can't upgrade non-owned, non-compromised devices
    }
    
    public Result UpgradeDevice(Device device, DeviceComponent component, int newLevel)
    {
        // Upgrading non-owned devices increases detection
        if (device.OwnerId != player.PlayerId)
        {
            detectionService.RecordSuspiciousActivity(device, new HackingAction
            {
                Type = "Hardware Tampering",
                DetectionRisk = 20, // High risk
                Duration = TimeSpan.FromMinutes(5) // Takes time
            });
        }
        
        // Apply upgrade
        switch (component)
        {
            case DeviceComponent.CPU:
                device.CPULevel = newLevel;
                break;
            // ... other components
        }
        
        return Result.Success($"Upgraded {component} to level {newLevel}");
    }
}
```

### Upgrade Gameplay Loop

**Example: Upgrading Work Computer to Make Job Easier**

1. Player works as data analyst at TechCorp
2. Job minigame: Sort data files, affected by CPU speed
3. Player's workstation: `CPULevel = 1` (slow, minigame takes 2 minutes)
4. **Plan:** Upgrade workstation CPU to level 3 (faster minigame)
5. **Risk:** Tampering with company property = high detection risk
6. **Execution:**
   - Stay late after hours (fewer witnesses)
   - Upgrade CPU (5 minutes, +20 suspicion)
   - Leave quickly before security patrol
7. **Result:**
   - Job minigame now takes 1 minute (2x faster)
   - Earn more money per day
   - But: Increased detection suspicion (must lay low for a few days)

---

## Device Events & Coordination

### Complete Event List

```csharp
public enum GameEventType
{
    // === Device Lifecycle ===
    DeviceRegistered,           // Device added to registry
    DeviceUnregistered,         // Device destroyed/removed
    DeviceDiscovered,           // Player discovers device (network scan)
    
    // === Device State Changes ===
    DeviceCompromised,          // Player gains access (any method)
    DeviceAccessMethodChanged,  // Access type upgraded (credentials → backdoor)
    DeviceOnlineStatusChanged,  // Device goes online/offline (power outage)
    DeviceUpgraded,             // Device hardware upgraded
    
    // === Device Location ===
    DeviceLocationChanged,      // Device moved (NPC schedule)
    
    // === Device Security ===
    DeviceBackdoorInstalled,    // Persistent backdoor added
    DeviceBackdoorRemoved,      // Backdoor detected and removed
    DeviceCredentialsRotated,   // Passwords changed (security response)
    
    // === Device Data ===
    DeviceFileSystemChanged,    // File added/removed/modified
    DeviceFileSystemAccessed,   // Player browsed file system
}
```

### Event Firing Order (via ProgressionCoordinator)

**Example: Player Hacks CoffeeShop POS Terminal**

```
1. Player executes exploit
   Event: (none yet, just internal hacking service logic)
   ↓
2. HackingService.ExecuteExploit() determines success
   Event: DeviceCompromised
   ↓
3. ProgressionCoordinator.CoordinateDeviceCompromise() [SYNCHRONOUS]
   ├─ 3a. Update Player state (player.CompromisedDeviceIds.Add())
   ├─ 3b. LeadManager.UpdateLeadsForDevice() (mark "Hack POS" lead complete)
   │      Event: LeadCompleted
   ├─ 3c. QuestManager.CheckObjectiveCompletion() (check tutorial quest)
   │      Event: QuestObjectiveCompleted
   └─ 3d. DetectionService.RecordSuspiciousActivity() (roll for detection)
          Event: (none if undetected, or DeviceDetectionTriggered)
   ↓
4. If quest objective completed → QuestManager.OnQuestCompleted
   Event: QuestCompleted
   ↓
5. ProgressionCoordinator.CoordinateQuestCompletion() [SYNCHRONOUS]
   ├─ 5a. LeadManager.ResolveRelatedLeads()
   │      Event: LeadResolved (for multiple leads)
   ├─ 5b. QuestManager.UnlockDependentQuests()
   │      Event: QuestUnlocked (next quest in chain)
   └─ 5c. DialogueService.UnlockDialogueNodes() (Sarah mentions the hack)
          Event: DialogueNodesUnlocked
   ↓
6. New quest unlocked → ProgressionCoordinator.UpdateLeadsForQuest()
   Event: LeadCreated (new lead for next quest)
   ↓
7. All coordination complete
   Event: CoordinationCompleted
```

**Key Characteristics:**

- **Guaranteed order:** Events fire in priority sequence (no race conditions)
- **Circular prevention:** Coordinator detects infinite loops (A → B → A)
- **Synchronous critical path:** Quest/lead updates happen immediately
- **Async non-critical:** UI updates, notifications deferred to next frame

---

## Device Software Architecture

### Interactive Apps vs Background Software

Devices run two fundamentally different types of software:

#### **Interactive Apps (Player-Facing UI)**

Applications the player can launch and interact with:

- **Definition:** Apps with UI that player controls directly
- **Examples:** Email client, Terminal, File Explorer, Web Browser, IRC client
- **Storage:** `Device.InteractiveApps` (List<IInteractiveApp>)
- **Management:** `ApplicationContainerService` manages running instances
- **Lifecycle:** Player launches → runs in container → player closes
- **OS-Specific:** Linux terminal vs Windows CMD vs macOS Terminal (different UIs)

**Usage Pattern:**

```csharp
// Player wants to read emails on compromised device
var emailApp = applicationContainer.LaunchApp(AppCategory.Email, device);
emailApp.ShowInbox(); // Displays UI with device's emails
```

#### **Background Software (Vulnerability Targets)**

Software running "in the background" (conceptually):

- **Definition:** Services with no player-facing UI, exist for hacking
- **Examples:** Apache web server, MySQL database, SSH daemon, FTP server
- **Storage:** `Device.InstalledSoftware` (List<Software>)
- **Management:** Read-only list, populated at device creation
- **Lifecycle:** Always "running" (conceptual), never actually instantiated
- **Vulnerabilities:** Each software has list of exploits that work against it

**Usage Pattern:**

```csharp
// Player wants to find exploitable services
var vulnerableServices = device.InstalledSoftware
    .Where(s => s.HasKnownVulnerabilities())
    .ToList();

// Hacking service checks which exploits work
foreach (var software in vulnerableServices)
{
    var workingExploits = exploitDatabase.GetExploitsFor(software);
    // Display to player: "Apache 2.2.14 - Vulnerable to Buffer Overflow"
}
```

### Discovery Process

**Interactive Apps:**

1. Player gains access to device (any method)
2. `ApplicationContainerService` queries `AppRegistry.GetAppsForOS(device.OS)`
3. Apps appear in device's app launcher UI
4. Player launches app to interact with device

**Background Software:**

1. **Option A:** Player runs port scan → discovers open ports → identifies services
   - Example: "Port 80 open → Apache 2.2.14 detected"
2. **Option B:** Player gains access → checks installed software list
   - Example: `cat /var/log/apache/version.txt`
3. **Option C:** Network scan auto-discovers services on devices
   - Example: Network scan shows "Web Server (Apache)" for all devices running Apache

---

## Performance Considerations

### Memory Management

**Target:** Support 1000+ devices without exceeding 50MB total memory usage.

| Component | Count | Per-Device Size | Total Memory |
|-----------|-------|-----------------|--------------|
| **Device Metadata** | 1000 | 1KB | 1MB |
| **Loaded File Systems** | 50 (active) | 100KB | 5MB |
| **Network Indexes** | 50 networks | 10KB | 500KB |
| **Ownership Indexes** | 100 owners | 5KB | 500KB |
| **Total** | | | **~7MB** |

**Lazy Loading Impact:**

- Without lazy loading: 1000 × 100KB = **100MB** (unacceptable)
- With lazy loading: 50 × 100KB = **5MB** (95% memory savings)

### Query Performance

**DeviceRegistry Indexes:**

All expensive queries are indexed for O(1) or O(n) performance:

```csharp
// Indexed queries (O(1) hash lookup)
deviceRegistry.GetDevice(deviceId)              // By ID
deviceRegistry.GetDeviceByHostname(hostname)    // By hostname
deviceRegistry.GetDeviceByIP(ip)                // By IP

// Indexed queries (O(n) where n = devices in result set)
deviceRegistry.GetDevicesInCity(cityId)         // By city
deviceRegistry.GetDevicesOnNetwork(networkId)   // By network
deviceRegistry.GetDevicesOwnedBy(ownerId)       // By owner

// Scan queries (O(n) where n = all devices)
deviceRegistry.GetCompromisedDevices()          // Filter scan
deviceRegistry.GetDevicesNearLocation(location) // Distance calculation
```

**Measured Performance (1000 devices):**

- Get device by ID: 0.001ms
- Get devices on network (50 devices): 0.05ms
- Get all compromised devices (10 devices): 0.8ms
- NPC schedule update (10 devices move): 0.2ms

### Network Topology Updates

**Problem:** NPCs move 6-8 times per day. If 100 NPCs each own 2 devices, that's 1200-1600 device movements per day.

**Solution:** Event-driven updates with batching safety net.

```csharp
// Event-driven (immediate consistency)
Device.MoveToLocation() 
  → Triggers GameEventType.DeviceLocationChanged
  → DeviceRegistry.OnDeviceMoved() updates indexes
  → NetworkService.UpdateDeviceNetworkMembership() updates topology
  
// Total time per move: ~0.2ms
// Total time per hour (100 devices move): 20ms (acceptable)
```

**Future Optimization (if needed):**

If profiling shows movement is expensive, batch updates:

```csharp
// Instead of updating immediately, queue updates
networkService.ScheduleTopologyUpdate(device);

// Process queue in Update() with frame budget
void Update(float deltaTime)
{
    int maxUpdatesPerFrame = 5;
    while (pendingUpdates.Count > 0 && maxUpdatesPerFrame > 0)
    {
        ProcessTopologyUpdate(pendingUpdates.Dequeue());
        maxUpdatesPerFrame--;
    }
}
```

---

## Save System Integration

### Device Save Data Structure

```csharp
[Serializable]
public class DeviceSaveData
{
    public const int CURRENT_VERSION = 1;
    
    // Only save devices with changes
    public List<DeviceState> ModifiedDevices;
    
    // Track destroyed devices (story events)
    public List<string> DestroyedDeviceIds;
    
    // Track procedurally generated devices
    public List<string> GeneratedDeviceIds;
    
    public int Version = CURRENT_VERSION;
}

[Serializable]
public class DeviceState
{
    // === Identity ===
    public string DeviceId;
    public string Hostname;
    public string IPAddress;
    
    // === Ownership ===
    public string OwnerId;
    public List<string> AuthorizedUserIds;
    
    // === Location ===
    public PhysicalLocation Location;
    public string NetworkId;
    public bool IsOnline;
    
    // === Security ===
    public SecurityLevel SecurityLevel;
    public bool IsCompromised;
    public Dictionary<string, DateTime> BackdoorConnections;
    public List<DeviceCredential> Credentials;
    
    // === Upgrades ===
    public int CPULevel;
    public int RAMLevel;
    public int StorageLevel;
    public int NetworkLevel;
    
    // === File System (only if modified) ===
    public VirtualFileSystemSnapshot FileSystem; // null if unmodified
    
    // === Detection ===
    public float SuspicionLevel;
    public DateTime LastSuspiciousActivity;
}
```

### Save Strategy: Modified Devices Only

**Problem:** Saving 1000 devices = huge save files (100MB+)

**Solution:** Only save devices that have changed from their initial state.

```csharp
public class DeviceRegistry : ISaveable
{
    public SaveData GetSaveData()
    {
        var modifiedDevices = devicesById.Values
            .Where(d => 
                d.HasFileSystemChanges ||      // Files added/removed
                d.IsCompromised ||              // Player hacked it
                d.BackdoorConnections.Count > 0 || // Backdoors installed
                d.CPULevel > 1 ||               // Hardware upgraded
                d.Location != d.InitialLocation // Moved from spawn point
            )
            .Select(d => SerializeDevice(d))
            .ToList();
        
        return new DeviceSaveData
        {
            ModifiedDevices = modifiedDevices,
            DestroyedDeviceIds = destroyedDevices.ToList(),
            GeneratedDeviceIds = procedurallyGeneratedDevices.ToList()
        };
    }
}
```

**Save File Sizes:**

- Start of game: ~10KB (only player's devices)
- After 10 hours: ~500KB (~50 devices modified)
- Full playthrough: ~2MB (~200 devices modified)

### Load Strategy: Reconstruct Unmodified

```csharp
public class DeviceRegistry : ISaveable
{
    public void LoadFromSave(SaveData data)
    {
        var deviceData = data as DeviceSaveData;
        
        // 1. Load story-critical devices (always exist)
        LoadStoryCriticalDevices();
        
        // 2. Apply saved changes to modified devices
        foreach (var savedDevice in deviceData.ModifiedDevices)
        {
            var device = devicesById[savedDevice.DeviceId];
            ApplyDeviceState(device, savedDevice);
        }
        
        // 3. Re-generate procedural devices
        foreach (var deviceId in deviceData.GeneratedDeviceIds)
        {
            if (!devicesById.ContainsKey(deviceId))
            {
                // Device was procedurally generated in previous session
                // Regenerate from template (state saved if modified)
                var device = RegenerateProceduralDevice(deviceId);
                RegisterDevice(device);
            }
        }
        
        // 4. Remove destroyed devices
        foreach (var deviceId in deviceData.DestroyedDeviceIds)
        {
            UnregisterDevice(deviceId);
        }
    }
}
```

---

## Device Type System

### Device Categories

Devices are organized into categories that determine behavior:

```csharp
public enum DeviceCategory
{
    Workstation,        // Desktop computers, fixed location
    Server,             // Servers, high security, remote access only
    Router,             // Network infrastructure, web interface
    IoTDevice,          // Smart devices, usually insecure, APIonly
    MobileDevice,       // Phones, tablets, moves with NPCs
    EmbeddedSystem,     // POS terminals, ATMs, kiosks
    IndustrialControl   // SCADA, factory equipment, high-impact hacks
}
```

### Device Type Configuration

Each device type defines:

- **Category:** Determines available interaction methods
- **Software weights:** Probability of software being installed
- **Default security:** Typical security level for this type
- **Interaction methods:** SSH, RDP, physical, web interface, API

**Example: Smartphone**

```csharp
new DeviceType(
    category: DeviceCategory.MobileDevice,
    name: "Smartphone",
    softwareWeights: new Dictionary<string, float>
    {
        { "mobile_os", 1.0f },        // Always installed
        { "messaging_app", 1.0f },    // Always installed
        { "email_client", 0.9f },     // 90% chance
        { "social_media", 0.8f },     // 80% chance
        { "banking_app", 0.5f },      // 50% chance
        { "games", 0.3f }             // 30% chance
    }
);
```

### Device Factory Usage

```csharp
// Story device: Explicit definition
var sarahPhone = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
{
    DeviceId = "phone_sarah",
    Hostname = "sarah-iphone",
    DeviceTypeId = "smartphone",
    OwnerId = "sarah",
    SecurityLevel = SecurityLevel.Medium
});

// Procedural device: Generated from template
var randomPhone = DeviceFactory.CreateFromTemplate(
    template: phoneTemplate,
    network: coffeeShopNetwork
);
// Generates: "phone_a8f3d2" owned by "CoffeeShop" org
```

---

## Integration with Other Systems

### NPCScheduler → Device Movement

When NPCs move between locations, their devices move too:

```csharp
public class NPCScheduler : INPCScheduler
{
    private void OnHourChanged(object data)
    {
        foreach (var npc in npcManager.GetAllNPCs())
        {
            var schedule = npc.GetScheduleForTime(currentTime);
            
            if (npc.Location != schedule.Location)
            {
                // Move NPC
                npc.Location = schedule.Location;
                
                // Move NPC's devices
                var devices = deviceRegistry.GetDevicesOwnedBy(npc.NpcId);
                foreach (var device in devices)
                {
                    if (device.DeviceType.Category == DeviceCategory.MobileDevice)
                    {
                        device.MoveToLocation(schedule.Location);
                    }
                }
            }
        }
    }
}
```

### LeadManager → Device Discovery

When player discovers a device, a lead is automatically created:

```csharp
public class LeadManager : ILeadManager
{
    private void OnDeviceDiscovered(object data)
    {
        var device = data as Device;
        
        // Only create leads for interesting devices
        if (!ShouldCreateLead(device))
            return;
        
        var lead = new Lead
        {
            LeadId = $"device_{device.DeviceId}",
            Type = LeadType.Device,
            Title = $"Unknown device: {device.Hostname}",
            Description = $"Discovered {device.DeviceType.Name} on {networkName}",
            Priority = DetermineLeadPriority(device),
            RelatedDeviceIds = new List<string> { device.DeviceId }
        };
        
        CreateLead(lead);
    }
    
    private bool ShouldCreateLead(Device device)
    {
        // Don't create leads for mundane devices
        if (device.SecurityLevel == SecurityLevel.VeryLow)
            return false;
        
        // Always create leads for story devices
        if (device.Metadata.ContainsKey("IsStoryCritical"))
            return true;
        
        // Create leads for devices with valuable data
        if (device.Metadata.ContainsKey("HasQuestData"))
            return true;
        
        return false;
    }
}
```

### QuestManager → Device Objectives

Quests can have device-related objectives:

```csharp
// Quest Objective: "Hack the CoffeeShop POS terminal"
new DeviceCompromisedCondition
{
    DeviceId = "pos_coffeeshop",
    RequiredAccessType = DeviceAccessType.Backdoor // Must install backdoor
};

// Quest Objective: "Find 3 vulnerable servers"
new DevicesCompromisedCountCondition
{
    RequiredCount = 3,
    DeviceTypeFilter = DeviceCategory.Server,
    NetworkFilter = "TechCorp_Internal"
};
```

---

## Debug Tools & Editor Extensions

### Device Inspector

**Purpose:** View complete device state during development.

**Features:**

- Device properties (ID, hostname, owner, location)
- Security state (compromised, backdoors, suspicion level)
- File system browser (view files without launching game)
- Network membership (which networks device is on)
- Access methods (how player can access device)

**Unity Menu:** Window → Device Inspector

### Network Topology Visualizer

**Purpose:** Visualize device-network relationships as graph.

**Features:**

- Nodes = Devices (color-coded by type)
- Edges = Network connections
- Filter by: city, network, owner, compromised status
- Click device to inspect
- Highlight story-critical devices

**Unity Menu:** Tools → Network Topology Visualizer

### Detection State Monitor

**Purpose:** Track detection levels across all compromised devices.

**Features:**

- List of compromised devices
- Suspicion level (bar graph)
- Time until detection (estimated)
- Recent hacking actions (log)
- "Trigger Detection" button (for testing)

**Unity Menu:** Tools → Detection Monitor

---

## Troubleshooting & Common Pitfalls

### Issue: Device Not Discovered on Network Scan

**Symptom:** Player scans network, expected device missing from results.

**Possible Causes:**

1. Device not registered in `DeviceRegistry`
   - **Fix:** Check `WorldInitializer` loads device definition
2. Device not added to network
   - **Fix:** Verify `network.AddDevice(device)` called
3. Device is offline (`IsOnline = false`)
   - **Fix:** Check if device location has power/network
4. Network scan filtering out device
   - **Fix:** Check scan service filtering logic

### Issue: File System Not Saving

**Symptom:** Player modifies files, but changes lost on reload.

**Possible Causes:**

1. `HasFileSystemChanges` not set
   - **Fix:** Call `device.MarkFileSystemDirty()` after modifications
2. Save system not serializing file systems
   - **Fix:** Check `DeviceSaveData` includes file system snapshot
3. File system unloaded before save
   - **Fix:** Don't unload file systems with pending changes

### Issue: Device Movement Not Updating Network Topology

**Symptom:** NPC moves, but their device still appears on old network.

**Possible Causes:**

1. `Device.MoveToLocation()` not called
   - **Fix:** `NPCScheduler` must call `device.MoveToLocation()`
2. `GameEventType.DeviceLocationChanged` not firing
   - **Fix:** Check event subscription in `DeviceRegistry`
3. `NetworkService` not subscribed to location changed event
   - **Fix:** Verify `NetworkService.Initialize()` subscribes to event

### Issue: Backdoor Removed Unexpectedly

**Symptom:** Player had backdoor, now it's gone.

**Possible Causes:**

1. Detection triggered
   - **Check:** Detection state monitor, suspicion level exceeded threshold
2. Device rebooted (story event)
   - **Check:** Quest log for power outage/reboot events
3. Security scan (scheduled maintenance)
   - **Check:** Device metadata for last security scan timestamp

---

## Future Enhancements

### Planned Features

1. **Device Pooling:** Reuse device instances instead of creating new ones
2. **File System Compression:** Compress file systems in save files (50% size reduction)
3. **Network Simulation:** Simulate packet latency, bandwidth for realism
4. **Device Aging:** Devices become more vulnerable over time (software gets outdated)
5. **Security Patches:** Devices periodically patch vulnerabilities (player must find new exploits)
6. **Device Cloning:** Player can clone devices (for testing exploits safely)
7. **Honeypots:** Fake vulnerable devices set as traps by security teams

### Performance Optimizations (If Needed)

1. **Aggressive File System Unloading:** Unload file systems after 30 minutes of inactivity
2. **Batched Topology Updates:** Queue device movements, process in batches
3. **Lazy Device Generation:** Generate filler devices one at a time (spread across frames)
4. **Device LOD System:** Reduce detail for devices far from player

---

## Conclusion

The Device System is the most complex system in the game, handling:

- **1000+ devices** with rich state (files, software, security)
- **Multiple ownership models** (player, NPCs, organizations, shared)
- **Dynamic network topology** (devices move with NPCs 6-8 times/day)
- **Complex access mechanics** (physical, credentials, backdoors, authorized)
- **Detection system** (suspicion tracking, escalating consequences)
- **Lazy loading** (file systems load on-demand, 95% memory savings)
- **Event-driven coordination** (device state changes cascade through game systems)

By separating concerns (ownership vs access, metadata vs file systems, story vs procedural), the system remains maintainable and performant despite its complexity. The hybrid ownership model provides the best of both worlds: fast queries for world simulation (devices owned by NPCs/orgs) and rich progression tracking (player access methods).

**Key Takeaways:**

- ✅ **Lazy loading is critical** for performance with 1000+ devices
- ✅ **Hybrid ownership** separates world state from player progression
- ✅ **Event-driven coordination** prevents tight coupling between systems
- ✅ **Story + procedural** balances authored content with emergent gameplay
- ✅ **Detection system** adds meaningful risk to hacking actions

This architecture supports the core gameplay loop: **Discover → Hack → Exploit → Upgrade → Detect → Adapt**.
