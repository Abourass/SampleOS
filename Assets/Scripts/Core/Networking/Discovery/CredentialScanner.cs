using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Linq;

namespace Core.Networking.Discovery
{
  public class CredentialScanner
  {
    private readonly List<ScanPattern> scanPatterns;

    public CredentialScanner()
    {
      scanPatterns = InitializeScanPatterns();
    }

    /// <summary>
    /// Scan a compromised system for credentials and network clues
    /// </summary>
    public ScanResults ScanSystemForCredentials(RemoteSystem system)
    {
      var results = new ScanResults
      {
        SystemId = system.Hostname,
        ScanTime = DateTime.Now
      };

      try
      {
        // Scan common credential locations
        ScanEmailFiles(system, results);
        ScanBrowserData(system, results);
        ScanConfigurationFiles(system, results);
        ScanDocuments(system, results);
        ScanLogFiles(system, results);
        ScanSSHKeys(system, results);

        // Analyze installed software for network tools
        ScanInstalledSoftware(system, results);

        results.Success = true;
      }
      catch (Exception ex)
      {
        results.Success = false;
        results.ErrorMessage = ex.Message;
      }

      return results;
    }

    private void ScanEmailFiles(RemoteSystem system, ScanResults results)
    {
      var emailPaths = new[]
      {
        "/home/*/Mail/inbox/*",
        "/home/*/Maildir/cur/*",
        "/var/mail/*",
        "/home/*/.thunderbird/*/ImapMail/*/*"
    };

      foreach (var pathPattern in emailPaths)
      {
        var filesResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (filesResult.IsSuccess)
        {
          foreach (var file in filesResult.Data)
          {
            if (file.Content != null)
            {
              ScanFileForCredentials(file.Name, file.Content, CredentialSource.Email, results);
            }
          }
        }
      }
    }

    private void ScanBrowserData(RemoteSystem system, ScanResults results)
    {
      var browserPaths = new[]
      {
                "/home/*/.mozilla/firefox/*/logins.json",
                "/home/*/.config/google-chrome/Default/Login Data",
                "/home/*/.config/chromium/Default/Login Data",
                "/home/*/Library/Safari/Passwords.plist"
            };

      foreach (var pathPattern in browserPaths)
      {
        var filesResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (filesResult.IsSuccess)
        {
          foreach (var file in filesResult.Data)
          {
            if (file.Content != null)
            {
              ScanFileForCredentials(file.Name, file.Content, CredentialSource.Browser, results);
            }
          }
        }
      }
    }

    private void ScanConfigurationFiles(RemoteSystem system, ScanResults results)
    {
      var configPaths = new[]
      {
                "/etc/openvpn/*.conf",
                "/etc/openvpn/*.ovpn",
                "/home/*/.ssh/config",
                "/etc/network/interfaces",
                "/etc/netplan/*.yaml",
                "/etc/NetworkManager/system-connections/*"
            };

      foreach (var pathPattern in configPaths)
      {
        var filesResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (filesResult.IsSuccess)
        {
          foreach (var file in filesResult.Data)
          {
            if (file.Content != null)
            {
              ScanFileForCredentials(file.Name, file.Content, CredentialSource.Configuration, results);
            }
          }
        }
      }
    }

    private void ScanDocuments(RemoteSystem system, ScanResults results)
    {
      var documentPaths = new[]
      {
                "/home/*/Documents/*.txt",
                "/home/*/Documents/*.doc*",
                "/home/*/Desktop/*.txt",
                "/shares/*/IT/*",
                "/var/www/html/admin/*"
            };

      foreach (var pathPattern in documentPaths)
      {
        var filesResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (filesResult.IsSuccess)
        {
          foreach (var file in filesResult.Data)
          {
            if (file.Content != null)
            {
              ScanFileForCredentials(file.Name, file.Content, CredentialSource.Document, results);
            }
          }
        }
      }
    }

    private void ScanLogFiles(RemoteSystem system, ScanResults results)
    {
      var logPaths = new[]
      {
                "/var/log/auth.log",
                "/var/log/syslog",
                "/var/log/messages",
                "/var/log/vpn.log"
            };

      foreach (var pathPattern in logPaths)
      {
        var filesResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (filesResult.IsSuccess)
        {
          foreach (var file in filesResult.Data)
          {
            if (file.Content != null)
            {
              ScanFileForCredentials(file.Name, file.Content, CredentialSource.Log, results);
            }
          }
        }
      }
    }

