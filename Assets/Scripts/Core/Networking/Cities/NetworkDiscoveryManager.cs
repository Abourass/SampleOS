using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Core.Networking;
using Core.Networking.Access;
using Core.Networking.Discovery;

namespace SampleOS.Core.Networking.Cities
{

  public class NetworkDiscoveryManager
  {
    private Dictionary<string, bool> discoveredNetworks = new Dictionary<string, bool>();
    private List<DiscoveryClue> discoveredClues = new List<DiscoveryClue>();
    private Dictionary<string, List<string>> networkDevices = new Dictionary<string, List<string>>();

    /// <summary>
    /// Get all discovered clues
    /// </summary>
    public List<DiscoveryClue> GetAllClues()
    {
      return new List<DiscoveryClue>(discoveredClues);
    }

    /// <summary>
    /// Get all clues of a specific type
    /// </summary>
    public List<DiscoveryClue> GetCluesOfType(DiscoveryClueType type)
    {
      return discoveredClues.Where(c => c.Type == type).ToList();
    }


    /// <summary>
    /// Get clues for a specific network
    /// </summary>
    public List<DiscoveryClue> GetCluesForNetwork(string networkId)
    {
      return discoveredClues.Where(c => c.NetworkId == networkId).ToList();
    }

    /// <summary>
    /// Add a new discovery clue
    /// </summary>
    public void AddClue(DiscoveryClue clue)
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

      if (reliableClues.Count >= 2 || networkClues.Any(c => c.IsSufficientForDiscovery()))
      {
        MarkNetworkDiscovered(clue.NetworkId);
      }
    }

    /// <summary>
    /// Mark a network as discovered by the player
    /// </summary>
    public void MarkNetworkDiscovered(string networkId)
    {
      if (!discoveredNetworks.ContainsKey(networkId))
      {
        discoveredNetworks[networkId] = true;
        UnityEngine.Debug.Log($"Network discovered: {networkId}");
      }
    }

    /// <summary>
    /// Check if network has been discovered
    /// </summary>
    public bool IsNetworkDiscovered(string networkId)
    {
      return discoveredNetworks.ContainsKey(networkId) && discoveredNetworks[networkId];
    }

    /// <summary>
    /// Record a device as part of a network
    /// </summary>
    public void AddDeviceToNetwork(string networkId, string deviceId)
    {
      if (!networkDevices.ContainsKey(networkId))
      {
        networkDevices[networkId] = new List<string>();
      }

      if (!networkDevices[networkId].Contains(deviceId))
      {
        networkDevices[networkId].Add(deviceId);
      }
    }

    /// <summary>
    /// Get all known devices in a network
    /// </summary>
    public List<string> GetNetworkDevices(string networkId)
    {
      if (networkDevices.ContainsKey(networkId))
      {
        return new List<string>(networkDevices[networkId]);
      }
      return new List<string>();
    }

    /// <summary>
    /// Get all discovered network IDs
    /// </summary>
    public List<string> GetDiscoveredNetworkIds()
    {
      return discoveredNetworks.Where(n => n.Value).Select(n => n.Key).ToList();
    }

