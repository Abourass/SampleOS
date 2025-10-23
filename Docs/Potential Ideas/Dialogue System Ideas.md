I'm thinking we should use Ink for this, so we can keep this section more organized and readable. We would have to wrap Ink in our own `DialogueManager` that:
- Handles NPC-specific relationship tracking
- Triggers lead creation
- Integrates with quest system
- Manages one-time conversation locks

**Example Ink script:**
```ink
=== talk_to_sarah ===
{ sarah_trust >= 50:
    Sarah greets you warmly.
    - else:
    Sarah seems guarded.
}

* [Ask about BigTechCorp job]
    -> ask_about_job
* [Ask about Phoenix] 
    { sarah_trust >= 70 } // Only shows if trust is high enough
    -> ask_about_phoenix
* [Leave]
    -> END

=== ask_about_job ===
Sarah: "Oh! I actually just heard back. I got the position!"

* [Congratulate her]
    ~ sarah_trust += 5
    ~ sarah_relationship = "supportive"
    Sarah smiles. "Thanks! I put in a good word for you too."
    -> mention_referral
    
* [Feel jealous]
    ~ sarah_trust -= 10
    ~ sarah_relationship = "competitive"
    Sarah notices your reaction and frowns slightly.
    -> awkward_silence
```

**Potential Integration with Unity:**
```csharp
public class DialogueManager : MonoBehaviour
{
    private Story currentStory;
    
    public void StartDialogue(TextAsset inkJSON, string npcId)
    {
        currentStory = new Story(inkJSON.text);
        
        // Bind external functions (called from Ink)
        currentStory.BindExternalFunction("add_lead", (string leadTitle, string leadDesc) => {
            LeadManager.Instance.CreateLead(LeadType.Person, leadTitle, leadDesc, ...);
        });
        
        currentStory.BindExternalFunction("unlock_quest", (string questId) => {
            QuestManager.Instance.UnlockQuest(questId);
        });
        
        // Set variables from game state
        currentStory.variablesState["player_karma"] = GameState.Instance.Player.Karma;
        currentStory.variablesState["sarah_trust"] = GameState.Instance.GetRelationship("sarah");
        
        ContinueStory();
    }
    
    private void ContinueStory()
    {
        if (currentStory.canContinue)
        {
            string text = currentStory.Continue();
            dialogueUI.ShowText(text);
        }
        
        if (currentStory.currentChoices.Count > 0)
        {
            dialogueUI.ShowChoices(currentStory.currentChoices);
        }
    }
    
    public void MakeChoice(int choiceIndex)
    {
        currentStory.ChooseChoiceIndex(choiceIndex);
        ContinueStory();
        
        // After dialogue, sync state back to game
        SyncStateFromInk();
    }
    
    private void SyncStateFromInk()
    {
        // Read variables back from Ink
        if (currentStory.variablesState.GlobalVariableExistsWithName("sarah_trust"))
        {
            int trust = (int)currentStory.variablesState["sarah_trust"];
            GameState.Instance.SetRelationship("sarah", trust);
        }
    }
}
```

**Track read nodes something like this maybe?**
```csharp
public class ConversationTracker
{
    // Track which dialogue nodes have been seen
    private Dictionary<string, HashSet<string>> npcSeenNodes;
    
    public void MarkNodeAsSeen(string npcId, string nodeId)
    {
        if (!npcSeenNodes.ContainsKey(npcId))
            npcSeenNodes[npcId] = new HashSet<string>();
        npcSeenNodes[npcId].Add(nodeId);
    }
    
    public bool HasSeenNode(string npcId, string nodeId)
    {
        return npcSeenNodes.ContainsKey(npcId) && 
               npcSeenNodes[npcId].Contains(nodeId);
    }
}
```

In Ink, I think then you'd use it like this:
```ink
* { not has_told_sarah_about_hack } [Tell Sarah you hacked the café]
    ~ mark_conversation_flag("told_sarah_about_hack")
    ~ sarah_trust -= 20
    Sarah looks shocked. "You... you hacked them? That's illegal!"
    -> sarah_upset
```