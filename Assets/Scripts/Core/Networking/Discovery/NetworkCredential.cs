using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Networking.Discovery
{
  public enum CredentialType
  {
    VPN,
    SSHKey,
    WebLogin,
    DatabaseConnection,
    Certificate,
    APIKey
  }

  [Serializable]
  public class NetworkCredentials
  {
    public VPNCredential VPNCredentials { get; set; }
    public List<SSHCredential> SSHCredentials { get; set; } = new List<SSHCredential>();
    public List<WebCredential> WebCredentials { get; set; } = new List<WebCredential>();
    public List<DatabaseCredential> DatabaseCredentials { get; set; } = new List<DatabaseCredential>();
    public List<CertificateCredential> CertificateCredentials { get; set; } = new List<CertificateCredential>();
    public List<APICredential> APICredentials { get; set; } = new List<APICredential>();

    public DateTime LastUpdated { get; set; } = DateTime.Now;
    public string SourceSystem { get; set; } // Which system these credentials came from

    public NetworkCredentials(string sourceSystem = null)
    {
      SourceSystem = sourceSystem;
    }

    /// <summary>
    /// Check if we have valid VPN credentials for a specific network
    /// </summary>
    public bool HasVPNCredentialsFor(string networkId)
    {
      return VPNCredentials?.NetworkId == networkId && VPNCredentials.IsValid();
    }

    /// <summary>
    /// Get SSH credentials for a specific hostname
    /// </summary>
    public SSHCredential GetSSHCredentialsFor(string hostname)
    {
      return SSHCredentials.FirstOrDefault(c =>
          c.Hostname.Equals(hostname, StringComparison.OrdinalIgnoreCase) && c.IsValid());
    }

    /// <summary>
    /// Merge credentials from another source
    /// </summary>
    public void MergeCredentials(NetworkCredentials other)
    {
      if (other.VPNCredentials != null && other.VPNCredentials.IsValid())
        VPNCredentials = other.VPNCredentials;

      SSHCredentials.AddRange(other.SSHCredentials.Where(c => c.IsValid()));
      WebCredentials.AddRange(other.WebCredentials.Where(c => c.IsValid()));
      DatabaseCredentials.AddRange(other.DatabaseCredentials.Where(c => c.IsValid()));
      CertificateCredentials.AddRange(other.CertificateCredentials.Where(c => c.IsValid()));
      APICredentials.AddRange(other.APICredentials.Where(c => c.IsValid()));

      LastUpdated = DateTime.Now;
    }
  }

  [Serializable]
  public abstract class BaseCredential
  {
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public string Name { get; set; }
    public string Description { get; set; }
    public DateTime DiscoveryDate { get; set; } = DateTime.Now;
    public string DiscoverySource { get; set; } // File path where found
    public CredentialType Type { get; protected set; }

    public abstract bool IsValid();
    public abstract string GetDisplayString();
  }

  [Serializable]
  public class VPNCredential : BaseCredential
  {
    public string NetworkId { get; set; }
    public string NetworkName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string ServerAddress { get; set; }
    public string Protocol { get; set; } = "OpenVPN";
    public int Port { get; set; } = 1194;
    public string ConfigFile { get; set; } // Path to .ovpn file if available

    public VPNCredential()
    {
      Type = CredentialType.VPN;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(Username) &&
             !string.IsNullOrEmpty(Password) &&
             !string.IsNullOrEmpty(ServerAddress) &&
             !string.IsNullOrEmpty(NetworkId);
    }

    public override string GetDisplayString()
    {
      return $"VPN: {NetworkName} ({Username}@{ServerAddress})";
    }
  }

  [Serializable]
  public class SSHCredential : BaseCredential
  {
    public string Hostname { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public int Port { get; set; } = 22;
    public string PrivateKeyPath { get; set; }
    public string PrivateKey { get; set; }
    public string KeyPassphrase { get; set; }

    public SSHCredential()
    {
      Type = CredentialType.SSHKey;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(Hostname) &&
             !string.IsNullOrEmpty(Username) &&
             (!string.IsNullOrEmpty(Password) || !string.IsNullOrEmpty(PrivateKey));
    }

    public override string GetDisplayString()
    {
      string authMethod = !string.IsNullOrEmpty(PrivateKey) ? "key" : "password";
      return $"SSH: {Username}@{Hostname}:{Port} ({authMethod})";
    }
  }

  [Serializable]
  public class WebCredential : BaseCredential
  {
    public string URL { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string Domain { get; set; }
    public List<string> Cookies { get; set; } = new List<string>();

    public WebCredential()
    {
      Type = CredentialType.WebLogin;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(URL) &&
             !string.IsNullOrEmpty(Username) &&
             !string.IsNullOrEmpty(Password);
    }

    public override string GetDisplayString()
    {
      return $"Web: {Username} @ {URL}";
    }
  }

  [Serializable]
  public class DatabaseCredential : BaseCredential
  {
    public string ServerAddress { get; set; }
    public int Port { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    public string Password { get; set; }
    public string DatabaseType { get; set; } // MySQL, PostgreSQL, etc.
    public string ConnectionString { get; set; }

    public DatabaseCredential()
    {
      Type = CredentialType.DatabaseConnection;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(ServerAddress) &&
             !string.IsNullOrEmpty(Username) &&
             !string.IsNullOrEmpty(Password);
    }

    public override string GetDisplayString()
    {
      return $"DB: {DatabaseType} - {Username}@{ServerAddress}:{Port}/{DatabaseName}";
    }
  }

  [Serializable]
  public class CertificateCredential : BaseCredential
  {
    public string CertificatePath { get; set; }
    public string CertificateData { get; set; } // PEM or other format
    public string PrivateKeyPath { get; set; }
    public string PrivateKeyData { get; set; }
    public string Passphrase { get; set; }
    public string CommonName { get; set; }
    public DateTime ExpiryDate { get; set; }
    public string Issuer { get; set; }

    public CertificateCredential()
    {
      Type = CredentialType.Certificate;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(CertificateData) &&
             ExpiryDate > DateTime.Now;
    }

    public override string GetDisplayString()
    {
      return $"Certificate: {CommonName} (expires {ExpiryDate:yyyy-MM-dd})";
    }
  }

  [Serializable]
  public class APICredential : BaseCredential
  {
    public string BaseURL { get; set; }
    public string APIKey { get; set; }
    public string SecretKey { get; set; }
    public string AccessToken { get; set; }
    public string RefreshToken { get; set; }
    public DateTime TokenExpiry { get; set; }
    public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    public APICredential()
    {
      Type = CredentialType.APIKey;
    }

    public override bool IsValid()
    {
      return !string.IsNullOrEmpty(BaseURL) &&
             (!string.IsNullOrEmpty(APIKey) || !string.IsNullOrEmpty(AccessToken));
    }

    public override string GetDisplayString()
    {
      string keyDisplay;
      if (string.IsNullOrEmpty(APIKey))
      {
        keyDisplay = "N/A";
      }
      else if (APIKey.Length < 8)
      {
        keyDisplay = APIKey;
      }
      else
      {
        keyDisplay = APIKey.Substring(0, 8) + "...";
      }
      return $"API: {BaseURL} (Key: {keyDisplay})";
    }
  }
}