    private void ScanSSHKeys(RemoteSystem system, ScanResults results)
    {
      var sshPaths = new[]
      {
                "/home/*/.ssh/id_rsa",
                "/home/*/.ssh/id_ecdsa",
                "/home/*/.ssh/id_ed25519",
                "/root/.ssh/id_rsa"
            };

      foreach (var pathPattern in sshPaths)
      {
        var keysResult = system.FileSystem.FindFilesByPattern(pathPattern);
        if (keysResult.IsSuccess)
        {
          foreach (var key in keysResult.Data)
          {
            if (key.Content != null)
            {
              var sshCred = ExtractSSHCredential(key.Name, key.Content);
              if (sshCred != null)
              {
                results.Credentials.SSHCredentials.Add(sshCred);
              }
            }
          }
        }
      }
    }

    private void ScanInstalledSoftware(RemoteSystem system, ScanResults results)
    {
      foreach (var software in system.InstalledSoftware)
      {
        if (IsVPNSoftware(software))
        {
          // Look for VPN configuration files
          var vpnClue = new DiscoveryClue("unknown", DiscoveryClueType.VPNConfiguration,
              $"VPN software {software.Name} detected");
          vpnClue.Properties["SoftwareName"] = software.Name;
          vpnClue.Properties["Version"] = software.Version.ToString();
          vpnClue.SourceSystemId = system.Hostname;

          results.DiscoveredClues.Add(vpnClue);
        }
      }
    }

    private void ScanFileForCredentials(string filePath, string content, CredentialSource source, ScanResults results)
    {
      foreach (var pattern in scanPatterns.Where(p => p.Source == source || p.Source == CredentialSource.Any))
      {
        var matches = pattern.Regex.Matches(content);
        foreach (Match match in matches)
        {
          var credential = pattern.ExtractCredential(match, filePath);
          if (credential != null)
          {
            AddCredentialToResults(credential, results);
          }
        }
      }

      // Also scan for network clues
      ScanFileForNetworkClues(filePath, content, results);
    }

    private void ScanFileForNetworkClues(string filePath, string content, ScanResults results)
    {
      // IP address patterns
      var ipPattern = new Regex(@"\b(?:[0-9]{1,3}\.){3}[0-9]{1,3}\b");
      var ipMatches = ipPattern.Matches(content);

      foreach (Match match in ipMatches)
      {
        var ip = match.Value;
        if (IsPrivateIP(ip))
        {
          var clue = new DiscoveryClue("unknown", DiscoveryClueType.IPAddressReference,
              $"Private IP address found: {ip}");
          clue.Properties["IPAddress"] = ip;
          clue.FilePath = filePath;
          clue.ReliabilityScore = 60;

          results.DiscoveredClues.Add(clue);
        }
      }

      // Domain name patterns
      var domainPattern = new Regex(@"\b[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?(\.[a-zA-Z0-9]([a-zA-Z0-9\-]{0,61}[a-zA-Z0-9])?)*\.corp\b|\.local\b|\.internal\b");
      var domainMatches = domainPattern.Matches(content);

      foreach (Match match in domainMatches)
      {
        var domain = match.Value;
        var clue = new DiscoveryClue("unknown", DiscoveryClueType.DomainReference,
            $"Internal domain found: {domain}");
        clue.Properties["DomainName"] = domain;
        clue.FilePath = filePath;
        clue.ReliabilityScore = 70;

        results.DiscoveredClues.Add(clue);
      }
    }

