using System.Collections.Generic;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking;

namespace SampleOS.Core.World
{
  /// <summary>
  /// Top-level world manager - contains all cities and networks
  /// </summary>
  public class GameWorld
  {
    private Dictionary<string, City> cities;
    private Dictionary<string, VirtualNetwork> networks;
    private Dictionary<string, Device> allDevices; // Global device registry

    public GameWorld()
    {
      cities = new Dictionary<string, City>();
      networks = new Dictionary<string, VirtualNetwork>();
      allDevices = new Dictionary<string, Device>();
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
      allDevices[device.DeviceId] = device;
    }

    public Device FindDevice(string identifier)
    {
      // Try device ID first
      if (allDevices.TryGetValue(identifier, out var device))
        return device;

      // Try hostname
      foreach (var dev in allDevices.Values)
      {
        if (dev.Hostname == identifier || dev.IPAddress == identifier)
          return dev;
      }

      return null;
    }

    public List<Device> GetDevicesAtLocation(string locationId)
    {
      var result = new List<Device>();
      foreach (var device in allDevices.Values)
      {
        if (device.LocationId == locationId)
          result.Add(device);
      }
      return result;
    }

    public VirtualNetwork GetDeviceNetwork(Device device)
    {
      return networks.TryGetValue(device.NetworkId, out var net) ? net : null;
    }
  }
}
