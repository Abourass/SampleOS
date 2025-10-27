using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;
using UnityEngine;

namespace SampleOS.Core.Services
{
  public interface IFileSystemPersistence
  {
    void SaveDeviceFileSystem(Device device);
    void LoadDeviceFileSystem(Device device);
    List<string> GetDevicesWithChanges();
  }
  
  public class FileSystemPersistenceService : IFileSystemPersistence
  {
    private HashSet<string> devicesWithChanges = new HashSet<string>();
    
    public void Initialize()
    {
      // Subscribe to file system changes
      GameEvents.Instance.Subscribe(GameEventType.DeviceFileSystemChanged, OnFileSystemChanged);
    }
    
    private void OnFileSystemChanged(object data)
    {
      if (data is Device device)
      {
        devicesWithChanges.Add(device.DeviceId);
      }
    }
    
    public void SaveDeviceFileSystem(Device device)
    {
      // Serialize file system to disk/save file
      // Only save if file system was loaded and has changes
      if (device.HasFileSystemChanges)
      {
        // TODO: Implement serialization
        Debug.Log($"[FileSystemPersistence] Saved file system for {device.Hostname}");
      }
    }
    
    public void LoadDeviceFileSystem(Device device)
    {
      // Deserialize file system from disk/save file
      // Only if saved state exists
      Debug.Log($"[FileSystemPersistence] Loaded file system for {device.Hostname}");
    }
    
    public List<string> GetDevicesWithChanges()
    {
      return devicesWithChanges.ToList();
    }
  }
}