    private List<ScanPattern> InitializeScanPatterns()
    {
      return new List<ScanPattern>
            {
                // VPN configuration patterns
                new ScanPattern
                {
                    Name = "OpenVPN Config",
                    Regex = new Regex(@"remote\s+([^\s]+)\s+(\d+)", RegexOptions.IgnoreCase),
                    Source = CredentialSource.Configuration,
                    ExtractCredential = (match, filePath) => new VPNCredential
                    {
                        ServerAddress = match.Groups[1].Value,
                        Port = int.Parse(match.Groups[2].Value),
                        Protocol = "OpenVPN",
                        DiscoverySource = filePath,
                        NetworkId = ExtractNetworkIdFromPath(filePath)
                    }
                },
                
                // Email VPN credentials
                new ScanPattern
                {
                    Name = "Email VPN Credentials",
                    Regex = new Regex(@"VPN.*?Server:\s*([^\r\n]+).*?Username:\s*([^\r\n]+).*?Password:\s*([^\r\n]+)",
                                    RegexOptions.IgnoreCase | RegexOptions.Singleline),
                    Source = CredentialSource.Email,
                    ExtractCredential = (match, filePath) => new VPNCredential
                    {
                        ServerAddress = match.Groups[1].Value.Trim(),
                        Username = match.Groups[2].Value.Trim(),
                        Password = match.Groups[3].Value.Trim(),
                        Protocol = "OpenVPN",
                        DiscoverySource = filePath,
                        NetworkId = "extracted_from_email"
                    }
                },
                
                // SSH connection strings
                new ScanPattern
                {
                    Name = "SSH Connection",
                    Regex = new Regex(@"ssh\s+([^@]+)@([^\s]+)", RegexOptions.IgnoreCase),
                    Source = CredentialSource.Any,
                    ExtractCredential = (match, filePath) => new SSHCredential
                    {
                        Username = match.Groups[1].Value,
                        Hostname = match.Groups[2].Value,
                        DiscoverySource = filePath
                    }
                }
            };
    }

    private bool IsVPNSoftware(Software software)
    {
      var vpnSoftwareNames = new[] { "openvpn", "wireguard", "strongswan", "cisco", "forticlient" };
      return vpnSoftwareNames.Any(name =>
          software.Name.ToLower().Contains(name));
    }

    private bool IsPrivateIP(string ip)
    {
      var parts = ip.Split('.').Select(int.Parse).ToArray();

      // 10.0.0.0/8
      if (parts[0] == 10) return true;

      // 172.16.0.0/12
      if (parts[0] == 172 && parts[1] >= 16 && parts[1] <= 31) return true;

      // 192.168.0.0/16
      if (parts[0] == 192 && parts[1] == 168) return true;

      return false;
    }

    private string ExtractNetworkIdFromPath(string filePath)
    {
      var fileName = System.IO.Path.GetFileNameWithoutExtension(filePath);
      return fileName.ToLower().Replace(" ", "_");
    }

    private void AddCredentialToResults(BaseCredential credential, ScanResults results)
    {
      switch (credential.Type)
      {
        case CredentialType.VPN:
          results.Credentials.VPNCredentials = (VPNCredential)credential;
          break;
        case CredentialType.SSHKey:
          results.Credentials.SSHCredentials.Add((SSHCredential)credential);
          break;
        case CredentialType.WebLogin:
          results.Credentials.WebCredentials.Add((WebCredential)credential);
          break;
        case CredentialType.DatabaseConnection:
          results.Credentials.DatabaseCredentials.Add((DatabaseCredential)credential);
          break;
      }
    }

    private SSHCredential ExtractSSHCredential(string keyPath, string keyContent)
    {
      if (keyContent.Contains("PRIVATE KEY"))
      {
        return new SSHCredential
        {
          PrivateKey = keyContent,
          PrivateKeyPath = keyPath,
          DiscoverySource = keyPath,
          // Username and hostname would need to be determined from context
        };
      }
      return null;
    }
  }

  public class ScanPattern
  {
    public string Name { get; set; }
    public Regex Regex { get; set; }
    public CredentialSource Source { get; set; }
    public Func<Match, string, BaseCredential> ExtractCredential { get; set; }
  }

  public enum CredentialSource
  {
    Email,
    Browser,
    Configuration,
    Document,
    Log,
    Any
  }

  public class ScanResults
  {
    public string SystemId { get; set; }
    public DateTime ScanTime { get; set; }
    public bool Success { get; set; }
    public string ErrorMessage { get; set; }

    public NetworkCredentials Credentials { get; set; } = new NetworkCredentials();
    public List<DiscoveryClue> DiscoveredClues { get; set; } = new List<DiscoveryClue>();

    public int GetTotalCredentialsFound()
    {
      int count = 0;
      if (Credentials.VPNCredentials != null) count++;
      count += Credentials.SSHCredentials.Count;
      count += Credentials.WebCredentials.Count;
      count += Credentials.DatabaseCredentials.Count;
      count += Credentials.CertificateCredentials.Count;
      count += Credentials.APICredentials.Count;
      return count;
    }

    public int GetTotalCluesFound()
    {
      return DiscoveredClues.Count;
    }
  }
}
