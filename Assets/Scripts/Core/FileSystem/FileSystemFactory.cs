using System.Collections.Generic;
using System;

/// <summary>
/// Factory class for creating and populating virtual file systems
/// </summary>
public static class FileSystemFactory
{
  /// <summary>
  /// Builds the default file system structure with standard Unix-like directories and files
  /// </summary>
  /// <param name="root">The root node of the file system</param>
  public static void BuildDefaultFileSystem(VirtualNode root)
  {
    // Create standard directories
    var bin = CreateDirectory(root, "bin");
    var etc = CreateDirectory(root, "etc");
    var home = CreateDirectory(root, "home");
    var usr = CreateDirectory(root, "usr");
    var var = CreateDirectory(root, "var");
    var tmp = CreateDirectory(root, "tmp");

    // Create user home directory
    var userHome = CreateDirectory(home, "user");

    // Add some configuration files
    CreateFile(etc, "hostname", "sampleos");
    CreateFile(etc, "hosts", "127.0.0.1 localhost\n192.168.1.100 raspberry\n");
    CreateFile(etc, "passwd", "root:x:0:0:root:/root:/bin/bash\nuser:x:1000:1000:Default User:/home/user:/bin/bash\n");

    // Add some files to user home
    CreateFile(userHome, "readme.txt", "Welcome to SampleOS!\n\nThis is a virtual terminal environment for learning command line basics.");
    CreateFile(userHome, ".bashrc", "# Sample bashrc file\nPS1='\\u@\\h:\\w\\$ '\nPATH=/bin:/usr/bin\n");

    // Create a projects directory with sample content
    var projects = CreateDirectory(userHome, "projects");
    CreateFile(projects, "notes.txt", "Project ideas:\n- Terminal game\n- Virtual OS\n- File explorer");

    // Create bin directory with some "executables"
    CreateFile(bin, "ls", "[BINARY CONTENT]");
    CreateFile(bin, "cd", "[BINARY CONTENT]");
    CreateFile(bin, "cat", "[BINARY CONTENT]");

    // Create usr structure
    var usrBin = CreateDirectory(usr, "bin");
    var usrLib = CreateDirectory(usr, "lib");
    CreateFile(usrBin, "grep", "[BINARY CONTENT]");
    CreateFile(usrBin, "find", "[BINARY CONTENT]");

    // Create var logs
    var log = CreateDirectory(var, "log");
    CreateFile(log, "system.log", GetRandomLogContent(30));
  }

  /// <summary>
  /// Helper method to create a directory in a parent node
  /// </summary>
  public static VirtualNode CreateDirectory(VirtualNode parent, string name)
  {
    var dir = new VirtualNode(name, true);
    parent.AddChild(dir);
    return dir;
  }

  /// <summary>
  /// Helper method to create a file in a parent directory
  /// </summary>
  public static VirtualNode CreateFile(VirtualNode parent, string name, string content)
  {
    var file = new VirtualNode(name, false, content);
    parent.AddChild(file);
    return file;
  }

  /// <summary>
  /// Generates random log content for sample log files
  /// </summary>
  private static string GetRandomLogContent(int lines)
  {
    string[] templates = new string[]
    {
            "[{0}] INFO: System initialized successfully",
            "[{0}] WARNING: Low disk space on /dev/sda1",
            "[{0}] INFO: User {1} logged in",
            "[{0}] INFO: Service {2} started",
            "[{0}] ERROR: Failed to connect to {3}",
            "[{0}] INFO: Package update completed",
            "[{0}] WARNING: CPU temperature above threshold"
    };

    string[] users = new string[] { "root", "user", "admin", "system" };
    string[] services = new string[] { "httpd", "sshd", "cron", "mysql", "docker" };
    string[] hosts = new string[] { "192.168.1.1", "server.local", "api.example.com", "database" };

    Random rand = new Random(42); // Fixed seed for reproducibility
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
          hosts[rand.Next(hosts.Length)]
      );

      logs.Add(entry);
    }

    return string.Join("\n", logs);
  }
}
