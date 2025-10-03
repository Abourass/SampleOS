using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Player;
using UnityEngine;

public class PlayerProgressManager
{
  // Core tracking data
  private Dictionary<string, OwnedSystemInfo> ownedSystems = new Dictionary<string, OwnedSystemInfo>();
  private Dictionary<string, HashSet<string>> discoveredVulnerabilities = new Dictionary<string, HashSet<string>>();
  private Dictionary<string, int> networkAccessLevels = new Dictionary<string, int>();

  // Reference to current network
  private VirtualNetwork currentNetwork;

  // Track the current system for connection tracking
  private string currentlyConnectedSystem = null;

  // Events for UI/other systems to react to progress
  public event Action<OwnedSystemInfo> OnSystemCompromised;
  public event Action<string> OnNetworkAccessIncreased;

  public PlayerProgressManager(VirtualNetwork network)
  {
    this.currentNetwork = network;
    LoadProgress();
  }

  public void SetNetwork(VirtualNetwork network)
  {
    this.currentNetwork = network;
  }

  #region System Compromise Tracking

  /// <summary>
  /// Record that a system has been compromised
  /// </summary>
  public void RecordSystemCompromise(RemoteSystem system, string exploitUsed, bool hasRoot)
  {
    if (system == null)
      return;

    string key = GetSystemKey(system.Hostname);

    if (!ownedSystems.ContainsKey(key))
    {
      // First time compromising this system
      var info = new OwnedSystemInfo
      {
        Hostname = system.Hostname,
        IPAddress = system.IPAddress,
        Type = system.Type,
        NetworkName = currentNetwork?.Metadata.Name ?? "Unknown",
        CompromiseDate = DateTime.Now,
        CompromiseMethod = DetermineCompromiseMethod(exploitUsed, hasRoot),
        HasUserAccess = true,
        HasRootAccess = hasRoot,
        LastAccessTime = DateTime.Now,
        IsCurrentlyConnected = true
      };

      ownedSystems[key] = info;

      // Record the exploit used
      if (!string.IsNullOrEmpty(exploitUsed))
      {
        info.ExploitsUsed.Add(exploitUsed);
        AddDiscoveredVulnerability(system.Hostname, exploitUsed);
      }

      // Increase network access level
      if (currentNetwork != null)
      {
        IncreaseNetworkAccessLevel(currentNetwork.NetworkId);
      }

      OnSystemCompromised?.Invoke(info);
      Debug.Log($"System compromised: {system.Hostname} via {exploitUsed}");
    }
    else
    {
      // System already owned - check for privilege escalation
      var info = ownedSystems[key];

      if (hasRoot && !info.HasRootAccess)
      {
        // Privilege escalation!
        info.HasRootAccess = true;
        info.CompromiseMethod = $"{info.CompromiseMethod} → Root via {exploitUsed}";
        info.LastAccessTime = DateTime.Now;

        if (!string.IsNullOrEmpty(exploitUsed) && !info.ExploitsUsed.Contains(exploitUsed))
        {
          info.ExploitsUsed.Add(exploitUsed);
          AddDiscoveredVulnerability(system.Hostname, exploitUsed);
        }

        // Additional access level increase for root
        if (currentNetwork != null)
        {
          IncreaseNetworkAccessLevel(currentNetwork.NetworkId);
        }

        Debug.Log($"Privilege escalation on {system.Hostname}: Root access obtained");
      }

      // Update last access time
      info.LastAccessTime = DateTime.Now;
      info.IsCurrentlyConnected = true;
    }

    SaveProgress();
  }

  /// <summary>
  /// Record that credentials were used to access a system
  /// </summary>
  public void RecordCredentialUse(string hostname, string credentialId)
  {
    string key = GetSystemKey(hostname);

    if (ownedSystems.TryGetValue(key, out var info))
    {
      if (!info.CredentialsUsed.Contains(credentialId))
      {
        info.CredentialsUsed.Add(credentialId);
        SaveProgress();
        Debug.Log($"Recorded credential use: {credentialId} on {hostname}");
      }
    }
  }

