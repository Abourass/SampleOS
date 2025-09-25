using System.Collections.Generic;
using Core.Networking.Access;
using Core.Networking.Discovery;

public static class CityNetworkFactory
{
  public static VirtualNetwork CreatePublicNetwork()
  {
    var metadata = new NetworkMetadata
    {
      Name = "Public Internet",
      Description = "Public internet access point",
      Organization = "ISP Provider",
      Type = NetworkType.ISP,
      IPRange = "203.0.113.0/24"
    };

    var securityProfile = new NetworkSecurityProfile
    {
      DefaultSecurityLevel = SecurityLevel.Medium,
      RequiresVPN = false,
      HasFirewall = false,
      AllowsGuestAccess = true
    };

    var network = new VirtualNetwork("public", metadata, securityProfile);

    // Add public facing systems
    network.AddSystem("dns", new RemoteSystem("dns", "dns.isp.net", "203.0.113.53", "server", "admin", SecurityLevel.Medium));
    network.AddSystem("webproxy", new RemoteSystem("webproxy", "proxy.isp.net", "203.0.113.10", "server", "admin", SecurityLevel.Medium));
    network.AddSystem("webmail", new RemoteSystem("webmail", "mail.public.com", "203.0.113.100", "server", "www", SecurityLevel.Low));
    network.AddSystem("vpnservice", new RemoteSystem("vpnservice", "vpn.secure.net", "203.0.113.200", "server", "admin", SecurityLevel.High));

    // Add gateways to other networks
    network.AddGateway(new NetworkGateway("residential_gw", "dns.isp.net", "residential_a", GatewayType.Router));
    network.AddGateway(new NetworkGateway("corporate_gw", "vpnservice", "megacorp", GatewayType.VPNServer));

    PopulatePublicFiles(network);

    return network;
  }

  private static void PopulatePublicFiles(VirtualNetwork network)
  {
    var vpnSystem = network.GetSystemByHostname("vpn.secure.net");
    if (vpnSystem != null)
    {
      vpnSystem.FileSystem.CreateDirectory("/var/www/html");
      vpnSystem.FileSystem.CreateFile("/var/www/html/index.html",
          "<html><body><h1>SecureVPN Service</h1><p>Login to access your VPN credentials.</p></body></html>");

      // Add a hint about corporate VPN
      vpnSystem.FileSystem.CreateFile("/var/www/html/clients.txt",
          "Client List (Partial):\n- MegaCorp Industries\n- TechStart Inc\n- City Hall\n- Police Department");
    }

    var webmailSystem = network.GetSystemByHostname("mail.public.com");
    if (webmailSystem != null)
    {
      webmailSystem.FileSystem.CreateDirectory("/var/mail/demo");
      webmailSystem.FileSystem.CreateFile("/var/mail/demo/welcome.eml",
          "From: admin@public.com\nTo: demo@public.com\nSubject: Welcome\n\n" +
          "Welcome to our public mail service. Your account has been created.");
    }
  }

  public static VirtualNetwork CreateGovernmentNetwork(string departmentName)
  {
    var metadata = new NetworkMetadata
    {
      Name = $"{departmentName} Network",
      Description = $"Government network for {departmentName}",
      Organization = departmentName,
      Type = NetworkType.Government,
      IPRange = "172.16.0.0/16"
    };

    var securityProfile = new NetworkSecurityProfile
    {
      DefaultSecurityLevel = SecurityLevel.VeryHigh,
      RequiresVPN = true,
      HasFirewall = true,
      NetworkSegmentation = true,
      RequiresMultiFactor = true,
      HasIntrusionDetection = true,
      LogsConnections = true,
      RequiresEncryption = true
    };

    var network = new VirtualNetwork($"gov_{departmentName.ToLower().Replace(" ", "_")}", metadata, securityProfile);

    // Add government systems
    network.AddSystem("mainframe", new RemoteSystem("mainframe", "main.gov.local", "172.16.1.10", "server", "sysadmin", SecurityLevel.VeryHigh));
    network.AddSystem("records", new RemoteSystem("records", "records.gov.local", "172.16.2.10", "server", "admin", SecurityLevel.High));
    network.AddSystem("secure-gateway", new RemoteSystem("secure-gw", "gateway.gov.local", "172.16.0.1", "router", "admin", SecurityLevel.VeryHigh));
    network.AddSystem("workstation", new RemoteSystem("workstation", "ws01.gov.local", "172.16.10.10", "desktop", "employee", SecurityLevel.High));

    // Add VPN gateway
    network.AddGateway(new NetworkGateway("gov_vpn", "gateway.gov.local", "public", GatewayType.VPNServer));

    PopulateGovernmentFiles(network, departmentName);

    return network;
  }

