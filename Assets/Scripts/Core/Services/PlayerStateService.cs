using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking.Access;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Player;
using SampleOS.Core.Session;
using SampleOS.Core.SoftwarePackages;
using UnityEngine;

namespace SampleOS.Core.Services
{
  public interface IPlayerStateService
  {
    // Current device and connection state
    PlayerDevice PlayerDevice { get; }
    Device CurrentDevice { get; }
    List<RemoteConnection> ActiveConnections { get; }
    bool IsOnRemoteDevice { get; }

    // Player subsystems
    PlayerInventory Inventory { get; }
    PlayerProgress Progress { get; }
    PlayerCredentials Credentials { get; }

    // Helper methods that do the vulnerability resolution
    Vulnerability ResolveVulnerability(string cve);
    Vulnerability ResolveVulnerability(DiscoveredVulnerability discoveredVuln);
    string GenerateVulnerabilityReport();

    // Initialization
    void Initialize(PlayerDevice startingDevice);

    // Device management
    void AddOwnedDevice(PlayerDevice device);
    void SwitchDevice(PlayerDevice device);
    Result<RemoteConnection> ConnectToDevice(Device target, string username, string password);
    void DisconnectFromDevice();

    // Progress tracking
    void RecordSystemCompromise(Device device, string exploitUsed, bool hasRoot);
    void RecordVulnerabilityDiscovery(string hostname, string softwareName, Vulnerability vuln);
    bool HasCompromisedSystem(string hostname);
    List<OwnedSystemInfo> GetOwnedSystems();

    // Credential management
    void StoreCredentials(string hostname, string username, string password);
    bool HasCredentialsFor(string hostname);
    (string username, string password) GetCredentialsFor(string hostname);

    // Network Discovery methods
    void AddDiscoveryClue(DiscoveryClue clue);
    void MarkNetworkDiscovered(string networkId);
    bool IsNetworkDiscovered(string networkId);
    List<DiscoveryClue> GetCluesForNetwork(string networkId);
    List<DiscoveryClue> GetAllClues();
    ScanResults ScanDeviceForSecrets(Device device);

    // Events
    event Action<PlayerDevice> OnDeviceAcquired;
    event Action<Device> OnDeviceChanged;
    event Action<RemoteConnection> OnConnectionEstablished;
    event Action<RemoteConnection> OnConnectionClosed;
    event Action<OwnedSystemInfo> OnSystemCompromised;

    // State management
    PlayerSaveData GetSaveData();
    void LoadFromSave(PlayerSaveData saveData);
  }

  public class PlayerStateService : IPlayerStateService
  {
    // Core state
    public PlayerDevice PlayerDevice { get; private set; }
    public Device CurrentDevice { get; private set; }
    public List<RemoteConnection> ActiveConnections { get; private set; }
    public bool IsOnRemoteDevice => ActiveConnections.Count > 0;

    // Player subsystems
    public PlayerInventory Inventory { get; private set; }
    public PlayerProgress Progress { get; private set; }
    public PlayerCredentials Credentials { get; private set; }

    // Clue / Network Discovery
    private Dictionary<string, bool> discoveredNetworks = new Dictionary<string, bool>();
    private List<DiscoveryClue> discoveredClues = new List<DiscoveryClue>();

    // Events
    public event Action<PlayerDevice> OnDeviceAcquired;
    public event Action<Device> OnDeviceChanged;
    public event Action<RemoteConnection> OnConnectionEstablished;
    public event Action<RemoteConnection> OnConnectionClosed;
    public event Action<OwnedSystemInfo> OnSystemCompromised;

    public void Initialize(PlayerDevice startingDevice)
    {
      PlayerDevice = startingDevice;
      CurrentDevice = startingDevice;
      ActiveConnections = new List<RemoteConnection>();

      // Initialize subsystems
      Inventory = new PlayerInventory();
      Inventory.AddDevice(startingDevice);

      Progress = new PlayerProgress();
      Credentials = new PlayerCredentials();

      // Subscribe to progress events
      Progress.OnSystemCompromised += (info) => OnSystemCompromised?.Invoke(info);

      Debug.Log($"Player state initialized with device: {startingDevice.Hostname}");
    }