  /// <summary>
  /// Record that a flag was found on a system
  /// </summary>
  public void RecordFlagFound(string hostname, string flagContent)
  {
    string key = GetSystemKey(hostname);

    if (ownedSystems.TryGetValue(key, out var info))
    {
      if (!info.FlagsFound.Contains(flagContent))
      {
        info.FlagsFound.Add(flagContent);
        SaveProgress();
        Debug.Log($"Flag captured on {hostname}: {flagContent}");
      }
    }
    else
    {
      // System not owned yet but flag found? Shouldn't happen, but log it
      Debug.LogWarning($"Flag found on unowned system {hostname}");
    }
  }

  /// <summary>
  /// Record data exfiltration from a system
  /// </summary>
  public void RecordDataExfiltration(string hostname, long bytes)
  {
    string key = GetSystemKey(hostname);

    if (ownedSystems.TryGetValue(key, out var info))
    {
      info.DataExfiltrated += bytes;
      SaveProgress();
      Debug.Log($"Data exfiltrated from {hostname}: {FormatBytes(bytes)}");
    }
  }

  /// <summary>
  /// Record file access on a system
  /// </summary>
  public void RecordFileAccess(string hostname, string filePath)
  {
    string key = GetSystemKey(hostname);

    if (ownedSystems.TryGetValue(key, out var info))
    {
      info.FilesAccessed++;
      SaveProgress();
    }
  }

  /// <summary>
  /// Record connection to a system
  /// </summary>
  public void RecordConnection(string hostname)
  {
    string key = GetSystemKey(hostname);

    // Update previous connection status
    if (!string.IsNullOrEmpty(currentlyConnectedSystem))
    {
      string previousKey = GetSystemKey(currentlyConnectedSystem);
      if (ownedSystems.TryGetValue(previousKey, out var previousInfo))
      {
        previousInfo.IsCurrentlyConnected = false;
      }
    }

    // Update current connection
    if (ownedSystems.TryGetValue(key, out var info))
    {
      info.ConnectionsMade++;
      info.LastAccessTime = DateTime.Now;
      info.IsCurrentlyConnected = true;
      currentlyConnectedSystem = hostname;
      SaveProgress();
    }
  }

  /// <summary>
  /// Record how this system was discovered
  /// </summary>
  public void RecordDiscovery(string hostname, string discoveredFrom, string discoveryMethod)
  {
    string key = GetSystemKey(hostname);

    if (ownedSystems.TryGetValue(key, out var info))
    {
      info.DiscoveredFrom = discoveredFrom;
      info.DiscoveryMethod = discoveryMethod;
      SaveProgress();
    }
  }

  #endregion

  #region Vulnerability Tracking

  public void AddDiscoveredVulnerability(string hostname, string cve)
  {
    if (!discoveredVulnerabilities.ContainsKey(hostname))
    {
      discoveredVulnerabilities[hostname] = new HashSet<string>();
    }

    discoveredVulnerabilities[hostname].Add(cve);
    SaveProgress();
  }

  public List<string> GetDiscoveredVulnerabilities(string hostname)
  {
    if (discoveredVulnerabilities.TryGetValue(hostname, out var vulns))
    {
      return new List<string>(vulns);
    }
    return new List<string>();
  }

  #endregion

  #region Network Access Level Tracking

  public int GetNetworkAccessLevel(string networkId)
  {
    if (networkAccessLevels.TryGetValue(networkId, out int level))
      return level;
    return 0;
  }

  public void IncreaseNetworkAccessLevel(string networkId)
  {
    if (!networkAccessLevels.ContainsKey(networkId))
      networkAccessLevels[networkId] = 0;

    networkAccessLevels[networkId]++;
    OnNetworkAccessIncreased?.Invoke(networkId);
    SaveProgress();
  }

