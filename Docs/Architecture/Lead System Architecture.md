# Lead System Architecture

## Overview

The Lead System manages **emergent objectives** that guide player investigation and discovery. Leads are automatically generated when players discover devices, hear about mysterious people, find strange files, or uncover plot hooks. The system creates a living "investigation board" that connects devices, NPCs, locations, and quests.

**Key Design Principles:**

- **Emergent discovery**: Leads auto-generate from player actions
- **Player agency**: Players can pin/unpin, prioritize leads
- **Quest integration**: Leads link to quests bidirectionally
- **Hybrid event + validation**: Event-driven with fallback polling
- **Investigation board UI**: Visual cork board with red string connections

---

## Core Architecture

### Component Hierarchy

```
LeadManager (ILeadManager) - Service
  ├── Manages all lead data (active, resolved, ignored)
  ├── Auto-generates leads from events (device discovery, dialogue)
  ├── Validates no leads were missed (fallback polling)
  ├── Links leads to quests, NPCs, devices, locations
  └── Prioritizes and categorizes leads

ProgressionCoordinator
  ├── Orchestrates Lead ↔ Quest interaction
  ├── Updates leads when quests progress
  └── Checks if leads unlock new quests

InvestigationBoardUI
  ├── Visual representation of lead network
  ├── Shows connections between leads
  └── Allows player to pin/unpin, ignore leads
```

---

## Service Interface

```csharp
public interface ILeadManager : IGameService
{
    // === Lead Creation ===
    Lead CreateLead(LeadType type, string title, string description, LeadSource source);
    Lead CreateLeadFromDevice(Device device);
    Lead CreateLeadFromNPC(string npcId, string context);
    Lead CreateLeadFromFile(VirtualNode file, string deviceId);
    
    // === Lead State ===
    Lead GetLead(string leadId);
    List<Lead> GetActiveLeads();
    List<Lead> GetResolvedLeads();
    List<Lead> GetPinnedLeads();
    List<Lead> GetLeadsByType(LeadType type);
    List<Lead> GetLeadsByPriority(LeadPriority priority);
    
    // === Lead Management ===
    void ResolveLead(string leadId);
    void IgnoreLead(string leadId);
    void PinLead(string leadId, bool isPinned);
    void UpdateLeadProgress(string leadId, float progress);
    
    // === Relationships ===
    void LinkLeadToQuest(string leadId, string questId);
    void LinkLeadToNPC(string leadId, string npcId);
    void LinkLeadToDevice(string leadId, string deviceId);
    void LinkLeadToLead(string leadId1, string leadId2);
    List<Quest> GetRelatedQuests(string leadId);
    
    // === Quest Integration ===
    void UpdateLeadsRelatedToQuest(Quest quest);
    
    // === Events ===
    event Action<Lead> OnLeadCreated;
    event Action<Lead> OnLeadResolved;
    event Action<Lead> OnLeadPinned;
    event Action<Lead, Lead> OnLeadsConnected;
}
```

---

## Data Structures

### Lead

```csharp
[System.Serializable]
public class Lead
{
    public string LeadId;
    public string Title;
    public string Description;
    public LeadType Type;
    public LeadPriority Priority;
    
    // === Discovery ===
    public LeadSource Source;           // Where did this come from?
    public DateTime DiscoveredAt;
    public bool IsPlayerPinned;         // Did player manually pin this?
    public bool IsAutoPinned;           // System pinned (critical leads)
    
    // === Relationships ===
    public List<string> RelatedQuestIds;    // Quests this lead is part of
    public List<string> RelatedDeviceIds;   // Devices involved
    public List<string> RelatedNPCIds;      // NPCs involved
    public List<string> RelatedLeadIds;     // Other connected leads
    public List<string> RelatedLocationIds; // Locations involved
    
    // === Investigation State ===
    public LeadState State = LeadState.Active;
    public float InvestigationProgress; // 0-1, how much player has explored this
    public DateTime ResolvedAt;
    
    // === Visual/Metadata ===
    public Sprite Icon;
    public Color CategoryColor;
    public string Notes; // Player-editable notes (future feature)
}

public enum LeadType
{
    Device,              // "Found unknown server on coffee shop network"
    Person,              // "Sarah mentioned someone named 'Phoenix'"
    Location,            // "Overheard mention of underground club"
    Mystery,             // "Strange encrypted file on compromised PC"
    Opportunity,         // "Job opening at BigTechCorp"
    Threat,              // "Someone is counter-hacking you"
    Information          // "Password database leak for small town"
}

public enum LeadPriority
{
    Critical,   // Auto-pinned, glowing red (main story beats)
    High,       // Important for progression (side quests)
    Medium,     // Interesting content (exploration)
    Low         // Flavor/optional (world-building)
}

public enum LeadState
{
    Active,     // Currently being investigated
    Resolved,   // Investigation complete
    Ignored     // Player marked as uninteresting
}

[System.Serializable]
public class LeadSource
{
    public LeadSourceType Type;
    public string SourceId;     // ID of the device/NPC/location that generated this
    public string SourceName;   // Human-readable name
    public DateTime Timestamp;  // When was this lead discovered
}

public enum LeadSourceType
{
    NetworkScan,        // From scanning a network
    DeviceExploit,      // From hacking a device
    FileDiscovery,      // From reading a file
    Dialogue,           // From NPC conversation
    QuestObjective,     // From completing a quest objective
    Environmental,      // From examining world objects
    Email,              // From receiving/reading email
    Manual              // Player manually created (future feature)
}
```

