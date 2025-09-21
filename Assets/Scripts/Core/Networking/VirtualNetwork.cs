using System.Collections.Generic;
using System.Linq;
using Core.Networking.Access;

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
  private Dictionary<string, RemoteSystem> systems = new Dictionary<string, RemoteSystem>();
  private RemoteSystem localSystem;
  public NetworkMetadata Metadata { get; private set; }
  public string NetworkId { get; private set; }
  public List<NetworkGateway> Gateways { get; private set; } = new List<NetworkGateway>();
  public NetworkSecurityProfile SecurityProfile { get; private set; }

  public VirtualNetwork(string id, NetworkMetadata metadata, NetworkSecurityProfile security)
  {
    // Create network information
    NetworkId = id;
    Metadata = metadata;
    SecurityProfile = security;

    // Create local system with high security
    localSystem = new RemoteSystem("localhost", "localhost", "127.0.0.1", "desktop", "user", SecurityLevel.High);

    // Give player root access to their own system
    localSystem.GiveRootAccess();

    // Create remote systems with varying security levels
    systems.Add("server", new RemoteSystem("server", "server.local", "192.168.1.10", "server", "admin", SecurityLevel.Medium));
    systems.Add("raspberry", new RemoteSystem("raspberry", "raspberrypi.local", "192.168.1.100", "embedded", "pi", SecurityLevel.Low)); // IoT device - less secure
    systems.Add("nas", new RemoteSystem("nas", "nas.local", "192.168.1.50", "storage", "admin", SecurityLevel.Medium));
    systems.Add("workstation", new RemoteSystem("workstation", "workstation.local", "192.168.1.20", "desktop", "user", SecurityLevel.High));
    systems.Add("router", new RemoteSystem("router", "router.local", "192.168.1.1", "router", "admin", SecurityLevel.Low)); // Often neglected

    // Add some special systems for progression
    systems.Add("legacy", new RemoteSystem("legacy", "legacy.local", "192.168.1.200", "server", "admin", SecurityLevel.VeryLow)); // Easy target for beginners
    systems.Add("secure", new RemoteSystem("secure", "secure.local", "192.168.1.250", "server", "admin", SecurityLevel.VeryHigh)); // Challenging target
  }

  public void CreateCustomNetwork(NetworkDifficultyProfile profile)
  {
    systems.Clear();

    // Add systems according to the difficulty profile
    foreach (var systemDef in profile.SystemDefinitions)
    {
      systems.Add(systemDef.Name, new RemoteSystem(
        systemDef.Name,
        systemDef.Hostname,
        systemDef.IPAddress,
        systemDef.Type,
        systemDef.DefaultUser,
        systemDef.SecurityLevel
      ));
    }
  }

  public Result<RemoteSystem> Connect(string host, string username, string password)
  {
    RemoteSystem system = GetSystemByHostname(host);

    if (system != null)
    {
      if (system.Authenticate(username, password))
      {
        return Result<RemoteSystem>.Success(system);
      }
      else
      {
        return Result<RemoteSystem>.Failure("Authentication failed");
      }
    }

    return Result<RemoteSystem>.Failure($"Host not found: {host}");
  }

  public RemoteSystem GetSystemByHostname(string host)
  {
    // Check if it's the local system
    if (host == "localhost" || host == "127.0.0.1")
      return localSystem;

    // Check by hostname
    foreach (var system in systems.Values)
    {
      if (system.Hostname == host || system.IPAddress == host)
      {
        return system;
      }
    }

    return null;
  }

  public RemoteSystem GetLocalSystem()
  {
    return localSystem;
  }

  public List<NetworkDevice> GetNetworkDevices()
  {
    List<NetworkDevice> devices = new List<NetworkDevice>();

    // Add the local machine
    devices.Add(new NetworkDevice
    {
      Name = "localhost",
      Hostname = "localhost",
      IPAddress = "127.0.0.1",
      Type = "local"
    });

    // Add all remote systems
    foreach (var system in systems.Values)
    {
      devices.Add(new NetworkDevice
      {
        Name = system.Name,
        Hostname = system.Hostname,
        IPAddress = system.IPAddress,
        Type = system.Type
      });
    }

    return devices;
  }

  public void AddGateway(NetworkGateway gateway)
  {
    Gateways.Add(gateway);
  }

  public void AddSystem(string systemId, RemoteSystem system)
  {
    // Add or replace the system with this ID
    systems[systemId] = system;
  }

  public List<RemoteSystem> GetGatewaySystems()
  {
    return Gateways.Where(g => g.IsActive).Select(g => GetSystemByHostname(g.SystemHostname)).ToList();
  }
}