  private static void PopulateGovernmentFiles(VirtualNetwork network, string departmentName)
  {
    var records = network.GetSystemByHostname("records.gov.local");
    if (records != null)
    {
      records.FileSystem.CreateDirectory("/var/data");
      records.FileSystem.CreateDirectory("/var/data/public");
      records.FileSystem.CreateDirectory("/var/data/classified");

      // Add public records
      records.FileSystem.CreateFile("/var/data/public/directory.txt",
          $"{departmentName} Directory\n\nMain Office: 555-123-4567\nRecords Department: 555-123-4568\nIT Support: 555-123-4569");

      // Add classified records (requiring higher privileges)
      records.FileSystem.CreateFile("/var/data/classified/network_access.txt",
          "Internal Network Access Points:\n" +
          "- Main Firewall: 172.16.0.1\n" +
          "- VPN Concentrator: 172.16.0.2\n" +
          "- Backup Access: vpn2.citygovt.local");
    }

    var gateway = network.GetSystemByHostname("gateway.gov.local");
    if (gateway != null)
    {
      // Add VPN configurations
      gateway.FileSystem.CreateDirectory("/etc/vpn");
      gateway.FileSystem.CreateFile("/etc/vpn/server.conf",
          "port 1194\nproto udp\ndev tun\n" +
          "ca ca.crt\ncert server.crt\nkey server.key\n" +
          "auth-user-pass-verify /usr/local/bin/validate.sh via-env\n" +
          "client-cert-not-required\nusername-as-common-name");
    }
  }

  public static VirtualNetwork CreateDarkNetwork(string networkName)
  {
    var metadata = new NetworkMetadata
    {
      Name = networkName,
      Description = $"Underground dark network: {networkName}",
      Organization = "Unknown",
      Type = NetworkType.Criminal,
      IPRange = "192.168.100.0/24"
    };

    var securityProfile = new NetworkSecurityProfile
    {
      DefaultSecurityLevel = SecurityLevel.High,
      RequiresVPN = true,
      HasFirewall = true,
      NetworkSegmentation = false,
      RequiresMultiFactor = false,
      RequiresEncryption = true
    };

    var network = new VirtualNetwork($"dark_{networkName.ToLower().Replace(" ", "_")}", metadata, securityProfile);

    // Add dark web systems
    network.AddSystem("marketplace", new RemoteSystem("marketplace", "market.onion", "192.168.100.10", "server", "admin", SecurityLevel.High));
    network.AddSystem("forum", new RemoteSystem("forum", "forum.onion", "192.168.100.20", "server", "admin", SecurityLevel.Medium));
    network.AddSystem("dropzone", new RemoteSystem("dropzone", "drop.onion", "192.168.100.30", "server", "anonymous", SecurityLevel.High));
    network.AddSystem("proxy", new RemoteSystem("proxy", "proxy.onion", "192.168.100.1", "router", "admin", SecurityLevel.Medium));

    // Add tor gateway
    network.AddGateway(new NetworkGateway("tor_gateway", "proxy.onion", "public", GatewayType.ProxyServer));

    PopulateDarkNetFiles(network);

    return network;
  }

