# Network System Architecture

## Overview

The Network System is the backbone of the game's hacking mechanics, managing how devices connect, communicate, and are discovered across multiple cities. It implements a **hybrid Gateway-Hierarchical model** that balances realistic network topology with clear gameplay objectives. The system handles network discovery, access control, connection management, and integration with the Device, Quest, Lead, and NPC systems.

**Key Design Principles:**

- **Gateway-focused progression**: Networks connect through specific hackable devices (routers, VPN servers, firewalls)
- **Hierarchical organization**: Three-tier structure (Internet/Backbone → ISP → End Networks) provides natural progression
- **Discovery-driven**: Networks aren't automatically visible; players discover them through exploration, hacking, or social interaction
- **Multiple access paths**: Strategic depth through ISP routes (monitored, easy) vs. direct connections (clean, harder to find)
- **Context-aware connections**: Connection method auto-selected based on player's location and available access methods
- **Performance-optimized**: Smart indexing and lazy loading support 1000+ devices across 50+ networks

---

## Design Philosophy

### What is a Network?

In the game world, a **network** represents a collection of interconnected devices that share:

- **Common infrastructure**: Routers, switches, firewalls that enable communication
- **Organizational identity**: Corporate networks, home networks, ISP networks, government networks
- **Security posture**: Unified security policies, intrusion detection, access controls
- **Geographic or logical boundary**: Physical location (coffee shop) or organizational scope (TechCorp)

Networks serve multiple gameplay purposes:

1. **Discovery targets**: Players scan networks to find new devices and leads
2. **Access gates**: Control progression by requiring specific credentials or compromises
3. **Social/organizational context**: Network topology reflects real-world relationships (TechCorp connects to their bank)
4. **Risk zones**: Different networks have different monitoring levels and consequences for detection
5. **Story structure**: Network access gates story progression and side content

### Networks vs Devices vs Locations

Understanding the relationship between these entities is crucial:

| Entity | Purpose | Contains | Examples |
|--------|---------|----------|----------|
| **Network** | Logical grouping of devices with shared infrastructure | Device IDs, gateways to other networks | "TechCorp_Internal", "CityNet_ISP", "CoffeeShop_Guest" |
| **Device** | Individual hackable system | Software, files, credentials | Router, VPN server, Sarah's laptop, POS terminal |
| **Location** | Physical place in game world | NPCs, devices (physical reference) | TechCorp office building, coffee shop, Sarah's apartment |

**Key Relationships:**

- **Device → Network**: A device can be on multiple networks (e.g., a router bridges two networks)
- **Location → Devices**: A location contains devices at specific physical positions
- **Device → Gateway**: Special devices act as gateways between networks
- **NPC → Devices**: NPCs own devices that move with them between locations
- **Network → Location**: Networks can be physically accessible at locations (e.g., coffee shop WiFi)

---

## Hierarchical Network Structure

### Three-Tier Architecture

The game uses a **three-tier hierarchy** that provides intuitive progression and realistic network modeling:

```
┌─────────────────────────────────────────────────────┐
│         Tier 1: Internet Backbone (Abstract)        │
│  • Not directly hackable                            │
│  • Represents global internet connectivity          │
│  • Used for flavor text and world-building          │
└─────────────────────────────────────────────────────┘
                        │
        ┌───────────────┼───────────────┐
        │               │               │
    ┌───▼────┐     ┌───▼────┐     ┌───▼────┐
    │ ISP A  │     │ ISP B  │     │Darknet │
    │(City A)│     │(City B)│     │ (Tor)  │
    └────┬───┘     └────┬───┘     └────┬───┘
         │              │              │
┌─────────────────────────────────────────────────────┐
│         Tier 2: Regional ISPs & Special Networks    │
│  • Strategic hubs that connect many networks        │
│  • Can be compromised for wide access               │
│  • High monitoring, high risk                       │
│  • Darknet: Special case for underground activities │
└─────────────────────────────────────────────────────┘
         │              │              │
    ┌────┴─────┬────┬───┴─────┬───────┘
    │    │     │    │    │    │
┌───▼─┐ ┌▼───┐ ┌▼───┐ ┌▼──┐ ┌▼───┐ ┌▼──────────┐
│Home │ │Café│ │Corp│ │Gov│ │Bank│ │Underground│
│WiFi │ │WiFi│ │Net │ │Net│ │Net │ │Hacker Grp │
└─────┘ └────┘ └───┬┘ └───┘ └──┬─┘ └───────────┘
                   │           │
                   └─private──┐│
                   connection  ││
                              ┌▼▼┐
┌─────────────────────────────▼──▼────────────────────┐
│         Tier 3: End Networks                        │
│  • Individual organizations, homes, locations       │
│  • Varying security levels                          │
│  • May have direct connections to each other        │
│  • Some air-gapped (no network access)              │
└─────────────────────────────────────────────────────┘
```

### Tier Characteristics

#### Tier 1: Internet Backbone (Abstract)
**Purpose:** World-building and flavor, not directly interactive

- Provides narrative context ("Data travels across global fiber networks")
- Referenced in logs, emails, and technical discussions
- Used to explain cross-city connectivity
- No direct gameplay mechanics

**Design Rationale:** Real internet backbone is too complex and abstract for meaningful gameplay. Simulating it adds no fun, only confusion.

#### Tier 2: ISPs and Special Networks
**Purpose:** Strategic hubs that balance risk vs. reward

**ISP Networks:**
- Connect most consumer and small business networks
- **Gameplay trade-off:** 
  - ✅ Easy access (public service)
  - ✅ Wide visibility (scan all connected networks)
  - ❌ Heavy monitoring (all activity logged)
  - ❌ Karma impact (ISP compromise is serious crime)
  
**Darknet:**
- Special network accessible via Tor/I2P
- Hub for underground hacking groups, black market tools, criminal networks
- Harder to discover, requires specific software or NPC introduction
- Lower detection risk, but attracts different kind of attention (criminal groups)

