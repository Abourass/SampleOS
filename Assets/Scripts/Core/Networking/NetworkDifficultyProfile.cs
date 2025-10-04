using System.Collections.Generic;
using SampleOS.Core.Devices;

namespace SampleOS.Core.Networking
{
  /// <summary>
  /// Represents a predefined network configuration with specific security levels
  /// </summary>
  public class NetworkDifficultyProfile
  {
    public string ProfileName { get; set; }
    public string Description { get; set; }
    public List<DeviceDefinition> DeviceDefinitions { get; set; } = new List<DeviceDefinition>();

    /// <summary>
    /// Example factory methods for predefined profiles
    /// </summary>
    public static NetworkDifficultyProfile CreateBeginnerProfile()
    {
      return new NetworkDifficultyProfile
      {
        ProfileName = "Beginner",
        Description = "Network with mostly vulnerable systems for beginners",
        DeviceDefinitions = new List<DeviceDefinition>
                {
                    new DeviceDefinition
                    {
                        DeviceId = "server-01",
                        Hostname = "server.local",
                        IPAddress = "192.168.1.10",
                        DeviceTypeId = "server",
                        SecurityLevel = SecurityLevel.Low,
                        DefaultUsername = "admin",
                        DefaultPassword = "password123"
                    },
                    new DeviceDefinition
                    {
                        DeviceId = "web-01",
                        Hostname = "web.local",
                        IPAddress = "192.168.1.11",
                        DeviceTypeId = "server",
                        SecurityLevel = SecurityLevel.VeryLow,
                        DefaultUsername = "www",
                        DefaultPassword = "webadmin"
                    }
                }
      };
    }

    public static NetworkDifficultyProfile CreateExpertProfile()
    {
      return new NetworkDifficultyProfile
      {
        ProfileName = "Expert",
        Description = "Network with well-secured systems for advanced players",
        DeviceDefinitions = new List<DeviceDefinition>
                {
                    new DeviceDefinition
                    {
                        DeviceId = "secure-01",
                        Hostname = "secure.local",
                        IPAddress = "192.168.1.10",
                        DeviceTypeId = "server",
                        SecurityLevel = SecurityLevel.VeryHigh,
                        DefaultUsername = "admin",
                        DefaultPassword = null // Will need to be discovered
                    },
                    new DeviceDefinition
                    {
                        DeviceId = "fw-01",
                        Hostname = "firewall.local",
                        IPAddress = "192.168.1.1",
                        DeviceTypeId = "router",
                        SecurityLevel = SecurityLevel.High,
                        DefaultUsername = "admin",
                        DefaultPassword = null
                    }
                }
      };
    }
  }

  /// <summary>
  /// Defines configuration for creating a device
  /// Used by DeviceFactory to instantiate devices
  /// </summary>
  public class DeviceDefinition
  {
    public string DeviceId { get; set; }
    public string Hostname { get; set; }
    public string IPAddress { get; set; }
    public string DeviceTypeId { get; set; } // References DeviceTypeDatabase
    public SecurityLevel SecurityLevel { get; set; }
    public string DefaultUsername { get; set; }
    public string DefaultPassword { get; set; }

    // Optional overrides
    public List<string> SoftwareOverrides { get; set; } = new List<string>();
    public bool IsPhysicallyAccessible { get; set; } = false;
    public string LocationId { get; set; }
  }

  public enum SecurityLevel
  {
    VeryLow,   // Extremely vulnerable systems (abandoned/legacy)
    Low,       // Poorly maintained systems
    Medium,    // Typical security level
    High,      // Well-maintained systems
    VeryHigh   // Highly secure systems (up-to-date, hardened)
  }
}
