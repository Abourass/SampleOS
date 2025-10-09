using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleOS.Core.Devices;
using SampleOS.Core.Session;
using SampleOS.Core.CommandSystem;
using UnityEngine;

namespace SampleOS.Core.Services
{
  public interface IHackingSessionService
  {
    // Current session state
    Device CurrentDevice { get; }
    List<RemoteConnection> ActiveConnections { get; }
    bool IsOnRemoteDevice { get; }

    // Initialization
    void Initialize(PlayerDevice playerDevice);
    void Update(float deltaTime);

    // Connection management
    Result<RemoteConnection> ConnectToDevice(Device target, string username, string password);
    void DisconnectFromDevice();
    void SwitchToConnection(RemoteConnection connection);

    // Exploit tracking
    void RegisterActiveExploit(ActiveExploit exploit);
    List<ActiveExploit> GetRunningExploits();

    // Events
    event Action<Device> OnDeviceChanged;
    event Action<RemoteConnection> OnConnectionEstablished;
    event Action<RemoteConnection> OnConnectionClosed;
    event Action<Device> OnDeviceCompromised;

    // State management
    object GetSaveData();
    void LoadFromSave(object saveData);
  }

  public class HackingSessionService : IHackingSessionService
  {
    private IPlayerStateService playerState;
    private CommandProcessor commandProcessor;
    private List<ActiveExploit> runningExploits;

    public Device CurrentDevice => playerState.CurrentDevice;
    public List<RemoteConnection> ActiveConnections => playerState.ActiveConnections;
    public bool IsOnRemoteDevice => playerState.IsOnRemoteDevice;
    public bool IsWaitingForInput { get; set; }

    public event Action<Device> OnDeviceChanged;
    public event Action<RemoteConnection> OnConnectionEstablished;
    public event Action<RemoteConnection> OnConnectionClosed;
    public event Action<Device> OnDeviceCompromised;

    public void Initialize(PlayerDevice playerDevice)
    {
      // Get player state service
      playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
      {
        Debug.LogError("PlayerStateService must be initialized before HackingSessionService!");
        return;
      }

      runningExploits = new List<ActiveExploit>();

      // Create command processor
      commandProcessor = new CommandProcessor();
      commandProcessor.Initialize();

      // Subscribe to player state events
      playerState.OnDeviceChanged += (device) => OnDeviceChanged?.Invoke(device);
      playerState.OnConnectionEstablished += (conn) => OnConnectionEstablished?.Invoke(conn);
      playerState.OnConnectionClosed += (conn) => OnConnectionClosed?.Invoke(conn);
      playerState.OnSystemCompromised += (info) => OnDeviceCompromised?.Invoke(CurrentDevice);

      Debug.Log($"Hacking session initialized");
    }

    public void Update(float deltaTime)
    {
      // Update active exploits
      for (int i = runningExploits.Count - 1; i >= 0; i--)
      {
        runningExploits[i].Update(deltaTime);
        if (runningExploits[i].IsComplete)
        {
          runningExploits.RemoveAt(i);
        }
      }
    }

    public Result<RemoteConnection> ConnectToDevice(Device target, string username, string password)
    {
      return playerState.ConnectToDevice(target, username, password);
    }

    public void DisconnectFromDevice()
    {
      playerState.DisconnectFromDevice();
    }

    public void SwitchToConnection(RemoteConnection connection)
    {
      // Implementation depends on how you want to handle multiple connections
      // For now, just switch to the target device
      OnDeviceChanged?.Invoke(connection.TargetDevice);
    }

    public void RegisterActiveExploit(ActiveExploit exploit)
    {
      runningExploits.Add(exploit);
    }

    public List<ActiveExploit> GetRunningExploits() => runningExploits;

    public async Task<CommandResult> ProcessCommandAsync(string input, object outputHandler)
    {
      // Delegate to command processor
      return await commandProcessor.ExecuteAsync(input);
    }

    public Device GetCurrentDevice() => CurrentDevice;

    public object GetSaveData()
    {
      return new HackingSaveData
      {
        activeConnectionCount = ActiveConnections.Count,
        runningExploitCount = runningExploits.Count
      };
    }

    public void LoadFromSave(object saveData)
    {
      if (saveData is HackingSaveData data)
      {
        // Restore hacking state
        Debug.Log($"Hacking session loaded: {data.activeConnectionCount} connections");
      }
    }

    private class HackingSaveData
    {
      public int activeConnectionCount;
      public int runningExploitCount;
    }
  }

  /// <summary>
  /// Represents an active exploit/script running
  /// </summary>
  public class ActiveExploit
  {
    public string Name { get; set; }
    public Device TargetDevice { get; set; }
    public float Progress { get; private set; }
    public bool IsComplete { get; private set; }

    private float duration;
    private float elapsed;

    public ActiveExploit(string name, Device target, float duration)
    {
      Name = name;
      TargetDevice = target;
      this.duration = duration;
    }

    public void Update(float deltaTime)
    {
      elapsed += deltaTime;
      Progress = Mathf.Clamp01(elapsed / duration);

      if (Progress >= 1f)
      {
        IsComplete = true;
      }
    }
  }
}
