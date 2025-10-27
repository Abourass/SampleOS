using System.Collections.Generic;
using System;

namespace SampleOS.Core.FileSystem
{
  /// <summary>
  /// Factory class for creating and populating virtual file systems
  /// based on device type and operating system
  /// </summary>
  public static class FileSystemFactory
  {
    public enum OSType
    {
      Linux,
      Windows,
      RouterOS,      // OpenWRT, DD-WRT, etc.
      EmbeddedLinux, // Minimal Linux for IoT
      Android,
      Custom
    }

    /// <summary>
    /// Creates a filesystem appropriate for the device type
    /// </summary>
    public static VirtualFileSystem CreateForDevice(DeviceType deviceType, string hostname = "device")
    {
      var fs = new VirtualFileSystem();
      var root = fs.GetRoot();

      switch (deviceType.Category)
      {
        case DeviceCategory.Workstation:
          BuildWorkstationFileSystem(root, hostname);
          break;

        case DeviceCategory.Server:
          BuildServerFileSystem(root, hostname);
          break;

        case DeviceCategory.Router:
          BuildRouterFileSystem(root, hostname);
          break;

        case DeviceCategory.IoTDevice:
        case DeviceCategory.EmbeddedSystem:
          BuildEmbeddedFileSystem(root, hostname);
          break;

        case DeviceCategory.MobileDevice:
          BuildMobileFileSystem(root, hostname);
          break;

        case DeviceCategory.IndustrialControl:
          BuildIndustrialFileSystem(root, hostname);
          break;

        default:
          BuildLinuxFileSystem(root, hostname);
          break;
      }

      return fs;
    }

    /// <summary>
    /// Creates a default Linux desktop filesystem (backward compatibility)
    /// </summary>
    public static VirtualFileSystem CreateDefault()
    {
      var fs = new VirtualFileSystem();
      BuildLinuxFileSystem(fs.GetRoot(), "localhost");
      return fs;
    }

    #region Workstation Filesystems

    private static void BuildWorkstationFileSystem(VirtualNode root, string hostname)
    {
      // Desktop systems have more user files, applications, etc.
      BuildLinuxFileSystem(root, hostname);

      // Add desktop-specific directories
      var userHome = root.FindNode("/home/user");
      if (userHome != null)
      {
        CreateDirectory(userHome, "Desktop");
        CreateDirectory(userHome, "Downloads");
        CreateDirectory(userHome, "Documents");
        CreateDirectory(userHome, "Pictures");
        CreateDirectory(userHome, "Music");
        CreateDirectory(userHome, "Videos");

        // Browser data
        var mozilla = CreateDirectory(userHome, ".mozilla");
        var firefox = CreateDirectory(mozilla, "firefox");
        var profiles = CreateDirectory(firefox, "profiles");
        var defaultProfile = CreateDirectory(profiles, "default");

        // Email client
        var mail = CreateDirectory(userHome, "Mail");
        CreateDirectory(mail, "inbox");
        CreateDirectory(mail, "sent");

        // Add some personal files
        var documents = userHome.FindNode("Documents");
        CreateFile(documents, "resume.txt", "John Doe\nSoftware Developer\n...");
        CreateFile(documents, "todo.txt", "- Finish project\n- Update server\n- Fix bug in auth system");
      }
    }

    #endregion

    #region Server Filesystems

