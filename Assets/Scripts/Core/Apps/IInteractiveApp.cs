using SampleOS.Core.Devices;
using System;

namespace SampleOS.Core.Apps
{
  /// <summary>
  /// Represents an interactive app instance running on a specific device
  /// Each instance is tied to a device and has its own state
  /// </summary>
  public interface IInteractiveApp
  {
    // Unique instance identifier (not app type)
    string InstanceId { get; }
    
    // App metadata
    string AppId { get; }           // e.g., "terminal", "email"
    string DisplayName { get; }     // e.g., "Terminal", "Email Client"
    AppCategory Category { get; }
    
    // Device binding (NEW)
    Device HostDevice { get; }
    
    // Lifecycle
    void Initialize(Device hostDevice);
    void Update(float deltaTime);
    void Shutdown();
    
    // UI integration
    void OnAppOpened();
    void OnAppClosed();
    void OnFocusGained();
    void OnFocusLost();
    
    // State management
    object SerializeState();
    void DeserializeState(object state);
    
    // Which devices can run this app?
    bool CanRunOnDevice(Device device);
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
    Security,
    Development,
    System,
    Game,
    Custom
  }
}
