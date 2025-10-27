using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using SampleOS.Core.World;
using UnityEngine;

namespace SampleOS.Core.Services
{
  public interface IDeviceRegistry
  {
    // Registration
    void RegisterDevice(Device device);
    void UnregisterDevice(string deviceId);

    // Queries by ID
    Device GetDevice(string deviceId);
    Device GetDeviceByHostname(string hostname);
    Device GetDeviceByIP(string ipAddress);

    // Queries by location
    List<Device> GetDevicesInCity(string cityId);
    List<Device> GetDevicesAtLocation(PhysicalLocation location);
    List<Device> GetDevicesNearLocation(PhysicalLocation location, float radiusMeters);

    // Queries by network
    List<Device> GetDevicesOnNetwork(string networkId);

    // Queries by properties
    List<Device> GetDevicesByType(DeviceType type);
    List<Device> GetCompromisedDevices();
    List<Device> GetDevicesWithBackdoors();

    // All devices
    List<Device> GetAllDevices();

    // Events
    event Action<Device> OnDeviceRegistered;
    event Action<Device> OnDeviceLocationChanged;
  }

  public class DeviceRegistry : IDeviceRegistry
  {
    // Primary registry
    private Dictionary<string, Device> devicesById;

    // Indexes for fast queries
    private Dictionary<string, Device> devicesByHostname;
    private Dictionary<string, Device> devicesByIP;
    private Dictionary<string, List<Device>> devicesByCity;
    private Dictionary<string, List<Device>> devicesByNetwork;

    public event Action<Device> OnDeviceRegistered;
    public event Action<Device> OnDeviceLocationChanged;

    public void Initialize()
    {
      devicesById = new Dictionary<string, Device>();
      devicesByHostname = new Dictionary<string, Device>();
      devicesByIP = new Dictionary<string, Device>();
      devicesByCity = new Dictionary<string, List<Device>>();
      devicesByNetwork = new Dictionary<string, List<Device>>();

      // Subscribe to device events
      GameEvents.Instance.Subscribe(GameEventType.DeviceLocationChanged, OnDeviceMoved);

      Debug.Log("[DeviceRegistry] Initialized");
    }

    public void RegisterDevice(Device device)
    {
      if (device == null || string.IsNullOrEmpty(device.DeviceId))
        return;

      if (devicesById.ContainsKey(device.DeviceId))
      {
        Debug.LogWarning($"[DeviceRegistry] Device {device.DeviceId} already registered");
        return;
      }

      // Primary registry
      devicesById[device.DeviceId] = device;

      // Build indexes
      if (!string.IsNullOrEmpty(device.Hostname))
        devicesByHostname[device.Hostname] = device;

      if (!string.IsNullOrEmpty(device.IPAddress))
        devicesByIP[device.IPAddress] = device;

      // Location index
      if (device.Location != null)
        AddToLocationIndex(device);

      // Network indexes
      foreach (var networkId in device.NetworkMemberships)
        AddToNetworkIndex(device, networkId);

      OnDeviceRegistered?.Invoke(device);
      Debug.Log($"[DeviceRegistry] Registered: {device.Hostname}");
    }

    private void AddToLocationIndex(Device device)
    {
      if (device.Location == null) return;

      string cityId = device.Location.CityId;
      if (string.IsNullOrEmpty(cityId))
      {
        Debug.LogWarning($"[DeviceRegistry] Device {device.Hostname} has location but no CityId");
        return;
      }

      if (!devicesByCity.ContainsKey(cityId))
        devicesByCity[cityId] = new List<Device>();

      if (!devicesByCity[cityId].Contains(device))
        devicesByCity[cityId].Add(device);
    }

    private void AddToNetworkIndex(Device device, string networkId)
    {
      if (!devicesByNetwork.ContainsKey(networkId))
        devicesByNetwork[networkId] = new List<Device>();

      if (!devicesByNetwork[networkId].Contains(device))
        devicesByNetwork[networkId].Add(device);
    }

    private void OnDeviceMoved(object data)
    {
      if (data is Device device)
      {
        // Rebuild location indexes
        RebuildLocationIndex(device);
        OnDeviceLocationChanged?.Invoke(device);
      }
    }

    private void RebuildLocationIndex(Device device)
    {
      // Remove from old location indexes
      foreach (var cityDevices in devicesByCity.Values)
        cityDevices.Remove(device);

      // Re-add to new location
      if (device.Location != null)
        AddToLocationIndex(device);
    }

    // Query implementations
    public Device GetDevice(string deviceId)
    {
      return devicesById.TryGetValue(deviceId, out var device) ? device : null;
    }

    public Device GetDeviceByHostname(string hostname)
    {
      return devicesByHostname.TryGetValue(hostname, out var device) ? device : null;
    }

    public Device GetDeviceByIP(string ipAddress)
    {
      return devicesByIP.TryGetValue(ipAddress, out var device) ? device : null;
    }

    public List<Device> GetDevicesOnNetwork(string networkId)
    {
      return devicesByNetwork.TryGetValue(networkId, out var devices)
        ? new List<Device>(devices)
        : new List<Device>();
    }

    public List<Device> GetDevicesAtLocation(PhysicalLocation location)
    {
      return devicesById.Values
        .Where(d => d.Location != null && d.Location.LocationId == location.LocationId)
        .ToList();
    }

    public List<Device> GetDevicesNearLocation(PhysicalLocation location, float radiusMeters)
    {
      // Spatial query - add distance calculation to PhysicalLocation
      return devicesById.Values
        .Where(d => d.Location != null &&
                    Vector3.Distance(d.Location.WorldPosition, location.WorldPosition) <= radiusMeters)
        .ToList();
    }

    public List<Device> GetCompromisedDevices()
    {
      return devicesById.Values.Where(d => d.IsCompromised).ToList();
    }

    public List<Device> GetDevicesWithBackdoors()
    {
      return devicesById.Values
        .Where(d => d.BackdoorConnections != null && d.BackdoorConnections.Count > 0)
        .ToList();
    }

    public List<Device> GetDevicesByType(DeviceType type)
    {
      return devicesById.Values.Where(d => d.DeviceType == type).ToList();
    }

    public List<Device> GetDevicesInCity(string cityId)
    {
      return devicesByCity.TryGetValue(cityId, out var devices)
        ? new List<Device>(devices)
        : new List<Device>();
    }

    public List<Device> GetAllDevices()
    {
      return devicesById.Values.ToList();
    }

    public void UnregisterDevice(string deviceId)
    {
      if (devicesById.TryGetValue(deviceId, out var device))
      {
        devicesById.Remove(deviceId);
        devicesByHostname.Remove(device.Hostname);
        devicesByIP.Remove(device.IPAddress);

        foreach (var cityDevices in devicesByCity.Values)
          cityDevices.Remove(device);

        foreach (var networkDevices in devicesByNetwork.Values)
          networkDevices.Remove(device);
      }
    }
  }
}
