using System.Collections.Generic;

// Represents a predefined network configuration with specific security levels
public class NetworkDifficultyProfile
{
  public string ProfileName { get; set; }
  public string Description { get; set; }
  public List<SystemDefinition> SystemDefinitions { get; set; } = new List<SystemDefinition>();

  // Example factory methods for predefined profiles
  public static NetworkDifficultyProfile CreateBeginnerProfile()
  {
    return new NetworkDifficultyProfile
    {
      ProfileName = "Beginner",
      Description = "Network with mostly vulnerable systems for beginners",
      SystemDefinitions = new List<SystemDefinition>
      {
        new SystemDefinition("server", "server.local", "192.168.1.10", "server", "admin", SecurityLevel.Low),
        new SystemDefinition("webserver", "web.local", "192.168.1.11", "server", "www", SecurityLevel.VeryLow),
        new SystemDefinition("router", "router.local", "192.168.1.1", "router", "admin", SecurityLevel.Low),
        // Add more beginner-friendly systems...
      }
    };
  }

  public static NetworkDifficultyProfile CreateExpertProfile()
  {
    return new NetworkDifficultyProfile
    {
      ProfileName = "Expert",
      Description = "Network with well-secured systems for advanced players",
      SystemDefinitions = new List<SystemDefinition>
      {
        new SystemDefinition("secure-server", "secure.local", "192.168.1.10", "server", "admin", SecurityLevel.VeryHigh),
        new SystemDefinition("corp-firewall", "firewall.local", "192.168.1.1", "router", "admin", SecurityLevel.High),
        new SystemDefinition("database", "db.local", "192.168.1.11", "server", "dbadmin", SecurityLevel.High),
        // Add more challenging systems...
      }
    };
  }
}

// Defines a system to be created in a network
public class SystemDefinition
{
  public string Name { get; set; }
  public string Hostname { get; set; }
  public string IPAddress { get; set; }
  public string Type { get; set; }
  public string DefaultUser { get; set; }
  public SecurityLevel SecurityLevel { get; set; }

  public SystemDefinition(string name, string hostname, string ip, string type, string user, SecurityLevel securityLevel)
  {
    Name = name;
    Hostname = hostname;
    IPAddress = ip;
    Type = type;
    DefaultUser = user;
    SecurityLevel = securityLevel;
  }
}
