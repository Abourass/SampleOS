# Dialogue System Architecture

## Overview

The Dialogue System uses **Ink** as the narrative scripting language, wrapped in a service-based architecture that integrates with NPCManager, QuestManager, and LeadManager. The system separates dialogue logic (stored in Ink scripts) from presentation (UI) and game state (services).

**Key Design Principles:**
- **Ink handles narrative flow**: Branching dialogue, conditions, variable tracking
- **DialogueService handles integration**: Bridges Ink with game systems
- **DialogueUIController handles presentation**: Pure visual layer, no logic
- **NPCManager stores conversation history**: Centralized player-NPC interaction data

---

## Core Architecture

### Component Hierarchy

```
DialogueService (IDialogueService)
  ├── Manages Ink Story instances
  ├── Binds external functions (add_lead, unlock_quest, etc.)
  ├── Syncs game state ↔ Ink variables
  └── Delegates storage to NPCManager

DialogueUIController (MonoBehaviour, scene-specific)
  ├── Displays dialogue text
  ├── Renders choice buttons
  ├── Handles animations (typewriter, portraits)
  └── Receives commands from DialogueService

NPCManager (INPCManager)
  ├── Stores conversation history (seen nodes)
  ├── Stores conversation metadata (timestamps, choices made)
  └── Provides conversation state to DialogueService
```

---

## Service Interface

```csharp
public interface IDialogueService : IGameService
{
    // === Dialogue Flow ===
    void StartDialogue(string npcId, Action<DialogueNode> onNodeReached);
    void MakeChoice(int choiceIndex);
    void EndDialogue();
    
    // === State Queries ===
    bool IsDialogueActive { get; }
    string CurrentNpcId { get; }
    bool HasSeenNode(string npcId, string nodeId);
    HashSet<string> GetSeenNodes(string npcId);
    
    // === Events ===
    event Action<string, DialogueNode> OnDialogueStarted;
    event Action<string> OnDialogueEnded;
    event Action<string, string> OnNodeReached; // (npcId, nodeId)
    event Action<string, int> OnChoiceMade; // (npcId, choiceIndex)
}

public class DialogueNode
{
    public string NodeId;           // Ink knot/stitch name
    public string SpeakerName;      // NPC name
    public string Text;             // Dialogue text
    public List<Choice> Choices;    // Available choices
    public string EmotionTag;       // For portrait system (e.g., "happy", "angry")
    public string AudioClipId;      // For voice-over system
}

public class Choice
{
    public int Index;
    public string Text;
    public bool IsAvailable;        // Based on Ink conditions
}
```

---

## Implementation

### DialogueService

