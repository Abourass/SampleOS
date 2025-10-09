using UnityEngine;
using System.Collections.Generic;
using SampleOS.Core.FileSystem;
using SampleOS.Core.SoftwarePackages;
using SampleOS.Core.Apps;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Networking;

namespace SampleOS.Core.Devices
{
  /// <summary>
  /// Base class for any networked device in the game world
  /// Can be physical (with a GameObject) or virtual (network-only)
  /// </summary>
  public abstract class Device
  {
    // Identity
    public string DeviceId { get; protected set; }
    public string Hostname { get; set; }
    public string IPAddress { get; set; }
    public DeviceType DeviceType { get; protected set; }

    // Network Location
    public string NetworkId { get; set; }
    public bool IsOnline { get; set; } = true;

    // Physical Location (optional)
    public Vector3? PhysicalPosition { get; set; }
    public string LocationId { get; set; } // "downtown_cafe", "corp_building_floor3"
    public bool IsPhysicallyAccessible { get; set; }

    // OS & Data
    public VirtualFileSystem FileSystem { get; protected set; }
    public List<Software> InstalledSoftware { get; protected set; }
    public List<DeviceCredential> Credentials { get; protected set; }

    // Interactive Apps (what the player can actually use)
    public List<IInteractiveApp> InteractiveApps { get; protected set; }

    // Security
    public SecurityLevel SecurityLevel { get; set; }
    /// <summary>
    /// Whether this device has been compromised by the player
    /// </summary>
    public bool IsCompromised { get; set; }

    // Story/Metadata
    public Dictionary<string, object> Metadata { get; protected set; }

    protected Device(string deviceId, string hostname, string ip, DeviceType type)
    {
      DeviceId = deviceId;
      Hostname = hostname;
      IPAddress = ip;
      DeviceType = type;

      FileSystem = FileSystemFactory.CreateForDevice(type, hostname);
      InstalledSoftware = new List<Software>();
      Credentials = new List<DeviceCredential>();
      InteractiveApps = new List<IInteractiveApp>();
      Metadata = new Dictionary<string, object>();
    }

    // Common device operations
    public abstract bool CanInteractDirectly(); // Can player walk up and use it?
    public abstract List<string> GetAvailableInteractionMethods(); // ["ssh", "rdp", "physical"]

    public virtual void AddCredential(string username, string password, bool isDefault = false)
    {
      Credentials.Add(new DeviceCredential
      {
        Username = username,
        Password = password,
        IsDefault = isDefault
      });
    }

    public virtual bool Authenticate(string username, string password)
    {
      return Credentials.Exists(c => c.Username == username && c.Password == password);
    }

    public virtual void InstallSoftware(Software software)
    {
      var existing = InstalledSoftware.Find(s => s.Name == software.Name);
      if (existing != null)
        InstalledSoftware.Remove(existing);

      InstalledSoftware.Add(software);
    }
  }

  public class DeviceCredential
  {
    public string Username { get; set; }
    public string Password { get; set; }
    public bool IsDefault { get; set; }
  }
}