---

## Lead Manager Implementation

```csharp
public class LeadManager : ILeadManager
{
    private Dictionary<string, Lead> allLeads;
    private List<Lead> activeLeads;
    private List<Lead> resolvedLeads;
    
    private IDeviceRegistry deviceRegistry;
    private INPCManager npcManager;
    private IQuestManager questManager;
    private ITimeManager timeManager;
    
    // Tracking for validation sweep
    private HashSet<string> processedDevices = new HashSet<string>();
    private HashSet<string> processedFiles = new HashSet<string>();
    
    // === Events ===
    public event Action<Lead> OnLeadCreated;
    public event Action<Lead> OnLeadResolved;
    public event Action<Lead> OnLeadPinned;
    public event Action<Lead, Lead> OnLeadsConnected;
    
    // === Initialization ===
    
    public void Initialize()
    {
        deviceRegistry = ServiceLocator.Get<IDeviceRegistry>();
        npcManager = ServiceLocator.Get<INPCManager>();
        questManager = ServiceLocator.Get<IQuestManager>();
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        allLeads = new Dictionary<string, Lead>();
        activeLeads = new List<Lead>();
        resolvedLeads = new List<Lead>();
        
        // Subscribe to game events (primary lead generation)
        SubscribeToGameEvents();
        
        // Start validation sweep (fallback)
        ServiceLocator.Get<IUpdateService>()?.RegisterUpdate(ValidationSweep);
        
        Debug.Log("[LeadManager] Initialized");
    }
    
    private void SubscribeToGameEvents()
    {
        GameEvents.Subscribe(GameEventType.DeviceDiscovered, OnDeviceDiscovered);
        GameEvents.Subscribe(GameEventType.DeviceCompromised, OnDeviceCompromised);
        GameEvents.Subscribe(GameEventType.FileDiscovered, OnFileDiscovered);
        GameEvents.Subscribe(GameEventType.NPCMentioned, OnNPCMentioned);
        GameEvents.Subscribe(GameEventType.LocationDiscovered, OnLocationDiscovered);
        GameEvents.Subscribe(GameEventType.MysteryEncountered, OnMysteryEncountered);
    }
    
    // === Lead Creation ===
    
    public Lead CreateLead(LeadType type, string title, string description, LeadSource source)
    {
        var lead = new Lead
        {
            LeadId = System.Guid.NewGuid().ToString(),
            Title = title,
            Description = description,
            Type = type,
            Source = source,
            DiscoveredAt = timeManager.CurrentGameTime,
            Priority = DeterminePriority(type, source),
            State = LeadState.Active,
            CategoryColor = GetColorForType(type),
            Icon = GetIconForType(type)
        };
        
        // Auto-pin critical leads
        if (lead.Priority == LeadPriority.Critical)
        {
            lead.IsAutoPinned = true;
        }
        
        // Store lead
        allLeads[lead.LeadId] = lead;
        activeLeads.Add(lead);
        
        Debug.Log($"[LeadManager] Created lead: {lead.Title} (Priority: {lead.Priority})");
        
        // Emit events
        OnLeadCreated?.Invoke(lead);
        GameEvents.Publish(GameEventType.LeadDiscovered, lead);
        
        // Check if this lead unlocks any quests
        questManager?.CheckIfLeadUnlocksQuests(lead);
        
        return lead;
    }
    
    public Lead CreateLeadFromDevice(Device device)
    {
        // Check if already processed
        if (processedDevices.Contains(device.DeviceId))
            return null;
        
        processedDevices.Add(device.DeviceId);
        
        var lead = CreateLead(
            LeadType.Device,
            $"Unknown Device: {device.Hostname}",
            BuildDeviceDescription(device),
            new LeadSource 
            { 
                Type = LeadSourceType.NetworkScan, 
                SourceId = device.DeviceId,
                SourceName = device.Hostname,
                Timestamp = timeManager.CurrentGameTime
            }
        );
        
        lead.RelatedDeviceIds.Add(device.DeviceId);
        LinkLeadToQuests(lead);
        
        return lead;
    }
    
    public Lead CreateLeadFromNPC(string npcId, string context)
    {
        var npc = npcManager.GetNPC(npcId);
        if (npc == null) return null;
        
        var lead = CreateLead(
            LeadType.Person,
            $"Person of Interest: {npc.Name}",
            context,
            new LeadSource 
            { 
                Type = LeadSourceType.Dialogue, 
                SourceId = npcId,
                SourceName = npc.Name,
                Timestamp = timeManager.CurrentGameTime
            }
        );
        
        lead.RelatedNPCIds.Add(npcId);
        LinkLeadToQuests(lead);
        
        return lead;
    }
    
    public Lead CreateLeadFromFile(VirtualNode file, string deviceId)
    {
        // Check if already processed
        string fileKey = $"{deviceId}:{file.Path}";
        if (processedFiles.Contains(fileKey))
            return null;
        
        processedFiles.Add(fileKey);
        
        var device = deviceRegistry.GetDevice(deviceId);
        
        var lead = CreateLead(
            LeadType.Information,
            $"Interesting File: {file.Name}",
            $"Found {file.Name} on {device?.Hostname ?? "unknown device"}. Contains information about...",
            new LeadSource 
            { 
                Type = LeadSourceType.FileDiscovery, 
                SourceId = deviceId,
                SourceName = device?.Hostname ?? "unknown",
                Timestamp = timeManager.CurrentGameTime
            }
        );
        
        lead.RelatedDeviceIds.Add(deviceId);
        LinkLeadToQuests(lead);
        
        return lead;
    }
    
    private string BuildDeviceDescription(Device device)
    {
        return $"Found {device.DeviceType} on {device.CurrentNetworkId ?? "unknown network"}.\n" +
               $"Running {device.OS.Name}.\n" +
               $"Security: {device.SecurityLevel}.\n" +
               $"Location: {device.CurrentLocation?.Name ?? "Unknown"}";
    }
    
    // === Priority Determination ===
    
    private LeadPriority DeterminePriority(LeadType type, LeadSource source)
    {
        // Critical: Main story devices, quest-giver NPCs
        if (source.Type == LeadSourceType.QuestObjective)
            return LeadPriority.Critical;
        
        if (type == LeadType.Threat)
            return LeadPriority.Critical;
        
        // High: Important NPCs, key devices
        if (type == LeadType.Person || type == LeadType.Opportunity)
            return LeadPriority.High;
        
        // Medium: Most devices, locations
        if (type == LeadType.Device || type == LeadType.Location)
            return LeadPriority.Medium;
        
        // Low: Everything else
        return LeadPriority.Low;
    }
    
    private Color GetColorForType(LeadType type)
    {
        return type switch
        {
            LeadType.Device => new Color(0.5f, 0.7f, 1f),      // Light blue
            LeadType.Person => new Color(1f, 0.8f, 0.5f),      // Orange
            LeadType.Location => new Color(0.5f, 1f, 0.5f),    // Green
            LeadType.Mystery => new Color(0.8f, 0.5f, 1f),     // Purple
            LeadType.Opportunity => new Color(1f, 1f, 0.5f),   // Yellow
            LeadType.Threat => new Color(1f, 0.3f, 0.3f),      // Red
            LeadType.Information => new Color(0.7f, 0.7f, 0.7f), // Gray
            _ => Color.white
        };
    }
    
    private Sprite GetIconForType(LeadType type)
    {
        // Load from Resources based on type
        return Resources.Load<Sprite>($"Icons/Leads/{type}");
    }
    
    // === Event Handlers ===
    
    private void OnDeviceDiscovered(object data)
    {
        var device = data as Device;
        CreateLeadFromDevice(device);
    }
    
    private void OnDeviceCompromised(object data)
    {
        var device = data as Device;
        
        // Find lead for this device
        var lead = activeLeads.FirstOrDefault(l => 
            l.RelatedDeviceIds.Contains(device.DeviceId));
        
        if (lead != null)
        {
            // Update lead description
            lead.Description += "\n\n✓ Device compromised! Access granted.";
            lead.InvestigationProgress = 1.0f;
        }
    }
    
    private void OnFileDiscovered(object data)
    {
        var fileData = data as FileDiscoveryData;
        CreateLeadFromFile(fileData.File, fileData.DeviceId);
    }
    
    private void OnNPCMentioned(object data)
    {
        var mentionData = data as NPCMentionData;
        CreateLeadFromNPC(mentionData.NpcId, mentionData.Context);
    }
    
    private void OnLocationDiscovered(object data)
    {
        var location = data as PhysicalLocation;
        
        var lead = CreateLead(
            LeadType.Location,
            $"Location: {location.Name}",
            $"Discovered {location.Name}. {location.Description}",
            new LeadSource 
            { 
                Type = LeadSourceType.Environmental, 
                SourceId = location.LocationId,
                SourceName = location.Name,
                Timestamp = timeManager.CurrentGameTime
            }
        );
        
        lead.RelatedLocationIds.Add(location.LocationId);
    }
    
    private void OnMysteryEncountered(object data)
    {
        var mystery = data as MysteryData;
        
        CreateLead(
            LeadType.Mystery,
            mystery.Title,
            mystery.Description,
            new LeadSource 
            { 
                Type = mystery.SourceType, 
                SourceId = mystery.SourceId,
                SourceName = mystery.SourceName,
                Timestamp = timeManager.CurrentGameTime
            }
        );
    }
    
    // === Validation Sweep (Fallback) ===
    
    private float timeSinceLastSweep = 0f;
    private const float SWEEP_INTERVAL = 60f; // Once per minute
    
    private void ValidationSweep(float deltaTime)
    {
        timeSinceLastSweep += deltaTime;
        
        if (timeSinceLastSweep >= SWEEP_INTERVAL)
        {
            RunValidationSweep();
            timeSinceLastSweep = 0f;
        }
    }
    
    private void RunValidationSweep()
    {
        int missedLeads = 0;
        
        // Check for devices we missed
        var allDevices = deviceRegistry.GetAllDevices();
        foreach (var device in allDevices)
        {
            if (!processedDevices.Contains(device.DeviceId))
            {
                Debug.LogWarning($"[LeadManager] Caught missed device: {device.Hostname}");
                CreateLeadFromDevice(device);
                missedLeads++;
            }
        }
        
        if (missedLeads > 0)
        {
            Debug.LogWarning($"[LeadManager] Validation sweep created {missedLeads} missed leads");
        }
    }
    
    // === Lead Management ===
    
    public void ResolveLead(string leadId)
    {
        var lead = GetLead(leadId);
        if (lead == null || lead.State != LeadState.Active) return;
        
        lead.State = LeadState.Resolved;
        lead.ResolvedAt = timeManager.CurrentGameTime;
        
        activeLeads.Remove(lead);
        resolvedLeads.Add(lead);
        
        Debug.Log($"[LeadManager] Lead resolved: {lead.Title}");
        OnLeadResolved?.Invoke(lead);
    }
    
    public void IgnoreLead(string leadId)
    {
        var lead = GetLead(leadId);
        if (lead == null || lead.State != LeadState.Active) return;
        
        lead.State = LeadState.Ignored;
        activeLeads.Remove(lead);
        
        Debug.Log($"[LeadManager] Lead ignored: {lead.Title}");
    }
    
    public void PinLead(string leadId, bool isPinned)
    {
        var lead = GetLead(leadId);
        if (lead == null) return;
        
        lead.IsPlayerPinned = isPinned;
        
        Debug.Log($"[LeadManager] Lead {(isPinned ? "pinned" : "unpinned")}: {lead.Title}");
        OnLeadPinned?.Invoke(lead);
    }
    
    public void UpdateLeadProgress(string leadId, float progress)
    {
        var lead = GetLead(leadId);
        if (lead == null) return;
        
        lead.InvestigationProgress = Mathf.Clamp01(progress);
        
        // Auto-resolve if progress reaches 100%
        if (lead.InvestigationProgress >= 1f)
        {
            ResolveLead(leadId);
        }
    }
    
    // === Relationships ===
    
    public void LinkLeadToQuest(string leadId, string questId)
    {
        var lead = GetLead(leadId);
        if (lead == null) return;
        
        if (!lead.RelatedQuestIds.Contains(questId))
        {
            lead.RelatedQuestIds.Add(questId);
            Debug.Log($"[LeadManager] Linked lead '{lead.Title}' to quest '{questId}'");
        }
    }
    
    public void LinkLeadToNPC(string leadId, string npcId)
    {
        var lead = GetLead(leadId);
        if (lead == null) return;
        
        if (!lead.RelatedNPCIds.Contains(npcId))
        {
            lead.RelatedNPCIds.Add(npcId);
        }
    }
    
    public void LinkLeadToDevice(string leadId, string deviceId)
    {
        var lead = GetLead(leadId);
        if (lead == null) return;
        
        if (!lead.RelatedDeviceIds.Contains(deviceId))
        {
            lead.RelatedDeviceIds.Add(deviceId);
        }
    }
    
    public void LinkLeadToLead(string leadId1, string leadId2)
    {
        var lead1 = GetLead(leadId1);
        var lead2 = GetLead(leadId2);
        if (lead1 == null || lead2 == null) return;
        
        if (!lead1.RelatedLeadIds.Contains(leadId2))
        {
            lead1.RelatedLeadIds.Add(leadId2);
        }
        
        if (!lead2.RelatedLeadIds.Contains(leadId1))
        {
            lead2.RelatedLeadIds.Add(leadId1);
        }
        
        OnLeadsConnected?.Invoke(lead1, lead2);
    }
    
    private void LinkLeadToQuests(Lead lead)
    {
        // Check if any active quests care about this lead
        foreach (var quest in questManager.GetActiveQuests())
        {
            bool isRelated = false;
            
            foreach (var objective in quest.Objectives)
            {
                // Check if objective references this lead's devices/NPCs/locations
                if (lead.RelatedDeviceIds.Any(id => objective.RelatedLeadIds.Contains(id)) ||
                    lead.RelatedNPCIds.Any(id => objective.RelatedLeadIds.Contains(id)))
                {
                    isRelated = true;
                    break;
                }
            }
            
            if (isRelated)
            {
                LinkLeadToQuest(lead.LeadId, quest.QuestId);
            }
        }
    }
    
    public List<Quest> GetRelatedQuests(string leadId)
    {
        var lead = GetLead(leadId);
        if (lead == null) return new List<Quest>();
        
        return lead.RelatedQuestIds
            .Select(id => questManager.GetQuest(id))
            .Where(q => q != null)
            .ToList();
    }
    
    // === Quest Integration ===
    
    public void UpdateLeadsRelatedToQuest(Quest quest)
    {
        // Find leads related to this quest
        var relatedLeads = activeLeads.Where(l => l.RelatedQuestIds.Contains(quest.QuestId));
        
        foreach (var lead in relatedLeads)
        {
            // Update lead description with quest info
            if (!lead.Description.Contains(quest.QuestName))
            {
                lead.Description += $"\n\n📋 Related Quest: {quest.QuestName}";
            }
            
            // Calculate progress based on quest objectives
            float progress = CalculateLeadProgressFromQuest(lead, quest);
            UpdateLeadProgress(lead.LeadId, progress);
        }
    }
    
    private float CalculateLeadProgressFromQuest(Lead lead, Quest quest)
    {
        // Count completed objectives that reference this lead
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
    
    // === Queries ===
    
    public Lead GetLead(string leadId)
    {
        allLeads.TryGetValue(leadId, out var lead);
        return lead;
    }
    
    public List<Lead> GetActiveLeads() => activeLeads.ToList();
    public List<Lead> GetResolvedLeads() => resolvedLeads.ToList();
    
    public List<Lead> GetPinnedLeads() => activeLeads
        .Where(l => l.IsPlayerPinned || l.IsAutoPinned)
        .OrderByDescending(l => l.Priority)
        .ToList();
    
    public List<Lead> GetLeadsByType(LeadType type) => activeLeads
        .Where(l => l.Type == type)
        .ToList();
    
    public List<Lead> GetLeadsByPriority(LeadPriority priority) => activeLeads
        .Where(l => l.Priority == priority)
        .ToList();
}
```

