using System.Collections.Generic;

public class VirtualNetwork
{
  private Dictionary<string, RemoteSystem> systems = new Dictionary<string, RemoteSystem>();

  public VirtualNetwork()
  {
    // Create some default remote systems
    systems.Add("server", new RemoteSystem("server", "server.local", "admin"));
    systems.Add("raspberry", new RemoteSystem("raspberry", "192.168.1.100", "pi"));
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
}
