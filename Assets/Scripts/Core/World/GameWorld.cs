using System.Collections.Generic;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking;
using SampleOS.Core.Services;

namespace SampleOS.Core.World
{
  /// <summary>
  /// Top-level world manager - contains all cities and networks
  /// </summary>
  public class GameWorld
  {
    private Dictionary<string, City> cities;
    private Dictionary<string, VirtualNetwork> networks;

    public GameWorld()
    {
      cities = new Dictionary<string, City>();
      networks = new Dictionary<string, VirtualNetwork>();
    }

    public void RegisterCity(City city)
    {
      cities[city.CityId] = city;

      // Register all city networks
      foreach (var network in city.GetAllNetworks())
      {
        RegisterNetwork(network);
      }
    }

    // Get all cities
    public List<City> GetAllCities()
    {
      return new List<City>(cities.Values);
    }

    public void RegisterNetwork(VirtualNetwork network)
    {
      networks[network.NetworkId] = network;

      // Register all devices on network
      foreach (var device in network.GetAllDevices())
      {
        RegisterDevice(device);
      }
    }

    public void RegisterDevice(Device device)
    {
        // Delegate to DeviceRegistry instead
        var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
        registry.RegisterDevice(device);
    }
    
    public Device FindDevice(string identifier)
    {
        var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
        
        // Try hostname, then IP
        var device = registry.GetDeviceByHostname(identifier);
        if (device == null)
            device = registry.GetDeviceByIP(identifier);
        
        return device;
    }

    public VirtualNetwork GetDeviceNetwork(Device device)
    {
      return networks.TryGetValue(device.NetworkId, out var net) ? net : null;
    }
  }
}
