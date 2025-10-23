**TSLM - The Secret Life of Malory**

# Pitch
The elevator pitch for this game would be, "Think Stardew Valley, if Stardew Valley was about hacking instead of farming, and had fleshed out job system instead of combat". What this means in practice is that we are designing a game world with multiple cities (at least two), in which the protagonist, Malory accidentally ends up in a conspiracy laden situation because her penchant for hacking. The game will have lots of NPCs (social, romance, and trust), lots of networks within the cities, and lots devices on those networks. Their will be two stat systems, primary stats that influence which jobs you are suited for, and how much money you'd get from them, and then their are secondary hacking stats that will affect the strength of vulnerabilities, change what social approaches you can take, change what hacking groups you can get into (and subsequently what types of exploits you can find / buy). Within each job path a big focus is not just the minigame that gets you money, but the people, network, and devices in that job path, what quest stems from those, what devices could be hacked to make your job easier (or the minigame easier, sort of like an upgrade system), or to get you further in your job, or sometimes just to increase the valuable information you have to sell (there will be a hidden karma system in the game that tracks black hat vs white hat hacks, and certain story beats / quest will be locked as you go to certain karma. Additionally there will be a different groups that are watching for the player, like law enforcement, other hacking groups the player encounters, so certain hacks or actions will increase the notice on the player). The cities are fairly explorable, most buildings that are stores or business the user will be able to enter, speak to people, interact with devices. Some devices in the game are only accessible physically, some through SSH only, some through remote desktop only, etc. In terms of intractable devices (computers, phone, atm, security cameras, mainframes, heavy equipment, etc), devices run two types of software, the first is interactive software (i.e. it's represented in the game as something you play with, pull up and physically interact with), like email, IRC, web browser, IM, Discord, and more; the other type of software is just background, these are used to run vulnerabilities against. We want to have two or three operating systems represented that all have their own version of apps, their own software, and their own vulnerabilities (some vulnerabilities will apply to all OS, some wont).

 This gives us some fun 

**Thoughts Currently on How OS specific apps would work:**
```
Resources/UI/Apps/
  ├── Linux/
  │   ├── TerminalUI.prefab (looks like GNOME Terminal)
  │   ├── EmailUI.prefab (Thunderbird-style)
  │   └── BrowserUI.prefab (Firefox-style)
  ├── Windows/
  │   ├── TerminalUI.prefab (looks like Windows Terminal)
  │   ├── EmailUI.prefab (Outlook-style)
  │   └── BrowserUI.prefab (Edge-style)
  └── Mobile/
      ├── TerminalUI.prefab (Termux-style)
      └── BrowserUI.prefab (Mobile browser chrome)
```

**The Story Begins**
You (a very computer savvy young woman) start in a small town, and your best friend ask if you want to be their roommate in the big city. All you have to do is get enough money together for half the rent, and a moving truck; they also mention that they think they are about the get a job as a graphic designer at a huge tech company (essentially Google or Amazon) and they want to put in a good word for you to help you get that job. As you are reading this IM conversation, a notification pops up your PC for a new message in a Discord group dedicated to the leaks of password, and it's a new release, the name of the file is the name of your town. You see the price on it $100, which is exactly what you have left, so you purchase it and find you now have some login information for some of the business in town; a perfect start to help you find a job for more money, after all now you can log into their server and delete any other applications in the file server, leaving only your own. (This is the beginning of the job system, where we have multiple career paths. You can get yourself into any job you have the stats for, and you can raise stats over time). But in doing so you also end up catching the attention of a hacking group that starts watching you, creating some tension later when you apply to big tech company, as they would like you to hack into this company to find out info on a secret project.

Some of the above is still very mutable, and subject to change as we flesh out the story. 
## Architecture
Right now we are thinking the smartest thing we could do is Service Location + Observer Pattern. Things like devices will be handled with ID / registry as there's several ways we could think about access; **Devices are registered once, referenced many ways**: A coffee shop's POS terminal exists in:

