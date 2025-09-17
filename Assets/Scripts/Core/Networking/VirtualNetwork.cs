using System.Collections.Generic;

public class VirtualNetwork
{
  private Dictionary<string, RemoteSystem> systems = new Dictionary<string, RemoteSystem>();

  public VirtualNetwork()
  {
    // Create some default remote systems
    systems.Add("server", new RemoteSystem("server", "server.local", "192.168.1.10", "server", "admin"));
    systems.Add("raspberry", new RemoteSystem("raspberry", "raspberrypi.local", "192.168.1.100", "embedded", "pi"));
    systems.Add("nas", new RemoteSystem("nas", "nas.local", "192.168.1.50", "storage", "admin"));
    systems.Add("workstation", new RemoteSystem("workstation", "workstation.local", "192.168.1.20", "desktop", "user"));
  }

  public Result<RemoteSystem> Connect(string host, string username, string password)
  {
    foreach (var system in systems.Values)
    {
      if (system.Hostname == host || system.IPAddress == host)
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
    }

    return Result<RemoteSystem>.Failure($"Host not found: {host}");
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
}