---

## Investigation Board UI

### Implementation

```csharp
public class InvestigationBoardUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject leadCardPrefab;
    [SerializeField] private Transform criticalLeadsContainer;
    [SerializeField] private Transform pinnedLeadsContainer;
    [SerializeField] private Transform devicesContainer;
    [SerializeField] private Transform peopleContainer;
    [SerializeField] private Transform resolvedContainer;
    [SerializeField] private LineRenderer connectionLinePrefab;
    
    private ILeadManager leadManager;
    private Dictionary<string, GameObject> leadCards;
    private List<LineRenderer> connectionLines;
    
    private void Start()
    {
        leadManager = ServiceLocator.Get<ILeadManager>();
        leadCards = new Dictionary<string, GameObject>();
        connectionLines = new List<LineRenderer>();
        
        // Subscribe to events
        leadManager.OnLeadCreated += OnLeadCreated;
        leadManager.OnLeadResolved += OnLeadResolved;
        leadManager.OnLeadsConnected += OnLeadsConnected;
        
        RefreshBoard();
    }
    
    private void RefreshBoard()
    {
        // Clear existing
        foreach (var card in leadCards.Values)
        {
            Destroy(card);
        }
        leadCards.Clear();
        
        ClearConnectionLines();
        
        // Populate board
        PopulateCriticalLeads();
        PopulatePinnedLeads();
        PopulateLeadsByType();
        
        // Draw connections
        DrawConnectionLines();
    }
    
    private void PopulateCriticalLeads()
    {
        var criticalLeads = leadManager.GetLeadsByPriority(LeadPriority.Critical);
        
        foreach (var lead in criticalLeads)
        {
            CreateLeadCard(lead, criticalLeadsContainer);
        }
    }
    
    private void PopulatePinnedLeads()
    {
        var pinnedLeads = leadManager.GetPinnedLeads()
            .Where(l => l.Priority != LeadPriority.Critical); // Don't duplicate critical
        
        foreach (var lead in pinnedLeads)
        {
            CreateLeadCard(lead, pinnedLeadsContainer);
        }
    }
    
    private void PopulateLeadsByType()
    {
        // Devices
        var deviceLeads = leadManager.GetLeadsByType(LeadType.Device)
            .Where(l => !l.IsPlayerPinned && !l.IsAutoPinned && l.Priority != LeadPriority.Critical);
        
        foreach (var lead in deviceLeads)
        {
            CreateLeadCard(lead, devicesContainer);
        }
        
        // People
        var personLeads = leadManager.GetLeadsByType(LeadType.Person)
            .Where(l => !l.IsPlayerPinned && !l.IsAutoPinned && l.Priority != LeadPriority.Critical);
        
        foreach (var lead in personLeads)
        {
            CreateLeadCard(lead, peopleContainer);
        }
    }
    
    private GameObject CreateLeadCard(Lead lead, Transform parent)
    {
        var card = Instantiate(leadCardPrefab, parent);
        var cardUI = card.GetComponent<LeadCardUI>();
        cardUI.SetLead(lead);
        
        leadCards[lead.LeadId] = card;
        return card;
    }
    
    private void DrawConnectionLines()
    {
        var allLeads = leadManager.GetActiveLeads();
        
        foreach (var lead in allLeads)
        {
            foreach (var relatedLeadId in lead.RelatedLeadIds)
            {
                if (leadCards.TryGetValue(lead.LeadId, out var card1) &&
                    leadCards.TryGetValue(relatedLeadId, out var card2))
                {
                    DrawConnectionLine(card1.transform.position, card2.transform.position);
                }
            }
        }
    }
    
    private void DrawConnectionLine(Vector3 from, Vector3 to)
    {
        var line = Instantiate(connectionLinePrefab);
        line.SetPosition(0, from);
        line.SetPosition(1, to);
        line.startColor = new Color(1f, 0.2f, 0.2f, 0.5f); // Red string
        line.endColor = new Color(1f, 0.2f, 0.2f, 0.5f);
        line.startWidth = 0.05f;
        line.endWidth = 0.05f;
        
        connectionLines.Add(line);
    }
    
    private void ClearConnectionLines()
    {
        foreach (var line in connectionLines)
        {
            Destroy(line.gameObject);
        }
        connectionLines.Clear();
    }
    
    private void OnLeadCreated(Lead lead)
    {
        // Determine where to place the card
        Transform parent = lead.Priority switch
        {
            LeadPriority.Critical => criticalLeadsContainer,
            _ when lead.IsPlayerPinned || lead.IsAutoPinned => pinnedLeadsContainer,
            _ when lead.Type == LeadType.Device => devicesContainer,
            _ when lead.Type == LeadType.Person => peopleContainer,
            _ => pinnedLeadsContainer // Default
        };
        
        CreateLeadCard(lead, parent);
        
        // Animate card appearance
        var card = leadCards[lead.LeadId];
        card.GetComponent<Animator>()?.SetTrigger("Appear");
    }
    
    private void OnLeadResolved(Lead lead)
    {
        if (leadCards.TryGetValue(lead.LeadId, out var card))
        {
            // Move to resolved section
            card.transform.SetParent(resolvedContainer);
            
            // Update visual
            var cardUI = card.GetComponent<LeadCardUI>();
            cardUI.MarkAsResolved();
        }
        
        // Redraw connections
        ClearConnectionLines();
        DrawConnectionLines();
    }
    
    private void OnLeadsConnected(Lead lead1, Lead lead2)
    {
        // Draw new connection
        if (leadCards.TryGetValue(lead1.LeadId, out var card1) &&
            leadCards.TryGetValue(lead2.LeadId, out var card2))
        {
            DrawConnectionLine(card1.transform.position, card2.transform.position);
        }
    }
}
```

