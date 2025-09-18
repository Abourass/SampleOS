using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class PlayerProgressManager
{
  private HashSet<string> compromisedSystems = new HashSet<string>();
  private Dictionary<string, HashSet<string>> discoveredVulnerabilities = new Dictionary<string, HashSet<string>>();
  private VirtualNetwork network;

  public PlayerProgressManager(VirtualNetwork network)
  {
    this.network = network;
    LoadProgress();
  }

  public void AddCompromisedSystem(string hostname)
  {
    compromisedSystems.Add(hostname);
    SaveProgress();
  }

  public bool HasCompromisedSystem(string hostname)
  {
    return compromisedSystems.Contains(hostname);
  }

  public List<string> GetCompromisedSystems()
  {
    return new List<string>(compromisedSystems);
  }

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

  private void SaveProgress()
  {
    // Create data structure to save
    var saveData = new SaveData
    {
      CompromisedSystems = new List<string>(compromisedSystems),
      DiscoveredVulnerabilities = new Dictionary<string, List<string>>()
    };

    foreach (var entry in discoveredVulnerabilities)
    {
      saveData.DiscoveredVulnerabilities[entry.Key] = new List<string>(entry.Value);
    }

    // Convert to JSON
    string json = JsonUtility.ToJson(saveData);
    
    // Save to PlayerPrefs for simplicity
    PlayerPrefs.SetString("HackingProgress", json);
    PlayerPrefs.Save();
  }

  private void LoadProgress()
  {
    if (PlayerPrefs.HasKey("HackingProgress"))
    {
      string json = PlayerPrefs.GetString("HackingProgress");
      var saveData = JsonUtility.FromJson<SaveData>(json);
      
      compromisedSystems = new HashSet<string>(saveData.CompromisedSystems);
      discoveredVulnerabilities = new Dictionary<string, HashSet<string>>();
      
      foreach (var entry in saveData.DiscoveredVulnerabilities)
      {
        discoveredVulnerabilities[entry.Key] = new HashSet<string>(entry.Value);
      }
    }
  }

  // Helper class for serialization
  [System.Serializable]
  private class SaveData
  {
    public List<string> CompromisedSystems;
    public Dictionary<string, List<string>> DiscoveredVulnerabilities;
  }
}
