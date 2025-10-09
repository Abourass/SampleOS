using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Networking;
using SampleOS.Core.Devices;
using SampleOS.Core.World;
using UnityEngine;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Networking.Access;
using SampleOS.Core.Networking.Connections;

namespace SampleOS.Core.Services
{
  public interface INetworkService
  {
    // Initialization
    void Initialize(List<VirtualNetwork> networks);
    void Update(float deltaTime);

    // Network queries (optimized for 10k+ devices)
    List<Device> GetDevicesInNetwork(string networkId);
    List<Device> GetDevicesAtLocation(PhysicalLocation location);
    Device GetDeviceById(string deviceId);
    VirtualNetwork GetNetwork(string networkId);

    // Connection Management
    Result<NetworkConnection> EstablishConnection(
        string sourceNetwork,
        string targetNetwork,
        ConnectionType type,
        Dictionary<string, object> parameters = null);
    Result<bool> DisconnectFromNetwork(string connectionId);
    NetworkConnection GetActiveConnection(string sourceNetwork, string targetNetwork);
    List<NetworkConnection> GetActiveConnections();
    string GenerateConnectionReport();

    // Network Access Management
    Result<bool> CheckNetworkAccess(string networkId);
    Result<bool> RequestNetworkAccess(string networkId, NetworkCredentials credentials);
    void UnlockNetwork(string networkId, NetworkAccessType accessType);
    NetworkAccessProfile GetAccessProfile(string networkId);
    List<string> GetAccessibleNetworks();

    // Network state updates (batched for performance)
    void UpdateSecurityStates(float deltaTime);
    void PropagateAlertState(Device sourceDevice);

    // Spatial queries (for physical device discovery)
    List<Device> GetDevicesInRadius(Vector3 center, float radius);

    // Events
    event System.Action<NetworkConnection> OnConnectionEstablished;
    event System.Action<NetworkConnection> OnConnectionLost;

    // State management
    object GetSaveData();
    void LoadFromSave(object saveData);
  }

  public class NetworkService : INetworkService
  {
    private List<VirtualNetwork> networks;
    private Dictionary<string, Device> deviceCache; // Fast lookup by DeviceId
    private Dictionary<string, List<Device>> locationIndex; // Fast lookup by LocationId

    // Connection management
    private Dictionary<string, NetworkConnection> activeConnections = new Dictionary<string, NetworkConnection>();
    private List<NetworkConnection> connectionHistory = new List<NetworkConnection>();
    private System.TimeSpan connectionTimeout = System.TimeSpan.FromMinutes(30);

    // Network access management
    private Dictionary<string, NetworkAccessProfile> accessProfiles = new Dictionary<string, NetworkAccessProfile>();
    private HashSet<string> unlockedNetworks = new HashSet<string>(); // Networks player can access

    // Performance optimization: only update security states periodically
    private float securityUpdateTimer = 0f;
    private const float SECURITY_UPDATE_INTERVAL = 5f; // Every 5 seconds

    // Events
    public event System.Action<NetworkConnection> OnConnectionEstablished;
    public event System.Action<NetworkConnection> OnConnectionLost;

    public void Initialize(List<VirtualNetwork> networkList)
    {
      networks = networkList;

      // Build indices for fast lookups
      BuildDeviceIndices();
      InitializeAccessProfiles();

      Debug.Log($"Network service initialized with {deviceCache.Count} devices across {networks.Count} networks");
    }

    private void BuildDeviceIndices()
    {
      deviceCache = new Dictionary<string, Device>();
      locationIndex = new Dictionary<string, List<Device>>();

      foreach (var network in networks)
      {
        foreach (var device in network.GetAllDevices())
        {
          // Add to device cache
          deviceCache[device.DeviceId] = device;

          // Add to location index if device has a physical location
          if (!string.IsNullOrEmpty(device.LocationId))
          {
            if (!locationIndex.ContainsKey(device.LocationId))
            {
              locationIndex[device.LocationId] = new List<Device>();
            }
            locationIndex[device.LocationId].Add(device);
          }
        }
      }
    }

    #region Connection Management