```csharp
public class DialogueService : IDialogueService
{
    private INPCManager npcManager;
    private IRelationshipService relationshipService;
    private IQuestManager questManager;
    private ILeadManager leadManager;
    private ITimeManager timeManager;
    
    private Story currentStory;
    private string currentNpcId;
    private Action<DialogueNode> currentNodeCallback;
    
    private Dictionary<string, TextAsset> inkScripts; // Cached Ink files
    
    public bool IsDialogueActive => currentStory != null;
    public string CurrentNpcId => currentNpcId;
    
    // === Initialization ===
    
    public void Initialize()
    {
        // Get service dependencies
        npcManager = ServiceLocator.Get<INPCManager>();
        relationshipService = ServiceLocator.Get<IRelationshipService>();
        questManager = ServiceLocator.Get<IQuestManager>();
        leadManager = ServiceLocator.Get<ILeadManager>();
        timeManager = ServiceLocator.Get<ITimeManager>();
        
        // Load all Ink scripts from Resources
        LoadInkScripts();
        
        Debug.Log("[DialogueService] Initialized");
    }
    
    private void LoadInkScripts()
    {
        inkScripts = new Dictionary<string, TextAsset>();
        
        // Load all .json files from Resources/Dialogue/
        var scripts = Resources.LoadAll<TextAsset>("Dialogue");
        foreach (var script in scripts)
        {
            inkScripts[script.name] = script;
        }
        
        Debug.Log($"[DialogueService] Loaded {inkScripts.Count} Ink scripts");
    }
    
    // === Dialogue Flow ===
    
    public void StartDialogue(string npcId, Action<DialogueNode> onNodeReached)
    {
        if (IsDialogueActive)
        {
            Debug.LogWarning($"[DialogueService] Already in dialogue with {currentNpcId}");
            return;
        }
        
        var npc = npcManager.GetNPC(npcId);
        if (npc == null)
        {
            Debug.LogError($"[DialogueService] NPC not found: {npcId}");
            return;
        }
        
        // Get Ink script for this NPC
        if (!inkScripts.TryGetValue(npc.DialogueScriptId, out var inkScript))
        {
            Debug.LogError($"[DialogueService] Ink script not found: {npc.DialogueScriptId}");
            return;
        }
        
        // Create Ink story instance
        currentStory = new Story(inkScript.text);
        currentNpcId = npcId;
        currentNodeCallback = onNodeReached;
        
        // Bind external functions
        BindExternalFunctions();
        
        // Inject game state into Ink variables
        InjectGameStateIntoInk(npcId);
        
        // Pause time during dialogue
        timeManager.SetTimeContext(TimeContext.Conversation);
        
        // Start dialogue flow
        OnDialogueStarted?.Invoke(npcId, null);
        ContinueStory();
    }
    
    public void MakeChoice(int choiceIndex)
    {
        if (!IsDialogueActive)
        {
            Debug.LogWarning("[DialogueService] No active dialogue");
            return;
        }
        
        if (choiceIndex < 0 || choiceIndex >= currentStory.currentChoices.Count)
        {
            Debug.LogError($"[DialogueService] Invalid choice index: {choiceIndex}");
            return;
        }
        
        // Record choice in history
        var choice = currentStory.currentChoices[choiceIndex];
        npcManager.RecordDialogueChoice(currentNpcId, choice.text);
        
        // Make choice in Ink
        currentStory.ChooseChoiceIndex(choiceIndex);
        OnChoiceMade?.Invoke(currentNpcId, choiceIndex);
        
        // Continue dialogue
        ContinueStory();
    }
    
    public void EndDialogue()
    {
        if (!IsDialogueActive) return;
        
        // Sync final Ink state back to game state
        SyncInkStateToGame(currentNpcId);
        
        // Update last interaction time
        npcManager.UpdateLastInteraction(currentNpcId, timeManager.CurrentGameTime);
        
        // Resume time
        timeManager.SetTimeContext(TimeContext.Walking);
        
        OnDialogueEnded?.Invoke(currentNpcId);
        
        // Cleanup
        currentStory = null;
        currentNpcId = null;
        currentNodeCallback = null;
    }
    
    private void ContinueStory()
    {
        if (!IsDialogueActive) return;
        
        // Continue Ink story
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue().Trim();
            
            // Extract current node ID (Ink's current path)
            string nodeId = currentStory.state.currentPathString;
            
            // Mark node as seen
            npcManager.MarkDialogueNodeSeen(currentNpcId, nodeId);
            OnNodeReached?.Invoke(currentNpcId, nodeId);
            
            // Build DialogueNode
            var dialogueNode = new DialogueNode
            {
                NodeId = nodeId,
                SpeakerName = npcManager.GetNPC(currentNpcId).Name,
                Text = text,
                Choices = BuildChoices(),
                EmotionTag = ExtractTag("emotion"),
                AudioClipId = ExtractTag("audio")
            };
            
            // Send to UI
            currentNodeCallback?.Invoke(dialogueNode);
        }
        else if (currentStory.currentChoices.Count == 0)
        {
            // Dialogue ended naturally
            EndDialogue();
        }
        else
        {
            // Show choices only
            var dialogueNode = new DialogueNode
            {
                NodeId = currentStory.state.currentPathString,
                SpeakerName = npcManager.GetNPC(currentNpcId).Name,
                Text = "", // No text, just choices
                Choices = BuildChoices()
            };
            
            currentNodeCallback?.Invoke(dialogueNode);
        }
    }
    
    private List<Choice> BuildChoices()
    {
        var choices = new List<Choice>();
        
        for (int i = 0; i < currentStory.currentChoices.Count; i++)
        {
            var inkChoice = currentStory.currentChoices[i];
            choices.Add(new Choice
            {
                Index = i,
                Text = inkChoice.text.Trim(),
                IsAvailable = true // Ink already filtered unavailable choices
            });
        }
        
        return choices;
    }
    
    private string ExtractTag(string tagPrefix)
    {
        // Ink tags: # emotion: happy
        foreach (var tag in currentStory.currentTags)
        {
            if (tag.StartsWith(tagPrefix + ":"))
            {
                return tag.Substring(tagPrefix.Length + 1).Trim();
            }
        }
        return null;
    }
    
    // === External Functions ===
    
    private void BindExternalFunctions()
    {
        // Lead creation
        currentStory.BindExternalFunction("add_lead", (string type, string title, string desc) =>
        {
            var leadType = Enum.Parse<LeadType>(type, ignoreCase: true);
            leadManager.CreateLead(leadType, title, desc, new LeadSource
            {
                Type = LeadSourceType.Dialogue,
                SourceId = currentNpcId,
                SourceName = npcManager.GetNPC(currentNpcId).Name
            });
        });
        
        // Quest unlocking
        currentStory.BindExternalFunction("unlock_quest", (string questId) =>
        {
            questManager.UnlockQuest(questId);
        });
        
        // Relationship modification
        currentStory.BindExternalFunction("modify_trust", (string npcId, int delta) =>
        {
            relationshipService.ModifyTrust(npcId, delta);
        });
        
        // Set conversation flags
        currentStory.BindExternalFunction("set_flag", (string flagName, bool value) =>
        {
            npcManager.SetConversationFlag(currentNpcId, flagName, value);
        });
        
        // Check if flag exists
        currentStory.BindExternalFunction("has_flag", (string flagName) =>
        {
            return npcManager.HasConversationFlag(currentNpcId, flagName);
        });
        
        // Time consumption
        currentStory.BindExternalFunction("consume_time", (int minutes) =>
        {
            timeManager.ConsumeTime(TimeSpan.FromMinutes(minutes), $"Dialogue with {currentNpcId}");
        });
    }
    
    // === State Synchronization ===
    
    private void InjectGameStateIntoInk(string npcId)
    {
        var vars = currentStory.variablesState;
        
        // Relationship data
        var relationship = npcManager.GetRelationship(npcId);
        if (relationship != null)
        {
            vars[$"{npcId}_trust"] = relationship.TrustLevel;
            vars[$"{npcId}_relationship"] = relationship.RelationshipType.ToString();
        }
        
        // Player stats
        var playerStats = ServiceLocator.Get<IPlayerManager>().GetStats();
        vars["player_karma"] = playerStats.Karma;
        vars["player_charisma"] = playerStats.Charisma;
        vars["player_intelligence"] = playerStats.Intelligence;
        
        // Conversation flags
        var flags = npcManager.GetConversationFlags(npcId);
        foreach (var flag in flags)
        {
            vars[flag.Key] = flag.Value;
        }
        
        // Time/date
        vars["current_hour"] = timeManager.CurrentGameTime.Hour;
        vars["current_day"] = timeManager.CurrentGameTime.DayOfWeek.ToString();
        
        // Quest states
        var activeQuests = questManager.GetActiveQuests();
        foreach (var quest in activeQuests)
        {
            vars[$"quest_{quest.QuestId}_active"] = true;
        }
    }
    
    private void SyncInkStateToGame(string npcId)
    {
        var vars = currentStory.variablesState;
        
        // Read relationship changes back from Ink
        if (vars.GlobalVariableExistsWithName($"{npcId}_trust"))
        {
            int newTrust = (int)vars[$"{npcId}_trust"];
            var currentTrust = npcManager.GetRelationship(npcId)?.TrustLevel ?? 0;
            int delta = newTrust - currentTrust;
            
            if (delta != 0)
            {
                relationshipService.ModifyTrust(npcId, delta);
            }
        }
        
        // Sync any other modified variables as needed
        // (Most changes should go through external functions, but this catches edge cases)
    }
    
    // === State Queries ===
    
    public bool HasSeenNode(string npcId, string nodeId)
    {
        return npcManager.HasSeenDialogueNode(npcId, nodeId);
    }
    
    public HashSet<string> GetSeenNodes(string npcId)
    {
        return npcManager.GetSeenNodes(npcId);
    }
    
    // === Events ===
    
    public event Action<string, DialogueNode> OnDialogueStarted;
    public event Action<string> OnDialogueEnded;
    public event Action<string, string> OnNodeReached;
    public event Action<string, int> OnChoiceMade;
}
```