- Inside something like a  Device Registry (authoritative source)
- Inside of a service like NetworkTopology (connected to coffee shop's internal network)
- Location (physically in downtown small town)
- Player Inventory (if compromised, stores reference to device ID)

Additionally some devices (tablets, laptops, and phones) will move throughout the day, as NPC will carry them with them (say from home to work, or work to gym). Additionally, a device can be on multiple networks (dual-homed, VPN connections), and network topology  can change (player creates backdoor routes). This means were looking at something like:
```txt
DeviceRegistry (flat registry)
  ├── All devices indexed by ID
  ├── Query by: location, network, type, etc.
  └── Devices can be in MULTIPLE networks

GameWorld
  ├── List<City>
  └── Query helper (delegates to DeviceRegistry)

City
  ├── CityId
  ├── List<PhysicalLocation> (buildings, streets)
  └── Query helper: GetDevicesInCity() → DeviceRegistry

VirtualNetwork
  ├── NetworkId
  ├── List<string> MemberDeviceIds (NOT Device objects)
  └── Query helper: GetNetworkDevices() → DeviceRegistry
```

Access Types:
```C#
public enum RemoteAccessType
{
    SSHOnly,        // Terminal only
    RDPCapable,     // Can show limited GUI
    WebInterface,   // Has web admin panel
    APIOnly,        // REST/API interface only
    Physical        // Must be at location to use
}
```

Right now there's some consideration into whether there should be a device upgrade system. I'm leaning towards yes, and my very rough idea is looking like [[Device]].

There's also some changes for the player specific devices (Phone / Laptop / Whatever PC she buys), as they should have some unique upgrades:
```C#
// Visual customization affects nothing mechanically but looks cute!
public List<Sticker> AppliedStickers;
public CaseColor CaseColor;
public KeyboardBacklight KeyboardColor;
```

Each device has it's own [[Virtual File System]], which is in turn composed of [[Virtual Node|Virtual Nodes]].

### Quest Data Model
We need a flexible system that can handle:
- Linear story progression (main plot)
- Branching choices (which job to pursue, quest from NPC)
- Hidden objectives (emergent threats,  and new group discovery)
- Emergent goals (discover a network, gain access)
- Timed events (friend gets job offer, NPC needs item by sundown, etc)

That being the case we need a hybrid event/poll system with really good logging. We should show "active leads" vs "main quests" separately.
### **My Architecture Recommendation**
```txt
QuestManager
├── Listens to GameEvents (NPC interactions, hacks, discoveries)
├── Polls GameState (time, relationships, location)
├── Manages quest dependencies and progression
└── Triggers DialogueManager, LeadManager, etc.

DialogueManager
├── Node-based dialogue trees (JSON/Ink)
├── Condition evaluation (checks game state)
├── Effect application (changes relationships, sets flags)
└── Emits events back to QuestManager

LeadManager (Emergent Objectives)
├── Stores discovered leads/objectives
├── Links leads to quests (both ways)
├── Prioritizes/categorizes leads
└── UI layer for investigation board

TimeManager
├── Tracks game time
├── Broadcasts time events (hour_changed, day_changed)
├── Handles pause states (in device UI)
└── Notifies NPCScheduleManager
```

- **Time system**: Pause during device use, continue everywhere else. Add a "skip time" function (like waiting in Skyrim) for when player wants to reach a specific NPC schedule.
- **Quests**: Hybrid event/poll system with clear quest logging. Show players "active leads" vs "main quests" separately.
- **Emergent objectives**: Investigation board UI. When player finds a device, it appears as a lead. Player can "pin" important leads. Some leads auto-pin when critical.
- **Dialogue**: Node-based with relationship tracking. Store conversation flags on per-NPC basis. Consider having some "one-time" conversation branches that lock out after choosing (adds weight to decisions).

### Stats
Primary Stats are still up in the air, but I've been leaning towards something like this:
```c#
public class PlayerStats
{
    // === SOCIAL STATS ===
    [Header("Social Skills")]
    public int Charisma;        // Affects: conversation options, social engineering success
    public int Confidence;      // Affects: job interviews, bluffing, demanding things
    public int Empathy;         // Affects: reading NPCs, romance options, manipulation detection
    public int Professionalism; // Affects: job performance, raises, corporate interactions
    
    // === KNOWLEDGE STATS ===
    [Header("General Knowledge")]
    public int TechLiteracy;    // Affects: learning hacking skills faster, tech job performance
    public int BusinessAcumen;  // Affects: corporate jobs, understanding org structure
    public int Creativity;      // Affects: graphic design jobs, finding unconventional solutions
    public int Research;        // Affects: OSINT gathering, finding vulnerabilities faster
    
    // === PHYSICAL/MENTAL ===
    [Header("Personal Attributes")]
    public int Focus;           // Affects: minigame time limits, error margins
    public int Stress;          // BAD stat - reduces other stats, gained from risky hacking
    public int Karma;      // (can be good or notorious)
    public int Strength;  // No useful for most things, but could get you into certain job tracts
}
```

#### Hacking Stats (Technical Skills)
Again these are still up in the air, these are more just rough thoughts for now

```csharp
public class HackingStats
{
    // === CORE HACKING ===
    [Header("Technical Skills")]
    public int NetworkMapping;    // Affects: discovery speed, network visualization detail
    public int Exploitation;      // Affects: success rate of exploits
    public int Cryptography;      // Affects: password cracking, encryption breaking
    public int Stealth;           // Affects: detection chance, log cleaning
    public int SocialEngineering; // Affects: phishing, pretexting, manipulation
    
    // === SPECIALIZED ===
    [Header("Advanced Skills")]
    public int ReverseEngineering; // Affects: finding zero-days, understanding malware
    public int Scripting;          // Affects: automation, custom tools
    public int Forensics;          // Affects: data recovery, finding hidden data
    public int PhysicalSecurity;   // Affects: lockpicking, bypassing physical access
    
    // === REPUTATION STATS ===
    [Header("Underground Reputation")]
    public int BlackhatRep;       // Reputation with criminal hackers
    public int WhitehatRep;       // Reputation with security researchers
    public int ActivistRep;       // Reputation with hacktivist groups
    public int CorporateRep;      // Reputation with legitimate security firms
    
    // === DERIVED STATS ===
    [Header("Computed Values")]
    public int HeatLevel;         // How much law enforcement is watching you (we will likely need to also track this per group)
    public int TraceResistance;   // How hard you are to track
    public int AccessLevel;       // What tier of targets you can realistically hack
}
```
## Style
We want something between kawaii-core and vaporwave with a hint of witchy vibes. I'm dubbing it Dreamwave style. *Think*: hacking from a bedroom filled with fairy lights, crystals next to your router, and your terminal glowing in soft purples and pinks.

Good terms for examples of what we are combining are:  
- Kawaii UI
- Vaporwave UI
- Pastel Goth UI
- Soft Witchy Aesthetic UI

My thinking is that our aesthetic combines:  
**Kawaii-Core:** Soft pastels, cute elements, rounded shapes  
**Vaporwave**: Neon accents, gradient overlays, retro-futuristic elements  
**Witchy** (Small Amount):  Dark undertones with a magical feel, (Rare) Mystical symbols, moons/stars

**Visual Characteristics:**  
- **Color palette**: Soft purples, pinks, blues with neon accents and occasional deep purples/blacks  
- **Typography**: Mix of rounded cute fonts with mysterious serif accents  
- **Elements**: Iconography in pastel/holographic treatments  
- **Textures**: Holographic gradients, soft glows, sparkles, mystical auras

**UI/UX Design**  
- **Terminal/Hacking Interface**: Soft gradient backgrounds (pink→purple→blue) with neon green/cyan text
- **Menu Systems**: Rounded, bubbly panels with subtle holographic overlays
- **Icons**: Cute pixel art with mystical touches (a heart-shaped firewall icon, etc)
- **Notifications**: Soft glow effects, sparkle particles when completing tasks

**Character & World Design**  
- **Protagonist**: Pastel goth aesthetic - over-sized hoodies, cat ear headphones, etc
- **Small Town**: Cozy, slightly retro (vaporwave influence) - neon signs with soft glows, local businesses with personality
- **Big City**: More intense vaporwave - cyberpunk meets dreamy, holographic billboards, purple/pink skylines