**Emergent gameplay loop:**
1. Player scans coffee shop network → Discovers POS terminal → **Lead created automatically**
2. Player clicks lead → Shows: "Windows-based POS, might have customer data, medium security"
3. Player hacks it → Finds emails mentioning "Phoenix" → **New lead created**: "Who is Phoenix?"
4. Player asks Sarah about Phoenix → Sarah's trust increases, reveals Phoenix is a hacktivist → **Lead updated**
5. Phoenix lead gets linked to a new quest that just unlocked: "Make Contact with Local Hackers"

**Lead System:**
```csharp
[System.Serializable]
public class Lead
{
    public string LeadId;
    public string Title;
    public string Description;
    public LeadType Type;
    public LeadPriority Priority;
    
    // Discovery
    public LeadSource Source;           // Where did this come from?
    public DateTime DiscoveredAt;
    public bool IsPlayerPinned;         // Did player manually pin this?
    public bool IsAutoPinned;           // System pinned (critical leads)
    
    // Relationships
    public List<string> RelatedQuestIds;    // Quests this lead is part of
    public List<string> RelatedDeviceIds;   // Devices involved
    public List<string> RelatedNPCIds;      // NPCs involved
    public List<string> RelatedLeadIds;     // Other connected leads
    
    // Investigation state
    public LeadState State = LeadState.Active;
    public float InvestigationProgress; // 0-1, how much player has explored this
    
    // Visual/metadata
    public Sprite Icon;
    public Color CategoryColor;
}

public enum LeadType
{
    Device,              // "Found unknown server on coffee shop network"
    Person,              // "Sarah mentioned someone named 'Phoenix'"
    Location,            // "Overheard mention of underground club"
    Mystery,             // "Strange encrypted file on compromised PC"
    Opportunity,         // "Job opening at BigTechCorp"
    Threat               // "Someone is counter-hacking you"
}

public enum LeadPriority
{
    Critical,   // Auto-pinned, glowing red
    High,       // Important for main story
    Medium,     // Interesting side content
    Low         // Flavor/optional
}

public class LeadSource
{
    public LeadSourceType Type;
    public string SourceId; // ID of the device/NPC/location that generated this
    public string SourceName; // Human-readable name
}
```

**LeadManager:**
```csharp
public class LeadManager : MonoBehaviour
{
    private Dictionary<string, Lead> allLeads;
    private List<Lead> activeLeads;
    private List<Lead> resolvedLeads;
    
    // UI references
    public InvestigationBoardUI investigationBoard;
    
    public Lead CreateLead(LeadType type, string title, string description, LeadSource source)
    {
        var lead = new Lead
        {
            LeadId = System.Guid.NewGuid().ToString(),
            Title = title,
            Description = description,
            Type = type,
            Source = source,
            DiscoveredAt = TimeManager.Instance.CurrentGameTime,
            Priority = DeterminePriority(type, source) // Smart prioritization
        };
        
        allLeads[lead.LeadId] = lead;
        activeLeads.Add(lead);
        
        // Auto-pin critical leads
        if (lead.Priority == LeadPriority.Critical)
        {
            lead.IsAutoPinned = true;
        }
        
        // Notify UI
        GameEvents.OnLeadDiscovered?.Invoke(lead);
        
        // Check if this lead unlocks any quests
        QuestManager.Instance.CheckForNewQuests();
        
        return lead;
    }
    
    // Called when player discovers a new device
    public void OnDeviceDiscovered(Device device)
    {
        // Create a lead for this device
        var lead = CreateLead(
            LeadType.Device,
            $"Unknown Device: {device.Hostname}",
            $"Found {device.DeviceType} on {device.NetworkId}. " +
            $"Running {device.OS.Name}. Security: {device.SecurityLevel}",
            new LeadSource { Type = LeadSourceType.NetworkScan, SourceId = device.DeviceId }
        );
        
        lead.RelatedDeviceIds.Add(device.DeviceId);
        
        // Link to any relevant quests
        LinkLeadToQuests(lead);
    }
    
    private void LinkLeadToQuests(Lead lead)
    {
        // Check if any active quests care about this lead
        foreach (var quest in QuestManager.Instance.GetActiveQuests())
        {
            foreach (var objective in quest.Objectives)
            {
                // Example: if objective is "hack coffee shop POS" and this lead is that device
                if (objective.Type == ObjectiveType.HackDevice && 
                    lead.RelatedDeviceIds.Contains(objective.TargetDeviceId))
                {
                    lead.RelatedQuestIds.Add(quest.QuestId);
                    objective.RelatedLeadIds.Add(lead.LeadId);
                }
            }
        }
    }
}
```