---

## UI Controller

```csharp
public class DialogueUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private CanvasGroup dialoguePanel;
    [SerializeField] private TextMeshProUGUI speakerNameText;
    [SerializeField] private TextMeshProUGUI dialogueText;
    [SerializeField] private Image portraitImage;
    [SerializeField] private Transform choiceButtonContainer;
    [SerializeField] private Button choiceButtonPrefab;
    
    [Header("Animation Settings")]
    [SerializeField] private float typewriterSpeed = 0.05f;
    [SerializeField] private float fadeDuration = 0.3f;
    
    private IDialogueService dialogueService;
    private Coroutine typewriterCoroutine;
    private List<Button> choiceButtons = new List<Button>();
    
    // === Future Integration Points ===
    // TODO: Voice-over system integration
    // TODO: Portrait emotion system integration
    // TODO: Choice timeout mechanics
    
    private void Start()
    {
        dialogueService = ServiceLocator.Get<IDialogueService>();
        
        // Hide initially
        dialoguePanel.alpha = 0;
        dialoguePanel.gameObject.SetActive(false);
    }
    
    public void ShowDialogue(string npcId)
    {
        dialogueService.StartDialogue(npcId, OnDialogueNodeReached);
    }
    
    private void OnDialogueNodeReached(DialogueNode node)
    {
        // Show panel if hidden
        if (!dialoguePanel.gameObject.activeSelf)
        {
            dialoguePanel.gameObject.SetActive(true);
            StartCoroutine(FadePanel(0, 1));
        }
        
        // Update speaker name
        speakerNameText.text = node.SpeakerName;
        
        // Typewriter effect for text
        if (typewriterCoroutine != null)
            StopCoroutine(typewriterCoroutine);
        
        if (!string.IsNullOrEmpty(node.Text))
        {
            typewriterCoroutine = StartCoroutine(TypewriterEffect(node.Text));
        }
        
        // Update portrait (future integration)
        UpdatePortrait(node.EmotionTag);
        
        // Play voice-over (future integration)
        PlayVoiceOver(node.AudioClipId);
        
        // Update choices
        UpdateChoices(node.Choices);
    }
    
    private IEnumerator TypewriterEffect(string fullText)
    {
        dialogueText.text = "";
        
        foreach (char c in fullText)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typewriterSpeed);
        }
        
        typewriterCoroutine = null;
    }
    
    private void UpdateChoices(List<Choice> choices)
    {
        // Clear existing buttons
        foreach (var button in choiceButtons)
        {
            Destroy(button.gameObject);
        }
        choiceButtons.Clear();
        
        // Create new buttons
        foreach (var choice in choices)
        {
            var button = Instantiate(choiceButtonPrefab, choiceButtonContainer);
            button.GetComponentInChildren<TextMeshProUGUI>().text = choice.Text;
            button.interactable = choice.IsAvailable;
            
            int index = choice.Index; // Capture for closure
            button.onClick.AddListener(() => OnChoiceClicked(index));
            
            choiceButtons.Add(button);
        }
    }
    
    private void OnChoiceClicked(int choiceIndex)
    {
        // Disable all buttons to prevent double-clicks
        foreach (var button in choiceButtons)
        {
            button.interactable = false;
        }
        
        dialogueService.MakeChoice(choiceIndex);
    }
    
    public void CloseDialogue()
    {
        StartCoroutine(FadeAndClose());
    }
    
    private IEnumerator FadeAndClose()
    {
        yield return StartCoroutine(FadePanel(1, 0));
        dialoguePanel.gameObject.SetActive(false);
        dialogueService.EndDialogue();
    }
    
    private IEnumerator FadePanel(float from, float to)
    {
        float elapsed = 0;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            dialoguePanel.alpha = Mathf.Lerp(from, to, elapsed / fadeDuration);
            yield return null;
        }
        
        dialoguePanel.alpha = to;
    }
    
    // === Future Integration Points ===
    
    private void UpdatePortrait(string emotionTag)
    {
        // TODO: Load portrait sprite based on NPC and emotion
        // portraitImage.sprite = portraitDatabase.GetSprite(currentNpcId, emotionTag);
    }
    
    private void PlayVoiceOver(string audioClipId)
    {
        // TODO: Play voice-over audio if available
        // audioSource.clip = audioDatabase.GetClip(audioClipId);
        // audioSource.Play();
    }
}
```

