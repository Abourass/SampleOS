using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using SampleOS.Core.SoftwarePackages;
using UnityEngine;

namespace SampleOS.Core.Player
{
  /// <summary>
  /// Player's inventory of devices and software
  /// </summary>
  public class PlayerInventory
  {
    public List<PlayerDevice> OwnedDevices { get; private set; }
    public List<Software> InstalledSoftware { get; private set; }

    public event Action<PlayerDevice> OnDeviceAcquired;
    public event Action<Software> OnSoftwareInstalled;

    public PlayerInventory()
    {
      OwnedDevices = new List<PlayerDevice>();
      InstalledSoftware = new List<Software>();
    }

    public void AddDevice(PlayerDevice device)
    {
      if (!OwnedDevices.Contains(device))
      {
        OwnedDevices.Add(device);
        OnDeviceAcquired?.Invoke(device);
        Debug.Log($"Device acquired: {device.Hostname}");
      }
    }

    public void AddSoftware(Software software)
    {
      if (!InstalledSoftware.Any(s => s.Name == software.Name))
      {
        InstalledSoftware.Add(software);
        OnSoftwareInstalled?.Invoke(software);
        Debug.Log($"Software installed: {software.Name}");
      }
    }

    public bool HasSoftware(string softwareName)
    {
      return InstalledSoftware.Any(s => s.Name.Equals(softwareName, StringComparison.OrdinalIgnoreCase));
    }

    public PlayerInventorySaveData GetSaveData()
    {
      return new PlayerInventorySaveData
      {
        ownedDeviceIds = OwnedDevices.Select(d => d.DeviceId).ToList(),
        installedSoftware = InstalledSoftware.Select(s => s.Name).ToList()
      };
    }

    public void LoadFromSave(PlayerInventorySaveData data)
    {
      if (data == null) return;

      // Device references will be restored by WorldService
      // Software will be re-instantiated as needed

      Debug.Log($"Inventory loaded: {data.ownedDeviceIds.Count} devices");
    }
  }

  [Serializable]
  public class PlayerInventorySaveData
  {
    public List<string> ownedDeviceIds = new List<string>();
    public List<string> installedSoftware = new List<string>();
  }
}
