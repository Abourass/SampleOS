using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SampleOS.Core.Player
{
  /// <summary>
  /// Manages discovered credentials. Pure data storage - no persistence logic.
  /// </summary>
  public class PlayerCredentials
  {
    private Dictionary<string, HostCredentials> sshCredentials = new Dictionary<string, HostCredentials>();
    private Dictionary<string, VPNCredentialInfo> vpnCredentials = new Dictionary<string, VPNCredentialInfo>();

    public event Action<string, string> OnCredentialDiscovered;

    #region SSH Credentials

    public void StoreSSHCredential(string hostname, string username, string password)
    {
      if (!sshCredentials.ContainsKey(hostname))
      {
        sshCredentials[hostname] = new HostCredentials { Hostname = hostname };
      }

      var hostCreds = sshCredentials[hostname];
      if (!hostCreds.Credentials.Any(c => c.Username == username))
      {
        hostCreds.Credentials.Add(new CredentialPair
        {
          Username = username,
          Password = password,
          DiscoveredAt = DateTime.Now
        });

        OnCredentialDiscovered?.Invoke(hostname, username);
        Debug.Log($"SSH credential stored: {username}@{hostname}");
      }
    }

    public bool HasCredentialsFor(string hostname)
    {
      return sshCredentials.ContainsKey(hostname) && sshCredentials[hostname].Credentials.Count > 0;
    }

    public (string username, string password) GetSSHCredentials(string hostname)
    {
      if (sshCredentials.TryGetValue(hostname, out var hostCreds) && hostCreds.Credentials.Count > 0)
      {
        // Return first valid credential
        var cred = hostCreds.Credentials[0];
        return (cred.Username, cred.Password);
      }

      return (null, null);
    }

    public List<CredentialPair> GetAllSSHCredentialsFor(string hostname)
    {
      return sshCredentials.TryGetValue(hostname, out var hostCreds)
          ? new List<CredentialPair>(hostCreds.Credentials)
          : new List<CredentialPair>();
    }

    #endregion

    #region VPN Credentials

    public void StoreVPNCredential(string networkId, string networkName, string username, string password, string server, int port = 1194, string protocol = "OpenVPN")
    {
      if (!vpnCredentials.ContainsKey(networkId))
      {
        vpnCredentials[networkId] = new VPNCredentialInfo
        {
          NetworkId = networkId,
          NetworkName = networkName,
          Username = username,
          Password = password,
          Server = server,
          Port = port,
          Protocol = protocol,
          DiscoveredAt = DateTime.Now
        };

        Debug.Log($"VPN credential stored for network: {networkId}");
      }
    }

    public bool HasVPNCredentialsFor(string networkId)
    {
      return vpnCredentials.ContainsKey(networkId);
    }

    public VPNCredentialInfo GetVPNCredentials(string networkId)
    {
      return vpnCredentials.TryGetValue(networkId, out var creds) ? creds : null;
    }

    #endregion

    #region Query Methods

    public List<string> GetHostsWithCredentials()
    {
      return sshCredentials.Keys.ToList();
    }

    public int GetTotalCredentialCount()
    {
      return sshCredentials.Values.Sum(h => h.Credentials.Count) + vpnCredentials.Count;
    }

    #endregion

    #region Save/Load

    public PlayerCredentialsSaveData GetSaveData()
    {
      return new PlayerCredentialsSaveData
      {
        sshCredentials = sshCredentials.Values.ToList(),
        vpnCredentials = vpnCredentials.Values.ToList()
      };
    }

    public void LoadFromSave(PlayerCredentialsSaveData data)
    {
      if (data == null) return;

      sshCredentials.Clear();
      foreach (var hostCred in data.sshCredentials)
      {
        sshCredentials[hostCred.Hostname] = hostCred;
      }

      vpnCredentials.Clear();
      foreach (var vpnCred in data.vpnCredentials)
      {
        vpnCredentials[vpnCred.NetworkId] = vpnCred;
      }

      Debug.Log($"Credentials loaded: {sshCredentials.Count} hosts, {vpnCredentials.Count} VPNs");
    }

    #endregion
  }

  [Serializable]
  public class HostCredentials
  {
    public string Hostname;
    public List<CredentialPair> Credentials = new List<CredentialPair>();
  }

  [Serializable]
  public class CredentialPair
  {
    public string Username;
    public string Password;
    public DateTime DiscoveredAt;
  }

  [Serializable]
  public class VPNCredentialInfo
  {
    public string NetworkId;
    public string NetworkName;
    public string Username;
    public string Password;
    public string Server;
    public int Port;
    public string Protocol; // (OpenVPN, WireGuard, etc.)
    public DateTime DiscoveredAt;
  }

  [Serializable]
  public class PlayerCredentialsSaveData
  {
    public List<HostCredentials> sshCredentials = new List<HostCredentials>();
    public List<VPNCredentialInfo> vpnCredentials = new List<VPNCredentialInfo>();
  }
}
