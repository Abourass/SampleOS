using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using SampleOS.Core.SoftwarePackages;
using UnityEngine;

namespace SampleOS.Core.Player
{
  /// <summary>
  /// Tracks player's hacking progress. Pure data tracking - no persistence logic.
  /// </summary>
  public class PlayerProgress
  {
    private Dictionary<string, OwnedSystemInfo> ownedSystems = new Dictionary<string, OwnedSystemInfo>();
    private Dictionary<string, List<DiscoveredVulnerability>> discoveredVulnerabilities = new Dictionary<string, List<DiscoveredVulnerability>>();

    // Events
    public event Action<OwnedSystemInfo> OnSystemCompromised;

    #region System Compromise Tracking

    public void RecordSystemCompromise(Device device, string exploitUsed, bool hasRoot)
    {
      if (device == null) return;

      string key = device.Hostname;

      if (!ownedSystems.ContainsKey(key))
      {
        // First compromise
        var info = new OwnedSystemInfo
        {
          Hostname = device.Hostname,
          IPAddress = device.IPAddress,
          Type = device.DeviceType.Name,
          NetworkName = device.NetworkId,
          CompromiseDate = DateTime.Now,
          CompromiseMethod = exploitUsed ?? "Unknown",
          HasUserAccess = true,
          HasRootAccess = hasRoot,
          LastAccessTime = DateTime.Now,
          IsCurrentlyConnected = true
        };

        if (!string.IsNullOrEmpty(exploitUsed))
        {
          info.ExploitsUsed.Add(exploitUsed);
        }

        ownedSystems[key] = info;
        OnSystemCompromised?.Invoke(info);

        Debug.Log($"System compromised: {device.Hostname}");
      }
      else
      {
        // Privilege escalation
        var info = ownedSystems[key];

        if (hasRoot && !info.HasRootAccess)
        {
          info.HasRootAccess = true;
          info.LastAccessTime = DateTime.Now;

          if (!string.IsNullOrEmpty(exploitUsed))
          {
            info.ExploitsUsed.Add(exploitUsed);
          }

          Debug.Log($"Privilege escalation: {device.Hostname} - Root obtained");
        }
      }
    }

    public void RecordConnection(string hostname)
    {
      if (ownedSystems.TryGetValue(hostname, out var info))
      {
        info.ConnectionsMade++;
        info.LastAccessTime = DateTime.Now;
        info.IsCurrentlyConnected = true;
      }
    }

    public void RecordDisconnection(string hostname)
    {
      if (ownedSystems.TryGetValue(hostname, out var info))
      {
        info.IsCurrentlyConnected = false;
      }
    }

    #endregion

    #region Vulnerability Tracking

    public void AddDiscoveredVulnerability(string hostname, string softwareName, Vulnerability vuln, int port = 0)
    {
      if (!discoveredVulnerabilities.ContainsKey(hostname))
      {
        discoveredVulnerabilities[hostname] = new List<DiscoveredVulnerability>();
      }

      var discoveredVuln = new DiscoveredVulnerability
      {
        Hostname = hostname,
        SoftwareName = softwareName,
        CVE = vuln.CVE,
        Port = port,
        DiscoveredAt = DateTime.Now,
        HasBeenExploited = false
      };

      // Check if already discovered
      if (!discoveredVulnerabilities[hostname].Any(v => v.CVE == vuln.CVE))
      {
        discoveredVulnerabilities[hostname].Add(discoveredVuln);
        Debug.Log($"Vulnerability discovered: {vuln.CVE} on {hostname}");
      }
    }

    /// <summary>
    /// Get a specific vulnerability by hostname and CVE
    /// </summary>
    public DiscoveredVulnerability GetVulnerability(string hostname, string cve)
    {
      if (!discoveredVulnerabilities.ContainsKey(hostname))
        return null;

      return discoveredVulnerabilities[hostname]
          .FirstOrDefault(v => v.CVE != null && v.CVE.Equals(cve, StringComparison.OrdinalIgnoreCase));
    }

    public List<DiscoveredVulnerability> GetVulnerabilitiesFor(string hostname)
    {
      return discoveredVulnerabilities.TryGetValue(hostname, out var vulns)
          ? new List<DiscoveredVulnerability>(vulns)
          : new List<DiscoveredVulnerability>();
    }

    public List<DiscoveredVulnerability> GetAllVulnerabilities()
    {
      return discoveredVulnerabilities.Values
          .SelectMany(v => v)
          .OrderByDescending(v => v.DiscoveredAt)
          .ToList();
    }