### Lead Card UI Component

```csharp
public class LeadCardUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI titleText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;
    [SerializeField] private Image backgroundImage;
    [SerializeField] private Slider progressSlider;
    [SerializeField] private Button pinButton;
    [SerializeField] private Button detailsButton;
    
    private Lead lead;
    private ILeadManager leadManager;
    
    private void Start()
    {
        leadManager = ServiceLocator.Get<ILeadManager>();
        
        pinButton.onClick.AddListener(OnPinClicked);
        detailsButton.onClick.AddListener(OnDetailsClicked);
    }
    
    public void SetLead(Lead lead)
    {
        this.lead = lead;
        
        titleText.text = lead.Title;
        descriptionText.text = lead.Description;
        iconImage.sprite = lead.Icon;
        backgroundImage.color = lead.CategoryColor;
        progressSlider.value = lead.InvestigationProgress;
        
        // Update pin button
        pinButton.GetComponentInChildren<TextMeshProUGUI>().text = 
            lead.IsPlayerPinned ? "📌" : "📍";
    }
    
    private void OnPinClicked()
    {
        leadManager.PinLead(lead.LeadId, !lead.IsPlayerPinned);
        SetLead(lead); // Refresh
    }
    
    private void OnDetailsClicked()
    {
        // Open detailed lead view
        LeadDetailsPanel.Instance.Show(lead);
    }
    
    public void MarkAsResolved()
    {
        backgroundImage.color = Color.gray;
        titleText.text = $"✓ {lead.Title}";
        pinButton.interactable = false;
    }
}
```