---

## Ink File Organization

### Folder Structure

```
Assets/Resources/Dialogue/
  ├── NPCs/
  │   ├── sarah.ink.json          # Sarah's main dialogue
  │   ├── marcus.ink.json          # Marcus's main dialogue
  │   ├── phoenix.ink.json         # Phoenix's main dialogue
  │   └── ...
  ├── Quests/
  │   ├── main_01_intro.ink.json   # Quest-specific dialogue
  │   ├── side_hacker_meetup.ink.json
  │   └── ...
  ├── Common/
  │   ├── greetings.ink            # Shared dialogue snippets
  │   ├── goodbyes.ink
  │   └── small_talk.ink
  └── Variables/
      └── global_variables.ink      # Shared Ink variables
```

### Naming Conventions

**Ink Files:**
- NPC dialogues: `{npcId}.ink` (e.g., `sarah.ink`)
- Quest dialogues: `{questId}.ink` (e.g., `main_01_intro.ink`)
- Compiled JSON: Same name with `.json` extension

**Ink Knots/Stitches:**
- Main conversation entry: `talk_to_{npcId}` (e.g., `talk_to_sarah`)
- Topic-based: `ask_about_{topic}` (e.g., `ask_about_phoenix`)
- Quest-related: `quest_{questId}_{stage}` (e.g., `quest_main_01_start`)

**Ink Variables:**
- NPC relationship: `{npcId}_trust`, `{npcId}_relationship`
- Player stats: `player_{stat}` (e.g., `player_karma`)
- Flags: `flag_{description}` (e.g., `flag_told_sarah_about_hack`)
- Quest states: `quest_{questId}_active`, `quest_{questId}_completed`

### Splitting Dialogue Across Files

**Per-NPC Approach (Recommended for Most NPCs):**
```ink
// sarah.ink

INCLUDE Common/greetings.ink
INCLUDE Common/goodbyes.ink

VAR sarah_trust = 50
VAR sarah_relationship = "neutral"

=== talk_to_sarah ===
{ sarah_trust >= 70:
    -> greeting_warm
- else:
    -> greeting_neutral
}

=== greeting_warm ===
Sarah: "Hey! Good to see you!"
-> main_topics

=== greeting_neutral ===
Sarah: "Oh, hi."
-> main_topics

=== main_topics ===
* [Ask about work] -> ask_about_work
* [Ask about Phoenix] { sarah_trust >= 70 } -> ask_about_phoenix
* [Goodbye] -> goodbye

// ... more content
```

**Per-Quest Approach (For Large Quest Chains):**
```ink
// main_01_intro.ink

INCLUDE NPCs/sarah.ink
INCLUDE NPCs/marcus.ink

=== quest_intro_sarah ===
// Quest-specific Sarah dialogue
Sarah: "I need to tell you something important..."
-> sarah.talk_to_sarah.quest_branch
```

**Hybrid Approach (Best Practice):**
- Store **NPC personality/recurring dialogue** in NPC files
- Store **quest-specific dialogue** in quest files
- Use `INCLUDE` and `->` (tunnel/thread) to link them

---

## External Function Registry

### Naming Conventions

**Pattern:** `{category}_{action}[_{target}]`

**Categories:**
- `lead_` - Lead management
- `quest_` - Quest management
- `rel_` - Relationship management
- `flag_` - Conversation flags
- `time_` - Time manipulation
- `item_` - Inventory management
- `world_` - World state changes

**Examples:**
```ink
~ lead_add("device", "Coffee Shop POS", "Found vulnerable POS system")
~ quest_unlock("main_02_investigation")
~ rel_modify_trust("sarah", 10)
~ flag_set("told_sarah_secret", true)
~ time_consume(30)
~ item_give("leaked_password_db")
~ world_set_device_compromised("coffee_shop_pos")
```