    private static void BuildServerFileSystem(VirtualNode root, string hostname)
    {
      // Servers have more services, logs, and less user content
      BuildLinuxFileSystem(root, hostname);

      // Server-specific directories
      CreateDirectory(root, "srv");
      var srv = root.FindNode("/srv");
      CreateDirectory(srv, "www");
      CreateDirectory(srv, "ftp");

      // Web server content
      var www = srv.FindNode("www");
      CreateFile(www, "index.html", "<html><body><h1>Welcome to " + hostname + "</h1></body></html>");
      CreateFile(www, "about.html", "<html><body><h1>About Us</h1></body></html>");

      // Configuration for various services
      var etc = root.FindNode("/etc");
      CreateFile(etc, "nginx.conf", GenerateNginxConfig(hostname));
      CreateFile(etc, "mysql.conf", GenerateMySQLConfig());

      // More extensive logging
      var log = root.FindNode("/var/log");
      CreateFile(log, "nginx.log", GetWebServerLogContent(50));
      CreateFile(log, "mysql.log", GetDatabaseLogContent(30));
      CreateFile(log, "auth.log", GetAuthLogContent(40));
      CreateFile(log, "syslog", GetRandomLogContent(100));

      // Database files
      var varLib = CreateDirectory(root.FindNode("/var"), "lib");
      var mysql = CreateDirectory(varLib, "mysql");
      CreateFile(mysql, "users.db", "[BINARY DATABASE CONTENT]");
      CreateFile(mysql, "products.db", "[BINARY DATABASE CONTENT]");
    }

    #endregion

    #region Router Filesystems

    private static void BuildRouterFileSystem(VirtualNode root, string hostname)
    {
      // Routers have minimal filesystems, mostly config files
      CreateDirectory(root, "bin");
      CreateDirectory(root, "sbin");
      CreateDirectory(root, "etc");
      CreateDirectory(root, "tmp");
      CreateDirectory(root, "var");

      var etc = root.FindNode("/etc");
      CreateFile(etc, "hostname", hostname);
      CreateFile(etc, "config", GenerateRouterConfig(hostname));
      CreateFile(etc, "firewall.user", "# Custom firewall rules\n");
      CreateFile(etc, "dhcp.conf", GenerateDHCPConfig());
      CreateFile(etc, "wireless.conf", GenerateWirelessConfig());

      // Admin web interface
      var www = CreateDirectory(root, "www");
      CreateFile(www, "index.html", "<html><head><title>Router Admin</title></head><body><h1>Router Configuration</h1></body></html>");
      CreateFile(www, "login.html", "<html><head><title>Login</title></head><body><form>Username: <input type='text'><br>Password: <input type='password'></form></body></html>");

      // Minimal logging
      var log = CreateDirectory(root.FindNode("/var"), "log");
      CreateFile(log, "messages", GetRouterLogContent(20));
    }

    #endregion

    #region Embedded/IoT Filesystems

    private static void BuildEmbeddedFileSystem(VirtualNode root, string hostname)
    {
      // Very minimal filesystem for IoT devices
      CreateDirectory(root, "bin");
      CreateDirectory(root, "etc");
      CreateDirectory(root, "tmp");

      var etc = root.FindNode("/etc");
      CreateFile(etc, "hostname", hostname);
      CreateFile(etc, "config.json", GenerateIoTConfig(hostname));
      CreateFile(etc, "wifi.conf", "ssid=IoT-Network\npsk=password123");

      // Minimal binaries
      var bin = root.FindNode("/bin");
      CreateFile(bin, "busybox", "[BINARY]");
      CreateFile(bin, "telnetd", "[BINARY]");

      // Device-specific data
      var data = CreateDirectory(root, "data");
      CreateFile(data, "sensor.log", GetSensorLogContent(10));
      CreateFile(data, "device.db", "[SQLITE DATABASE]");
    }

    #endregion

    #region Mobile Filesystems

    private static void BuildMobileFileSystem(VirtualNode root, string hostname)
    {
      // Android-like structure
      CreateDirectory(root, "system");
      CreateDirectory(root, "data");
      CreateDirectory(root, "sdcard");

      var system = root.FindNode("/system");
      CreateDirectory(system, "app");
      CreateDirectory(system, "bin");

      var data = root.FindNode("/data");
      var dataData = CreateDirectory(data, "data");

      // App data
      var appPackages = new[] { "com.android.browser", "com.android.email", "com.android.contacts" };
      foreach (var package in appPackages)
      {
        var appDir = CreateDirectory(dataData, package);
        CreateDirectory(appDir, "cache");
        CreateDirectory(appDir, "files");
        CreateFile(appDir, "shared_prefs.xml", "<preferences></preferences>");
      }

      // User storage
      var sdcard = root.FindNode("/sdcard");
      CreateDirectory(sdcard, "DCIM");
      CreateDirectory(sdcard, "Download");
      CreateDirectory(sdcard, "Documents");
    }