---

## Save System Integration

### Lead Save Data

```csharp
[System.Serializable]
public class LeadSaveData
{
    public const int CURRENT_VERSION = 1;
    
    public int version = CURRENT_VERSION;
    public List<LeadProgress> leadProgress;
    public HashSet<string> processedDevices;
    public HashSet<string> processedFiles;
    
    [System.Serializable]
    public class LeadProgress
    {
        public string leadId;
        public LeadState state;
        public float investigationProgress;
        public bool isPlayerPinned;
        public DateTime discoveredAt;
        public DateTime resolvedAt;
    }
}

public class LeadManager : ILeadManager, ISaveable
{
    public SaveData GetSaveData()
    {
        var saveData = new LeadSaveData
        {
            leadProgress = new List<LeadSaveData.LeadProgress>(),
            processedDevices = processedDevices,
            processedFiles = processedFiles
        };
        
        foreach (var lead in allLeads.Values)
        {
            saveData.leadProgress.Add(new LeadSaveData.LeadProgress
            {
                leadId = lead.LeadId,
                state = lead.State,
                investigationProgress = lead.InvestigationProgress,
                isPlayerPinned = lead.IsPlayerPinned,
                discoveredAt = lead.DiscoveredAt,
                resolvedAt = lead.ResolvedAt
            });
        }
        
        return saveData;
    }
    
    public void LoadSaveData(SaveData data)
    {
        var leadData = data as LeadSaveData;
        
        processedDevices = leadData.processedDevices;
        processedFiles = leadData.processedFiles;
        
        // Restore lead state
        foreach (var progress in leadData.leadProgress)
        {
            var lead = GetLead(progress.leadId);
            if (lead == null) continue;
            
            lead.State = progress.state;
            lead.InvestigationProgress = progress.investigationProgress;
            lead.IsPlayerPinned = progress.isPlayerPinned;
            lead.DiscoveredAt = progress.discoveredAt;
            lead.ResolvedAt = progress.resolvedAt;
            
            // Add to appropriate lists
            switch (lead.State)
            {
                case LeadState.Active:
                    activeLeads.Add(lead);
                    break;
                case LeadState.Resolved:
                    resolvedLeads.Add(lead);
                    break;
            }
        }
        
        Debug.Log($"[LeadManager] Loaded {activeLeads.Count} active leads");
    }
}
```