### Current External Functions

```csharp
// === Lead Management ===
add_lead(type: string, title: string, description: string)
// Example: ~ add_lead("person", "Phoenix", "Mysterious hacker mentioned by Sarah")

// === Quest Management ===
unlock_quest(questId: string)
// Example: ~ unlock_quest("side_hacker_meetup")

complete_quest(questId: string)
// Example: ~ complete_quest("main_01_intro")

// === Relationship Management ===
modify_trust(npcId: string, delta: int)
// Example: ~ modify_trust("sarah", 10)

set_relationship_type(npcId: string, type: string)
// Example: ~ set_relationship_type("sarah", "friend")

// === Conversation Flags ===
set_flag(flagName: string, value: bool)
// Example: ~ set_flag("told_sarah_about_hack", true)

has_flag(flagName: string) -> bool
// Example: { has_flag("told_sarah_about_hack") }

// === Time Management ===
consume_time(minutes: int)
// Example: ~ consume_time(45)
```

### Adding New External Functions

**Step 1: Define the function in DialogueService**
```csharp
private void BindExternalFunctions()
{
    // ... existing functions ...
    
    // New function
    currentStory.BindExternalFunction("your_function_name", (string param1, int param2) =>
    {
        // Implementation
        var result = SomeService.DoSomething(param1, param2);
        return result; // Optional return value
    });
}
```

**Step 2: Document in this registry**
Add to the list above with:
- Function signature
- Description
- Example usage in Ink

**Step 3: Test in Ink**
```ink
=== test_new_function ===
~ your_function_name("test", 42)
* [Continue] -> END
```

---

## Conversation History Storage

### NPCManager Integration

```csharp
public interface INPCManager
{
    // === Dialogue History ===
    void MarkDialogueNodeSeen(string npcId, string nodeId);
    bool HasSeenDialogueNode(string npcId, string nodeId);
    HashSet<string> GetSeenNodes(string npcId);
    
    void RecordDialogueChoice(string npcId, string choiceText);
    List<string> GetDialogueChoices(string npcId);
    
    void SetConversationFlag(string npcId, string flagName, bool value);
    bool HasConversationFlag(string npcId, string flagName);
    Dictionary<string, bool> GetConversationFlags(string npcId);
    
    void UpdateLastInteraction(string npcId, DateTime time);
    DateTime GetLastInteraction(string npcId);
}
```

### Data Structure

```csharp
[System.Serializable]
public class ConversationHistory
{
    public string NpcId;
    
    // Seen dialogue nodes (Ink knot paths)
    public HashSet<string> SeenNodes = new HashSet<string>();
    
    // Choices made (for analytics/debugging)
    public List<ConversationChoice> ChoicesMade = new List<ConversationChoice>();
    
    // Conversation flags (persistent state)
    public Dictionary<string, bool> Flags = new Dictionary<string, bool>();
    
    // Metadata
    public DateTime FirstInteraction;
    public DateTime LastInteraction;
    public int TotalInteractions;
}

[System.Serializable]
public class ConversationChoice
{
    public string NodeId;           // Where this choice was made
    public string ChoiceText;       // What the player chose
    public DateTime Timestamp;      // When they made this choice
}
```

### Save/Load Strategy

**Recommended Approach:** Store completed nodes only

**Why?**
- ✅ Smaller save file size
- ✅ Sufficient for Ink's conditional logic (`{ not visited_node }`)
- ✅ Matches event-driven architecture
- ✅ Easy to query ("Has player seen this node?")

**What to Save:**
```csharp
[System.Serializable]
public class DialogueSaveData
{
    // Per-NPC conversation history
    public Dictionary<string, ConversationHistory> npcConversations;
    
    // Save metadata
    public DateTime lastSaved;
    public int saveVersion; // For migration
}
```

**What NOT to Save:**
- ❌ Current Ink story state (no mid-conversation saves)
- ❌ Ink variable values (re-injected from game state)
- ❌ UI state (transient)

### Metadata for Debugging/Analytics

```csharp
public class ConversationAnalytics
{
    // Track which choices are most popular
    public Dictionary<string, int> ChoicePopularity = new Dictionary<string, int>();
    
    // Track average relationship changes per conversation
    public Dictionary<string, List<int>> TrustDeltaHistory = new Dictionary<string, List<int>>();
    
    // Track conversation abandonment (started but not completed)
    public int AbandonedConversations;
    
    // Log for debugging
    public List<ConversationEvent> EventLog = new List<ConversationEvent>();
}

public class ConversationEvent
{
    public DateTime Timestamp;
    public string NpcId;
    public string EventType; // "started", "choice_made", "ended"
    public string Details;
}
```

---

## Save System Integration

### Save Versioning