    public Result<NetworkConnection> EstablishConnection(
        string sourceNetwork,
        string targetNetwork,
        ConnectionType type,
        Dictionary<string, object> parameters = null)
    {
      try
      {
        // Check if connection already exists
        var existingConnection = GetActiveConnection(sourceNetwork, targetNetwork);
        if (existingConnection != null)
        {
          existingConnection.UpdateActivity();
          return Result<NetworkConnection>.Success(existingConnection);
        }

        // Create new connection
        var connection = new NetworkConnection(sourceNetwork, targetNetwork, type);

        if (parameters != null)
        {
          foreach (var param in parameters)
            connection.Parameters[param.Key] = param.Value;
        }

        // Simulate connection establishment
        var metrics = SimulateConnectionMetrics(type);
        connection.Status = ConnectionStatus.Connected;
        connection.Latency = metrics.Latency;
        connection.Bandwidth = metrics.Bandwidth;
        connection.IsEncrypted = metrics.IsEncrypted;

        activeConnections[connection.ConnectionId] = connection;
        connectionHistory.Add(connection);

        OnConnectionEstablished?.Invoke(connection);

        return Result<NetworkConnection>.Success(connection);
      }
      catch (System.Exception ex)
      {
        return Result<NetworkConnection>.Failure($"Connection error: {ex.Message}");
      }
    }

    public Result<bool> DisconnectFromNetwork(string connectionId)
    {
      if (!activeConnections.TryGetValue(connectionId, out NetworkConnection connection))
      {
        return Result<bool>.Failure("Connection not found");
      }

      connection.Status = ConnectionStatus.Disconnected;
      activeConnections.Remove(connectionId);

      OnConnectionLost?.Invoke(connection);

      return Result<bool>.Success(true);
    }

    public NetworkConnection GetActiveConnection(string sourceNetwork, string targetNetwork)
    {
      return activeConnections.Values.FirstOrDefault(c =>
          c.SourceNetworkId == sourceNetwork &&
          c.TargetNetworkId == targetNetwork &&
          c.Status == ConnectionStatus.Connected);
    }

    public List<NetworkConnection> GetActiveConnections()
    {
      return new List<NetworkConnection>(activeConnections.Values);
    }

    public string GenerateConnectionReport()
    {
      var report = new System.Text.StringBuilder();
      report.AppendLine("ACTIVE NETWORK CONNECTIONS");
      report.AppendLine("=========================");
      report.AppendLine();

      if (!activeConnections.Any())
      {
        report.AppendLine("No active connections.");
        return report.ToString();
      }

      foreach (var connection in activeConnections.Values)
      {
        report.AppendLine($"Connection: {connection.ConnectionId}");
        report.AppendLine($"  Route: {connection.SourceNetworkId} -> {connection.TargetNetworkId}");
        report.AppendLine($"  Type: {connection.Type}");
        report.AppendLine($"  Quality: {connection.GetQualityScore()}/100");
        report.AppendLine($"  Encrypted: {(connection.IsEncrypted ? "Yes" : "No")}");
        report.AppendLine();
      }

      return report.ToString();
    }

    private ConnectionMetrics SimulateConnectionMetrics(ConnectionType type)
    {
      switch (type)
      {
        case ConnectionType.VPN:
          return new ConnectionMetrics
          {
            Latency = UnityEngine.Random.Range(50f, 200f) + 20f,
            Bandwidth = UnityEngine.Random.Range(10f, 100f) * 0.8f,
            IsEncrypted = true
          };

        case ConnectionType.SSH:
          return new ConnectionMetrics
          {
            Latency = UnityEngine.Random.Range(30f, 150f),
            Bandwidth = UnityEngine.Random.Range(5f, 50f),
            IsEncrypted = true
          };

        case ConnectionType.Direct:
          return new ConnectionMetrics
          {
            Latency = UnityEngine.Random.Range(1f, 50f),
            Bandwidth = UnityEngine.Random.Range(50f, 1000f),
            IsEncrypted = false
          };

        default:
          return new ConnectionMetrics
          {
            Latency = UnityEngine.Random.Range(100f, 500f),
            Bandwidth = UnityEngine.Random.Range(1f, 25f),
            IsEncrypted = false
          };
      }
    }