    #endregion

    #region Industrial Control Filesystems

    private static void BuildIndustrialFileSystem(VirtualNode root, string hostname)
    {
      // Industrial control systems (SCADA, PLC)
      BuildLinuxFileSystem(root, hostname);

      // Industrial-specific directories
      var opt = CreateDirectory(root, "opt");
      var scada = CreateDirectory(opt, "scada");
      CreateFile(scada, "config.xml", GenerateSCADAConfig());
      CreateFile(scada, "ladder_logic.ld", "[LADDER LOGIC PROGRAM]");

      var data = CreateDirectory(scada, "data");
      CreateFile(data, "sensor_readings.csv", GetIndustrialDataLog(20));
      CreateFile(data, "alarms.log", GetIndustrialAlarmLog(15));

      // More logging for compliance
      var log = root.FindNode("/var/log");
      CreateFile(log, "audit.log", GetAuditLogContent(50));
      CreateFile(log, "access.log", GetAccessLogContent(30));
    }

    #endregion

    #region Base Linux Filesystem

    private static void BuildLinuxFileSystem(VirtualNode root, string hostname)
    {
      // Standard Unix directory structure
      var bin = CreateDirectory(root, "bin");
      var etc = CreateDirectory(root, "etc");
      var home = CreateDirectory(root, "home");
      var usr = CreateDirectory(root, "usr");
      var var = CreateDirectory(root, "var");
      var tmp = CreateDirectory(root, "tmp");

      // User home
      var userHome = CreateDirectory(home, "user");
      CreateFile(userHome, ".bashrc", "# Sample bashrc file\nPS1='\\u@\\h:\\w\\$ '\nPATH=/bin:/usr/bin\n");
      CreateFile(userHome, ".bash_history", "ls -la\ncd /etc\ncat passwd\n");

      // System config
      CreateFile(etc, "hostname", hostname);
      CreateFile(etc, "hosts", $"127.0.0.1 localhost\n192.168.1.100 {hostname}\n");
      CreateFile(etc, "passwd", "root:x:0:0:root:/root:/bin/bash\nuser:x:1000:1000:User:/home/user:/bin/bash\n");
      CreateFile(etc, "shadow", "root:$6$...:18000:0:99999:7:::\nuser:$6$...:18000:0:99999:7:::\n");

      // Binaries
      CreateFile(bin, "bash", "[BINARY]");
      CreateFile(bin, "ls", "[BINARY]");
      CreateFile(bin, "cat", "[BINARY]");

      // Usr structure
      var usrBin = CreateDirectory(usr, "bin");
      var usrLib = CreateDirectory(usr, "lib");
      CreateFile(usrBin, "grep", "[BINARY]");
      CreateFile(usrBin, "find", "[BINARY]");

      // Logs
      var log = CreateDirectory(var, "log");
      CreateFile(log, "syslog", GetRandomLogContent(30));

      // Root home
      var rootDir = CreateDirectory(root, "root");
      CreateFile(rootDir, ".bash_history", "apt update\napt upgrade\nreboot\n");
    }

    #endregion

    #region Helper Methods

    private static VirtualNode CreateDirectory(VirtualNode parent, string name)
    {
      var dir = new VirtualNode(name, true);
      parent.AddChild(dir);
      return dir;
    }

    private static VirtualNode CreateFile(VirtualNode parent, string name, string content)
    {
      var file = new VirtualNode(name, false, content);
      parent.AddChild(file);
      return file;
    }

    #endregion

    #region Content Generators

