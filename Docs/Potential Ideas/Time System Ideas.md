```C#
public class TimeManager : MonoBehaviour
{
    // Time configuration
    public float RealSecondsPerGameMinute = 0.7f; // ~17 minutes real time = 24 game hours (I actually think this is too short, but hey we gotta start somewhere)
    
    // Current time state
    private DateTime currentGameTime;
    private bool isPaused;
    
    // Time scaling for different activities
    private Dictionary<TimeContext, float> timeScales = new()
    {
        { TimeContext.Walking, 1f },           // Normal time
        { TimeContext.DeviceUse, 0f },         // Paused during hacking
        { TimeContext.Conversation, 0f },      // Paused during dialogue
        { TimeContext.JobMinigame, 0.5f },     // Slower during work tasks
        { TimeContext.Travel, 3f }             // Faster for long walks
    };
    
    // Events other systems can subscribe to
    public event Action<int> OnMinutePassed;
    public event Action<int> OnHourChanged;
    public event Action<DayOfWeek> OnDayChanged;
    
    // Special action time costs
    public void ConsumeTime(TimeSpan duration, string reason)
    {
        // "Sent that phishing email" - costs 5 minutes
        // "Ran automated script" - costs 30 minutes
        // etc.
    }
}
```

I'm thinking we should pause time while interacting with a device with some actions on the device causing time to be spent. My thoughts are that:
- Players can take their time learning hacking mechanics without stress
- Allows for puzzle-solving without time pressure
- BUT you can still have scripts/exploits that take time when executed (consume time when you hit "run")