    private class ConnectionMetrics
    {
      public float Latency { get; set; }
      public float Bandwidth { get; set; }
      public bool IsEncrypted { get; set; }
    }

    #endregion

    #region Network Access Management

    public Result<bool> CheckNetworkAccess(string networkId)
    {
      // Fast path: already unlocked
      if (unlockedNetworks.Contains(networkId))
      {
        return Result<bool>.Success(true);
      }

      if (!accessProfiles.TryGetValue(networkId, out var profile))
      {
        // No profile = create default (VPN access)
        profile = new NetworkAccessProfile(networkId, NetworkAccessType.VPN);
        accessProfiles[networkId] = profile;
      }

      // Check if requirements are met
      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
      {
        return Result<bool>.Failure("Player state not available");
      }

      switch (profile.AccessType)
      {
        case NetworkAccessType.Public:
          unlockedNetworks.Add(networkId);
          return Result<bool>.Success(true);

        case NetworkAccessType.VPN:
          return CheckVPNAccess(profile, playerState);

        case NetworkAccessType.Compromised:
          return CheckCompromisedAccess(profile, playerState);

        default:
          return Result<bool>.Failure("Access denied");
      }
    }

    public Result<bool> RequestNetworkAccess(string networkId, NetworkCredentials credentials)
    {
      if (!accessProfiles.TryGetValue(networkId, out var profile))
      {
        return Result<bool>.Failure("Network not found");
      }

      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
      {
        return Result<bool>.Failure("Player state not available");
      }

      // Validate credentials based on access type
      switch (profile.AccessType)
      {
        case NetworkAccessType.VPN:
          var result = ValidateVPNCredentials(profile, credentials);
          if (result.IsSuccess)
          {
            unlockedNetworks.Add(networkId);
            GameEvents.Instance.Trigger(GameEventType.NetworkChanged, networkId);
            Debug.Log($"Network {networkId} unlocked via VPN");
          }
          return result;

        default:
          return Result<bool>.Failure("Access type does not support credentials");
      }
    }

    public void UnlockNetwork(string networkId, NetworkAccessType accessType)
    {
      if (!unlockedNetworks.Contains(networkId))
      {
        unlockedNetworks.Add(networkId);
        GameEvents.Instance.Trigger(GameEventType.NetworkChanged, networkId);
        Debug.Log($"Network {networkId} unlocked via {accessType}");
      }
    }

    public NetworkAccessProfile GetAccessProfile(string networkId)
    {
      accessProfiles.TryGetValue(networkId, out var profile);
      return profile;
    }

    public List<string> GetAccessibleNetworks()
    {
      return new List<string>(unlockedNetworks);
    }

    private Result<bool> CheckVPNAccess(NetworkAccessProfile profile, IPlayerStateService playerState)
    {
      // Check if player has VPN credentials
      bool hasVpnCreds = playerState.Credentials.HasVPNCredentialsFor(profile.NetworkId);

      if (hasVpnCreds)
      {
        var vpnCreds = playerState.Credentials.GetVPNCredentials(profile.NetworkId);

        // Validate against requirements
        foreach (var requirement in profile.Requirements.OfType<VPNCredentialRequirement>())
        {
          if (vpnCreds.Username == requirement.RequiredUsername &&
              vpnCreds.Password == requirement.RequiredPassword &&
              vpnCreds.Server == requirement.RequiredServer)
          {
            return Result<bool>.Success(true);
          }
        }
      }

      return Result<bool>.Failure("VPN credentials required");
    }

    private Result<bool> CheckCompromisedAccess(NetworkAccessProfile profile, IPlayerStateService playerState)
    {
      // Check if player has compromised required gateway systems
      foreach (var requirement in profile.Requirements.OfType<CompromisedSystemRequirement>())
      {
        if (!playerState.HasCompromisedSystem(requirement.RequiredSystemHostname))
        {
          return Result<bool>.Failure($"Must compromise gateway system: {requirement.RequiredSystemHostname}");
        }

        // Check if they have root access (required for gateway pivot)
        var systemInfo = playerState.Progress.GetSystemInfo(requirement.RequiredSystemHostname);
        if (systemInfo == null || !systemInfo.HasRootAccess)
        {
          return Result<bool>.Failure($"Root access required on: {requirement.RequiredSystemHostname}");
        }
      }

      return Result<bool>.Success(true);
    }

