**What needs to be saved?**
- Player stats, inventory, compromised devices
- All quest states and objective progress
- All leads and their investigation states
- NPC relationships and conversation history
- World state (device states, network topologies, file systems)
- Time and date
- Karma and reputation with various groups
- Heat levels with different factions
**When do we save?**
- Autosave after major events, with manual saving allowed?
- Do we care about about Save Scumming at all?

Thoughts so far:
- Use JSON for human-readable saves during development (easy to debug/edit)
- Plan to switch to binary serialization later for security/size