---

## Performance Considerations

### Validation Sweep Optimization

**Problem:** Checking all devices every minute is expensive

**Solution:** Only check recently discovered devices

```csharp
private Queue<string> recentDevices = new Queue<string>();
private const int MAX_RECENT_DEVICES = 50;

private void OnDeviceDiscovered(object data)
{
    var device = data as Device;
    
    recentDevices.Enqueue(device.DeviceId);
    if (recentDevices.Count > MAX_RECENT_DEVICES)
    {
        recentDevices.Dequeue();
    }
    
    CreateLeadFromDevice(device);
}

private void RunValidationSweep()
{
    // Only validate recent devices
    foreach (var deviceId in recentDevices)
    {
        if (!processedDevices.Contains(deviceId))
        {
            var device = deviceRegistry.GetDevice(deviceId);
            if (device != null)
            {
                Debug.LogWarning($"[LeadManager] Caught missed device: {device.Hostname}");
                CreateLeadFromDevice(device);
            }
        }
    }
}
```

### Connection Line Optimization

**Problem:** Drawing many connection lines is expensive

**Solution:** Use object pooling and only draw visible connections

```csharp
private ObjectPool<LineRenderer> linePool;

private void DrawConnectionLine(Vector3 from, Vector3 to)
{
    // Only draw if both points are visible on screen
    if (!IsVisibleOnScreen(from) || !IsVisibleOnScreen(to))
        return;
    
    var line = linePool.Get();
    line.SetPosition(0, from);
    line.SetPosition(1, to);
    connectionLines.Add(line);
}

private bool IsVisibleOnScreen(Vector3 position)
{
    var viewportPoint = Camera.main.WorldToViewportPoint(position);
    return viewportPoint.x >= 0 && viewportPoint.x <= 1 &&
           viewportPoint.y >= 0 && viewportPoint.y <= 1;
}
```