    private Result<bool> ValidateVPNCredentials(NetworkAccessProfile profile, NetworkCredentials credentials)
    {
      if (credentials?.VPNCredentials == null)
      {
        return Result<bool>.Failure("VPN credentials required");
      }

      foreach (var requirement in profile.Requirements.OfType<VPNCredentialRequirement>())
      {
        if (credentials.VPNCredentials.Username != requirement.RequiredUsername)
        {
          return Result<bool>.Failure("Invalid VPN username");
        }

        if (credentials.VPNCredentials.Password != requirement.RequiredPassword)
        {
          return Result<bool>.Failure("Invalid VPN password");
        }

        if (credentials.VPNCredentials.ServerAddress != requirement.RequiredServer)
        {
          return Result<bool>.Failure("Invalid VPN server");
        }
      }

      return Result<bool>.Success(true);
    }

    private void InitializeAccessProfiles()
    {
      foreach (var network in networks)
      {
        // Default access profiles based on network type
        var accessType = DetermineAccessType(network.Metadata.Type);

        var profile = new NetworkAccessProfile(network.NetworkId, accessType);

        // Add requirements based on access type
        switch (accessType)
        {
          case NetworkAccessType.Public:
            // No requirements - always accessible
            unlockedNetworks.Add(network.NetworkId);
            break;

          case NetworkAccessType.VPN:
            // Requires VPN credentials
            profile.AddRequirement(new VPNCredentialRequirement
            {
              NetworkId = network.NetworkId,
              RequiredUsername = "vpnuser",
              RequiredPassword = "vpnpass123", // Would be generated dynamically
              RequiredServer = $"vpn.{network.Metadata.Organization.ToLower()}.com"
            });
            break;

          case NetworkAccessType.Compromised:
            // Requires compromising a gateway system
            var gateways = network.GetActiveGatewayDevices();
            if (gateways.Count > 0)
            {
              profile.AddRequirement(new CompromisedSystemRequirement
              {
                RequiredSystemHostname = gateways[0].Hostname
              });
            }
            break;
        }

        accessProfiles[network.NetworkId] = profile;
      }

      Debug.Log($"Initialized access profiles for {accessProfiles.Count} networks");
    }

    private NetworkAccessType DetermineAccessType(NetworkType networkType)
    {
      switch (networkType)
      {
        case NetworkType.ISP:
        case NetworkType.Residential:
          return NetworkAccessType.Public;

        case NetworkType.Corporate:
        case NetworkType.Government:
        case NetworkType.Financial:
          return NetworkAccessType.VPN;

        case NetworkType.Criminal:
          return NetworkAccessType.Compromised;

        default:
          return NetworkAccessType.VPN;
      }
    }

    #endregion

    #region Device Queries

    public List<Device> GetDevicesInNetwork(string networkId)
    {
      var network = networks.FirstOrDefault(n => n.NetworkId == networkId);
      return network?.GetAllDevices() ?? new List<Device>();
    }

    public List<Device> GetDevicesAtLocation(PhysicalLocation location)
    {
      // Use location index for O(1) lookup instead of iterating all devices
      if (locationIndex.TryGetValue(location.LocationId, out var devices))
      {
        return new List<Device>(devices);
      }

      return new List<Device>();
    }

    public Device GetDeviceById(string deviceId)
    {
      deviceCache.TryGetValue(deviceId, out var device);
      return device;
    }

    public List<Device> GetDevicesInRadius(Vector3 center, float radius)
    {
      // Spatial query for devices within radius
      // This is O(n) but only called when player is physically exploring
      var devicesInRadius = new List<Device>();
      float radiusSquared = radius * radius; // Avoid sqrt calculations

      foreach (var device in deviceCache.Values)
      {
        if (device.PhysicalPosition.HasValue)
        {
          float distanceSquared = (device.PhysicalPosition.Value - center).sqrMagnitude;
          if (distanceSquared <= radiusSquared)
          {
            devicesInRadius.Add(device);
          }
        }
      }

      return devicesInRadius;
    }

    public VirtualNetwork GetNetwork(string networkId)
    {
      return networks.FirstOrDefault(n => n.NetworkId == networkId);
    }