    #region Vulnerability Resolution (bridges Progress and VulnDb)

    /// <summary>
    /// Resolve a CVE ID to its full Vulnerability object
    /// </summary>
    public Vulnerability ResolveVulnerability(string cve)
    {
      var vulnDb = ServiceLocator.Instance.Get<IVulnerabilityDatabaseService>();
      return vulnDb?.GetVulnerability(cve);
    }

    /// <summary>
    /// Resolve a DiscoveredVulnerability to its full Vulnerability object
    /// </summary>
    public Vulnerability ResolveVulnerability(DiscoveredVulnerability discoveredVuln)
    {
      return ResolveVulnerability(discoveredVuln.CVE);
    }

    /// <summary>
    /// Generate vulnerability report with full details
    /// </summary>
    public string GenerateVulnerabilityReport()
    {
      return Progress.GenerateVulnerabilityReport(cve => ResolveVulnerability(cve));
    }

    #endregion

    #region Device Management
    public void AddOwnedDevice(PlayerDevice device)
    {
      Inventory.AddDevice(device);
      OnDeviceAcquired?.Invoke(device);
      Debug.Log($"Acquired device: {device.Hostname}");
    }

    public void SwitchDevice(PlayerDevice device)
    {
      if (!Inventory.OwnedDevices.Contains(device))
      {
        Debug.LogWarning($"Cannot switch to device {device.Hostname} - not owned");
        return;
      }

      // Clear active connections when switching devices
      ActiveConnections.Clear();
      CurrentDevice = device;

      OnDeviceChanged?.Invoke(device);
      GameEvents.Instance.Trigger(GameEventType.PlayerDeviceSwitched, device);

      Debug.Log($"Switched to device: {device.Hostname}");
    }

    public Result<RemoteConnection> ConnectToDevice(Device target, string username, string password)
    {
      if (!target.Authenticate(username, password))
      {
        return Result<RemoteConnection>.Failure("Authentication failed");
      }

      var connection = new RemoteConnection(PlayerDevice, target, username);
      ActiveConnections.Add(connection);

      CurrentDevice = target;

      OnDeviceChanged?.Invoke(target);
      OnConnectionEstablished?.Invoke(connection);

      Debug.Log($"Connected to {target.Hostname} as {username}");
      return Result<RemoteConnection>.Success(connection);
    }

    public void DisconnectFromDevice()
    {
      if (ActiveConnections.Count == 0)
        return;

      var connection = ActiveConnections[ActiveConnections.Count - 1];
      ActiveConnections.RemoveAt(ActiveConnections.Count - 1);

      CurrentDevice = ActiveConnections.Count > 0
          ? ActiveConnections[ActiveConnections.Count - 1].TargetDevice
          : PlayerDevice;

      OnDeviceChanged?.Invoke(CurrentDevice);
      OnConnectionClosed?.Invoke(connection);

      Debug.Log($"Disconnected from {connection.TargetDevice.Hostname}");
    }

    #endregion

    #region Network Discovery

    public void AddDiscoveryClue(DiscoveryClue clue)
    {
      // Avoid duplicates
      var existing = discoveredClues.FirstOrDefault(c =>
          c.NetworkId == clue.NetworkId &&
          c.Type == clue.Type &&
          c.FilePath == clue.FilePath);

      if (existing != null)
        return;

      discoveredClues.Add(clue);

      // Auto-discover networks if we have enough reliable clues
      var networkClues = GetCluesForNetwork(clue.NetworkId);
      var reliableClues = networkClues.Where(c => c.ReliabilityScore >= 70).ToList();

      if (reliableClues.Count >= 2)
      {
        MarkNetworkDiscovered(clue.NetworkId);
      }
    }

