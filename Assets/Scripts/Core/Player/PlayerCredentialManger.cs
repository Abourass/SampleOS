using System;
using System.Collections.Generic;
using System.Linq;
using Core.Networking.Discovery;
using UnityEngine;

public class PlayerCredentialManager
{
  private Dictionary<string, NetworkCredentials> storedCredentials = new Dictionary<string, NetworkCredentials>();
  private Dictionary<string, bool> systemRootAccess = new Dictionary<string, bool>();

  // Event for when new credentials are discovered
  public event Action<NetworkCredentials> OnCredentialsDiscovered;

  public PlayerCredentialManager()
  {
    LoadStoredCredentials();
  }

  /// <summary>
  /// Store discovered VPN credentials
  /// </summary>
  public void StoreVPNCredentials(VPNCredential credentials)
  {
    if (credentials == null || !credentials.IsValid())
      return;

    string networkId = credentials.NetworkId;

    if (!storedCredentials.TryGetValue(networkId, out NetworkCredentials existing))
    {
      existing = new NetworkCredentials(credentials.DiscoverySource);
      storedCredentials[networkId] = existing;
    }

    existing.VPNCredentials = credentials;

    SaveStoredCredentials();
    OnCredentialsDiscovered?.Invoke(existing);
  }

  /// <summary>
  /// Store SSH credentials
  /// </summary>
  public void StoreSSHCredentials(SSHCredential credentials)
  {
    if (credentials == null || !credentials.IsValid())
      return;

    string hostname = credentials.Hostname;
    string networkId = DetermineNetworkFromHostname(hostname);

    if (!storedCredentials.TryGetValue(networkId, out NetworkCredentials existing))
    {
      existing = new NetworkCredentials(credentials.DiscoverySource);
      storedCredentials[networkId] = existing;
    }

    // Check if we already have these credentials
    if (!existing.SSHCredentials.Any(c => c.Hostname == credentials.Hostname && c.Username == credentials.Username))
    {
      existing.SSHCredentials.Add(credentials);

      SaveStoredCredentials();
      OnCredentialsDiscovered?.Invoke(existing);
    }
  }

  /// <summary>
  /// Store scan results from a compromised system
  /// </summary>
  public void StoreCredentialScanResults(ScanResults results)
  {
    if (results == null || !results.Success)
      return;

    // Create a new NetworkCredentials collection
    NetworkCredentials newCredentials = new NetworkCredentials(results.SystemId);

    // Copy all the credentials from the scan results
    if (results.Credentials.VPNCredentials != null && results.Credentials.VPNCredentials.IsValid())
      newCredentials.VPNCredentials = results.Credentials.VPNCredentials;

    newCredentials.SSHCredentials.AddRange(results.Credentials.SSHCredentials.Where(c => c.IsValid()));
    newCredentials.WebCredentials.AddRange(results.Credentials.WebCredentials.Where(c => c.IsValid()));
    newCredentials.DatabaseCredentials.AddRange(results.Credentials.DatabaseCredentials.Where(c => c.IsValid()));

    // Store the credentials by network ID
    string networkId = results.Credentials.VPNCredentials?.NetworkId ?? results.SystemId;

    if (!storedCredentials.ContainsKey(networkId))
      storedCredentials[networkId] = newCredentials;
    else
      storedCredentials[networkId].MergeCredentials(newCredentials);

    SaveStoredCredentials();
    OnCredentialsDiscovered?.Invoke(newCredentials);
  }

  /// <summary>
  /// Record root access to a system
  /// </summary>
  public void RecordRootAccess(string hostname)
  {
    systemRootAccess[hostname] = true;
    SaveStoredCredentials();
  }

  /// <summary>
  /// Check if player has root access to a system
  /// </summary>
  public bool HasRootAccess(string hostname)
  {
    return systemRootAccess.ContainsKey(hostname) && systemRootAccess[hostname];
  }

  /// <summary>
  /// Get VPN credentials for a specific network
  /// </summary>
  public VPNCredential GetVPNCredentialsFor(string networkId)
  {
    if (storedCredentials.TryGetValue(networkId, out NetworkCredentials creds))
      return creds.VPNCredentials;

    return null;
  }

  /// <summary>
  /// Get SSH credentials for a specific hostname
  /// </summary>
  public SSHCredential GetSSHCredentialsFor(string hostname)
  {
    foreach (var creds in storedCredentials.Values)
    {
      var sshCred = creds.GetSSHCredentialsFor(hostname);
      if (sshCred != null)
        return sshCred;
    }

    return null;
  }

  /// <summary>
  /// Get all stored credentials for UI display
  /// </summary>
  public Dictionary<string, NetworkCredentials> GetAllStoredCredentials()
  {
    return new Dictionary<string, NetworkCredentials>(storedCredentials);
  }

  /// <summary>
  /// Get a list of systems with root access
  /// </summary>
  public List<string> GetSystemsWithRootAccess()
  {
    return systemRootAccess.Where(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
  }

  // Helper methods
  private string DetermineNetworkFromHostname(string hostname)
  {
    // Simple determination based on hostname/domain
    if (hostname.Contains("corp"))
      return "corp_megacorp";
    else if (hostname.Contains("gov"))
      return "gov_cityhall";
    else if (hostname.Contains("police"))
      return "gov_police";
    else if (hostname.Contains("onion"))
      return "dark_underground";
    else
      return "public";
  }

  /// <summary>
  /// Get VPN credentials for a specific network
  /// </summary>
  public VPNCredential GetCredentialsForNetwork(string networkId)
  {
    if (storedCredentials.TryGetValue(networkId, out NetworkCredentials creds))
    {
      return creds.VPNCredentials;
    }
    return null;
  }

  private void StoreWebCredentials(WebCredential credentials)
  {
    // Implementation similar to StoreSSHCredentials
  }

  private void StoreDatabaseCredentials(DatabaseCredential credentials)
  {
    // Implementation similar to StoreSSHCredentials
  }

  private void SaveStoredCredentials()
  {
    // Save credentials to PlayerPrefs (simplified for example)
    try
    {
      // In a real implementation, this would serialize to JSON and save
      PlayerPrefs.Save();
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to save credentials: {ex.Message}");
    }
  }

  private void LoadStoredCredentials()
  {
    // Load credentials from PlayerPrefs (simplified for example)
    try
    {
      // In a real implementation, this would deserialize from JSON
    }
    catch (Exception ex)
    {
      Debug.LogError($"Failed to load credentials: {ex.Message}");
    }
  }
}
