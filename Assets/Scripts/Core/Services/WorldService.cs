using System;
using System.Collections.Generic;
using SampleOS.Core.World;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking;
using SampleOS.Core.Networking.Access;
using UnityEngine;

namespace SampleOS.Core.Services
{
  public interface IWorldService
  {
    // Initialization
    void Initialize(string startingCityId);
    void Update(float deltaTime);

    // World queries
    GameWorld GetGameWorld();
    City GetCurrentCity();
    List<VirtualNetwork> GetAllNetworks();
    PlayerDevice GetPlayerDevice();

    // Physical location queries
    List<Device> GetDevicesAtLocation(PhysicalLocation location);
    PhysicalLocation GetPlayerLocation();
    void SetPlayerLocation(PhysicalLocation location);

    // Events
    event Action<PhysicalLocation> OnPlayerLocationChanged;
    event Action<float> OnTimeProgressed;

    // State management
    void UpdateDeviceOwnership(Device device);
    object GetSaveData();
    void LoadFromSave(object saveData);
  }

  public class WorldService : IWorldService
  {
    private GameWorld gameWorld;
    private City currentCity;
    private PlayerDevice playerDevice;
    private PhysicalLocation playerLocation;

    public event Action<PhysicalLocation> OnPlayerLocationChanged;
    public event Action<float> OnTimeProgressed;

    public void Initialize(string startingCityId)
    {
      gameWorld = new GameWorld();

      // Create starting city
      currentCity = new City(startingCityId, "Metropolis");
      gameWorld.RegisterCity(currentCity);

      // Create player's home network
      var homeNetworkMetadata = new NetworkMetadata
      {
          Name = "Home Network",
          Description = "Player's home network",
          Organization = "Personal",
          Type = NetworkType.Residential,
          IPRange = "192.168.1.0/24"
      };

      var homeNetworkSecurity = new NetworkSecurityProfile
      {
          DefaultSecurityLevel = SecurityLevel.Low,
          RequiresVPN = false,
          HasFirewall = true,
          AllowsGuestAccess = true
      };

      var homeNetwork = new VirtualNetwork(
          id: "home_network",
          metadata: homeNetworkMetadata,
          security: homeNetworkSecurity
      );
      currentCity.AddNetwork(homeNetwork);

      // Create player's device
      playerDevice = DeviceFactory.CreatePlayerDevice(
          "player_laptop",
          "localhost",
          PlayerDevice.DeviceForm.Laptop
      );

      gameWorld.RegisterDevice(playerDevice);
      homeNetwork.AddDevice(playerDevice);

      // Set initial location
      playerLocation = new PhysicalLocation
      {
        LocationId = "player_apartment",
        LocationName = "Player's Apartment",
        Type = PhysicalLocation.LocationType.Apartment,
        WorldPosition = Vector3.zero
      };

      Debug.Log($"World initialized with city: {currentCity.CityName}");
    }

    public void Update(float deltaTime)
    {
      // Update time progression
      OnTimeProgressed?.Invoke(deltaTime);

      // Trigger event for other systems
      GameEvents.Instance.Trigger(GameEventType.TimeProgressed, deltaTime);
    }

    public GameWorld GetGameWorld() => gameWorld;
    public City GetCurrentCity() => currentCity;
    public PlayerDevice GetPlayerDevice() => playerDevice;

    public List<VirtualNetwork> GetAllNetworks()
    {
      var networks = new List<VirtualNetwork>();
      foreach (var city in gameWorld.GetAllCities())
      {
        networks.Add(city.CurrentNetwork);
      }
      return networks;
    }

    public List<Device> GetDevicesAtLocation(PhysicalLocation location)
    {
      var registry = ServiceLocator.Instance.Get<IDeviceRegistry>();
      return registry.GetDevicesAtLocation(location);
    }

    public void UpdateDeviceOwnership(Device device)
    {
      Debug.Log($"Device {device.Hostname} is now compromised");

      device.IsCompromised = true;
      // Trigger event
      GameEvents.Instance.Trigger(GameEventType.DeviceCompromised, device);
    }

    public void SetPlayerLocation(PhysicalLocation location)
    {
      playerLocation = location;

      // Trigger events
      OnPlayerLocationChanged?.Invoke(location);
      GameEvents.Instance.Trigger(GameEventType.PlayerLocationChanged, location);
    }

    public PhysicalLocation GetPlayerLocation() => playerLocation;

    public object GetSaveData()
    {
      return new WorldSaveData
      {
        currentCityId = currentCity.CityId,
        playerLocation = playerLocation,
      };
    }

    public void LoadFromSave(object saveData)
    {
      if (saveData is WorldSaveData data)
      {
        // Restore world state
        playerLocation = data.playerLocation;
        // Load cities, networks, etc.
      }
    }

    private class WorldSaveData
    {
      public string currentCityId;
      public PhysicalLocation playerLocation;
    }
  }
}
