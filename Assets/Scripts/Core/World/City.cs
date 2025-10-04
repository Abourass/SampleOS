using System.Collections.Generic;
using Core.Networking.Connections;
using Core.Networking.Discovery;
using SampleOS.Core.Networking;
using SampleOS.Core.Networking.Cities;
using UnityEngine;

namespace SampleOS.Core.World
{
  /// <summary>
  /// Represents a city in the game world with networks and locations
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

    // Discovery system for finding networks/devices
    public NetworkDiscoveryManager DiscoveryManager { get; private set; }

    // Connection manager for VPN connections
    private ConnectionManager connectionManager;

    // Currently active network (for backwards compatibility)
    private string currentNetworkId;

    public City(string id, string name)
    {
      CityId = id;
      CityName = name;
      networks = new Dictionary<string, VirtualNetwork>();
      locations = new Dictionary<string, PhysicalLocation>();
      DiscoveryManager = new NetworkDiscoveryManager();
      connectionManager = new ConnectionManager();
    }

    /// <summary>
    /// Attempts to connect to a network via VPN
    /// </summary>
    public Result<VirtualNetwork> ConnectToNetwork(string networkId, NetworkCredentials credentials)
    {
      var network = GetNetwork(networkId);
      if (network == null)
        return Result<VirtualNetwork>.Failure($"Network {networkId} not found");

      // Validate VPN credentials
      if (credentials?.VPNCredentials == null)
        return Result<VirtualNetwork>.Failure("Invalid VPN credentials");

      // Verify the credentials work
      var vpnCred = credentials.VPNCredentials;
      if (string.IsNullOrEmpty(vpnCred.Username) || string.IsNullOrEmpty(vpnCred.Password))
        return Result<VirtualNetwork>.Failure("Invalid VPN credentials");

      // Use ConnectionManager's Connect method instead of manually creating connections
      var connectResult = connectionManager.EstablishConnection(
        currentNetworkId ?? "public", // Source network
        networkId,                     // Target network
        ConnectionType.VPN,            // Connection type
        credentials,                   // VPN credentials
        new Dictionary<string, object> // Optional parameters
        {
          { "VPNServer", vpnCred.ServerAddress },
          { "VPNProtocol", vpnCred.Protocol },
          { "Port", vpnCred.Port }
        }
      );

      if (connectResult.IsFailure)
        return Result<VirtualNetwork>.Failure(connectResult.ErrorMessage);

      // Switch to the new network
      SetCurrentNetwork(networkId);

      return Result<VirtualNetwork>.Success(network);
    }

    // Network management
    public void AddNetwork(VirtualNetwork network)
    {
      networks[network.NetworkId] = network;

      // Set as current if it's the first one
      if (string.IsNullOrEmpty(currentNetworkId))
        currentNetworkId = network.NetworkId;
    }

    public VirtualNetwork GetNetwork(string networkId)
    {
      networks.TryGetValue(networkId, out var network);
      return network;
    }

    public List<VirtualNetwork> GetAllNetworks()
    {
      return new List<VirtualNetwork>(networks.Values);
    }

    /// <summary>
    /// Gets list of discovered network IDs
    /// </summary>
    public List<string> GetDiscoveredNetworks()
    {
      return DiscoveryManager.GetDiscoveredNetworkIds();
    }

    public VirtualNetwork CurrentNetwork => GetNetwork(currentNetworkId);

    public void SetCurrentNetwork(string networkId)
    {
      if (networks.ContainsKey(networkId))
        currentNetworkId = networkId;
    }

    // Location management
    public void AddLocation(PhysicalLocation location)
    {
      locations[location.LocationId] = location;
    }

    public PhysicalLocation GetLocation(string locationId)
    {
      locations.TryGetValue(locationId, out var loc);
      return loc;
    }

    public List<PhysicalLocation> GetAllLocations()
    {
      return new List<PhysicalLocation>(locations.Values);
    }

    // Discovery
    public void RegisterDiscoveryClue(DiscoveryClue clue)
    {
      DiscoveryManager.AddClue(clue);
    }

    public List<DiscoveryClue> GetAvailableClues()
    {
      return DiscoveryManager.GetAllClues();
    }

    /// <summary>
    /// Gets the connection manager for this city
    /// </summary>
    public ConnectionManager GetConnectionManager()
    {
      return connectionManager;
    }

    /// <summary>
    /// Gets network metadata information
    /// </summary>
    public Result<NetworkMetadata> GetNetworkInfo(string networkId)
    {
      var network = GetNetwork(networkId);
      if (network == null)
        return Result<NetworkMetadata>.Failure($"Network {networkId} not found");

      return Result<NetworkMetadata>.Success(network.Metadata);
    }

    /// <summary>
    /// Gets clues available at a specific location
    /// Used when player visits a location
    /// </summary>
    public List<DiscoveryClue> GetCluesAtLocation(string locationId)
    {
      var allClues = DiscoveryManager.GetAllClues();
      var result = new List<DiscoveryClue>();

      foreach (var clue in allClues)
      {
        if (clue.LocationId == locationId)
          result.Add(clue);
      }

      return result;
    }

    /// <summary>
    /// Gets clues that could be found in files on a specific device
    /// Used when player scans/searches a compromised device
    /// </summary>
    public List<DiscoveryClue> GetCluesOnDevice(string deviceId)
    {
      var allClues = DiscoveryManager.GetAllClues();
      var result = new List<DiscoveryClue>();

      foreach (var clue in allClues)
      {
        if (clue.SourceDeviceId == deviceId)
          result.Add(clue);
      }

      return result;
    }
  }
}
