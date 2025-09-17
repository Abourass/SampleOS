using System;
using System.Collections.Generic;
using UnityEngine;

public class SoftwareDatabase
{
  private Dictionary<string, List<SoftwareTemplate>> softwareByCategory = new Dictionary<string, List<SoftwareTemplate>>();

  private class SoftwareTemplate
  {
    public string Name { get; set; }
    public string OldVersion { get; set; }
    public string NewVersion { get; set; }
    public int[] DefaultPorts { get; set; }
  }

  public SoftwareDatabase()
  {
    // Web servers
    softwareByCategory["webserver"] = new List<SoftwareTemplate>
        {
            new SoftwareTemplate {
                Name = "Apache",
                OldVersion = "2.2.3",
                NewVersion = "2.4.54",
                DefaultPorts = new[] { 80, 443 }
            },
            new SoftwareTemplate {
                Name = "Nginx",
                OldVersion = "0.8.0",
                NewVersion = "1.22.0",
                DefaultPorts = new[] { 80, 443 }
            },
            new SoftwareTemplate {
                Name = "Tomcat",
                OldVersion = "6.0.0",
                NewVersion = "10.0.23",
                DefaultPorts = new[] { 8080, 8443 }
            }
        };

    // Databases
    softwareByCategory["database"] = new List<SoftwareTemplate>
        {
            new SoftwareTemplate {
                Name = "MySQL",
                OldVersion = "5.1.0",
                NewVersion = "8.0.30",
                DefaultPorts = new[] { 3306 }
            },
            new SoftwareTemplate {
                Name = "PostgreSQL",
                OldVersion = "8.4.0",
                NewVersion = "14.5.0",
                DefaultPorts = new[] { 5432 }
            },
            new SoftwareTemplate {
                Name = "MongoDB",
                OldVersion = "2.0.0",
                NewVersion = "6.0.0",
                DefaultPorts = new[] { 27017 }
            }
        };

    // Add more categories as needed: CMS, Firewalls, File servers, etc.
    softwareByCategory["cms"] = new List<SoftwareTemplate>
        {
            new SoftwareTemplate {
                Name = "WordPress",
                OldVersion = "3.0.0",
                NewVersion = "6.0.2",
                DefaultPorts = new[] { 80, 443 }
            },
            new SoftwareTemplate {
                Name = "Drupal",
                OldVersion = "6.0.0",
                NewVersion = "9.4.5",
                DefaultPorts = new[] { 80, 443 }
            }
        };

    softwareByCategory["firewall"] = new List<SoftwareTemplate>
        {
            new SoftwareTemplate {
                Name = "IPTables",
                OldVersion = "1.3.0",
                NewVersion = "1.8.8",
                DefaultPorts = new int[] { }
            },
            new SoftwareTemplate {
                Name = "UFW",
                OldVersion = "0.20",
                NewVersion = "0.36",
                DefaultPorts = new int[] { }
            }
        };

    softwareByCategory["fileserver"] = new List<SoftwareTemplate>
        {
            new SoftwareTemplate {
                Name = "Samba",
                OldVersion = "3.0.0",
                NewVersion = "4.16.4",
                DefaultPorts = new[] { 139, 445 }
            },
            new SoftwareTemplate {
                Name = "NFS",
                OldVersion = "3.0",
                NewVersion = "4.2",
                DefaultPorts = new[] { 2049 }
            }
        };
  }

  public Software GenerateRandomSoftware(string category, DateTime systemCreationDate)
  {
    if (!softwareByCategory.TryGetValue(category, out List<SoftwareTemplate> templates))
      return null;

    // Pick a random software from this category
    int index = UnityEngine.Random.Range(0, templates.Count);
    SoftwareTemplate template = templates[index];

    // Calculate a version based on the system's age
    Version oldVer = new Version(template.OldVersion);
    Version newVer = new Version(template.NewVersion);

    // Software should be newer than the system but not too new
    DateTime releaseDate = systemCreationDate.AddDays(UnityEngine.Random.Range(30, 365));

    // Determine how "recent" this software would be
    float ageRatio = (float)((DateTime.Now - releaseDate).TotalDays / 1500);
    ageRatio = Mathf.Clamp01(ageRatio);

    // Interpolate between old and new versions
    int major = oldVer.Major + (int)((newVer.Major - oldVer.Major) * (1 - ageRatio));
    int minor = oldVer.Minor + (int)((newVer.Minor - oldVer.Minor) * (1 - ageRatio));
    int build = UnityEngine.Random.Range(0, 100);

    string version = $"{major}.{minor}.{build}";

    // Create the software
    Software software = new Software(template.Name, version, category, releaseDate);

    // Add the default ports
    foreach (int port in template.DefaultPorts)
    {
      software.AddPort(port);
    }

    return software;
  }
}