**Special Networks:**
- Research networks, military networks, intelligence agencies
- May have dedicated infrastructure bypassing public internet
- High security, require special story progression to access

**Design Rationale:** ISPs provide an "easy mode" path for players who want to brute-force progression, but with significant consequences. This creates meaningful player choice: stealth vs. speed.

#### Tier 3: End Networks
**Purpose:** Actual hacking targets, quest objectives, and exploration rewards

**Network Categories:**

1. **Consumer Networks** (Residential, Small Business)
   - Single ISP connection, simple topology
   - 1-5 devices typically
   - Low to medium security
   - Example: Sarah's home network, coffee shop WiFi

2. **Corporate Networks** (Medium-Large Business)
   - ISP connection + possible direct connections to partners
   - 10-50 devices
   - Medium to high security
   - Internal segmentation (guest WiFi, employee network, executive network)
   - Example: TechCorp, local bank branch

3. **High-Security Networks** (Government, Finance, Critical Infrastructure)
   - Multiple redundant connections or air-gapped
   - Dedicated private circuits to trusted partners
   - 30 - 60 devices
   - Very high security with active monitoring
   - Heavily segmented with strict access controls
   - Example: City government network, major bank headquarters

4. **Criminal Networks** (Underground Operations)
   - Darknet-only or highly obscured connections
   - Moderate security (depends on sophistication)
   - Hidden from normal discovery
   - Example: Drug trafficking operation, illegal gambling ring

**Design Rationale:** Network type determines difficulty curve and gameplay style. Consumer networks teach basics, corporate networks provide meat of gameplay, high-security networks are endgame challenges.

---

## Gateway System: Device-Based Network Connections

### Core Concept

Networks don't magically connect to each other. Instead, **specific devices act as gateways** that bridge networks. To access a new network, players must:

1. **Discover** a gateway device exists (through scanning, logs, network diagrams, NPC dialogue)
2. **Locate** the gateway device (may require physical access or finding it on another network)
3. **Compromise** the gateway device (exploit vulnerabilities, steal credentials, social engineering)
4. **Establish connection** through the compromised gateway

This creates clear, device-focused objectives that align with the game's core hacking loop.

### Gateway Types

Each gateway type has unique characteristics and gameplay implications:

#### VPN Server
**Description:** Server that provides encrypted tunnel into a private network

**Access Requirements:**
- VPN client software (usually available)
- Valid credentials (username/password, sometimes + certificate)
- Network connectivity to VPN server

**Discovery Methods:**
- Find VPN config file on compromised device
- Extract credentials from email/password manager
- Social engineering (NPC gives you VPN access)
- Network scan reveals VPN service port

**Gameplay Characteristics:**
- **Pros:** Clean access, designed for remote use, often well-documented
- **Cons:** Requires credentials (must steal/crack), activity may be logged per-user
- **Risk Level:** Medium (logged by user, but encrypted traffic)

**Example Quest Flow:**
```
Quest: "Hack into TechCorp to find evidence"
1. Player befriends Sarah (TechCorp employee)
2. Sarah mentions "I VPN in from home sometimes"
3. Lead created: "TechCorp VPN Access"
4. Player hacks Sarah's home laptop
5. Finds VPN config file with server address
6. Extracts saved password from credentials file
7. Uses VPN to connect to TechCorp network
8. TechCorp devices now scannable/hackable
```

#### Router
**Description:** Network device that forwards traffic between networks

**Access Requirements:**
- Admin credentials OR vulnerability exploit
- Network connectivity to router (must be on one of the networks it bridges)

**Discovery Methods:**
- Default gateway of devices on network (visible in network scan)
- Routing tables on compromised devices
- Physical observation (see physical router in location)

**Gameplay Characteristics:**
- **Pros:** Bridges networks directly, central visibility of traffic
- **Cons:** Heavily logged, compromise often detected quickly, usually hardened
- **Risk Level:** High (critical infrastructure, closely monitored)

**Example Quest Flow:**
```
Quest: "Access the secure government database"
1. Player scans City Hall public WiFi
2. Discovers router at 192.168.1.1
3. Lead created: "City Hall Router - Gateway to Internal Network"
4. Router has firewall, requires exploit or credentials
5. Player finds router manual in IT office (physical infiltration)
6. Manual reveals default credentials (rarely changed)
7. Compromise router, see two networks: PublicWiFi + GovInternal
8. Bridge connection established to GovInternal
```

#### Firewall
**Description:** Security device controlling traffic between trusted/untrusted networks

**Access Requirements:**
- Admin access (very difficult)
- OR exploit in firewall software
- OR misconfigured rules allowing bypass

**Discovery Methods:**
- Network scan shows filtered ports (firewall signature)
- Security policies found in documents
- Network diagrams showing firewall placement

**Gameplay Characteristics:**
- **Pros:** Once compromised, very clean access (designed to pass traffic)
- **Cons:** Extremely difficult to compromise, high-value target
- **Risk Level:** Very High (security team will investigate compromise immediately)

**Example:**
Late-game corporate espionage mission where compromising the firewall is the "stealth" path vs. going through the noisy ISP route.

#### Jump Box / Bastion Host
**Description:** Hardened server specifically designed for remote access into secure network

**Access Requirements:**
- SSH key + valid user account
- Sometimes requires multi-factor authentication
- IP whitelisting (must connect from approved network)

**Discovery Methods:**
- SSH config files referencing jump host
- IT documentation ("Use jumpbox.corp.com to access internal systems")
- Network scan from privileged position

**Gameplay Characteristics:**
- **Pros:** Legitimate remote access path, less suspicious if you have valid credentials
- **Cons:** Strong authentication, logs very detailed, may require MFA
- **Risk Level:** Medium-High (monitored, but designed for remote access)

**Example:**
Player steals SSH key from sysadmin's home computer, uses it to access corporate jump box, which then allows access to internal production servers.

#### VPN Client
**Description:** Regular device (laptop, phone) with VPN client software installed

**Access Requirements:**
- Compromise the device itself
- VPN credentials already saved on device

