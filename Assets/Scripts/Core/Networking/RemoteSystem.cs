using System;
using System.Collections.Generic;

public class RemoteSystem
{
  public string Name { get; private set; }
  public string IPAddress { get; private set; }
  public string Hostname { get; private set; }
  public string Type { get; private set; }
  public VirtualFileSystem FileSystem { get; private set; }
  public List<Software> InstalledSoftware { get; private set; } = new List<Software>();
  public DateTime CreationDate { get; private set; }

  public string Username { get; private set; }
  public bool HasRootAccess { get; private set; } = false;
  private bool vulnerabilitiesGenerated = false;

  public RemoteSystem(string name, string hostname, string ipAddress, string type, string defaultUser)
  {
    Name = name;
    Hostname = hostname;
    IPAddress = ipAddress;
    Type = type;
    Username = defaultUser;
    FileSystem = new VirtualFileSystem();
    CreationDate = DateTime.Now.AddDays(-UnityEngine.Random.Range(30, 1500)); // 1 month to ~4 years old

    // Customize file system for this remote machine
    CustomizeFileSystem();

    // Generate software based on system type
    GenerateSoftware();
  }

  private void CustomizeFileSystem()
  {
    // Add system directories
    FileSystem.CreateDirectory("/bin");
    FileSystem.CreateDirectory("/etc");
    FileSystem.CreateDirectory("/usr");
    FileSystem.CreateDirectory("/usr/bin");
    FileSystem.CreateDirectory("/var");
    FileSystem.CreateDirectory("/var/log");
    FileSystem.CreateDirectory("/home");

    // Add user directories
    FileSystem.CreateDirectory($"/home/{Username}");

    // Add system files
    FileSystem.CreateFile("/etc/hostname", Hostname);
    FileSystem.CreateFile("/etc/hosts", $"127.0.0.1 localhost\n{IPAddress} {Hostname}");
  }

  private void GenerateSoftware()
  {
    // Generate software based on system type
    DeviceTypeDatabase typeDb = new DeviceTypeDatabase();
    DeviceType deviceType = typeDb.GetDeviceType(Type);

    // Common software for all systems
    InstalledSoftware.Add(new Software("OpenSSH", GetVersionBasedOnAge("5.3", "9.3"), "service",
                          CreationDate.AddDays(-UnityEngine.Random.Range(10, 90))));
    InstalledSoftware[0].AddPort(22);

    // Add type-specific software
    SoftwareDatabase softwareDb = new SoftwareDatabase();
    foreach (var softwareCategory in deviceType.SoftwareWeights)
    {
      if (UnityEngine.Random.value <= softwareCategory.Value)
      {
        // Add this type of software
        Software sw = softwareDb.GenerateRandomSoftware(softwareCategory.Key, CreationDate);
        if (sw != null)
        {
          InstalledSoftware.Add(sw);
        }
      }
    }
  }

  // Generate vulnerabilities for this system's software
  public void GenerateVulnerabilities()
  {
    if (vulnerabilitiesGenerated) return;

    VulnerabilityDatabase vulnDb = new VulnerabilityDatabase();
    foreach (Software sw in InstalledSoftware)
    {
      // Probability increases with age
      if (UnityEngine.Random.value <= sw.GetVulnerabilityProbability())
      {
        Vulnerability vulnerability = vulnDb.GetRandomVulnerabilityFor(sw);
        if (vulnerability != null)
        {
          sw.Vulnerabilities.Add(vulnerability);
        }
      }
    }

    vulnerabilitiesGenerated = true;
  }

  private string GetVersionBasedOnAge(string oldVersion, string newVersion)
  {
    // Calculate a version between oldVersion and newVersion based on creation date
    Version old = new Version(oldVersion);
    Version newVer = new Version(newVersion);  // Fixed: renamed to 'newVer'

    // Determine how old this system is (0.0 = brand new, 1.0 = max age)
    float ageRatio = (float)((DateTime.Now - CreationDate).TotalDays / 1500);

    // Interpolate between versions
    int major = old.Major + (int)((newVer.Major - old.Major) * (1 - ageRatio));
    int minor = UnityEngine.Random.Range(0, 10);

    return $"{major}.{minor}";
  }

  public void GiveRootAccess()
  {
    if (!HasRootAccess)
    {
      HasRootAccess = true;

      // Create a file indicating root access
      FileSystem.CreateFile("/root/.hacked", "Root access obtained on " + DateTime.Now.ToString());

      // Update some files/permissions to reflect root access
      FileSystem.CreateDirectory("/root");
      FileSystem.CreateFile("/root/README.txt", "You now have administrative access to this system.");

      // Add root user files to show access was gained
      FileSystem.CreateFile("/etc/sudoers", "# User privilege specification\nroot ALL=(ALL:ALL) ALL\n" + Username + " ALL=(ALL:ALL) ALL\n");

      // Add a hidden flag file for the player to find
      FileSystem.CreateFile("/root/.flag", "FLAG{" + Hostname.Replace(".", "_") + "_r00t_access}");
    }
  }

  // Check if the user has permission to access a specific file/directory
  public bool HasPermission(string path)
  {
    // If player has root access, they can access anything
    if (HasRootAccess)
      return true;

    // Otherwise check if it's a restricted path
    if (path.StartsWith("/root") || path == "/etc/shadow" || path == "/etc/sudoers")
      return false;

    return true;
  }

  public bool Authenticate(string user, string password)
  {
    // Simple authentication for demo purposes
    return user == Username && (password == "password" || string.IsNullOrEmpty(password));
  }

  public List<int> GetOpenPorts()
  {
    List<int> ports = new List<int>();
    foreach (var software in InstalledSoftware)
    {
      if (software.IsRunning)
      {
        ports.AddRange(software.ListeningPorts);
      }
    }
    return ports;
  }

  public Software GetSoftwareOnPort(int port)
  {
    foreach (var software in InstalledSoftware)
    {
      if (software.IsRunning && software.ListeningPorts.Contains(port))
      {
        return software;
      }
    }
    return null;
  }
}
