Questions we still need to answer about our game:
**File Systems:**
- **Question**: Are device file systems static templates or dynamically generated?
  **Answer**: I think a Hybrid with core state and lazy details makes the most sense
- **Question**: If a player deletes a file, does it stay deleted?
  **Answer**: Yes, absolutely
- **Question**: Do NPCs add files over time? (e.g., Sarah's laptop gains new emails each day)
  **Answer**: Yes, NPC's device should feel dynamic with the NPC's adding files, emails, and chats

**Network Topology:**
- **Question**: Are networks fixed or do they change? (e.g., coffee shop adds a new security camera)
  **Answer**: Some quest should absolutely change network topology. Sometimes NPC's schedules change network topology (phone / tablet / laptop moving through out the day)
- **Question**: Can the player's backdoors be discovered and closed?
  **Answer**: Yes, sometimes they will be close just by a patch coming out, sometimes it will be discovered and their heat raised and it closed
- **Question**: Do networks have "states" (e.g., high alert after a hack)?
  **Answer**: We probably should... and some honeypot networks. 
- **Question**: Do devices reboot and kick the player out?
  **Answer**: Probably not
  
## Device Code Right Now
(/Scripts/Devices/Device.cs)

Changes that will likely need to be thought about:
```C#
public class Device
{
    // Always in memory (small)
    public string DeviceId;
    public bool IsCompromised;
    public List<string> ActiveBackdoors;
    
    // Lazy-loaded (large)
    private VirtualFileSystem _fileSystem;
    public VirtualFileSystem FileSystem
    {
        get
        {
            if (_fileSystem == null)
                _fileSystem = FileSystemFactory.CreateForDevice(this);
            return _fileSystem;
        }
    }
}
```
