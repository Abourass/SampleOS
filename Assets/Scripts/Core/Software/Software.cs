using System;
using System.Collections.Generic;
using UnityEngine;

public class Software
{
  public string Name { get; private set; }
  public SoftwareVersion Version { get; private set; }
  public string InstallPath { get; private set; }
  public bool IsRunning { get; set; } = true;
  public List<int> ListeningPorts { get; private set; } = new List<int>();
  public List<Vulnerability> Vulnerabilities { get; private set; } = new List<Vulnerability>();
  public string Category { get; private set; }
  public DateTime ReleaseDate { get; private set; }

  public Software(string name, string version, string category, DateTime releaseDate)
  {
    Name = name;
    Version = new SoftwareVersion(version);
    Category = category;
    ReleaseDate = releaseDate;
    InstallPath = GenerateInstallPath();
  }

  private string GenerateInstallPath()
  {
    switch (Category)
    {
      case "webserver":
        return "/usr/sbin/" + Name.ToLower();
      case "database":
        return "/opt/" + Name.ToLower();
      default:
        return "/usr/bin/" + Name.ToLower();
    }
  }

  public void AddPort(int port)
  {
    if (!ListeningPorts.Contains(port))
      ListeningPorts.Add(port);
  }

  // Returns true if this software has a vulnerability
  public bool HasVulnerability()
  {
    return Vulnerabilities.Count > 0;
  }

  // Calculate probability of having a vulnerability based on age
  public float GetVulnerabilityProbability()
  {
    // Older software is more likely to have vulnerabilities
    TimeSpan age = DateTime.Now - ReleaseDate;
    return Mathf.Min(0.9f, (float)(age.TotalDays / 1000 * 0.1f)); // 10% per ~3 years, max 90%
  }
}
