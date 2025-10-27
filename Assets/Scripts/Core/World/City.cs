using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Services;
using UnityEngine;

namespace SampleOS.Core.World
{
  /// <summary>
  /// Represents a city in the game world with networks and locations.
  /// Pure data container - connection/discovery logic handled by services.
  /// </summary>
  public class City
  {
    public string CityId { get; private set; }
    public string CityName { get; private set; }
    public string Description { get; set; }

    // Networks in this city
    private Dictionary<string, VirtualNetwork> networks;

    // Physical locations players can visit
    private Dictionary<string, PhysicalLocation> locations;

    // Discovery clues found in this city
    private List<DiscoveryClue> clues;

    // Currently active network (what player is connected to)
    private string currentNetworkId;

    public City(string id, string name)
    {
      CityId = id;
      CityName = name;
      networks = new Dictionary<string, VirtualNetwork>();
      locations = new Dictionary<string, PhysicalLocation>();
      clues = new List<DiscoveryClue>();
    }

    #region Network Management

    /// <summary>
    /// Add a network to this city
    /// </summary>
    public void AddNetwork(VirtualNetwork network)
    {
      networks[network.NetworkId] = network;

      // Set as current if it's the first one
      if (string.IsNullOrEmpty(currentNetworkId))
      {
        currentNetworkId = network.NetworkId;
      }
    }

    /// <summary>
    /// Get a specific network by ID
    /// </summary>
    public VirtualNetwork GetNetwork(string networkId)
    {
      networks.TryGetValue(networkId, out var network);
      return network;
    }

    /// <summary>
    /// Get all networks in this city
    /// </summary>
    public List<VirtualNetwork> GetAllNetworks()
    {
      return new List<VirtualNetwork>(networks.Values);
    }

    /// <summary>
    /// Get the currently active network
    /// </summary>
    public VirtualNetwork CurrentNetwork => GetNetwork(currentNetworkId);

    /// <summary>
    /// Set which network is currently active (for UI/context)
    /// Note: Actual connections are managed by NetworkService
    /// </summary>
    public void SetCurrentNetwork(string networkId)
    {
      if (networks.ContainsKey(networkId))
      {
        currentNetworkId = networkId;
      }
    }

    /// <summary>
    /// Check if a network exists in this city
    /// </summary>
    public bool HasNetwork(string networkId)
    {
      return networks.ContainsKey(networkId);
    }

    #endregion

    #region Location Management

    /// <summary>
    /// Add a physical location to this city
    /// </summary>
    public void AddLocation(PhysicalLocation location)
    {
      locations[location.LocationId] = location;
    }

    /// <summary>
    /// Get a specific location by ID
    /// </summary>
    public PhysicalLocation GetLocation(string locationId)
    {
      locations.TryGetValue(locationId, out var loc);
      return loc;
    }

    /// <summary>
    /// Get all locations in this city
    /// </summary>
    public List<PhysicalLocation> GetAllLocations()
    {
      return new List<PhysicalLocation>(locations.Values);
    }

    /// <summary>
    /// Find locations by type
    /// </summary>
    public List<PhysicalLocation> GetLocationsByType(PhysicalLocation.LocationType type)
    {
      return locations.Values
          .Where(l => l.Type == type)
          .ToList();
    }

    #endregion

    #region Discovery Clue Management

    /// <summary>
    /// Register a discovery clue found in this city
    /// </summary>
    public void AddDiscoveryClue(DiscoveryClue clue)
    {
      // Avoid duplicates
      var existing = clues.FirstOrDefault(c =>
          c.NetworkId == clue.NetworkId &&
          c.Type == clue.Type &&
          c.FilePath == clue.FilePath &&
          c.SourceDeviceId == clue.SourceDeviceId);

      if (existing == null)
      {
        clues.Add(clue);
      }
    }

    /// <summary>
    /// Get all discovery clues in this city
    /// </summary>
    public List<DiscoveryClue> GetAllClues()
    {
      return new List<DiscoveryClue>(clues);
    }

    /// <summary>
    /// Get clues for a specific network
    /// </summary>
    public List<DiscoveryClue> GetCluesForNetwork(string networkId)
    {
      return clues.Where(c => c.NetworkId == networkId).ToList();
    }

    /// <summary>
    /// Get clues available at a specific location
    /// Used when player visits a location
    /// </summary>
    public List<DiscoveryClue> GetCluesAtLocation(string locationId)
    {
      return clues.Where(c => c.LocationId == locationId).ToList();
    }

    /// <summary>
    /// Get clues that could be found in files on a specific device
    /// Used when player scans/searches a compromised device
    /// </summary>
    public List<DiscoveryClue> GetCluesOnDevice(string deviceId)
    {
      return clues.Where(c => c.SourceDeviceId == deviceId).ToList();
    }

    #endregion

    #region Query Methods

    /// <summary>
    /// Get network metadata information
    /// </summary>
    public Result<NetworkMetadata> GetNetworkInfo(string networkId)
    {
      var network = GetNetwork(networkId);
      if (network == null)
      {
        return Result<NetworkMetadata>.Failure($"Network {networkId} not found");
      }

      return Result<NetworkMetadata>.Success(network.Metadata);
    }

    /// <summary>
    /// Get statistics about this city
    /// </summary>
    public CityStatistics GetStatistics()
    {
      return new CityStatistics
      {
        TotalNetworks = networks.Count,
        TotalLocations = locations.Count,
        TotalDevices = networks.Values.Sum(n => n.GetAllDevices().Count),
        TotalClues = clues.Count
      };
    }

    #endregion

    #region Device Management

    public List<Device> GetDevicesInCity()
    {
      var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
      return registry.GetDevicesInCity(CityId);
    }

    public List<Device> GetDevicesAtLocation(PhysicalLocation location)
    {
      var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
      return registry.GetDevicesAtLocation(location);
    }

    #endregion

    #region Save/Load

    public CitySaveData GetSaveData()
    {
      return new CitySaveData
      {
        cityId = CityId,
        currentNetworkId = currentNetworkId,
        discoveredClues = clues.ToList()
      };
    }

    public void LoadFromSave(CitySaveData data)
    {
      if (data == null) return;

      currentNetworkId = data.currentNetworkId;
      clues = data.discoveredClues ?? new List<DiscoveryClue>();

      Debug.Log($"City {CityName} loaded from save");
    }

    #endregion
  }

  /// <summary>
  /// City statistics for display
  /// </summary>
  public class CityStatistics
  {
    public int TotalNetworks;
    public int TotalLocations;
    public int TotalDevices;
    public int TotalClues;
  }

  [System.Serializable]
  public class CitySaveData
  {
    public string cityId;
    public string currentNetworkId;
    public List<DiscoveryClue> discoveredClues;
  }
}