    /// <summary>
    /// Mark a vulnerability as exploited
    /// </summary>
    public void MarkVulnerabilityExploited(string hostname, string cve)
    {
      var vuln = GetVulnerability(hostname, cve);
      if (vuln != null)
      {
        vuln.HasBeenExploited = true;
        vuln.ExploitedAt = DateTime.Now;
        Debug.Log($"Marked {cve} as exploited on {hostname}");
      }
    }

    /// <summary>
    /// Generate vulnerability report (requires vulnerability database for details)
    /// Call this from PlayerStateService which has access to VulnDb
    /// </summary>
    public string GenerateVulnerabilityReport(Func<string, Vulnerability> vulnResolver)
    {
      var content = new System.Text.StringBuilder();
      content.AppendLine("VULNERABILITY DATABASE");
      content.AppendLine("=====================");
      content.AppendLine();
      content.AppendLine("CVE          SEVERITY  TARGET           SOFTWARE        NAME");
      content.AppendLine("------------------------------------------------------------------");

      foreach (var hostVulns in discoveredVulnerabilities)
      {
        foreach (var vuln in hostVulns.Value)
        {
          string target = $"{vuln.Hostname}:{vuln.Port}";
          string exploited = vuln.HasBeenExploited ? "[EXPLOITED]" : "";

          // Resolve vulnerability details via callback
          var vulnDetails = vulnResolver(vuln.CVE);
          string cve = vulnDetails?.CVE ?? vuln.CVE;
          int severity = vulnDetails?.Severity ?? 0;
          string name = vulnDetails?.Name ?? "Unknown";

          content.AppendLine($"{cve,-13}{severity,-10}{target,-17}{vuln.SoftwareName,-15}{name} {exploited}");
        }
      }

      return content.ToString();
    }

    #endregion

    #region Query Methods

    public bool HasCompromisedSystem(string hostname)
    {
      return ownedSystems.ContainsKey(hostname);
    }

    public OwnedSystemInfo GetSystemInfo(string hostname)
    {
      return ownedSystems.TryGetValue(hostname, out var info) ? info : null;
    }

    public List<OwnedSystemInfo> GetOwnedSystems()
    {
      return ownedSystems.Values
          .OrderByDescending(info => info.CompromiseDate)
          .ToList();
    }

    public ProgressStatistics GetStatistics()
    {
      return new ProgressStatistics
      {
        TotalSystemsCompromised = ownedSystems.Count,
        SystemsWithRootAccess = ownedSystems.Values.Count(info => info.HasRootAccess),
        TotalVulnerabilitiesFound = discoveredVulnerabilities.Values.Sum(v => v.Count),
        UniqueExploitsUsed = ownedSystems.Values
              .SelectMany(info => info.ExploitsUsed)
              .Distinct()
              .Count()
      };
    }

    #endregion

    #region Save/Load

    public PlayerProgressSaveData GetSaveData()
    {
      return new PlayerProgressSaveData
      {
        ownedSystems = ownedSystems.Values.ToList(),
        discoveredVulnerabilities = discoveredVulnerabilities.ToDictionary(
              kvp => kvp.Key,
              kvp => kvp.Value.ToList()
          )
      };
    }

    public void LoadFromSave(PlayerProgressSaveData data)
    {
      if (data == null) return;

      ownedSystems.Clear();
      foreach (var info in data.ownedSystems)
      {
        ownedSystems[info.Hostname] = info;
        info.IsCurrentlyConnected = false; // Reset connection status
      }

      discoveredVulnerabilities = data.discoveredVulnerabilities ?? new Dictionary<string, List<DiscoveredVulnerability>>();

      Debug.Log($"Progress loaded: {ownedSystems.Count} systems");
    }

    #endregion
  }

  [Serializable]
  public class PlayerProgressSaveData
  {
    public List<OwnedSystemInfo> ownedSystems = new List<OwnedSystemInfo>();
    public Dictionary<string, List<DiscoveredVulnerability>> discoveredVulnerabilities = new Dictionary<string, List<DiscoveredVulnerability>>();
  }

  [Serializable]
  public class DiscoveredVulnerability
  {
    // Context: WHERE and WHEN player found it
    public string Hostname;
    public string SoftwareName;
    public int Port;
    public DateTime DiscoveredAt;

    // Exploitation tracking
    public bool HasBeenExploited;
    public DateTime? ExploitedAt;

    // Reference to vulnerability (don't store full object)
    public string CVE; // Just store the ID!
  }

  [Serializable]
  public class ProgressStatistics
  {
    public int TotalSystemsCompromised;
    public int SystemsWithRootAccess;
    public int TotalVulnerabilitiesFound;
    public int UniqueExploitsUsed;
  }
}