    public void MarkNetworkDiscovered(string networkId)
    {
      if (!discoveredNetworks.ContainsKey(networkId))
      {
        discoveredNetworks[networkId] = true;

        // Unlock the network in NetworkService
        var networkService = ServiceLocator.Instance.Get<INetworkService>();
        networkService?.UnlockNetwork(networkId, NetworkAccessType.DirectConnection);

        Debug.Log($"Network discovered: {networkId}");
      }
    }

    public bool IsNetworkDiscovered(string networkId)
    {
      return discoveredNetworks.ContainsKey(networkId) && discoveredNetworks[networkId];
    }

    public List<DiscoveryClue> GetCluesForNetwork(string networkId)
    {
      return discoveredClues.Where(c => c.NetworkId == networkId).ToList();
    }

    public List<DiscoveryClue> GetAllClues()
    {
      return new List<DiscoveryClue>(discoveredClues);
    }

    /// <summary>
    /// Scan device files for network clues and credentials
    /// Call this when player compromises a device
    /// </summary>
    public ScanResults ScanDeviceForSecrets(Device device)
    {
      var scanner = new CredentialScanner();
      var results = scanner.ScanDeviceForCredentials(device);

      // Process found credentials
      foreach (var sshCred in results.Credentials.SSHCredentials)
      {
        Credentials.StoreSSHCredential(sshCred.Hostname, sshCred.Username, sshCred.Password);
      }

      if (results.Credentials.VPNCredentials != null && results.Credentials.VPNCredentials.IsValid())
      {
        var vpn = results.Credentials.VPNCredentials;
        Credentials.StoreVPNCredential(
          vpn.NetworkId, vpn.NetworkName, vpn.Username,
          vpn.Password, vpn.ServerAddress, vpn.Port, vpn.Protocol
        );
      }

      // Process discovery clues
      foreach (var clue in results.DiscoveredClues)
      {
        AddDiscoveryClue(clue);
      }

      return results;
    }

    #endregion

    #region Progress Tracking (Delegated)

    public void RecordSystemCompromise(Device device, string exploitUsed, bool hasRoot)
    {
      Progress.RecordSystemCompromise(device, exploitUsed, hasRoot);
    }

    public void RecordVulnerabilityDiscovery(string hostname, string softwareName, Vulnerability vuln)
    {
      Progress.AddDiscoveredVulnerability(hostname, softwareName, vuln);
    }

    public bool HasCompromisedSystem(string hostname)
    {
      return Progress.HasCompromisedSystem(hostname);
    }

    public List<OwnedSystemInfo> GetOwnedSystems()
    {
      return Progress.GetOwnedSystems();
    }

    #endregion

    #region Credential Management (Delegated)

    public void StoreCredentials(string hostname, string username, string password)
    {
      Credentials.StoreSSHCredential(hostname, username, password);
    }

    public bool HasCredentialsFor(string hostname)
    {
      return Credentials.HasCredentialsFor(hostname);
    }

    public (string username, string password) GetCredentialsFor(string hostname)
    {
      return Credentials.GetSSHCredentials(hostname);
    }

    #endregion

    #region Save/Load

    public PlayerSaveData GetSaveData()
    {
      return new PlayerSaveData
      {
        playerDeviceId = PlayerDevice.DeviceId,
        currentDeviceId = CurrentDevice.DeviceId,
        activeConnectionCount = ActiveConnections.Count,

        // Subsystem data
        inventoryData = Inventory.GetSaveData(),
        progressData = Progress.GetSaveData(),
        credentialsData = Credentials.GetSaveData()
      };
    }

    public void LoadFromSave(PlayerSaveData data)
    {
      if (data == null) return;

      // Restore subsystems
      Inventory.LoadFromSave(data.inventoryData);
      Progress.LoadFromSave(data.progressData);
      Credentials.LoadFromSave(data.credentialsData);

      Debug.Log("Player state loaded from save");
    }

    #endregion
  }

  [Serializable]
  public class PlayerSaveData
  {
    public string playerDeviceId;
    public string currentDeviceId;
    public int activeConnectionCount;

    public PlayerInventorySaveData inventoryData;
    public PlayerProgressSaveData progressData;
    public PlayerCredentialsSaveData credentialsData;
  }

}