  public Dictionary<string, int> GetAllNetworkAccessLevels()
  {
    return new Dictionary<string, int>(networkAccessLevels);
  }

  #endregion

  #region Query Methods

  /// <summary>
  /// Check if a system has been compromised
  /// </summary>
  public bool HasCompromisedSystem(string hostname)
  {
    string key = GetSystemKey(hostname);
    return ownedSystems.ContainsKey(key);
  }

  /// <summary>
  /// Get list of compromised system hostnames (legacy compatibility)
  /// </summary>
  public List<string> GetCompromisedSystems()
  {
    return ownedSystems.Values.Select(info => info.Hostname).ToList();
  }

  /// <summary>
  /// Get detailed information about all owned systems
  /// </summary>
  public List<OwnedSystemInfo> GetOwnedSystems()
  {
    // Return systems from current network if available
    if (currentNetwork != null)
    {
      return ownedSystems.Values
          .Where(info => info.NetworkName == currentNetwork.Metadata.Name)
          .OrderByDescending(info => info.CompromiseDate)
          .ToList();
    }

    // Return all systems
    return ownedSystems.Values
        .OrderByDescending(info => info.CompromiseDate)
        .ToList();
  }

  /// <summary>
  /// Get owned systems from a specific network
  /// </summary>
  public List<OwnedSystemInfo> GetOwnedSystemsInNetwork(string networkName)
  {
    return ownedSystems.Values
        .Where(info => info.NetworkName == networkName)
        .OrderByDescending(info => info.CompromiseDate)
        .ToList();
  }

  /// <summary>
  /// Get information about a specific owned system
  /// </summary>
  public OwnedSystemInfo GetOwnedSystemInfo(string hostname)
  {
    string key = GetSystemKey(hostname);
    return ownedSystems.TryGetValue(key, out var info) ? info : null;
  }

  /// <summary>
  /// Get statistics across all owned systems
  /// </summary>
  public ProgressStatistics GetStatistics()
  {
    var stats = new ProgressStatistics
    {
      TotalSystemsCompromised = ownedSystems.Count,
      SystemsWithRootAccess = ownedSystems.Values.Count(info => info.HasRootAccess),
      TotalFlagsFound = ownedSystems.Values.Sum(info => info.FlagsFound.Count),
      TotalDataExfiltrated = ownedSystems.Values.Sum(info => info.DataExfiltrated),
      UniqueExploitsUsed = ownedSystems.Values
            .SelectMany(info => info.ExploitsUsed)
            .Distinct()
            .Count(),
      NetworksAccessed = networkAccessLevels.Count
    };

    return stats;
  }

  #endregion

  #region Persistence

  public void SaveProgress()
  {
    try
    {
      var saveData = new SaveData
      {
        OwnedSystems = ownedSystems.Values.ToList(),
        DiscoveredVulnerabilities = discoveredVulnerabilities.ToDictionary(
              kvp => kvp.Key,
              kvp => kvp.Value.ToList()
          ),
        NetworkAccessLevels = new Dictionary<string, int>(networkAccessLevels),
        CurrentlyConnectedSystem = currentlyConnectedSystem
      };

      string json = JsonUtility.ToJson(saveData, prettyPrint: true);
      PlayerPrefs.SetString("HackingProgress", json);
      PlayerPrefs.Save();

      Debug.Log($"Progress saved: {ownedSystems.Count} systems, {networkAccessLevels.Count} networks");
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to save progress: {ex.Message}");
    }
  }