**Discovery Methods:**
- Compromise NPC's personal device
- Find VPN client in software list

**Gameplay Characteristics:**
- **Pros:** Credentials often saved, piggyback on legitimate user's access
- **Cons:** Limited by when device is online and connected
- **Risk Level:** Low-Medium (activity appears to be legitimate user)

**Example:**
Player compromises Sarah's laptop. Sarah has VPN configured to auto-connect to TechCorp. When Sarah's online and connected, player can route through her device to access TechCorp network.

#### Proxy Server
**Description:** Intermediary server that relays requests

**Access Requirements:**
- May be open (public proxy)
- Or require authentication

**Discovery Methods:**
- Public proxy lists (for open proxies)
- Corporate proxy mentioned in browser configs
- Compromise reveals proxy in network settings

**Gameplay Characteristics:**
- **Pros:** Hides source IP, useful for anonymity
- **Cons:** May log traffic, limited to HTTP/HTTPS typically
- **Risk Level:** Low (designed to relay traffic)

**Example:**
Player chains multiple compromised proxy servers to create anonymous route to target, reducing detection risk.

### Gateway Discovery and Lead System Integration

When players encounter evidence of a gateway, the Lead System automatically creates investigation objectives:

**Discovery Events that Create Leads:**

1. **Network Scan Reveals Gateway:** "Unknown VPN server detected on 192.168.1.50"
2. **File Discovery:** Email mentions "Use the new VPN to access the office from home"
3. **NPC Dialogue:** Sarah says "I hate when the VPN is slow, it takes forever to log in"
4. **Physical Observation:** Player sees router in office during infiltration
5. **Routing Table Analysis:** Compromised device shows routes to unknown network

**Lead Types Created:**

```
Lead Type: Device
Title: "TechCorp VPN Server - Gateway to Internal Network"
Description: "VPN server at vpn.techcorp.com provides remote access to TechCorp 
             internal network. Requires credentials."
Related Quest: "Corporate Espionage: TechCorp"
Discovery Source: Email on Sarah's laptop
Priority: High (blocks quest progress)
```

This creates a natural flow: Exploration → Discovery → Lead → Investigation → Access.

---

## Network Discovery Progression

### Discovery Philosophy: Hybrid Approach

Networks use a **hybrid discovery model** that balances:
- **Story-gated networks:** Main quest networks that unlock at specific story beats
- **Exploration-based networks:** Side networks discovered by exploring, hacking, or talking to NPCs
- **Hidden networks:** Require multiple clues from different sources to discover

This prevents players from accessing endgame networks too early while still rewarding exploration.

### Discovery Methods

#### 1. Physical Proximity Discovery
**How it works:** Player enters a location, nearby WiFi networks become discoverable

**Examples:**
- Walk into coffee shop → "CoffeeBean_Guest" WiFi appears in network list
- Enter TechCorp lobby → "TechCorp_Guest" WiFi visible
- Walk through mall → Multiple store WiFi networks appear

**Implementation:**
- Location has metadata: `AvailableNetworks = ["coffeeshop_guest", "coffeeshop_internal"]`
- When player enters location trigger zone, networks marked as discovered
- Some networks require specific locations: sitting at specific table, standing near router

**Design Rationale:** Mimics real-world WiFi experience. Encourages physical exploration. Some networks (like internal corporate networks) may be physically present but hidden from casual scan.

#### 2. Network Scan Discovery
**How it works:** Player scans a network they're connected to, discovers devices, some devices are gateways to other networks

**Examples:**
- Scan CoffeeShop_Guest network → Discover router device
- Examine router → Routing table shows connection to CoffeeShop_Internal network
- Lead created: "Internal coffee shop network (employee devices)"

**Implementation:**
- NetworkScan operation returns devices on current network
- Router devices include gateway metadata showing bridged networks
- Networks on other side of gateway marked as "discovered but inaccessible"

**Design Rationale:** Rewards technical curiosity. Creates clear objectives (compromise that router to access internal network).

#### 3. Document Discovery
**How it works:** Player finds files, emails, network diagrams that mention or describe networks

**Examples:**
- Email on compromised laptop: "Don't forget to VPN into the office before accessing the database"
  - Discovers: TechCorp_Internal network
  - Creates lead: "TechCorp VPN access"
  
- Network diagram PDF in file system shows corporate network topology
  - Discovers: Multiple internal networks, their relationships
  - Creates leads for each network segment

- IT documentation mentions jump box for remote access
  - Discovers: Production network accessible via jump host

**Implementation:**
- Files have metadata tags: `NetworkReferences = ["techcorp_internal", "techcorp_dmz"]`
- When player reads file, referenced networks marked as discovered
- Lead system creates investigation objectives based on context

**Design Rationale:** Rewards thorough investigation. Makes hacking about information gathering, not just exploits. Provides narrative context for network topology.

#### 4. NPC Dialogue Discovery
**How it works:** NPCs mention networks in conversation, unlocking discovery

**Examples:**
- Sarah: "I have to VPN in tonight to finish the Johnson report"
  - Discovers: TechCorp has VPN access
  - Creates lead: "TechCorp VPN - Sarah has access"
  
- Random NPC in bar: "I work at the power plant. Their network security is a joke."
  - Discovers: PowerCorp network exists
  - Sets security level expectation (low)

- Hacker contact: "If you want the good exploits, you need to get on DarkMarket. Here's the Tor address."
  - Discovers: Darknet network
  - Provides access method