```csharp
public class DialogueSaveData
{
    public const int CURRENT_VERSION = 1;
    
    public int version = CURRENT_VERSION;
    public Dictionary<string, ConversationHistory> conversations;
}

public class DialogueSaveDataMigrator
{
    public static DialogueSaveData Migrate(DialogueSaveData oldData)
    {
        if (oldData.version == DialogueSaveData.CURRENT_VERSION)
            return oldData; // Already current
        
        var migratedData = oldData;
        
        // Apply migrations sequentially
        if (oldData.version < 1)
            migratedData = MigrateV0ToV1(migratedData);
        
        // Future migrations:
        // if (oldData.version < 2)
        //     migratedData = MigrateV1ToV2(migratedData);
        
        migratedData.version = DialogueSaveData.CURRENT_VERSION;
        return migratedData;
    }
    
    private static DialogueSaveData MigrateV0ToV1(DialogueSaveData oldData)
    {
        // Example: Convert old flag format to new format
        // This is called when loading a save from an older version
        
        var newData = new DialogueSaveData
        {
            version = 1,
            conversations = new Dictionary<string, ConversationHistory>()
        };
        
        // Perform migration logic here
        // ...
        
        return newData;
    }
}
```

### Handling Legacy Saves

**Strategy: Graceful Degradation**

```csharp
public class SaveSystem
{
    public GameSaveData LoadGame(string slotName)
    {
        try
        {
            var json = File.ReadAllText(GetSavePath(slotName));
            var saveData = JsonUtility.FromJson<GameSaveData>(json);
            
            // Check version
            if (saveData.saveVersion < GameSaveData.CURRENT_VERSION)
            {
                Debug.LogWarning($"[SaveSystem] Loading legacy save (v{saveData.saveVersion})");
                saveData = MigrateSaveData(saveData);
            }
            
            return saveData;
        }
        catch (Exception e)
        {
            Debug.LogError($"[SaveSystem] Failed to load save: {e.Message}");
            
            // Fallback: Try to load with minimal data
            return AttemptPartialLoad(slotName);
        }
    }
    
    private GameSaveData AttemptPartialLoad(string slotName)
    {
        // Load only critical data, skip corrupted sections
        // Better to lose some conversation history than the entire save
        
        Debug.LogWarning("[SaveSystem] Attempting partial load (some data may be lost)");
        
        var partialData = new GameSaveData();
        // Load player data, world state, etc.
        // Skip dialogue history if corrupted
        
        return partialData;
    }
}
```

**Migration Example: Adding New Field**

```csharp
// Version 1: Original
public class ConversationHistory_V1
{
    public HashSet<string> SeenNodes;
    public DateTime LastInteraction;
}

// Version 2: Added choice tracking
public class ConversationHistory_V2
{
    public HashSet<string> SeenNodes;
    public List<ConversationChoice> ChoicesMade; // NEW
    public DateTime LastInteraction;
}

// Migration
private static ConversationHistory_V2 MigrateConversationHistory_V1_To_V2(ConversationHistory_V1 old)
{
    return new ConversationHistory_V2
    {
        SeenNodes = old.SeenNodes,
        ChoicesMade = new List<ConversationChoice>(), // Empty list for old saves
        LastInteraction = old.LastInteraction
    };
}
```

---

## Future Feature Integration Points

### Voice-Over System

**Integration Points:**
1. **Ink Tags:** `# audio: sarah_greeting_01`
2. **DialogueNode:** `AudioClipId` property
3. **DialogueUIController:** `PlayVoiceOver(string clipId)` method

**Planned Implementation:**
```csharp
public class VoiceOverSystem
{
    private AudioSource audioSource;
    private Dictionary<string, AudioClip> voiceClips;
    
    public void PlayVoiceOver(string clipId)
    {
        if (voiceClips.TryGetValue(clipId, out var clip))
        {
            audioSource.clip = clip;
            audioSource.Play();
        }
    }
}
```

### Portrait/Emotion System

**Integration Points:**
1. **Ink Tags:** `# emotion: happy`, `# emotion: angry`
2. **DialogueNode:** `EmotionTag` property
3. **DialogueUIController:** `UpdatePortrait(string emotion)` method

**Planned Implementation:**
```csharp
public class PortraitSystem
{
    private Dictionary<string, Dictionary<string, Sprite>> npcPortraits;
    
    public Sprite GetPortrait(string npcId, string emotion)
    {
        if (npcPortraits.TryGetValue(npcId, out var emotions))
        {
            if (emotions.TryGetValue(emotion, out var portrait))
                return portrait;
        }
        
        return emotions["neutral"]; // Fallback
    }
}
```

### Choice Timeout Mechanics

**Use Case:** Timed dialogue choices (e.g., "Quick! What do you do?")

**Integration Points:**
1. **Ink Tags:** `# timeout: 5` (seconds)
2. **DialogueNode:** `TimeoutSeconds` property
3. **DialogueUIController:** Timer coroutine

**Planned Implementation:**
```csharp
public class DialogueUIController
{
    private Coroutine timeoutCoroutine;
    
    private void UpdateChoices(List<Choice> choices, float? timeout)
    {
        // ... existing code ...
        
        if (timeout.HasValue)
        {
            timeoutCoroutine = StartCoroutine(ChoiceTimeout(timeout.Value));
        }
    }
    
    private IEnumerator ChoiceTimeout(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        
        // Auto-select first choice if no choice made
        if (dialogueService.IsDialogueActive)
        {
            Debug.LogWarning("[DialogueUI] Choice timeout - auto-selecting first option");
            OnChoiceClicked(0);
        }
    }
}
```

### Text Animation Effects

