using SampleOS.Core.Devices;
using SampleOS.Core.Apps;
using System.Collections.Generic;

namespace SampleOS.Core.Session
{
  /// <summary>
  /// Manages the player's current session state
  /// Which device they're using, which apps are open, etc.
  /// </summary>
  public class PlayerSession
  {
    // Current device player is using
    public PlayerDevice CurrentDevice { get; private set; }

    // Active remote connections from current device
    public List<RemoteConnection> ActiveConnections { get; private set; }

    // Which app is currently focused
    public IInteractiveApp FocusedApp { get; private set; }

    // Player inventory (owned devices)
    public List<PlayerDevice> OwnedDevices { get; private set; }

    public PlayerSession()
    {
      ActiveConnections = new List<RemoteConnection>();
      OwnedDevices = new List<PlayerDevice>();
    }

    public void SwitchToDevice(PlayerDevice device)
    {
      if (!OwnedDevices.Contains(device))
        return;

      CurrentDevice = device;
      // Load device UI state
    }

    public Result<RemoteConnection> ConnectToRemote(Device target, string username, string password)
    {
      if (!target.Authenticate(username, password))
        return Result<RemoteConnection>.Failure("Authentication failed");

      var connection = new RemoteConnection(CurrentDevice, target, username);
      ActiveConnections.Add(connection);

      return Result<RemoteConnection>.Success(connection);
    }

    public void OpenApp(IInteractiveApp app)
    {
      if (!app.CanRunOnDevice(CurrentDevice))
        return;

      app.OnAppOpened();
      FocusedApp = app;
    }
  }

  public class RemoteConnection
  {
    public Device SourceDevice { get; private set; }
    public Device TargetDevice { get; private set; }
    public string Username { get; private set; }
    public string ConnectionType { get; private set; } // "ssh", "rdp", etc.
    public System.DateTime ConnectedAt { get; private set; }

    public RemoteConnection(Device source, Device target, string user)
    {
      SourceDevice = source;
      TargetDevice = target;
      Username = user;
      ConnectedAt = System.DateTime.Now;
    }
  }
}
