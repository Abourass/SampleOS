using System.Collections.Generic;
using Core.Networking.Access;
using Core.Networking.Discovery;
using SampleOS.Core.Networking;
using SampleOS.Core.Devices;
using UnityEngine;

namespace SampleOS.Core.World
{
  /// <summary>
  /// Factory for creating predefined city networks
  /// </summary>
  public static class CityNetworkFactory
  {
    private static DeviceTypeDatabase deviceTypeDb = new DeviceTypeDatabase();

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

      // Add public facing systems using DeviceFactory
      var dns = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "dns",
        Hostname = "dns.isp.net",
        IPAddress = "203.0.113.53",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.Medium,
        DefaultUsername = "admin",
        DefaultPassword = "dns_admin123"
      });
      network.AddDevice(dns);

      var webproxy = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "webproxy",
        Hostname = "proxy.isp.net",
        IPAddress = "203.0.113.10",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.Medium,
        DefaultUsername = "admin",
        DefaultPassword = "proxy_pass"
      });
      network.AddDevice(webproxy);

      var webmail = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "webmail",
        Hostname = "mail.public.com",
        IPAddress = "203.0.113.100",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.Low,
        DefaultUsername = "www",
        DefaultPassword = "webmail123"
      });
      network.AddDevice(webmail);

      var vpnservice = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "vpnservice",
        Hostname = "vpn.secure.net",
        IPAddress = "203.0.113.200",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "vpn_secure_pass"
      });
      network.AddDevice(vpnservice);

      // Add gateways to other networks
      network.AddGateway(new NetworkGateway("residential_gw", "dns.isp.net", "residential_a", GatewayType.Router));
      network.AddGateway(new NetworkGateway("corporate_gw", "vpn.secure.net", "megacorp", GatewayType.VPNServer));

      PopulatePublicFiles(network);

      return network;
    }

    private static void PopulatePublicFiles(VirtualNetwork network)
    {
      var vpnDevice = network.GetDeviceByHostname("vpn.secure.net");
      if (vpnDevice != null)
      {
        vpnDevice.FileSystem.CreateDirectory("/var/www/html");
        vpnDevice.FileSystem.CreateFile("/var/www/html/index.html",
            "<html><body><h1>SecureVPN Service</h1><p>Login to access your VPN credentials.</p></body></html>");

        // Add a hint about corporate VPN
        vpnDevice.FileSystem.CreateFile("/var/www/html/clients.txt",
            "Client List (Partial):\n- MegaCorp Industries\n- TechStart Inc\n- City Hall\n- Police Department");
      }

      var webmailDevice = network.GetDeviceByHostname("mail.public.com");
      if (webmailDevice != null)
      {
        webmailDevice.FileSystem.CreateDirectory("/var/mail/demo");
        webmailDevice.FileSystem.CreateFile("/var/mail/demo/welcome.eml",
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
      var mainframe = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "mainframe",
        Hostname = "main.gov.local",
        IPAddress = "172.16.1.10",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.VeryHigh,
        DefaultUsername = "sysadmin",
        DefaultPassword = null // Will need to be discovered
      });
      network.AddDevice(mainframe);

      var records = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "records",
        Hostname = "records.gov.local",
        IPAddress = "172.16.2.10",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "records_admin"
      });
      network.AddDevice(records);

      var gateway = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "secure-gw",
        Hostname = "gateway.gov.local",
        IPAddress = "172.16.0.1",
        DeviceTypeId = "router",
        SecurityLevel = SecurityLevel.VeryHigh,
        DefaultUsername = "admin",
        DefaultPassword = null
      });
      network.AddDevice(gateway);

      var workstation = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "workstation",
        Hostname = "ws01.gov.local",
        IPAddress = "172.16.10.10",
        DeviceTypeId = "desktop",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "employee",
        DefaultPassword = "Summer2024!"
      });
      network.AddDevice(workstation);

      // Add VPN gateway
      network.AddGateway(new NetworkGateway("gov_vpn", "gateway.gov.local", "public", GatewayType.VPNServer));

      PopulateGovernmentFiles(network, departmentName);

      return network;
    }

    private static void PopulateGovernmentFiles(VirtualNetwork network, string departmentName)
    {
      var records = network.GetDeviceByHostname("records.gov.local");
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

      var gateway = network.GetDeviceByHostname("gateway.gov.local");
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
      var marketplace = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "marketplace",
        Hostname = "market.onion",
        IPAddress = "192.168.100.10",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "market_hidden"
      });
      network.AddDevice(marketplace);

      var forum = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "forum",
        Hostname = "forum.onion",
        IPAddress = "192.168.100.20",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.Medium,
        DefaultUsername = "admin",
        DefaultPassword = "forum123"
      });
      network.AddDevice(forum);

      var dropzone = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "dropzone",
        Hostname = "drop.onion",
        IPAddress = "192.168.100.30",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "anonymous",
        DefaultPassword = "drop_anon"
      });
      network.AddDevice(dropzone);

      var proxy = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "proxy",
        Hostname = "proxy.onion",
        IPAddress = "192.168.100.1",
        DeviceTypeId = "router",
        SecurityLevel = SecurityLevel.Medium,
        DefaultUsername = "admin",
        DefaultPassword = "tor_proxy"
      });
      network.AddDevice(proxy);

      // Add tor gateway
      network.AddGateway(new NetworkGateway("tor_gateway", "proxy.onion", "public", GatewayType.ProxyServer));

      PopulateDarkNetFiles(network);

      return network;
    }

    private static void PopulateDarkNetFiles(VirtualNetwork network)
    {
      var marketplace = network.GetDeviceByHostname("market.onion");
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

      var forum = network.GetDeviceByHostname("forum.onion");
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
      var dc = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "dc",
        Hostname = "dc.corp.local",
        IPAddress = "10.0.1.10",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.VeryHigh,
        DefaultUsername = "administrator",
        DefaultPassword = null
      });
      network.AddDevice(dc);

      var exchange = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "exchange",
        Hostname = "mail.corp.local",
        IPAddress = "10.0.2.50",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "Exchange2024!"
      });
      network.AddDevice(exchange);

      var fileserver = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "fileserver",
        Hostname = "files.corp.local",
        IPAddress = "10.0.3.100",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "FileServer123"
      });
      network.AddDevice(fileserver);

      var workstation01 = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "ws01",
        Hostname = "ws01.corp.local",
        IPAddress = "10.0.10.151",
        DeviceTypeId = "desktop",
        SecurityLevel = SecurityLevel.Medium,
        DefaultUsername = "jdoe",
        DefaultPassword = "Welcome2024"
      });
      network.AddDevice(workstation01);

      // Add VPN gateway
      var vpnGateway = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "vpn-gw",
        Hostname = "vpn.corp.local",
        IPAddress = "10.0.1.1",
        DeviceTypeId = "server",
        SecurityLevel = SecurityLevel.High,
        DefaultUsername = "admin",
        DefaultPassword = "VPN_Gateway_2024"
      });
      network.AddDevice(vpnGateway);
      network.AddGateway(new NetworkGateway("corp_vpn", "vpn.corp.local", "public", GatewayType.VPNServer));

      // Populate with corporate-specific files and credentials
      PopulateCorporateFiles(network);

      return network;
    }

    private static void PopulateCorporateFiles(VirtualNetwork network)
    {
      // Add VPN credentials to various systems
      var workstation = network.GetDeviceByHostname("ws01.corp.local");
      if (workstation != null)
      {
        var credentials = new List<NetworkCredentials>
        {
          new NetworkCredentials("ws01.corp.local")
          {
            VPNCredentials = new VPNCredential
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
      var fileServer = network.GetDeviceByHostname("files.corp.local");
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
      var router = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "router",
        Hostname = "192.168.1.1",
        IPAddress = "192.168.1.1",
        DeviceTypeId = "router",
        SecurityLevel = SecurityLevel.VeryLow,
        DefaultUsername = "admin",
        DefaultPassword = "admin"
      });
      network.AddDevice(router);

      var laptop = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "laptop",
        Hostname = "laptop",
        IPAddress = "192.168.1.100",
        DeviceTypeId = "desktop",
        SecurityLevel = SecurityLevel.Low,
        DefaultUsername = "user",
        DefaultPassword = "password"
      });
      network.AddDevice(laptop);

      var smartTv = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "tv",
        Hostname = "smart-tv",
        IPAddress = "192.168.1.200",
        DeviceTypeId = "embedded",
        SecurityLevel = SecurityLevel.VeryLow,
        DefaultUsername = "root",
        DefaultPassword = ""
      });
      network.AddDevice(smartTv);

      var thermostat = DeviceFactory.CreateRemoteDevice(new DeviceDefinition
      {
        DeviceId = "nest",
        Hostname = "thermostat",
        IPAddress = "192.168.1.201",
        DeviceTypeId = "embedded",
        SecurityLevel = SecurityLevel.VeryLow,
        DefaultUsername = "admin",
        DefaultPassword = "1234"
      });
      network.AddDevice(thermostat);

      PopulateResidentialFiles(network);

      return network;
    }

    private static void PopulateResidentialFiles(VirtualNetwork network)
    {
      var laptop = network.GetDeviceByHostname("laptop");
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
            VPNCredentials = new VPNCredential
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

      var router = network.GetDeviceByHostname("192.168.1.1");
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
}
