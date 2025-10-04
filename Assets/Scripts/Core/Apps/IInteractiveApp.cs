using SampleOS.Core.Devices;

namespace SampleOS.Core.Apps
{
  /// <summary>
  /// Represents an app that the player can actually interact with
  /// Not just software that exists, but software with a UI
  /// </summary>
  public interface IInteractiveApp
  {
    string AppId { get; }
    string DisplayName { get; }
    AppCategory Category { get; }

    // Which devices can run this app?
    bool CanRunOnDevice(Device device);

    // UI integration
    void OnAppOpened();
    void OnAppClosed();
    void RenderUI(); // Called by Unity UI system

    // Save state when switching apps
    object SerializeState();
    void DeserializeState(object state);
  }

  public enum AppCategory
  {
    Terminal,
    Email,
    WebBrowser,
    FileManager,
    TextEditor,
    IRC,
    Messenger,
    Security,     // Firewall, antivirus, etc.
    Development,  // IDEs, debuggers
    System,       // Settings, task manager
    Game,         // In-game minigames
    Custom        // Story-specific apps
  }
}