    #endregion

    #region Update & Security

    public void Update(float deltaTime)
    {
      // Clean up timed out connections
      CleanupConnections();

      // Only update security states periodically to avoid performance hit
      securityUpdateTimer += deltaTime;

      if (securityUpdateTimer >= SECURITY_UPDATE_INTERVAL)
      {
        UpdateSecurityStates(securityUpdateTimer);
        securityUpdateTimer = 0f;
      }
    }

    private void CleanupConnections()
    {
      var timedOutConnections = activeConnections.Values
          .Where(c => c.IsTimedOut(connectionTimeout))
          .ToList();

      foreach (var connection in timedOutConnections)
      {
        connection.Status = ConnectionStatus.Timeout;
        activeConnections.Remove(connection.ConnectionId);
        OnConnectionLost?.Invoke(connection);
      }
    }

    public void UpdateSecurityStates(float deltaTime)
    {
      // Batch update security states for all devices
      // This is where we'd implement things like:
      // - Alert timers cooling down
      // - IDS systems detecting intrusions
      // - Firewall rules updating
      // - Security patches being applied

      foreach (var network in networks)
      {
        // Only update online devices
        var onlineDevices = network.GetAllDevices().Where(d => d.IsOnline).ToList();

        // Batch processing for performance
        foreach (var device in onlineDevices)
        {
          // Example: Compromised devices might trigger alerts over time
          if (device.IsCompromised)
          {
            // Check if device has been compromised long enough to be detected
            // This would tie into your security/detection systems
          }
        }
      }
    }

    public void PropagateAlertState(Device sourceDevice)
    {
      // When one device detects intrusion, alert nearby devices on same network
      var network = networks.FirstOrDefault(n => n.NetworkId == sourceDevice.NetworkId);
      if (network == null) return;

      // Get devices in same subnet or security zone
      var nearbyDevices = network.GetAllDevices()
          .Where(d => d.IsOnline && d.DeviceId != sourceDevice.DeviceId)
          .ToList();

      // Propagate alert based on network topology
      // This is where you'd implement alert propagation logic
      foreach (var device in nearbyDevices)
      {
        // Increase security level, start monitoring, etc.
        GameEvents.Instance.Trigger(GameEventType.NetworkAlertTriggered, device);
      }
    }

    #endregion

    #region Save/Load

    public object GetSaveData()
    {
      return new NetworkSaveData
      {
        networks = networks.Select(n => new NetworkData
        {
          networkId = n.NetworkId,
          devices = n.GetAllDevices().Select(d => new DeviceData
          {
            deviceId = d.DeviceId,
            isCompromised = d.IsCompromised,
            isOnline = d.IsOnline,
            securityLevel = d.SecurityLevel
          }).ToList()
        }).ToList(),
        unlockedNetworks = unlockedNetworks.ToList()
      };
    }

    public void LoadFromSave(object saveData)
    {
      if (saveData is NetworkSaveData data)
      {
        // Restore device states
        foreach (var networkData in data.networks)
        {
          var network = networks.FirstOrDefault(n => n.NetworkId == networkData.networkId);
          if (network == null) continue;

          foreach (var deviceData in networkData.devices)
          {
            var device = network.GetAllDevices().FirstOrDefault(d => d.DeviceId == deviceData.deviceId);
            if (device != null)
            {
              device.IsCompromised = deviceData.isCompromised;
              device.IsOnline = deviceData.isOnline;
              device.SecurityLevel = deviceData.securityLevel;
            }
          }
        }

        // Restore unlocked networks
        unlockedNetworks = new HashSet<string>(data.unlockedNetworks ?? new List<string>());

        BuildDeviceIndices();
      }
    }

    // Save data structures
    [System.Serializable]
    public class NetworkSaveData
    {
      public List<NetworkData> networks;
      public List<string> unlockedNetworks;
    }

    [System.Serializable]
    public class NetworkData
    {
      public string networkId;
      public List<DeviceData> devices;
    }

    [System.Serializable]
    public class DeviceData
    {
      public string deviceId;
      public bool isCompromised;
      public bool isOnline;
      public SecurityLevel securityLevel;
    }

    #endregion
  }
}
