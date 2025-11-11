using System;
using UnityEngine;
using SampleOS.Core.Networking;
using SampleOS.Core.FileSystem;
using SampleOS.Core.SoftwarePackages;

namespace SampleOS.Core.Devices
{
  /// <summary>
  /// Factory for creating devices from definitions
  /// </summary>
  public static class DeviceFactory
  {
    private static DeviceTypeDatabase deviceTypeDb = new DeviceTypeDatabase();
    private static SoftwareDatabase softwareDb = new SoftwareDatabase();

    /// <summary>
    /// Creates a RemoteDevice from a definition
    /// </summary>
    public static RemoteDevice CreateRemoteDevice(DeviceDefinition definition)
    {
      // Get device type configuration
      var deviceType = deviceTypeDb.GetDeviceType(definition.DeviceTypeId);

      // Determine remote access type based on device type
      var accessType = DetermineAccessType(deviceType);

      // Create the device
      var device = new RemoteDevice(
          definition.DeviceId,
          definition.Hostname,
          definition.IPAddress,
          deviceType,
          accessType
      );

      // Set security level
      device.SecurityLevel = definition.SecurityLevel;

      // Set physical accessibility
      device.IsPhysicallyAccessible = definition.IsPhysicallyAccessible;

      // Note: Location is not set here because DeviceFactory doesn't have access to WorldService
      // Location should be set by the DeviceRegistryService when the device is registered
      // The LocationId from the definition can be used to look up the PhysicalLocation via WorldService.GetLocation()

      // Add default credentials if provided
      if (!string.IsNullOrEmpty(definition.DefaultUsername))
      {
        device.AddCredential(
            definition.DefaultUsername,
            definition.DefaultPassword ?? GeneratePassword(),
            isDefault: true
        );
      }

      // Generate or add software
      if (definition.SoftwareOverrides != null && definition.SoftwareOverrides.Count > 0)
      {
        // Use specified software
        foreach (var softwareId in definition.SoftwareOverrides)
        {
          var software = softwareDb.GetSoftware(softwareId);
          if (software != null)
            device.InstallSoftware(software);
        }
      }
      else
      {
        // Generate software based on device type
        GenerateSoftwareForDevice(device, deviceType);
      }

      return device;
    }

    /// <summary>
    /// Creates a PlayerDevice
    /// </summary>
    public static PlayerDevice CreatePlayerDevice(
        string id,
        string hostname,
        PlayerDevice.DeviceForm formFactor)
    {
      var deviceTypeId = formFactor == PlayerDevice.DeviceForm.Phone ? "mobile" : "desktop";
      var deviceType = deviceTypeDb.GetDeviceType(deviceTypeId);

      var device = new PlayerDevice(
          id,
          hostname,
          "127.0.0.1",
          deviceType,
          formFactor
      );

      // Player devices have root access
      device.AddCredential("user", "", isDefault: true);

      return device;
    }

    private static RemoteDevice.RemoteAccessType DetermineAccessType(DeviceType deviceType)
    {
      switch (deviceType.Category)
      {
        case DeviceCategory.Server:
          return RemoteDevice.RemoteAccessType.SSHOnly;
        case DeviceCategory.Workstation:
          return RemoteDevice.RemoteAccessType.RDPCapable;
        case DeviceCategory.Router:
        case DeviceCategory.EmbeddedSystem:
          return RemoteDevice.RemoteAccessType.WebInterface;
        case DeviceCategory.IoTDevice:
          return RemoteDevice.RemoteAccessType.APIOnly;
        default:
          return RemoteDevice.RemoteAccessType.SSHOnly;
      }
    }

    private static void GenerateSoftwareForDevice(Device device, DeviceType deviceType)
    {
      var creationDate = DateTime.Now.AddDays(-UnityEngine.Random.Range(30, 730));

      foreach (var softwareCategory in deviceType.SoftwareWeights)
      {
        if (UnityEngine.Random.value <= softwareCategory.Value)
        {
          var software = softwareDb.GenerateRandomSoftware(
              softwareCategory.Key,
              creationDate
          );

          if (software != null)
            device.InstallSoftware(software);
        }
      }
    }

    private static string GeneratePassword()
    {
      // Generate a random password for devices without explicit passwords
      const string chars = "abcdefghijklmnopqrstuvwxyz0123456789";
      var password = "";
      for (int i = 0; i < 12; i++)
        password += chars[UnityEngine.Random.Range(0, chars.Length)];
      return password;
    }
  }
}
