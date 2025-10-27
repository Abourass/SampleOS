using UnityEngine;
using System.Collections.Generic;
using SampleOS.Core.FileSystem;
using SampleOS.Core.SoftwarePackages;
using SampleOS.Core.Apps;
using SampleOS.Core.Networking;
using System;
using SampleOS.Core.World;
using SampleOS.Core.Services;

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
    public List<string> NetworkMemberships { get; protected set; }
    public bool IsOnline { get; set; } = true;

    // Physical Location (can be null for virtual/cloud devices)
    public PhysicalLocation Location { get; set; }
    public bool IsPhysicallyAccessible { get; set; }

    // Upgrades
    public int CPULevel; // Affects processing speed in minigames
    public int RAMLevel; // Affects how many apps you can run simultaneously
    public int StorageLevel; // How much data/apps you can store
    public int NetworkLevel; // Affects number of connections and quality/speed

    // OS & Data
    public OSInfo OS { get; set; }
    // We have to lazy load the FileSystem so we have good gameplay
    private VirtualFileSystem _fileSystem;
    private bool _fileSystemLoaded = false;
    public bool HasFileSystemChanges { get; private set; }
    public List<Software> InstalledSoftware { get; protected set; }
    public List<DeviceCredential> Credentials { get; protected set; }

    // Interactive Apps (what the player can actually use)
    public List<IInteractiveApp> InteractiveApps { get; protected set; }
    public Dictionary<string, object> AppConfigs { get; protected set; }

    // Security
    public SecurityLevel SecurityLevel { get; set; }
    /// <summary>
    /// Whether this device has been compromised by the player
    /// </summary>
    public bool IsCompromised { get; set; }
    public Dictionary<string, DateTime> BackdoorConnections { get; protected set; }

    // Story/Metadata
    public Dictionary<string, object> Metadata { get; protected set; }

    protected Device(string deviceId, string hostname, string ip, DeviceType type)
    {
      DeviceId = deviceId;
      Hostname = hostname;
      IPAddress = ip;
      DeviceType = type;

      // Initialize new properties
      BackdoorConnections = new Dictionary<string, DateTime>();
      NetworkMemberships = new List<string>();
      InstalledSoftware = new List<Software>();
      Credentials = new List<DeviceCredential>();
      InteractiveApps = new List<IInteractiveApp>();
      AppConfigs = new Dictionary<string, object>();
      Metadata = new Dictionary<string, object>();

      IsOnline = true;
      SecurityLevel = SecurityLevel.Medium;
    }

    // Common device operations

    public VirtualFileSystem FileSystem
    {
      get
      {
        if (!_fileSystemLoaded)
        {
          _fileSystem = FileSystemFactory.CreateForDevice(DeviceType, Hostname);
          _fileSystemLoaded = true;
          Debug.Log($"[Device] Lazy-loaded file system for {Hostname}");
        }
        return _fileSystem;
      }
    }

    public void MarkFileSystemDirty()
    {
      HasFileSystemChanges = true;
      GameEvents.Instance.Trigger(GameEventType.DeviceFileSystemChanged, this);
    }

    // Support for devices moving between networks
    public void MoveToLocation(PhysicalLocation newLocation)
    {
      Location = newLocation;

      // Mobile devices may change networks when moving
      if (DeviceType.Category == DeviceCategory.MobileDevice)
      {
        UpdateNetworkMembership(newLocation);
      }

      GameEvents.Instance.Trigger(GameEventType.DeviceLocationChanged, this);
    }

    private void UpdateNetworkMembership(PhysicalLocation location)
    {
      // This will be implemented when we add NPC schedules
      // For now, just log
      Debug.Log($"[Device] {Hostname} moved to {location.LocationName}");
    }

    // NEW: Backdoor management
    public void AddBackdoor(string networkId, DateTime installedAt)
    {
      BackdoorConnections[networkId] = installedAt;
      IsCompromised = true;
      GameEvents.Instance.Trigger(GameEventType.DeviceBackdoorInstalled, this);
    }

    public void RemoveBackdoor(string networkId)
    {
      if (BackdoorConnections.Remove(networkId))
      {
        GameEvents.Instance.Trigger(GameEventType.DeviceBackdoorRemoved, this);
      }
    }

    public bool HasBackdoorTo(string networkId)
    {
      return BackdoorConnections.ContainsKey(networkId);
    }

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

    public void JoinNetwork(string networkId)
    {
      if (!NetworkMemberships.Contains(networkId))
      {
        NetworkMemberships.Add(networkId);
      }
    }

    public void LeaveNetwork(string networkId)
    {
      NetworkMemberships.Remove(networkId);
    }

    public bool IsOnNetwork(string networkId)
    {
      // Check both primary network and memberships
      return NetworkId == networkId || NetworkMemberships.Contains(networkId);
    }

    public T GetAppConfig<T>(string appId) where T : class
    {
      return AppConfigs.TryGetValue(appId, out var config) ? config as T : null;
    }

    public void SetAppConfig<T>(string appId, T config) where T : class
    {
      AppConfigs[appId] = config;
    }
  }

  [Serializable]
  public class OSInfo
  {
    public string Name { get; set; }        // "Linux", "Windows", "Android"
    public string Version { get; set; }     // "Ubuntu 22.04", "Windows 11"
    public string CurrentUser { get; set; } // "user", "root", "admin"

    public OSInfo(string name, string version, string currentUser = "user")
    {
      Name = name;
      Version = version;
      CurrentUser = currentUser;
    }
  }

  public class DeviceCredential
  {
    public string Username { get; set; }
    public string Password { get; set; }
    public bool IsDefault { get; set; }
  }

  public class DeviceSpecs
  {
    public int ProcessingPower; // 1-10
    public int Memory; // 1-10  
    public int ConnectionQuality; // 1-10

    // These affect minigames:
    public float MinigameSpeedModifier => 1f - (ProcessingPower * 0.05f);
    public int SimultaneousConnections => Memory * 2;
    public float DetectionResistance => ConnectionQuality * 0.1f;
  }
}