  private void LoadProgress()
  {
    if (!PlayerPrefs.HasKey("HackingProgress"))
    {
      Debug.Log("No saved progress found, starting fresh");
      return;
    }

    try
    {
      string json = PlayerPrefs.GetString("HackingProgress");
      var saveData = JsonUtility.FromJson<SaveData>(json);

      if (saveData != null)
      {
        // Load owned systems
        ownedSystems.Clear();
        if (saveData.OwnedSystems != null)
        {
          foreach (var info in saveData.OwnedSystems)
          {
            string key = GetSystemKey(info.Hostname);
            ownedSystems[key] = info;
            // Clear connection status on load (we're not connected yet)
            info.IsCurrentlyConnected = false;
          }
        }

        // Load vulnerabilities
        discoveredVulnerabilities.Clear();
        if (saveData.DiscoveredVulnerabilities != null)
        {
          foreach (var entry in saveData.DiscoveredVulnerabilities)
          {
            if (entry.Value != null)
            {
              discoveredVulnerabilities[entry.Key] = new HashSet<string>(entry.Value);
            }
          }
        }

        // Load network access levels
        networkAccessLevels = saveData.NetworkAccessLevels != null
            ? new Dictionary<string, int>(saveData.NetworkAccessLevels)
            : new Dictionary<string, int>();

        currentlyConnectedSystem = saveData.CurrentlyConnectedSystem;

        Debug.Log($"Progress loaded: {ownedSystems.Count} systems, {networkAccessLevels.Count} networks");
      }
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to load progress: {ex.Message}. Starting fresh.");
      ownedSystems.Clear();
      discoveredVulnerabilities.Clear();
      networkAccessLevels.Clear();
    }
  }

  public bool HasUnsavedProgress()
  {
    // Could implement dirty tracking if needed
    return ownedSystems.Count > 0 || networkAccessLevels.Count > 0;
  }

  /// <summary>
  /// Clear all progress (for new game or reset)
  /// </summary>
  public void ClearProgress()
  {
    ownedSystems.Clear();
    discoveredVulnerabilities.Clear();
    networkAccessLevels.Clear();
    currentlyConnectedSystem = null;

    PlayerPrefs.DeleteKey("HackingProgress");
    PlayerPrefs.Save();

    Debug.Log("Progress cleared");
  }

  #endregion

  #region Helper Methods

  private string GetSystemKey(string hostname)
  {
    // Create a unique key for this system
    // Include network if available to handle systems with same hostname in different networks
    if (currentNetwork != null)
      return $"{hostname}@{currentNetwork.NetworkId}";
    return hostname;
  }

  private string DetermineCompromiseMethod(string exploitUsed, bool hasRoot)
  {
    if (string.IsNullOrEmpty(exploitUsed))
      return hasRoot ? "Unknown - Root access" : "Unknown";

    // Check if it looks like a CVE
    if (exploitUsed.StartsWith("CVE-"))
      return $"Exploit {exploitUsed}";

    // Check for common methods
    if (exploitUsed.ToLower().Contains("ssh"))
      return "SSH Login";
    if (exploitUsed.ToLower().Contains("brute"))
      return "Brute Force";
    if (exploitUsed.ToLower().Contains("credential"))
      return "Stolen Credentials";

    return exploitUsed;
  }

  private string FormatBytes(long bytes)
  {
    string[] sizes = { "B", "KB", "MB", "GB", "TB" };
    double len = bytes;
    int order = 0;

    while (len >= 1024 && order < sizes.Length - 1)
    {
      order++;
      len /= 1024;
    }

    return $"{len:0.##} {sizes[order]}";
  }

  #endregion

  #region Data Classes

  [Serializable]
  private class SaveData
  {
    public List<OwnedSystemInfo> OwnedSystems;
    public Dictionary<string, List<string>> DiscoveredVulnerabilities;
    public Dictionary<string, int> NetworkAccessLevels;
    public string CurrentlyConnectedSystem;
  }

  public class ProgressStatistics
  {
    public int TotalSystemsCompromised;
    public int SystemsWithRootAccess;
    public int TotalFlagsFound;
    public long TotalDataExfiltrated;
    public int UniqueExploitsUsed;
    public int NetworksAccessed;
  }

  #endregion
}