**Future Enhancements:**
- Rich text effects (shake, wave, rainbow)
- Character-by-character sound effects
- Pause tags (e.g., `{pause:0.5}` in Ink)
- Skip animation on click

---

## Performance Considerations

### Ink Story Instance Management

**Problem:** Creating Story instances is expensive (~100ms for large scripts)

**Solution: Object Pooling**
```csharp
public class InkStoryPool
{
    private Dictionary<string, Queue<Story>> storyPool = new Dictionary<string, Queue<Story>>();
    private Dictionary<string, TextAsset> inkScripts;
    
    public Story GetStory(string scriptId)
    {
        if (storyPool.TryGetValue(scriptId, out var pool) && pool.Count > 0)
        {
            var story = pool.Dequeue();
            story.ResetState(); // Reset to beginning
            return story;
        }
        
        // Create new instance
        return new Story(inkScripts[scriptId].text);
    }
    
    public void ReturnStory(string scriptId, Story story)
    {
        if (!storyPool.ContainsKey(scriptId))
            storyPool[scriptId] = new Queue<Story>();
        
        storyPool[scriptId].Enqueue(story);
    }
}
```

### Memory Management

**Best Practices:**
- ✅ Cache compiled Ink JSON (don't reload each time)
- ✅ Use `Resources.UnloadUnusedAssets()` after dialogue ends
- ✅ Limit conversation history size (e.g., last 1000 nodes per NPC)
- ✅ Serialize conversation history to disk, not in memory

**Conversation History Pruning:**
```csharp
public void PruneConversationHistory(string npcId, int maxNodes = 1000)
{
    var history = npcManager.GetConversationHistory(npcId);
    
    if (history.SeenNodes.Count > maxNodes)
    {
        // Keep only most recent nodes
        var sortedNodes = history.SeenNodes
            .OrderByDescending(node => GetNodeTimestamp(node))
            .Take(maxNodes)
            .ToHashSet();
        
        history.SeenNodes = sortedNodes;
    }
}
```

---

## Unity Editor Tools

### Dialogue Inspector

**Custom Editor Window: `DialogueDebugger`**

```csharp
public class DialogueDebugger : EditorWindow
{
    [MenuItem("Tools/Dialogue Debugger")]
    public static void ShowWindow()
    {
        GetWindow<DialogueDebugger>("Dialogue Debugger");
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to debug dialogue", MessageType.Info);
            return;
        }
        
        var dialogueService = ServiceLocator.Get<IDialogueService>();
        
        EditorGUILayout.LabelField("Current Dialogue", EditorStyles.boldLabel);
        EditorGUILayout.LabelField("Active:", dialogueService.IsDialogueActive.ToString());
        
        if (dialogueService.IsDialogueActive)
        {
            EditorGUILayout.LabelField("NPC:", dialogueService.CurrentNpcId);
            
            if (GUILayout.Button("Force End Dialogue"))
            {
                dialogueService.EndDialogue();
            }
        }
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Conversation History", EditorStyles.boldLabel);
        
        // List all NPCs and their seen nodes
        var npcManager = ServiceLocator.Get<INPCManager>();
        foreach (var npc in npcManager.GetAllNPCs())
        {
            var seenNodes = dialogueService.GetSeenNodes(npc.NpcId);
            EditorGUILayout.LabelField($"{npc.Name}: {seenNodes.Count} nodes seen");
        }
    }
}
```

### Ink Script Validator

**Editor Script: Validates Ink files on import**

```csharp
public class InkScriptValidator : AssetPostprocessor
{
    private static void OnPostprocessAllAssets(
        string[] importedAssets,
        string[] deletedAssets,
        string[] movedAssets,
        string[] movedFromAssetPaths)
    {
        foreach (var assetPath in importedAssets)
        {
            if (assetPath.EndsWith(".ink.json"))
            {
                ValidateInkScript(assetPath);
            }
        }
    }
    
    private static void ValidateInkScript(string path)
    {
        var textAsset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
        
        try
        {
            var story = new Story(textAsset.text);
            Debug.Log($"[InkValidator] ✓ {path} is valid");
        }
        catch (Exception e)
        {
            Debug.LogError($"[InkValidator] ✗ {path} has errors: {e.Message}");
        }
    }
}
```

### Conversation History Viewer

**Custom Editor Window: View NPC conversation history**

```csharp
public class ConversationHistoryViewer : EditorWindow
{
    private Vector2 scrollPosition;
    private string selectedNpcId;
    
    [MenuItem("Tools/Conversation History Viewer")]
    public static void ShowWindow()
    {
        GetWindow<ConversationHistoryViewer>("Conversation History");
    }
    
    private void OnGUI()
    {
        if (!Application.isPlaying)
        {
            EditorGUILayout.HelpBox("Enter Play Mode to view conversation history", MessageType.Info);
            return;
        }
        
        var npcManager = ServiceLocator.Get<INPCManager>();
        var dialogueService = ServiceLocator.Get<IDialogueService>();
        
        // NPC selection dropdown
        EditorGUILayout.LabelField("Select NPC:", EditorStyles.boldLabel);
        var npcs = npcManager.GetAllNPCs().ToList();
        var npcNames = npcs.Select(n => n.Name).ToArray();
        int selectedIndex = npcs.FindIndex(n => n.NpcId == selectedNpcId);
        
        selectedIndex = EditorGUILayout.Popup(selectedIndex, npcNames);
        if (selectedIndex >= 0)
        {
            selectedNpcId = npcs[selectedIndex].NpcId;
        }
        
        if (string.IsNullOrEmpty(selectedNpcId))
            return;
        
        EditorGUILayout.Space();
        
        // Display conversation history
        var seenNodes = dialogueService.GetSeenNodes(selectedNpcId);
        var history = npcManager.GetConversationHistory(selectedNpcId);
        
        EditorGUILayout.LabelField("Conversation Statistics:", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Total Interactions: {history.TotalInteractions}");
        EditorGUILayout.LabelField($"Nodes Seen: {seenNodes.Count}");
        EditorGUILayout.LabelField($"Last Interaction: {history.LastInteraction}");
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Seen Nodes:", EditorStyles.boldLabel);
        
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);
        foreach (var node in seenNodes)
        {
            EditorGUILayout.LabelField($"  • {node}");
        }
        EditorGUILayout.EndScrollView();
        
        EditorGUILayout.Space();
        
        if (GUILayout.Button("Clear History (Debug Only)"))
        {
            if (EditorUtility.DisplayDialog("Clear History?",
                $"Are you sure you want to clear conversation history for {npcs[selectedIndex].Name}?",
                "Yes", "Cancel"))
            {
                npcManager.ClearConversationHistory(selectedNpcId);
            }
        }
    }
}
```

---

## Troubleshooting & Common Pitfalls

### Issue: Ink Variables Not Syncing

**Symptom:** Changes to `sarah_trust` in Ink don't reflect in game

**Cause:** Forgot to call `SyncInkStateToGame()` after dialogue ends

**Solution:**
```csharp
public void EndDialogue()
{
    SyncInkStateToGame(currentNpcId); // ← Must call this!
    // ... rest of cleanup
}
```

### Issue: External Functions Not Found

**Symptom:** `ERROR: [Ink] External function 'add_lead' not found`

**Cause:** External function name mismatch between Ink and C#

**Solution:**
```csharp
// In Ink:
~ add_lead("device", "POS", "Found POS")

// In C#: Name must match EXACTLY
currentStory.BindExternalFunction("add_lead", (string type, string title, string desc) => {
    // ...
});
```

### Issue: Dialogue UI Not Showing

**Symptom:** `StartDialogue()` called but UI doesn't appear

**Cause:** `OnDialogueNodeReached` callback not set

**Solution:**
```csharp
// WRONG:
dialogueService.StartDialogue("sarah", null); // ← Callback is null!

// CORRECT:
dialogueService.StartDialogue("sarah", OnDialogueNodeReached);
```

### Issue: Conversation History Not Saving

**Symptom:** After reload, NPCs repeat conversations

**Cause:** `MarkDialogueNodeSeen()` not being called

**Solution:**
```csharp
private void ContinueStory()
{
    string nodeId = currentStory.state.currentPathString;
    npcManager.MarkDialogueNodeSeen(currentNpcId, nodeId); // ← Must call this!
    // ...
}
```

### Issue: Time Not Pausing During Dialogue

**Symptom:** Time continues ticking during conversations

**Cause:** Forgot to set time context

**Solution:**
```csharp
public void StartDialogue(string npcId, Action<DialogueNode> onNodeReached)
{
    // ...
    timeManager.SetTimeContext(TimeContext.Conversation); // ← Add this!
    // ...
}
```

---

## Quick Start Guide

### 1. Install Ink

```bash
# Unity Package Manager
# Add: https://github.com/inkle/ink-unity-integration.git#upm
```

### 2. Create Your First Ink Script

```ink
// Assets/Resources/Dialogue/NPCs/test_npc.ink

VAR test_trust = 50

=== talk_to_test_npc ===
Hello! This is a test conversation.

* [Option 1] -> option_1
* [Option 2] -> option_2

=== option_1 ===
~ test_trust += 10
You chose option 1! Trust increased.
-> END

=== option_2 ===
~ test_trust -= 5
You chose option 2. Trust decreased.
-> END
```

### 3. Compile Ink to JSON

In Unity: Right-click `.ink` file → Ink → Compile

### 4. Create NPC Data

```csharp
var testNpc = new NPC
{
    NpcId = "test_npc",
    Name = "Test NPC",
    DialogueScriptId = "test_npc", // Matches ink filename
    // ...
};

npcManager.RegisterNPC(testNpc);
```

### 5. Trigger Dialogue

```csharp
// In your interaction script:
void OnNPCClicked()
{
    dialogueUIController.ShowDialogue("test_npc");
}
```

---

## Summary

The Dialogue System achieves:

- ✅ **Ink Integration:** Narrative designers write in Ink, programmers expose game systems
- ✅ **Service Architecture:** DialogueService handles logic, NPCManager stores history
- ✅ **Clean Separation:** UI is pure presentation, no business logic
- ✅ **Extensible:** Easy to add external functions, voice-over, portraits, timeouts
- ✅ **Save-Friendly:** Conversation history stored in centralized NPCManager
- ✅ **Production-Ready:** Versioning, migration, debugging tools included