    private static string GenerateNginxConfig(string hostname)
    {
      return $@"server {{
    listen 80;
    server_name {hostname};
    root /srv/www;
    index index.html;
}}";
    }

    private static string GenerateMySQLConfig()
    {
      return @"[mysqld]
datadir=/var/lib/mysql
socket=/var/lib/mysql/mysql.sock
port=3306
bind-address=0.0.0.0";
    }

    private static string GenerateRouterConfig(string hostname)
    {
      return $@"config system 'global'
    option hostname '{hostname}'
    option timezone 'UTC'

config interface 'lan'
    option proto 'static'
    option ipaddr '192.168.1.1'
    option netmask '255.255.255.0'";
    }

    private static string GenerateDHCPConfig()
    {
      return @"config dhcp 'lan'
    option interface 'lan'
    option start '100'
    option limit '150'
    option leasetime '12h'";
    }

    private static string GenerateWirelessConfig()
    {
      return @"config wifi-device 'radio0'
    option type 'mac80211'
    option channel '11'

config wifi-iface
    option ssid 'OpenWrt'
    option encryption 'psk2'
    option key 'password123'";
    }

    private static string GenerateIoTConfig(string hostname)
    {
      return $@"{{
  ""device_id"": ""{hostname}"",
  ""api_endpoint"": ""https://iot.cloud.com/api"",
  ""update_interval"": 60,
  ""sensors"": [""temperature"", ""humidity""]
}}";
    }

    private static string GenerateSCADAConfig()
    {
      return @"<?xml version='1.0'?>
<scada>
  <plc address='192.168.1.10' protocol='modbus' />
  <sensors count='24' scan_rate='1000' />
  <alarms enabled='true' log_file='/opt/scada/data/alarms.log' />
</scada>";
    }

    private static string GetWebServerLogContent(int lines)
    {
      var log = new System.Text.StringBuilder();
      var random = new Random(42);
      var ips = new[] { "203.0.113.1", "198.51.100.5", "192.0.2.100" };
      var paths = new[] { "/", "/about.html", "/api/users", "/admin" };
      var codes = new[] { 200, 200, 200, 304, 404, 500 };

      for (int i = 0; i < lines; i++)
      {
        var timestamp = DateTime.Now.AddHours(-random.Next(1, 72)).ToString("dd/MMM/yyyy:HH:mm:ss");
        var ip = ips[random.Next(ips.Length)];
        var path = paths[random.Next(paths.Length)];
        var code = codes[random.Next(codes.Length)];
        log.AppendLine($"{ip} - - [{timestamp}] \"GET {path} HTTP/1.1\" {code} {random.Next(100, 50000)}");
      }

      return log.ToString();
    }

    private static string GetDatabaseLogContent(int lines)
    {
      var templates = new[]
      {
        "[{0}] INFO: Connection established from {1}",
        "[{0}] QUERY: SELECT * FROM users WHERE id={2}",
        "[{0}] SLOW QUERY: Query took {3}ms",
        "[{0}] ERROR: Deadlock detected"
      };

      return GenerateLogFromTemplates(templates, lines);
    }

    private static string GetAuthLogContent(int lines)
    {
      var templates = new[]
      {
        "[{0}] SUCCESS: User '{1}' logged in from {4}",
        "[{0}] FAILURE: Failed login attempt for user '{1}' from {4}",
        "[{0}] INFO: User '{1}' logged out",
        "[{0}] WARNING: Multiple failed login attempts from {4}"
      };

      return GenerateLogFromTemplates(templates, lines);
    }

    private static string GetRouterLogContent(int lines)
    {
      var templates = new[]
      {
        "[{0}] INFO: DHCP lease assigned to {4}",
        "[{0}] INFO: Device connected to wireless",
        "[{0}] WARNING: High CPU usage detected",
        "[{0}] INFO: Firewall rule triggered for {4}"
      };

      return GenerateLogFromTemplates(templates, lines);
    }

    private static string GetSensorLogContent(int lines)
    {
      var log = new System.Text.StringBuilder();
      var random = new Random(42);

      for (int i = 0; i < lines; i++)
      {
        var timestamp = DateTime.Now.AddMinutes(-i * 5).ToString("yyyy-MM-dd HH:mm:ss");
        var temp = 20 + random.Next(-5, 15);
        var humidity = 45 + random.Next(-10, 20);
        log.AppendLine($"[{timestamp}] temperature={temp}C humidity={humidity}%");
      }

      return log.ToString();
    }

    private static string GetIndustrialDataLog(int lines)
    {
      var log = new System.Text.StringBuilder();
      log.AppendLine("timestamp,sensor_id,value,unit,status");

      var random = new Random(42);
      for (int i = 0; i < lines; i++)
      {
        var timestamp = DateTime.Now.AddMinutes(-i * 10).ToString("yyyy-MM-dd HH:mm:ss");
        var sensorId = $"SENSOR_{random.Next(1, 10):D2}";
        var value = random.Next(0, 100);
        var unit = random.Next(2) == 0 ? "PSI" : "RPM";
        var status = value > 80 ? "WARNING" : "NORMAL";
        log.AppendLine($"{timestamp},{sensorId},{value},{unit},{status}");
      }

      return log.ToString();
    }

    private static string GetIndustrialAlarmLog(int lines)
    {
      var alarms = new[]
      {
        "High temperature in Zone A",
        "Pressure exceeds threshold",
        "Emergency stop activated",
        "Motor speed abnormal",
        "Communication lost with PLC"
      };

      var log = new System.Text.StringBuilder();
      var random = new Random(42);

      for (int i = 0; i < lines; i++)
      {
        var timestamp = DateTime.Now.AddHours(-random.Next(1, 168)).ToString("yyyy-MM-dd HH:mm:ss");
        var alarm = alarms[random.Next(alarms.Length)];
        var severity = random.Next(3) switch { 0 => "WARNING", 1 => "ALARM", _ => "CRITICAL" };
        log.AppendLine($"[{timestamp}] {severity}: {alarm}");
      }

      return log.ToString();
    }

    private static string GetAuditLogContent(int lines)
    {
      var templates = new[]
      {
        "[{0}] USER_ACTION: User '{1}' modified configuration",
        "[{0}] ACCESS: User '{1}' accessed sensitive data",
        "[{0}] CHANGE: System setting changed by '{1}'",
        "[{0}] LOGIN: Administrative access by '{1}' from {4}"
      };

      return GenerateLogFromTemplates(templates, lines);
    }

    private static string GetAccessLogContent(int lines)
    {
      return GetAuthLogContent(lines); // Similar format
    }

    private static string GetRandomLogContent(int lines)
    {
      string[] templates = new[]
      {
        "[{0}] INFO: System initialized successfully",
        "[{0}] WARNING: Low disk space on /dev/sda1",
        "[{0}] INFO: User {1} logged in",
        "[{0}] INFO: Service {2} started",
        "[{0}] ERROR: Failed to connect to {3}",
        "[{0}] INFO: Package update completed",
        "[{0}] WARNING: CPU temperature above threshold"
      };

      return GenerateLogFromTemplates(templates, lines);
    }

    private static string GenerateLogFromTemplates(string[] templates, int lines)
    {
      string[] users = new[] { "root", "user", "admin", "system" };
      string[] services = new[] { "httpd", "sshd", "cron", "mysql", "docker" };
      string[] hosts = new[] { "192.168.1.1", "server.local", "api.example.com", "database" };
      string[] ips = new[] { "203.0.113.1", "198.51.100.5", "192.0.2.100", "10.0.0.50" };

      Random rand = new Random(42);
      DateTime timestamp = DateTime.Now.AddDays(-1);
      List<string> logs = new List<string>();

      for (int i = 0; i < lines; i++)
      {
        timestamp = timestamp.AddMinutes(rand.Next(1, 60));
        string timeStr = timestamp.ToString("yyyy-MM-dd HH:mm:ss");
        int templateIndex = rand.Next(templates.Length);

        string entry = string.Format(
          templates[templateIndex],
          timeStr,
          users[rand.Next(users.Length)],
          services[rand.Next(services.Length)],
          rand.Next(100, 5000),
          ips[rand.Next(ips.Length)]
        );

        logs.Add(entry);
      }

      return string.Join("\n", logs);
    }

    #endregion
  }
}
