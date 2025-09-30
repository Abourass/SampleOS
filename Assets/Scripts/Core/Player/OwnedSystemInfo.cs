using System;
using System.Collections.Generic;

namespace SampleOS.Core.Player
{
  /// <summary>
  /// Represents detailed information about a compromised system.
  /// This is a data transfer object used to display owned systems to the player.
  /// </summary>
  [Serializable]
  public class OwnedSystemInfo
  {
    // Basic system identification
    public string Hostname { get; set; }
    public string IPAddress { get; set; }
    public string Type { get; set; }
    public string NetworkName { get; set; }

    // Access information
    public bool HasRootAccess { get; set; }
    public bool HasUserAccess { get; set; }

    // Compromise details
    public DateTime CompromiseDate { get; set; }
    public string CompromiseMethod { get; set; } // e.g., "SSH bruteforce", "Exploit CVE-2021-1234"

    // Resources used/gained
    public List<string> CredentialsUsed { get; set; } = new List<string>();
    public List<string> ExploitsUsed { get; set; } = new List<string>();
    public List<string> FlagsFound { get; set; } = new List<string>();

    // Statistics
    public long DataExfiltrated { get; set; } // bytes
    public int FilesAccessed { get; set; }
    public int ConnectionsMade { get; set; }

    // Current status
    public bool IsCurrentlyConnected { get; set; }
    public DateTime? LastAccessTime { get; set; }

    // Discovery chain (optional - shows how you found this system)
    public string DiscoveredFrom { get; set; } // hostname of system where you found this
    public string DiscoveryMethod { get; set; } // e.g., "Found credentials in email", "Network scan"
  }
}