  private static void PopulateDarkNetFiles(VirtualNetwork network)
  {
    var marketplace = network.GetSystemByHostname("market.onion");
    if (marketplace != null)
    {
      marketplace.FileSystem.CreateDirectory("/var/www/html");
      marketplace.FileSystem.CreateFile("/var/www/html/index.php",
          "<?php\n// Underground Marketplace\n// Access restricted to verified members only\n?>\n" +
          "<html><body><h1>Underground Market</h1><p>Login to access services.</p></body></html>");

      // Add hints about other networks
      marketplace.FileSystem.CreateDirectory("/var/www/data");
      marketplace.FileSystem.CreateFile("/var/www/data/targets.txt",
          "Potential Targets:\n" +
          "- City Hall (172.16.x.x range)\n" +
          "- Police Department (172.17.x.x range)\n" +
          "- MegaCorp Industries (10.0.x.x range)\n\n" +
          "VPN access details available for purchase.");
    }

    var forum = network.GetSystemByHostname("forum.onion");
    if (forum != null)
    {
      forum.FileSystem.CreateDirectory("/var/www/html/forum");
      forum.FileSystem.CreateFile("/var/www/html/forum/config.php",
          "<?php\n$db_host = 'localhost';\n$db_user = 'forum_user';\n$db_pass = 'un5ecureP@55w0rd';\n$db_name = 'forum';\n?>");

      // Add forum posts with hints about other networks
      forum.FileSystem.CreateFile("/var/www/html/forum/posts.txt",
          "Thread: Government Network Access\n\n" +
          "User: shadow_hacker\nPost: Has anyone tried the City Hall VPN? I heard they're using default credentials on some systems.\n\n" +
          "User: netrunner\nPost: Yeah, I got in through their public-facing server. The IT admin password was hilariously weak.\n\n" +
          "User: system_breach\nPost: I'm selling access to Police Department internal network. PM for details.");
    }
  }

  public static VirtualNetwork CreateCorporateNetwork(string companyName)
  {
    var metadata = new NetworkMetadata
    {
      Name = $"{companyName} Corporate Network",
      Description = $"Internal corporate network for {companyName}",
      Organization = companyName,
      Type = NetworkType.Corporate,
      IPRange = "10.0.0.0/16"
    };

    var securityProfile = new NetworkSecurityProfile
    {
      DefaultSecurityLevel = SecurityLevel.High,
      RequiresVPN = true,
      HasFirewall = true,
      NetworkSegmentation = true
    };

    var network = new VirtualNetwork($"corp_{companyName.ToLower().Replace(" ", "")}", metadata, securityProfile);

    // Add corporate systems
    network.AddSystem("dc", new RemoteSystem("dc", "dc.corp.local", "10.0.1.10", "server", "administrator", SecurityLevel.VeryHigh));
    network.AddSystem("exchange", new RemoteSystem("exchange", "mail.corp.local", "10.0.2.50", "server", "admin", SecurityLevel.High));
    network.AddSystem("fileserver", new RemoteSystem("fileserver", "files.corp.local", "10.0.3.100", "storage", "admin", SecurityLevel.High));
    network.AddSystem("workstation01", new RemoteSystem("ws01", "ws01.corp.local", "10.0.10.151", "desktop", "jdoe", SecurityLevel.Medium));

    // Add VPN gateway
    var vpnGateway = new RemoteSystem("vpn-gw", "vpn.corp.local", "10.0.1.1", "server", "admin", SecurityLevel.High);
    network.AddSystem("vpn-gw", vpnGateway);
    network.AddGateway(new NetworkGateway("corp_vpn", "vpn.corp.local", "public", GatewayType.VPNServer));

    // Populate with corporate-specific files and credentials
    PopulateCorporateFiles(network);

    return network;
  }

  private static void PopulateCorporateFiles(VirtualNetwork network)
  {
    // Add VPN credentials to various systems
    var workstation = network.GetSystemByHostname("ws01.corp.local");
    if (workstation != null)
    {
      var credentials = new List<NetworkCredentials>
    {
      new NetworkCredentials("ws01.corp.local")
      {
        VPNCredentials = new Core.Networking.Discovery.VPNCredential
        {
          NetworkId = "partner_corp",
          NetworkName = "Partner Corporation VPN",
          Username = "jdoe@corp.com",
          Password = "SecurePass123!",
          ServerAddress = "vpn.partnercorp.com",
          Protocol = "OpenVPN"
        }
      }
    };

      workstation.FileSystem.AddNetworkDiscoveryFiles(credentials);
    }

    // Add network documentation to file server
    var fileServer = network.GetSystemByHostname("files.corp.local");
    if (fileServer != null)
    {
      fileServer.FileSystem.CreateDirectory("/shares/IT");
      fileServer.FileSystem.CreateFile("/shares/IT/network_topology.txt", GenerateNetworkDocumentation());
      fileServer.FileSystem.CreateFile("/shares/IT/vpn_client_list.csv", GenerateVPNClientList());
    }
  }

