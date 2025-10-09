using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Networking.Access;
using SampleOS.Core.Devices;

namespace SampleOS.Core.Networking
{
  public class NetworkMetadata
  {
    public string Name { get; set; }
    public string Description { get; set; }
    public string Organization { get; set; }
    public NetworkType Type { get; set; }
    public string IPRange { get; set; }
    public List<string> ConnectedNetworks { get; set; } = new List<string>();
  }

  public enum NetworkType
  {
    Corporate,      // Company internal networks
    Government,     // Municipal, federal networks
    Residential,    // Home networks, small business
    Educational,    // Universities, schools
    Healthcare,     // Hospitals, clinics
    Financial,      // Banks, credit unions
    Criminal,       // Underground, illegal operations
    ISP,           // Internet service providers
    Industrial     // Manufacturing, utilities
  }

  public class VirtualNetwork
  {
    public string NetworkId { get; private set; }
    public NetworkMetadata Metadata { get; private set; }
    public NetworkSecurityProfile SecurityProfile { get; private set; }

    private Dictionary<string, Device> Devices = new Dictionary<string, Device>();
    public List<NetworkGateway> Gateways { get; private set; } = new List<NetworkGateway>();


    public VirtualNetwork(string id, NetworkMetadata metadata, NetworkSecurityProfile security)
    {
      NetworkId = id;
      Metadata = metadata;
      SecurityProfile = security;
      Devices = new Dictionary<string, Device>();
      Gateways = new List<NetworkGateway>();
    }

    // public VirtualNetwork(string id, NetworkMetadata metadata, NetworkSecurityProfile security)
    // {
    //   // Create network information
    //   NetworkId = id;
    //   Metadata = metadata;
    //   SecurityProfile = security;

    //   // Create local system with high security
    //   localSystem = new RemoteSystem("localhost", "localhost", "127.0.0.1", "desktop", "user", SecurityLevel.High);

    //   // Give player root access to their own system
    //   localSystem.GiveRootAccess();

    //   // Create remote systems with varying security levels
    //   systems.Add("server", new RemoteSystem("server", "server.local", "192.168.1.10", "server", "admin", SecurityLevel.Medium));
    //   systems.Add("raspberry", new RemoteSystem("raspberry", "raspberrypi.local", "192.168.1.100", "embedded", "pi", SecurityLevel.Low)); // IoT device - less secure
    //   systems.Add("nas", new RemoteSystem("nas", "nas.local", "192.168.1.50", "storage", "admin", SecurityLevel.Medium));
    //   systems.Add("workstation", new RemoteSystem("workstation", "workstation.local", "192.168.1.20", "desktop", "user", SecurityLevel.High));
    //   systems.Add("router", new RemoteSystem("router", "router.local", "192.168.1.1", "router", "admin", SecurityLevel.Low)); // Often neglected

    //   // Add some special systems for progression
    //   systems.Add("legacy", new RemoteSystem("legacy", "legacy.local", "192.168.1.200", "server", "admin", SecurityLevel.VeryLow)); // Easy target for beginners
    //   systems.Add("secure", new RemoteSystem("secure", "secure.local", "192.168.1.250", "server", "admin", SecurityLevel.VeryHigh)); // Challenging target
    // }

    /// <summary>
    /// Adds a device to this network
    /// </summary>
    public void AddDevice(Device device)
    {
      device.NetworkId = NetworkId;
      Devices[device.DeviceId] = device;
    }

    /// <summary>
    /// Gets a device by its ID
    /// </summary>
    public Device GetDevice(string deviceId)
    {
      Devices.TryGetValue(deviceId, out var device);
      return device;
    }

    /// <summary>
    /// Finds a device by IP address
    /// </summary>
    public Device GetDeviceByIP(string ip)
    {
      return Devices.Values.FirstOrDefault(d => d.IPAddress == ip);
    }

    /// <summary>
    /// Finds a device by hostname
    /// </summary>
    public Device GetDeviceByHostname(string hostname)
    {
      return Devices.Values.FirstOrDefault(d => d.Hostname == hostname);
    }

    /// <summary>
    /// Gets all devices on this network
    /// </summary>
    public List<Device> GetAllDevices()
    {
      return Devices.Values.ToList();
    }

    /// <summary>
    /// Adds a gateway for inter-network routing
    /// </summary>
    public void AddGateway(NetworkGateway gateway)
    {
      Gateways.Add(gateway);
    }

    /// <summary>
    /// Gets all gateways
    /// </summary>
    public List<NetworkGateway> GetGateways()
    {
      return Gateways;
    }

    /// <summary>
    /// Clears all devices and gateways
    /// </summary>
    public void Clear()
    {
      Devices.Clear();
      Gateways.Clear();
    }

    /// <summary>
    /// Gets gateway devices that are active
    /// </summary>
    public List<Device> GetActiveGatewayDevices()
    {
      return Gateways
          .Where(g => g.IsActive)
          .Select(g => GetDeviceByHostname(g.SystemHostname))
          .Where(d => d != null)
          .ToList();
    }

    /// <summary>
    /// Legacy method for backwards compatibility with commands
    /// </summary>
    public Result<Device> Connect(string host, string username, string password)
    {
      Device device = GetDeviceByHostname(host);

      if (device == null)
      {
        // Try IP address
        device = GetDeviceByIP(host);
      }

      if (device != null)
      {
        if (device.Authenticate(username, password))
        {
          return Result<Device>.Success(device);
        }
        else
        {
          return Result<Device>.Failure("Authentication failed");
        }
      }

      return Result<Device>.Failure($"Host not found: {host}");
    }
  }
}