---

## Troubleshooting & Common Pitfalls

### Issue: Leads Not Auto-Generating

**Symptom:** Player discovers device but no lead appears

**Cause:** Event not firing or event handler not subscribed

**Solution:**

```csharp
// Verify event subscription
private void SubscribeToGameEvents()
{
    GameEvents.Subscribe(GameEventType.DeviceDiscovered, OnDeviceDiscovered);
    Debug.Log("[LeadManager] Subscribed to DeviceDiscovered event");
}

// Verify event is firing
public void DiscoverDevice(Device device)
{
    Debug.Log($"[DeviceRegistry] Publishing DeviceDiscovered event for {device.Hostname}");
    GameEvents.Publish(GameEventType.DeviceDiscovered, device);
}
```

### Issue: Duplicate Leads

**Symptom:** Multiple leads for the same device

**Cause:** Processed tracking not working

**Solution:**

```csharp
public Lead CreateLeadFromDevice(Device device)
{
    // MUST check before creating
    if (processedDevices.Contains(device.DeviceId))
    {
        Debug.LogWarning($"[LeadManager] Already have lead for {device.Hostname}");
        return null;
    }
    
    processedDevices.Add(device.DeviceId); // ← Add BEFORE creating
    
    // ... create lead
}
```

### Issue: Connections Not Drawing