  public static VirtualNetwork CreateResidentialNetwork(string neighborhoodName)
  {
    var metadata = new NetworkMetadata
    {
      Name = $"{neighborhoodName} Residential",
      Description = $"Home network in {neighborhoodName}",
      Organization = "Residential",
      Type = NetworkType.Residential,
      IPRange = "192.168.1.0/24"
    };

    var securityProfile = new NetworkSecurityProfile
    {
      DefaultSecurityLevel = SecurityLevel.Low,
      RequiresVPN = false,
      HasFirewall = false,
      NetworkSegmentation = false
    };

    var network = new VirtualNetwork($"res_{neighborhoodName.ToLower().Replace(" ", "_")}", metadata, securityProfile);

    // Add home systems
    network.AddSystem("router", new RemoteSystem("router", "192.168.1.1", "192.168.1.1", "router", "admin", SecurityLevel.VeryLow));
    network.AddSystem("laptop", new RemoteSystem("laptop", "laptop", "192.168.1.100", "desktop", "user", SecurityLevel.Low));
    network.AddSystem("smart_tv", new RemoteSystem("tv", "smart-tv", "192.168.1.200", "embedded", "root", SecurityLevel.VeryLow));
    network.AddSystem("thermostat", new RemoteSystem("nest", "thermostat", "192.168.1.201", "embedded", "admin", SecurityLevel.VeryLow));

    PopulateResidentialFiles(network);

    return network;
  }

  private static void PopulateResidentialFiles(VirtualNetwork network)
  {
    var laptop = network.GetSystemByHostname("laptop");
    if (laptop != null)
    {
      laptop.FileSystem.CreateDirectory("/home/user/Documents");
      laptop.FileSystem.CreateDirectory("/home/user/Downloads");

      // Add some personal files with network hints
      laptop.FileSystem.CreateFile("/home/user/Documents/work_notes.txt",
          "Need to remember to connect to the VPN before accessing work files:\n" +
          "Server: vpn.megacorp.com\nUsername: j.smith\nPassword: Summer2023!");

      // Add browser history with corporate website
      laptop.FileSystem.CreateFile("/home/user/.mozilla/firefox/places.sqlite",
          "browsing_history_entry: https://www.megacorp.com/employee-portal (accessed 3 days ago)");

      var credentials = new List<NetworkCredentials>
      {
          new NetworkCredentials("laptop")
          {
              VPNCredentials = new Core.Networking.Discovery.VPNCredential
              {
                  NetworkId = "corp_megacorp",
                  NetworkName = "MegaCorp VPN",
                  Username = "j.smith",
                  Password = "Summer2023!",
                  ServerAddress = "vpn.megacorp.com"
              }
          }
      };

      laptop.FileSystem.AddNetworkDiscoveryFiles(credentials);
    }

    var router = network.GetSystemByHostname("192.168.1.1");
    if (router != null)
    {
      // Add router configuration with connected networks
      router.FileSystem.CreateFile("/etc/config/network",
          "config interface 'wan'\n\toption proto 'dhcp'\n\toption ifname 'eth0'\n" +
          "config interface 'lan'\n\toption proto 'static'\n\toption ipaddr '192.168.1.1'\n\toption netmask '255.255.255.0'");
    }
  }

  private static string GenerateNetworkDocumentation()
  {
    return "CORPORATE NETWORK TOPOLOGY\n" +
           "=========================\n\n" +
           "Main Subnets:\n" +
           "- 10.0.1.0/24 - Core Services\n" +
           "- 10.0.2.0/24 - Email & Communication\n" +
           "- 10.0.3.0/24 - File Storage\n" +
           "- 10.0.10.0/24 - User Workstations\n\n" +
           "Key Systems:\n" +
           "- Domain Controller: dc.corp.local (10.0.1.10)\n" +
           "- Email: mail.corp.local (10.0.2.50)\n" +
           "- File Server: files.corp.local (10.0.3.100)\n" +
           "- VPN Gateway: vpn.corp.local (10.0.1.1)\n\n" +
           "External Connections:\n" +
           "- Site-to-site VPN with partner_corp network\n" +
           "- VPN access for remote users";
  }

  private static string GenerateVPNClientList()
  {
    return "Username,Group,Last Connected,IP Address\n" +
           "jdoe,Executives,2023-08-15 09:45:22,10.0.10.100\n" +
           "msmith,IT,2023-08-15 08:30:15,10.0.10.101\n" +
           "rjohnson,Engineering,2023-08-14 17:20:30,10.0.10.102\n" +
           "partner_admin,Partners,2023-08-15 11:10:05,10.0.20.50\n";
  }
}