**Investigation Board UI:**
We will create pixel art of a physical cork board with red string connecting things:
```
┌─────────────────────────────────────────┐
│         INVESTIGATION BOARD             │
├─────────────────────────────────────────┤
│  [CRITICAL LEADS]                       │
│  🔴 Someone is Tracking You             │
│     └─> Related: Unknown IPs in logs    │
│                                         │
│  [PINNED]                               │
│  📌 BigTechCorp Server (192.168.1.50)   │
│     └─> Quest: Corporate Espionage      │
│  📌 Phoenix (Mysterious Hacker)         │
│     └─> Mentioned by: Sarah, Marcus     │
│                                         │
│  [DEVICES]                              │
│  💻 Coffee Shop POS Terminal            │
│  📱 Sarah's Phone (discovered)          │
│  🖥️ Unknown Server (City Hall network)  │
│                                         │
│  [PEOPLE]                               │
│  👤 Marcus (IT Admin) - Trust: 45%      │
│  👤 Sarah (Friend) - Trust: 85%         │
│                                         │
│  [RESOLVED]                             │
│  ✓ Leaked Password Database             │
│  ✓ Underground Hacker Meetup Location   │
└─────────────────────────────────────────┘
```

# Sererate But Coordinated

We need to keep the Lead System and the Quest system tightly synced to we should likely handle something like

```C#
public class ProgressionCoordinator{
    private IQuestManager questManager;
    private ILeadManager leadManager;

    public void Initialize()
    {
        // This service orchestrates the two
        leadManager.OnLeadCreated += (lead) =>
        {
            questManager.CheckIfLeadUnlocksQuests(lead);
        };
        questManager.OnQuestUnlocked += (quest) =>
        {
            leadManager.UpdateLeadsRelatedToQuest(quest);
        };
    }
}
```

Keep them separate but add a coordinator service that handles their interaction. Best of both worlds.

### Lead Auto-Generation

Hybrid Event + Validation (My Recommendation)

```C#
public class LeadManager : ILeadManager
{
    private HashSet<string> processedDevices = new HashSet<string>();
    
    public void Initialize()
    {
        // Primary: Event-driven
        GameEvents.Instance.Subscribe(GameEventType.DeviceDiscovered, OnDeviceDiscovered);
        
        // Fallback: Occasional validation sweep
        StartCoroutine(ValidationSweep());
    }
    
    private void OnDeviceDiscovered(object data)
    {
        var device = data as Device;
        
        if (processedDevices.Contains(device.DeviceId))
            return; // Already have a lead for this
            
        CreateLeadForDevice(device);
        processedDevices.Add(device.DeviceId);
    }
    
    private IEnumerator ValidationSweep()
    {
        while (true)
        {
            yield return new WaitForSeconds(60f); // Once per minute
            
            // Catch any devices we missed
            var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
            var allDevices = registry.GetAllDevices();
            
            foreach (var device in allDevices)
            {
                if (!processedDevices.Contains(device.DeviceId))
                {
                    Debug.LogWarning($"[LeadManager] Caught missed device: {device.Hostname}");
                    CreateLeadForDevice(device);
                }
            }
        }
    }
}
```

**Pros**:

- ✅ Fast primary path: Events give instant feedback
- ✅ Safety net: Polling catches anything events missed
- ✅ Best of both: Performance of events + reliability of polling
- ✅ Debuggable: Warning log shows if events are failing

**Cons**:

- ❌ More complex: Two code paths to maintain
- ❌ Small overhead: Still does occasional sweeps