**Symptom:** Lead cards appear but no red string

**Cause:** Cards not yet positioned when drawing connections

**Solution:**

```csharp
private void RefreshBoard()
{
    // ... populate cards ...
    
    // Wait for layout to complete before drawing connections
    StartCoroutine(DrawConnectionsAfterLayout());
}

private IEnumerator DrawConnectionsAfterLayout()
{
    yield return new WaitForEndOfFrame();
    yield return new WaitForEndOfFrame(); // Wait 2 frames for layout
    
    DrawConnectionLines();
}
```

---

## Quick Start Guide

### 1. Initialize LeadManager

```csharp
// In ServiceLocator initialization:
var leadManager = new LeadManager();
leadManager.Initialize();
ServiceLocator.Register<ILeadManager>(leadManager);
```

### 2. Publish Events for Lead Generation

```csharp
// When player discovers a device:
GameEvents.Publish(GameEventType.DeviceDiscovered, device);

// When NPC mentions someone:
GameEvents.Publish(GameEventType.NPCMentioned, new NPCMentionData
{
    NpcId = "phoenix",
    Context = "Sarah mentioned a mysterious hacker named Phoenix"
});
```

### 3. Link Leads to Quests

```csharp
// In quest setup:
quest.RelatedLeadIds.Add(leadId);

// Or manually:
leadManager.LinkLeadToQuest(leadId, questId);
```

### 4. Show Investigation Board

```csharp
// In UI code:
investigationBoardUI.gameObject.SetActive(true);
```

---

## Summary

The Lead System achieves:

- ✅ **Emergent discovery**: Leads auto-generate from player actions
- ✅ **Hybrid event + validation**: Fast event-driven with fallback polling
- ✅ **Quest coordination**: Bidirectional lead ↔ quest linking
- ✅ **Player agency**: Pin/unpin, prioritize, ignore leads
- ✅ **Investigation board**: Visual cork board with red string connections
- ✅ **Performance optimized**: Object pooling, validation sweeps, viewport culling
- ✅ **Save/load ready**: Serializes lead progress and tracking state
- ✅ **Debug-friendly**: Validation sweeps log missed leads