    /// <summary>
    /// Process file content to automatically extract clues
    /// Used when player reads files or scans filesystems
    /// </summary>
    public List<DiscoveryClue> ScanFileForClues(string deviceId, string filePath, string content, string networkId = "unknown")
    {
      var foundClues = new List<DiscoveryClue>();

      // Scan for IP addresses
      var ipMatches = Regex.Matches(content, @"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b");
      foreach (Match match in ipMatches)
      {
        var ip = match.Value;
        if (IsPrivateIP(ip))
        {
          var clue = new DiscoveryClue(networkId, DiscoveryClueType.IPAddressReference,
              $"Private IP address found: {ip}");
          clue.Properties["IPAddress"] = ip;
          clue.FilePath = filePath;
          clue.SourceSystemId = deviceId;
          clue.ReliabilityScore = 60;

          AddClue(clue);
          foundClues.Add(clue);
        }
      }

      // Scan for domain references
      var domainMatches = Regex.Matches(content,
          @"\b[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*\.(corp|local|internal)\b");
      foreach (Match match in domainMatches)
      {
        var domain = match.Value;
        var clue = new DiscoveryClue(networkId, DiscoveryClueType.DomainReference,
            $"Internal domain found: {domain}");
        clue.Properties["DomainName"] = domain;
        clue.FilePath = filePath;
        clue.SourceSystemId = deviceId;
        clue.ReliabilityScore = 70;

        AddClue(clue);
        foundClues.Add(clue);
      }

      // Scan for VPN references
      if (content.Contains("VPN") || content.Contains("vpn") || content.Contains("OpenVPN"))
      {
        var vpnMatch = Regex.Match(content,
            @"(?:VPN|vpn).*?(?:server|remote)[\s:]+([^\s,;\r\n]+)",
            RegexOptions.IgnoreCase);

        if (vpnMatch.Success)
        {
          var serverAddress = vpnMatch.Groups[1].Value;
          var clue = new DiscoveryClue(networkId, DiscoveryClueType.VPNConfiguration,
              $"VPN configuration found for {serverAddress}");
          clue.Properties["ServerAddress"] = serverAddress;
          clue.FilePath = filePath;
          clue.SourceSystemId = deviceId;
          clue.ReliabilityScore = 85;

          AddClue(clue);
          foundClues.Add(clue);
        }
      }

      // Scan for network names
      var networkNameMatch = Regex.Match(content,
          @"(?:network|Network)\s*[:=]\s*[""']?([^""'\r\n,;]+)[""']?",
          RegexOptions.IgnoreCase);

      if (networkNameMatch.Success)
      {
        var netName = networkNameMatch.Groups[1].Value.Trim();
        var clue = new DiscoveryClue(networkId, DiscoveryClueType.EmailReference,
            $"Network reference: {netName}");
        clue.Properties["NetworkName"] = netName;
        clue.FilePath = filePath;
        clue.SourceSystemId = deviceId;
        clue.ReliabilityScore = 65;

        AddClue(clue);
        foundClues.Add(clue);
      }

      return foundClues;
    }

    /// <summary>
    /// Get clues grouped by network
    /// </summary>
    public Dictionary<string, List<DiscoveryClue>> GetCluesByNetwork()
    {
      var grouped = new Dictionary<string, List<DiscoveryClue>>();

      foreach (var clue in discoveredClues)
      {
        if (!grouped.ContainsKey(clue.NetworkId))
        {
          grouped[clue.NetworkId] = new List<DiscoveryClue>();
        }
        grouped[clue.NetworkId].Add(clue);
      }

      return grouped;
    }

    /// <summary>
    /// Get discovery progress for a network (0.0 to 1.0)
    /// </summary>
    public float GetNetworkDiscoveryProgress(string networkId)
    {
      var clues = GetCluesForNetwork(networkId);
      var reliableClues = clues.Where(c => c.ReliabilityScore >= 70).Count();

      // Need at least 2 reliable clues to discover
      return Math.Min(1.0f, reliableClues / 2.0f);
    }

    private bool IsPrivateIP(string ip)
    {
      var parts = ip.Split('.');
      if (parts.Length != 4)
        return false;

      int[] octets = new int[4];
      for (int i = 0; i < 4; i++)
      {
        if (!int.TryParse(parts[i], out octets[i]) || octets[i] < 0 || octets[i] > 255)
          return false;
      }

      // 10.0.0.0/8
      if (octets[0] == 10) return true;

      // 172.16.0.0/12
      if (octets[0] == 172 && octets[1] >= 16 && octets[1] <= 31) return true;

      // 192.168.0.0/16
      if (octets[0] == 192 && octets[1] == 168) return true;

      return false;
    }
  }
}