**Implementation:**
- Ink dialogue scripts include commands: `~ NetworkDiscovered("techcorp_internal")`
- Dialogue system notifies Lead Manager
- Relationship level may gate certain network mentions (won't tell you about secret networks unless trusted)

**Design Rationale:** Integrates networks with social gameplay. Makes befriending NPCs mechanically valuable. Provides organic tutorial ("Here's how to access X").

#### 5. Quest-Gated Discovery
**How it works:** Networks automatically discovered when quest reaches specific stage

**Examples:**
- Main Quest 3 starts → Player automatically discovers Government network
  - Rationale: Story requires it, no busywork
  
- Side quest "Corporate Espionage" accepted → TechCorp network discovered
  - Rationale: Quest is about TechCorp, would be frustrating to not know target

**Implementation:**
- Quest definition includes: `OnQuestStart: DiscoverNetworks = ["government_cityA"]`
- Quest manager notifies network service on state change
- Network marked as discovered, but still requires access

**Design Rationale:** Prevents frustration. Some story beats require network access; discovery shouldn't gate progress. Player still has to figure out *how* to access the network.

#### 6. Clue-Based Hidden Network Discovery
**How it works:** Special high-value networks require assembling multiple clues

**Example Flow:**
```
Hidden Network: "Project_Phoenix" (secret government research network)

Clue 1 (Document): "Project Phoenix funding approved" 
  → Player learns network exists
  
Clue 2 (Overheard conversation): "Phoenix servers are at the university"
  → Player learns physical location
  
Clue 3 (Network scan at university): Air-gapped network segment detected
  → Player confirms location
  
Clue 4 (Email on researcher's device): "Phoenix terminal in Lab 7"
  → Player learns exact access point

When all 4 clues found: Network fully discovered, quest unlocks
```

**Implementation:**
```csharp
public class NetworkAccessProfile
{
    public List<DiscoveryRequirement> DiscoveryRequirements;
    public bool IsDiscovered; // Only true when ALL requirements met
}

// Example requirements
var phoenixNetwork = new NetworkAccessProfile
{
    NetworkId = "project_phoenix",
    DiscoveryRequirements = new List<DiscoveryRequirement>
    {
        new ClueDiscoveryRequirement 
        { 
            RequiredClueType = DiscoveryClueType.Document,
            MinimumClues = 2,
            Description = "Find documents mentioning Project Phoenix"
        },
        new PhysicalLocationRequirement
        {
            RequiredLocationId = "university_cs_building",
            Description = "Visit the Computer Science building"
        },
        new DeviceCompromiseRequirement
        {
            RequiredDeviceHostname = "researcher-laptop-03",
            Description = "Compromise Dr. Chen's laptop"
        }
    }
};
```

**Design Rationale:** Creates investigation gameplay. Rewards players who explore thoroughly. Makes important networks feel earned, not given.

---

## Network Access Control

### Access Control Philosophy

Networks use **layered security** that requires players to overcome multiple barriers:

1. **Discovery:** Know the network exists
2. **Access:** Have a method to connect (credentials, gateway, physical access)
3. **Authentication:** Prove identity to network (if required)
4. **Authorization:** Bypass firewall rules, access controls

This creates graduated difficulty and multiple solution paths.

### Access Types

#### Public Access
**Characteristics:**
- No credentials required
- Anyone can connect
- Often restricted (filtered ports, limited bandwidth)
- Heavily monitored

**Examples:**
- Coffee shop guest WiFi
- Library public computers
- Airport WiFi

**Gameplay:**
- Tutorial networks for learning
- Easy access for story progression
- High detection risk discourages abuse

#### VPN Access
**Characteristics:**
- Requires VPN credentials (discovered elsewhere)
- Encrypted connection (harder to intercept)
- User-specific logging (activity tied to credential owner)
- Remote access from anywhere

**Examples:**
- Corporate VPN for remote employees
- University VPN for students
- Government VPN for contractors

**Gameplay:**
- Primary method for accessing corporate networks
- Credential theft is key gameplay loop
- Balances convenience (remote access) with risk (logged per-user)

#### Direct Connection (Compromised Gateway)
**Characteristics:**
- Must compromise specific gateway device
- Only works when on network with gateway
- Cleaner logs (traffic appears internal)
- Limited by network topology

**Examples:**
- Compromise router to access internal network
- Hack firewall to bypass restrictions
- Exploit jump box for production access

**Gameplay:**
- Stealth option (less logged than VPN)
- Requires device hacking skills
- Creates clear objectives (find and hack gateway)

#### Physical Access
**Characteristics:**
- Must physically visit location
- Connect to internal network port
- Very clean (looks like internal employee)
- Requires physical infiltration

**Examples:**
- Plug into Ethernet jack in office
- Use terminal in restricted area
- Access air-gapped system in secure facility

**Gameplay:**
- Highest security networks (government, research)
- Combines stealth/social gameplay with hacking
- Feels like "heist" missions

#### Invitation Access (Darknet)
**Characteristics:**
- Requires introduction from existing member
- Invitation code or key
- Reputation-based

**Examples:**
- Underground hacking forums
- Black market networks
- Exclusive hacker groups

**Gameplay:**
- Social puzzle (find and befriend right NPCs)
- Karma-gated (black hat NPCs won't trust white hat player)
- Special case for criminal storylines

### Connection Priority System

When player has multiple ways to access a network, system auto-selects based on context:

**Priority Rules:**

1. **If physically at location with direct network access:**
   - Use physical connection (most authentic, least suspicious)
   
2. **Else if player has VPN credentials:**
   - Use VPN (convenient, designed for remote use)
   
3. **Else if player has compromised gateway on current network:**
   - Route through compromised gateway (stealth option)
   
4. **Else if player is on ISP that bridges to target:**
   - Route through ISP (easy but monitored)
   
5. **Else:**
   - Show error: "No route to network" with hints about access methods

**Player Override:**
Player can manually select connection method through network management UI:
- Useful for stealth (avoid VPN logs by using compromised router)
- Useful for speed (VPN might be slower than ISP route)
- Useful for learning (see different connection types in action)

---

## Network Segmentation (Large Networks)

### Why Segmentation?

Large organizations don't have flat networks. Internal segmentation creates:
- **Gameplay depth:** Progressive access through organization
- **Realism:** Matches real corporate security practices
- **Story beats:** Different segments have different information/devices
- **Difficulty curve:** Guest WiFi → Employee → Engineering → Executive

### Segmentation Types

#### By Trust Level
```
TechCorp Network Hierarchy:

TechCorp_Guest (Public WiFi)
  ├─ Internet access only
  ├─ No internal device access
  └─ Gateway: None visible
  
TechCorp_Employee (Internal Network)
  ├─ Access to shared drives, printers, common servers
  ├─ Requires VPN or internal connection
  └─ Gateway: VPN server, or internal router from Guest
  
TechCorp_Engineering (Restricted)
  ├─ Access to source code repos, build servers
  ├─ Requires employee badge + VPN, or compromise
  └─ Gateway: Engineering subnet router
  
TechCorp_Executive (Highly Restricted)
  ├─ Access to financial servers, strategic docs
  ├─ Requires executive credentials or physical access
  └─ Gateway: Executive floor network device
```

**Gameplay Flow:**
1. Player connects to TechCorp_Guest (easy, public)
2. Scans network, finds limited devices (printers, guest portals)
3. Discovers router that bridges to TechCorp_Employee
4. Lead created: "Access TechCorp internal employee network"
5. Quest objective: Steal VPN credentials OR compromise router
6. Access unlocks, new devices visible
7. Process repeats for Engineering and Executive segments

#### By Department
```
CityGov Network Segmentation:

CityGov_Public
  └─ Public services, permit applications
  
CityGov_Administrative
  ├─ Email servers, office productivity
  └─ HR, Finance departments
  
CityGov_Police
  ├─ Criminal databases, case files
  └─ Highly sensitive, separate segment
  
CityGov_Infrastructure
  ├─ Traffic control, utilities management
  └─ SCADA systems, physical security
```

**Design Rationale:** Different departments have different information. Player might need police network for one quest, infrastructure network for another. Can't access everything by compromising one device.

#### By Location (Geographic)
```
MegaCorp (Multi-City Corporation):

MegaCorp_HQ (City A)
  └─ Corporate headquarters, C-suite, main servers
  
MegaCorp_Branch_CityB
  └─ Regional office, sales team
  
MegaCorp_Datacenter (Remote)
  └─ Production servers, cloud infrastructure
```

**Design Rationale:** Physical location matters. Player in City A can't physically access City B office. Remote hacking vs. physical infiltration choice.

### Implementation Approach

Rather than making segments separate networks, they're **sub-networks with additional access controls**:

```csharp
public class VirtualNetwork
{
    public string NetworkId;
    public NetworkType Type;
    public NetworkTier Tier; // Consumer, Corporate, HighSecurity, Darknet
    
    // Segmentation
    public string ParentNetworkId; // null if top-level, otherwise parent segment
    public List<string> ChildSegmentIds; // Sub-networks within this network
    public NetworkSegmentType SegmentType; // TrustLevel, Department, Geographic
    
    // Access control
    public NetworkAccessProfile AccessProfile;
    public List<NetworkGateway> Gateways; // Devices that bridge to this segment
}

public enum NetworkSegmentType
{
    None,        // Not segmented, flat network
    TrustLevel,  // Guest → Employee → Restricted → Executive
    Department,  // By organizational unit
    Geographic,  // By physical location
    Hybrid       // Multiple segmentation schemes
}
```

**Visibility Rules:**
- Player can only see devices on segments they have access to
- Parent segment access doesn't grant child access automatically
- Must discover and compromise gateways to each segment

---

## Multi-City Network Architecture

### Cross-City Connectivity

The game features multiple cities (at least two). Networks span cities through:

#### 1. National Organizations
**Concept:** MegaCorp has offices in multiple cities, connected by private network

```
MegaCorp_Global (Logical parent)
  ├─ MegaCorp_CityA_HQ
  ├─ MegaCorp_CityB_Branch
  └─ MegaCorp_Datacenter
```

**Access Patterns:**
- **From CityA office:** Can access CityA devices directly, can VPN to CityB
- **From CityB office:** Can access CityB devices directly, can VPN to CityA
- **Remote (either city):** VPN gives access to both, but latency/quality varies
- **Physical infiltration:** Can only access local city's physical devices

**Gameplay Implications:**
- Same organization, different locations provide variety
- Quests can require information from multiple cities
- Player must travel physically for some objectives (can't hack air-gapped system in City B from City A)

#### 2. ISP Regional Boundaries
**Concept:** Each city has its own regional ISP

```
CityA_ISP (serves City A)
  └─ Connects to CityA consumer/business networks
  
CityB_ISP (serves City B)
  └─ Connects to CityB consumer/business networks
  
Backbone (Abstract)
  └─ Connects regional ISPs
```

**Access Patterns:**
- Player in City A on CityA_ISP can see City A networks easily
- Accessing City B networks requires VPN or cross-ISP routing (possible but slower)
- Some networks may block out-of-region access

**Gameplay Implications:**
- Encourages players to establish bases in both cities
- Can't just hack everything from City A
- Regional ISP compromise gives local advantage

#### 3. Location-Aware Access Control
**Concept:** Some networks/devices only respond to local connections

```
Example: TechCorp_CityA_Executive_Floor

AccessRules:
  - VPN access: View-only (can read files, emails)
  - Physical access: Full control (can modify systems)
  
Rationale: High-security areas require physical presence
```

**Implementation:**
```csharp
public class NetworkAccessProfile
{
    public bool RequiresPhysicalProximity; // For high-security networks
    public List<string> AllowedCityIds; // Geographic restrictions
    public float MaxDistanceMeters; // For location-based WiFi
}

// When player attempts access
public bool CanAccess(Network network, Player player)
{
    if (network.AccessProfile.RequiresPhysicalProximity)
    {
        if (player.Location.CityId != network.PrimaryCityId)
            return false; // Wrong city
            
        if (player.Location.DistanceTo(network.PhysicalLocation) > network.AccessProfile.MaxDistanceMeters)
            return false; // Too far away
    }
    
    return true; // Other checks...
}
```

**Gameplay Implications:**
- Some missions require travel to other city
- Can't solve everything remotely
- Physical infiltration is sometimes only option

---

## Connection Management

### Connection Lifecycle

#### 1. Connection Establishment
**Player initiates connection to target network:**

```
Player Action: "Connect to TechCorp_Internal"
  ↓
System checks available access methods:
  ✓ Has VPN credentials
  ✓ Has compromised router on current network
  ✗ Not physically at location
  ↓
System selects best method (VPN, user overrides to router for stealth)
  ↓
Connection established through router gateway
  ↓
NetworkConnection object created:
  - Source: Current network (CoffeeShop_Guest)
  - Target: TechCorp_Internal
  - Type: Bounce (through compromised device)
  - Gateway: Router at coffee shop
  - Quality: High (good bandwidth, low latency)
  - Encrypted: Yes
  ↓
Target network devices now visible in scans
Player can interact with TechCorp devices
```

#### 2. Connection Maintenance
**Persistent connections with timeout:**

- **Active connection:** Player regularly interacts with target network
  - LastActivity timestamp updated on each action
  - Connection stays alive
  
- **Idle connection:** Player not using connection
  - After 30 minutes of inactivity, connection times out
  - Must reconnect (quick if credentials still valid)
  
- **Broken connection:** Gateway device goes offline
  - Router is rebooted, VPN server crashes
  - Connection automatically lost
  - Player notified: "Connection to TechCorp lost"
  - Must re-establish connection

#### 3. Connection Quality
**Affects gameplay:**

```csharp
public class NetworkConnection
{
    public float Latency; // milliseconds
    public float Bandwidth; // Mbps
    public int PacketLoss; // percentage
    
    public int GetQualityScore() // 0-100
    {
        // Good connection: 100
        // Medium connection: 50-75
        // Poor connection: 0-50
    }
}
```

**Quality Impact:**
- **High quality:** Fast file downloads, no hacking penalty
- **Medium quality:** Slower downloads, minor hacking penalty
- **Low quality:** Very slow, significant hacking penalty, may disconnect

**Quality Factors:**
- Connection path length (more hops = slower)
- Gateway device performance (old router = slow)
- Network congestion (business hours = busy)
- Distance (cross-city = higher latency)

**Gameplay:**
Players seeking cleaner connection will find better gateways or VPN servers, adding technical depth.

### Multiple Active Connections

**Player can maintain multiple connections simultaneously:**

```
Player's Active Connections:
1. Home_Network (always connected)
2. CoffeeShop_Guest (physical proximity)
3. TechCorp_Internal (VPN)
4. Darknet (Tor)
```

**Use Cases:**
- **Monitoring:** Watch multiple networks for events
- **Comparison:** Scan and compare networks side-by-side
- **Chain routing:** Route through Network A to access Network B to access Network C (advanced stealth technique)

**Limitations:**
- Bandwidth shared across connections (many connections = slower)
- Detection risk increases (more activity = more suspicious)
- UI complexity (must clearly show which network is active)

---

## Network Detection & Intrusion Response

### Detection Mechanics

Networks track player activity and respond to suspicious behavior:

#### Detection Accumulation

```
Player Actions Add "Heat":
- Successful hack: +10 heat
- Failed hack attempt: +5 heat
- Multiple scans: +2 heat per scan
- File access: +1 heat per file
- Suspicious time (3 AM): +5 heat multiplier

Heat Dissipation:
- No activity: -1 heat per minute
- Clean actions (normal app usage): -0.5 heat per action
- Complete disconnect: Heat drops to 0 after timeout
```

#### Detection Thresholds

```
Heat Level 0-25: No detection
  - Normal operation
  
Heat Level 26-50: Passive monitoring
  - IDS starts logging activity
  - No immediate response
  
Heat Level 51-75: Active investigation
  - Security team alerted
  - Harder to hack (increased difficulty)
  - Account may be flagged
  
Heat Level 76-100: Active response
  - Connection terminated
  - Account banned
  - All gateways on this network blocked
  - Law enforcement notified (if serious crime)
```

#### Network-Specific Detection

Different networks have different monitoring levels:

**Home Networks:**
- Usually no detection (owner doesn't monitor)
- Exception: Tech-savvy NPC might notice

**Small Business:**
- Basic logging, rarely checked
- Only noticed if major damage

**Corporate:**
- Active IDS, security team monitors
- Will investigate suspicious activity

**Government/High-Security:**
- Advanced IDS, AI-powered anomaly detection
- Immediate response to intrusion
- Persistent tracking (player marked long-term)

### Intrusion Response Actions

When network detects intrusion, various responses:

#### Logging & Monitoring
**Low-key response:**
- Detailed activity logs captured
- May be used as evidence later
- Quest consequences (if caught, quest fails)

#### Account Lockout
**Medium response:**
- Specific user account disabled
- Affects VPN access using those credentials
- Must find new credentials or different access method

#### Gateway Shutdown
**Aggressive response:**
- Gateway device taken offline or reconfigured
- Closes access path
- Must find alternative route

#### Network-Wide Alert
**Nuclear response:**
- All security levels raised
- All devices harder to hack
- May trigger quest failure or story consequences

#### Law Enforcement Notification
**Legal consequences:**
- Police start investigating player
- Heat mechanic across all networks (player is "known")
- May lead to arrest (game over or story event)

### Stealth Strategies

Players can minimize detection:

**Clean Connection Paths:**
- Use compromised gateway instead of stolen VPN (less logged)
- Route through proxy chains (hide source IP)
- Use dormant backdoors (old compromise, not actively monitored)

**Time-Based Actions:**
- Hack during business hours (more traffic, easier to hide)
- Avoid 3 AM hacking (suspicious timing)

**Credential Rotation:**
- Steal multiple accounts, rotate usage
- Distribute activity across credentials

**Network Segmentation Abuse:**
- Hack lower-security segment, pivot to target
- Looks like internal threat, not external

---

## Save System Integration

### Network State Persistence

**What Gets Saved:**

```csharp
public class NetworkSaveData
{
    // Discovery state
    public HashSet<string> DiscoveredNetworks;
    public Dictionary<string, bool> NetworkAccessUnlocked; // NetworkId → Has Access
    
    // Credentials
    public Dictionary<string, List<NetworkCredential>> CredentialsByNetwork;
    
    // Active connections
    public List<NetworkConnectionData> ActiveConnections;
    
    // Gateway compromise state
    public Dictionary<string, GatewayAccessData> CompromisedGateways;
    
    // Detection state
    public Dictionary<string, float> NetworkHeatLevels;
    public Dictionary<string, DateTime> NetworkLockouts; // When lockout expires
    
    // History
    public List<NetworkConnectionHistory> ConnectionHistory;
}
```

**Connection Save Strategy:**

**Option A (Current):** Save active connections
- Restore exact state on load
- Problem: What if gateway device no longer exists? Must validate on load

**Option B (Alternative):** Don't save connections, reconstruct
- On load, player must manually reconnect
- Pro: Simpler, no stale connection issues
- Con: Slightly less seamless

**Chosen Approach:** Save connections with validation
- On load, check if gateway still exists and accessible
- If valid, restore connection
- If invalid, silently drop connection (player reconnects manually)

### Network Topology Evolution

**Static vs. Dynamic:**

**Static Elements (Never Change):**
- Major corporate/government networks
- ISP infrastructure
- Story-critical networks

**Dynamic Elements (Can Change):**
- NPC-owned devices move between networks (following NPC schedule)
- Procedurally generated networks discovered mid-game
- Temporary networks (pop-up shops, events)

**Save Strategy:**
- Save static network configuration once (initial state)
- Save dynamic changes as deltas
- On load, reconstruct by applying deltas to static base

---

## Performance Optimization

### Challenge: 1000+ Devices, 50+ Networks

**Naive approach would be slow:**
- Scanning 1000 devices every frame: 😱
- Checking reachability for every network constantly: 😱
- Updating all network connections every frame: 😱

### Optimization Strategies

#### 1. Spatial Indexing
**Don't check devices player can't reach:**

```
DeviceRegistry maintains indexes:
- devicesByNetwork: Dictionary<NetworkId, List<Device>>
- devicesByCity: Dictionary<CityId, List<Device>>
- devicesByLocation: Dictionary<LocationId, List<Device>>

When player scans current network:
  1. Get player's current network ID
  2. Index lookup: devicesByNetwork[currentNetworkId]
  3. Returns only relevant devices (10-100), not all 1000+
```

#### 2. Lazy Network Discovery
**Don't evaluate all discovery requirements constantly:**

```
Discovery only evaluated when:
- Player enters new location (proximity discovery)
- Player compromises device (scan for gateway references)
- Player reads file (document-based discovery)
- Player completes dialogue (NPC mention)
- Quest state changes (quest-gated discovery)

NOT evaluated:
- Every frame
- On timer
```

#### 3. Connection Pooling
**Don't create/destroy connection objects constantly:**

```
ConnectionPool maintains reusable NetworkConnection objects
- Establish connection: Get from pool or create new
- Disconnect: Return to pool
- Reduces GC pressure
```

#### 4. Cached Reachability
**Don't recalculate routes repeatedly:**

```
NetworkService caches:
- Which networks are reachable from current network
- Best path to reach target network
- Cached for 60 seconds, invalidated on topology change

Player tries to connect to NetworkA:
  1. Check cache: Can I reach NetworkA from here?
  2. Cache hit: Use cached path
  3. Cache miss: Calculate route, cache result
```

#### 5. Event-Driven Topology Updates
**Only recalculate when something changes:**

```
Network topology only rebuilt when:
- Device moves to new location (NPC schedule update)
- Gateway device compromised/lost
- Network discovered/locked
- Save game loaded

NOT rebuilt:
- Every frame
- On timer
- "Just in case"
```

---

## UI/UX Considerations

### Network Visualization

**Network Map UI:**
- Graph visualization showing discovered networks
- Nodes: Networks
- Edges: Gateways connecting networks
- Colors indicate:
  - Green: Accessible
  - Yellow: Discovered but not accessible
  - Red: High security/monitoring
  - Gray: Unknown/undiscovered

**Connection Status:**
- Always-visible indicator showing:
  - Current network
  - Active connections (count)
  - Connection quality (signal bars)
- Click to expand for details

**Network Details Panel:**
- Network name and description
- Security level
- Number of devices
- Access methods available
- Current connection status
- Detection/heat level

### Player Guidance

**Tutorial Flow:**
1. **Home Network:** Player starts connected, familiar safe space
2. **Coffee Shop WiFi:** First external network, teaches public WiFi
3. **Corporate Guest Network:** Introduces segmentation (can see guest, not internal)
4. **VPN Access:** Teach credential theft and VPN connection
5. **Gateway Compromise:** Teach router hacking for stealthy access
6. **Multi-City:** Eventually expand to second city

**Error Messages:**
- ❌ "Cannot connect: No route to network"
  - Hint: "Find a gateway device or VPN credentials"
- ❌ "Connection failed: Invalid credentials"
  - Hint: "Credentials may have been revoked"
- ❌ "Connection lost: Gateway device offline"
  - Hint: "Find an alternative access method"

---

## Integration with Other Systems

### Lead System
**Networks create leads:**
- Discovered network with no access → Lead: "Find way to access X network"
- Gateway device discovered → Lead: "Compromise gateway to access Y network"
- Network mention in file → Lead: "Investigate mentioned network"

### Quest System
**Quests drive network access:**
- Quest requires specific network → Discovery auto-triggered
- Quest objective: "Access X network" → Player must figure out how
- Quest reward: VPN credentials → New network becomes accessible

### NPC System
**NPCs bridge player to networks:**
- Befriend corporate employee → Learn about company network
- High trust → NPC shares VPN credentials
- NPC schedule → Their devices move between networks, changing topology

### Device System
**Devices are network members:**
- Device compromise reveals network structure
- Gateway devices bridge networks
- Device movement (with NPC) changes network topology

### Time System
**Time affects networks:**
- Business hours: More devices online, more traffic (easier to hide)
- After hours: Fewer devices, less traffic (more suspicious)
- Maintenance windows: Security temporarily lowered

### Karma/Heat System
**Actions have consequences:**
- ISP compromise: Massive karma hit, permanent heat increase
- Corporate network intrusion: Medium karma hit, temporary heat
- Reading files: Minor karma impact
- Destructive actions: Major karma hit, law enforcement notification

---

## Example: Complete Network Hacking Flow

### Scenario: Access TechCorp's confidential research data

**Step 1: Discovery**
```
Player talks to Sarah (NPC, works at TechCorp)
Sarah: "I've been working late on the Prometheus project..."

→ Lead created: "Prometheus Project - TechCorp Research"
→ TechCorp_Internal network discovered
→ Quest unlocked: "Corporate Espionage: Steal Prometheus Data"
```

**Step 2: Investigation**
```
Player checks network map:
  - TechCorp_Internal exists
  - Status: Discovered, not accessible
  - Possible access methods:
    • VPN (need credentials)
    • Physical access (go to TechCorp office)
    • Gateway device (need to find and compromise)

Player decides to pursue VPN credentials (remote access)
```

**Step 3: Credential Acquisition**
```
Player befriends Sarah (conversation, trust building)
Trust level reaches "Friend"

→ Sarah invites player to her apartment for dinner
→ Player visits Sarah's home

While at apartment:
  - Discovers Sarah's laptop on home network
  - Lead created: "Sarah's Laptop - May contain TechCorp access"
  
Player hacks Sarah's laptop (while she's in other room):
  - Finds VPN config file: techcorp_vpn.ovpn
  - Finds saved credentials: sarah.chen / [password]
  - VPN access to TechCorp_Internal acquired!
```

**Step 4: Network Access**
```
Player (back at home):
  - Opens network manager
  - Selects "Connect to TechCorp_Internal"
  - System uses VPN credentials (auto-selected)
  - Connection established (via VPN)

Connection Details:
  - Source: Player_Home
  - Target: TechCorp_Internal
  - Type: VPN
  - Quality: Good (low latency, encrypted)
  - Detection Risk: Medium (logged as Sarah's access)
```

**Step 5: Network Exploration**
```
Player scans TechCorp_Internal:
  - Discovers 45 devices
  - Servers, workstations, printers
  - One device labeled: "prometheus-research-01"

→ Lead updated: "Prometheus research server identified"
→ Quest objective: "Access prometheus-research-01"
```

**Step 6: Data Exfiltration**
```
Player attempts to hack prometheus-research-01:
  - Security level: High
  - Requires additional privileges

Player searches for credentials on Sarah's workstation:
  - Finds email with temp research server password
  - Uses password to authenticate

Successfully access research server:
  - Downloads Prometheus project files
  - Quest completed!

Detection:
  - Heat level increased to 45 (medium)
  - Sarah's account flagged for unusual file access
  - Security team will investigate in 24 game-hours
```

**Step 7: Consequences**
```
Next day (game time):
  - Sarah's VPN credentials revoked
  - Player loses TechCorp access via VPN
  - Sarah confronts player (dialogue choice: admit or lie)
  - Relationship impact: Trust decreased
  - Story branches based on player's response

Long-term:
  - TechCorp security improved (harder to hack)
  - Alternative access methods needed for future missions
  - Data obtained unlocks new quest line
```

---

## Design Rationale Summary

### Why This Architecture?

**Gateway-Focused:**
- Creates clear objectives (find and hack specific devices)
- Aligns with core device hacking gameplay
- More accessible than abstract routing concepts

**Hierarchical Structure:**
- Natural difficulty progression
- Intuitive mental model (simple → complex)
- Scales well across 40+ hour game

**Hybrid Discovery:**
- Story networks prevent wandering
- Exploration networks reward curiosity
- Hidden networks provide depth for engaged players

**Multiple Access Paths:**
- Player choice (stealth vs. speed, risk vs. reward)
- Replayability (different approaches each playthrough)
- Supports different playstyles (social vs. technical)

**Context-Aware Access:**
- Reduces friction (auto-selects sensible connection)
- Maintains depth (player can override for strategy)
- Teaches networking concepts organically

**Performance-Friendly:**
- Supports massive scale (1000+ devices, 50+ networks)
- Event-driven updates (no wasteful polling)
- Smart caching and indexing

**Integration-Ready:**
- Works seamlessly with Quests, Leads, NPCs, Devices
- Progression Coordinator prevents tight coupling
- Events enable loose communication between systems

### Future Expansion Opportunities

**Potential additions that fit this architecture:**

1. **Player-Created Networks:**
   - Set up own VPN for anonymity
   - Create botnet from compromised devices
   - Build proxy chains for stealth

2. **Dynamic Network Events:**
   - Security updates that break exploits
   - Network mergers (companies acquired)
   - Infrastructure failures (ISP outage affects multiple networks)

3. **Network Warfare:**
   - Competing hackers on same network
   - DDoS attacks (temporarily disable networks)
   - Counter-intrusion (defend your networks)

4. **Advanced Routing:**
   - Manual route selection (choose specific path)
   - Traffic shaping (prioritize certain connections)
   - Connection quality optimization mini-game

All additions would layer on top of existing architecture without requiring fundamental changes.

---

## Conclusion

The Network System provides the foundation for the game's hacking gameplay by modeling realistic network topology and access control while maintaining clear objectives and player agency. The hybrid Gateway-Hierarchical architecture balances depth and accessibility, creating a system that scales from tutorial coffee shop WiFi to endgame government networks. Integration with Quest, Lead, NPC, and Device systems ensures networks feel like living parts of the game world, not just abstract containers for devices.

The focus on **device-based gateways** creates tangible objectives ("hack that router"), while the **hierarchical structure** provides natural progression and strategic depth. **Multiple access paths** support different playstyles and create meaningful choices. **Discovery systems** reward exploration without overwhelming players with options.

This architecture supports the game's vision: a Stardew Valley-style life sim where hacking is farming, networks are crops, and every connection tells a story.
