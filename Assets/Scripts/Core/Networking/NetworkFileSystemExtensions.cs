using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Networking.Discovery;

public static class NetworkFileSystemExtensions
{
  public static void AddNetworkDiscoveryFiles(this VirtualFileSystem fs, List<NetworkCredentials> credentialsList)
  {
    // Create directory structure if needed
    fs.CreateDirectory("/home/user/Mail/inbox");
    fs.CreateDirectory("/home/user/.mozilla/firefox/profiles/default");
    fs.CreateDirectory("/etc/openvpn");
    fs.CreateDirectory("/home/user/.ssh");

    // Extract all VPN credentials from the collections
    List<VPNCredential> allVpnCreds = new List<VPNCredential>();
    foreach (var creds in credentialsList)
    {
      if (creds.VPNCredentials != null && creds.VPNCredentials.IsValid())
      {
        allVpnCreds.Add(creds.VPNCredentials);
      }
    }

    // Email with VPN information
    string emailContent = GenerateVPNEmail(allVpnCreds);
    fs.CreateFile("/home/user/Mail/inbox/vpn_setup.eml", emailContent);

    // Extract all web credentials
    List<WebCredential> allWebCreds = new List<WebCredential>();
    foreach (var creds in credentialsList)
    {
      allWebCreds.AddRange(creds.WebCredentials);
    }

    // Browser saved passwords
    string passwordFile = GenerateBrowserPasswords(allWebCreds);
    fs.CreateFile("/home/user/.mozilla/firefox/profiles/default/logins.json", passwordFile);

    // Network configuration files
    foreach (var vpnCred in allVpnCreds)
    {
      string configContent = GenerateVPNConfig(vpnCred);
      fs.CreateFile($"/etc/openvpn/{vpnCred.NetworkId}.ovpn", configContent);
    }

    // Extract all SSH credentials
    List<SSHCredential> allSshCreds = new List<SSHCredential>();
    foreach (var creds in credentialsList)
    {
      allSshCreds.AddRange(creds.SSHCredentials);
    }

    // SSH keys for jump hosts
    foreach (var sshCred in allSshCreds)
    {
      if (!string.IsNullOrEmpty(sshCred.PrivateKey))
      {
        string keyName = $"id_{sshCred.Hostname.Replace('.', '_')}";
        fs.CreateFile($"/home/user/.ssh/{keyName}", sshCred.PrivateKey);

        // Generate a public key if needed
        string publicKey = GeneratePublicKeyFromPrivate(sshCred.PrivateKey, sshCred.Username);
        fs.CreateFile($"/home/user/.ssh/{keyName}.pub", publicKey);
      }
    }
  }

  private static string GenerateVPNEmail(IEnumerable<VPNCredential> vpnCreds)
  {
    StringBuilder email = new StringBuilder();
    email.AppendLine("From: it-admin@company.com");
    email.AppendLine("To: user@company.com");
    email.AppendLine("Subject: VPN Access Configuration");
    email.AppendLine("Date: " + DateTime.Now.AddDays(-UnityEngine.Random.Range(1, 30)).ToString("R"));
    email.AppendLine();
    email.AppendLine("Hi,");
    email.AppendLine();
    email.AppendLine("As requested, here are your VPN connection details:");
    email.AppendLine();

    foreach (var cred in vpnCreds)
    {
      email.AppendLine($"Network: {cred.NetworkName}");
      email.AppendLine($"Server: {cred.ServerAddress}");
      email.AppendLine($"Username: {cred.Username}");
      email.AppendLine($"Password: {cred.Password}");
      email.AppendLine($"Protocol: {cred.Protocol}");
      email.AppendLine();
    }

    email.AppendLine("Please keep this information secure.");
    email.AppendLine("IT Department");

    return email.ToString();
  }

  private static string GenerateBrowserPasswords(IEnumerable<WebCredential> webCreds)
  {
    StringBuilder json = new StringBuilder();
    json.AppendLine("{");
    json.AppendLine("  \"logins\": [");

    bool first = true;
    foreach (var cred in webCreds)
    {
      if (!first) json.AppendLine(",");
      first = false;

      json.AppendLine("    {");
      json.AppendLine($"      \"url\": \"{cred.URL}\",");
      json.AppendLine($"      \"username\": \"{cred.Username}\",");
      json.AppendLine($"      \"password\": \"{cred.Password}\",");
      json.AppendLine($"      \"timeCreated\": \"{DateTime.Now.AddDays(-UnityEngine.Random.Range(1, 90)).ToString("o")}\",");
      json.AppendLine($"      \"timeLastUsed\": \"{DateTime.Now.AddDays(-UnityEngine.Random.Range(0, 30)).ToString("o")}\",");
      json.AppendLine("      \"timesUsed\": " + UnityEngine.Random.Range(1, 50));
      json.AppendLine("    }");
    }

    json.AppendLine("  ],");
    json.AppendLine("  \"version\": 3");
    json.AppendLine("}");

    return json.ToString();
  }

  private static string GenerateVPNConfig(VPNCredential vpnCred)
  {
    StringBuilder config = new StringBuilder();
    config.AppendLine("client");
    config.AppendLine("dev tun");
    config.AppendLine($"proto {vpnCred.Protocol.ToLower()}");
    config.AppendLine($"remote {vpnCred.ServerAddress} {vpnCred.Port}");
    config.AppendLine("resolv-retry infinite");
    config.AppendLine("nobind");
    config.AppendLine("persist-key");
    config.AppendLine("persist-tun");
    config.AppendLine("remote-cert-tls server");
    config.AppendLine("cipher AES-256-CBC");
    config.AppendLine("verb 3");
    config.AppendLine();
    config.AppendLine("# Authentication");
    config.AppendLine("auth-user-pass");
    config.AppendLine($"# Username: {vpnCred.Username}");
    config.AppendLine($"# Password: {vpnCred.Password}");
    config.AppendLine();
    config.AppendLine("# Network settings");
    config.AppendLine($"# Network ID: {vpnCred.NetworkId}");
    config.AppendLine($"# Network Name: {vpnCred.NetworkName}");

    return config.ToString();
  }

  private static string GeneratePublicKeyFromPrivate(string privateKey, string username)
  {
    // In a real implementation, this would generate a proper public key
    // For simulation, we'll create a plausible-looking public key
    string keyType = privateKey.Contains("BEGIN RSA") ? "ssh-rsa" : "ssh-ed25519";
    string keyData = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    + Convert.ToBase64String(Guid.NewGuid().ToByteArray())
                    + Convert.ToBase64String(Guid.NewGuid().ToByteArray());

    return $"{keyType} {keyData} {username}";
  }
}
