using System;
using System.Collections.Generic;

namespace Core.Networking.Discovery
{
  [Serializable]
  public class DiscoveryClue
  {
    public string ClueId { get; set; } = Guid.NewGuid().ToString();
    public string NetworkId { get; set; }
    public DiscoveryClueType Type { get; set; }
    public string Description { get; set; }
    public string Content { get; set; }
    public string FilePath { get; set; }
    public string SourceSystemId { get; set; }
    public DateTime DiscoveryTime { get; set; } = DateTime.Now;
    public Dictionary<string, string> Properties { get; set; } = new Dictionary<string, string>();
    public int ReliabilityScore { get; set; } = 100; // 0-100, how reliable this clue is

    public DiscoveryClue(string networkId, DiscoveryClueType type, string description)
    {
      NetworkId = networkId;
      Type = type;
      Description = description;
    }

    /// <summary>
    /// Extract network information from this clue
    /// </summary>
    public NetworkClueData ExtractNetworkData()
    {
      var data = new NetworkClueData
      {
        NetworkId = NetworkId,
        ClueType = Type
      };

      switch (Type)
      {
        case DiscoveryClueType.VPNConfiguration:
          data.VPNServerAddress = Properties.GetValueOrDefault("ServerAddress");
          data.VPNProtocol = Properties.GetValueOrDefault("Protocol");
          data.NetworkName = Properties.GetValueOrDefault("NetworkName");
          break;

        case DiscoveryClueType.IPAddressReference:
          data.IPRange = Properties.GetValueOrDefault("IPRange");
          data.SubnetMask = Properties.GetValueOrDefault("SubnetMask");
          break;

        case DiscoveryClueType.DomainReference:
          data.DomainName = Properties.GetValueOrDefault("DomainName");
          data.OrganizationName = Properties.GetValueOrDefault("OrganizationName");
          break;

        case DiscoveryClueType.NetworkDiagram:
          data.TopologyInfo = Properties.GetValueOrDefault("TopologyInfo");
          data.SystemList = Properties.GetValueOrDefault("SystemList");
          break;
      }

      return data;
    }

    /// <summary>
    /// Check if this clue provides sufficient information for network discovery
    /// </summary>
    public bool IsSufficientForDiscovery()
    {
      switch (Type)
      {
        case DiscoveryClueType.VPNConfiguration:
          return Properties.ContainsKey("ServerAddress") &&
                 Properties.ContainsKey("NetworkName");

        case DiscoveryClueType.NetworkDiagram:
          return Properties.ContainsKey("SystemList") &&
                 ReliabilityScore >= 80;

        case DiscoveryClueType.EmailReference:
          return Properties.ContainsKey("NetworkName") &&
                 Properties.ContainsKey("ContactInfo");

        default:
          return ReliabilityScore >= 50;
      }
    }
  }

  public enum DiscoveryClueType
  {
    VPNConfiguration,     // VPN config files, connection settings
    EmailReference,       // Email mentioning networks or systems
    NetworkDiagram,       // Network topology documents
    IPAddressReference,   // IP addresses or ranges mentioned in files
    DomainReference,      // Domain names and DNS information
    Certificate,          // SSL/TLS certificates
    DocumentMention,      // Networks mentioned in documents
    BrowserBookmark,      // Bookmarked internal sites
    ConfigurationFile,    // Network configuration files
    LogEntry,            // Log files mentioning other networks
    CredentialFile       // Stored credentials referencing networks
  }

  [Serializable]
  public class NetworkClueData
  {
    public string NetworkId { get; set; }
    public DiscoveryClueType ClueType { get; set; }

    // VPN-specific data
    public string VPNServerAddress { get; set; }
    public string VPNProtocol { get; set; }
    public string NetworkName { get; set; }

    // Network topology data
    public string IPRange { get; set; }
    public string SubnetMask { get; set; }
    public string TopologyInfo { get; set; }
    public string SystemList { get; set; }

    // Domain/organization data
    public string DomainName { get; set; }
    public string OrganizationName { get; set; }

    // Additional metadata
    public Dictionary<string, string> AdditionalData { get; set; } = new Dictionary<string, string>();
  }
}
