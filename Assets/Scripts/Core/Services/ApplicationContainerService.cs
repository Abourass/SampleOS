using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Apps;
using SampleOS.Core.Devices;
using UnityEngine;

namespace SampleOS.Core.Services
{
  /// <summary>
  /// Manages all running app instances across all devices
  /// Tracks which device/app the player is currently focused on
  /// Replaces HackingSessionService with clearer responsibilities
  /// </summary>
  public interface IApplicationContainerService
  {
    // App lifecycle
    Result<IInteractiveApp> LaunchApp(AppCategory category, Device device);
    void CloseApp(string instanceId);
    void Update(float deltaTime);
    
    // Queries
    IInteractiveApp GetApp(string instanceId);
    List<IInteractiveApp> GetAllRunningApps();
    List<IInteractiveApp> GetAppsOnDevice(Device device);
    List<IInteractiveApp> GetAppsByCategory(AppCategory category);
    
    // Focus management
    IInteractiveApp FocusedApp { get; }
    Device FocusedDevice { get; }
    void SetFocusedApp(IInteractiveApp app);
    
    // Events
    event Action<IInteractiveApp> OnAppLaunched;
    event Action<IInteractiveApp> OnAppClosed;
    event Action<IInteractiveApp> OnAppFocusChanged;
    event Action<Device> OnFocusedDeviceChanged;
    
    // Save/Load
    object GetSaveData();
    void LoadFromSave(object saveData);
  }

  public class ApplicationContainerService : IApplicationContainerService
  {
    // Registry of all running app instances
    private Dictionary<string, IInteractiveApp> runningApps;
    
    // Index: Device -> List of apps running on it
    private Dictionary<Device, List<IInteractiveApp>> appsByDevice;
    
    // Current focus
    private IInteractiveApp focusedApp;
    private Device focusedDevice;
    
    // Events
    public event Action<IInteractiveApp> OnAppLaunched;
    public event Action<IInteractiveApp> OnAppClosed;
    public event Action<IInteractiveApp> OnAppFocusChanged;
    public event Action<Device> OnFocusedDeviceChanged;
    
    public IInteractiveApp FocusedApp => focusedApp;
    public Device FocusedDevice => focusedDevice;
    
    public void Initialize()
    {
      runningApps = new Dictionary<string, IInteractiveApp>();
      appsByDevice = new Dictionary<Device, List<IInteractiveApp>>();
      
      Debug.Log("[ApplicationContainerService] Initialized");
    }
    
    public Result<IInteractiveApp> LaunchApp(AppCategory category, Device device)
    {
      if (device == null)
        return Result<IInteractiveApp>.Failure("Cannot launch app on null device");
      
      // Create app instance based on category
      IInteractiveApp app = CreateAppInstance(category);
      
      if (app == null)
        return Result<IInteractiveApp>.Failure($"Unknown app category: {category}");
      
      // Check if app can run on this device
      if (!app.CanRunOnDevice(device))
        return Result<IInteractiveApp>.Failure($"{app.DisplayName} cannot run on {device.Hostname}");
      
      // Initialize app with device binding
      app.Initialize(device);
      
      // Register app
      runningApps[app.InstanceId] = app;
      
      // Index by device
      if (!appsByDevice.ContainsKey(device))
        appsByDevice[device] = new List<IInteractiveApp>();
      
      appsByDevice[device].Add(app);
      
      // Fire lifecycle hooks
      app.OnAppOpened();
      OnAppLaunched?.Invoke(app);
      
      Debug.Log($"[ApplicationContainerService] Launched {app.DisplayName} on {device.Hostname} (ID: {app.InstanceId})");
      
      return Result<IInteractiveApp>.Success(app);
    }
    
    public void CloseApp(string instanceId)
    {
      if (!runningApps.TryGetValue(instanceId, out var app))
      {
        Debug.LogWarning($"[ApplicationContainerService] Cannot close app {instanceId} - not found");
        return;
      }
      
      // If this was the focused app, clear focus
      if (focusedApp == app)
      {
        focusedApp = null;
        focusedDevice = null;
      }
      
      // Unregister from device index
      if (appsByDevice.ContainsKey(app.HostDevice))
      {
        appsByDevice[app.HostDevice].Remove(app);
        
        if (appsByDevice[app.HostDevice].Count == 0)
          appsByDevice.Remove(app.HostDevice);
      }
      
      // Fire lifecycle hooks
      app.OnAppClosed();
      app.Shutdown();
      
      // Remove from registry
      runningApps.Remove(instanceId);
      
      OnAppClosed?.Invoke(app);
      
      Debug.Log($"[ApplicationContainerService] Closed {app.DisplayName} (ID: {instanceId})");
    }
    
    public void Update(float deltaTime)
    {
      // Update all running apps
      foreach (var app in runningApps.Values)
      {
        app.Update(deltaTime);
      }
    }
    
    public void SetFocusedApp(IInteractiveApp app)
    {
      if (focusedApp == app)
        return;
      
      // Notify old app it lost focus
      if (focusedApp != null)
        focusedApp.OnFocusLost();
      
      // Update focus
      var previousDevice = focusedDevice;
      focusedApp = app;
      focusedDevice = app?.HostDevice;
      
      // Notify new app it gained focus
      if (focusedApp != null)
        focusedApp.OnFocusGained();
      
      // Fire events
      OnAppFocusChanged?.Invoke(app);
      
      if (focusedDevice != previousDevice)
        OnFocusedDeviceChanged?.Invoke(focusedDevice);
    }
    
    // Queries
    public IInteractiveApp GetApp(string instanceId)
    {
      return runningApps.TryGetValue(instanceId, out var app) ? app : null;
    }
    
    public List<IInteractiveApp> GetAllRunningApps()
    {
      return runningApps.Values.ToList();
    }
    
    public List<IInteractiveApp> GetAppsOnDevice(Device device)
    {
      return appsByDevice.ContainsKey(device) 
        ? appsByDevice[device] 
        : new List<IInteractiveApp>();
    }
    
    public List<IInteractiveApp> GetAppsByCategory(AppCategory category)
    {
      return runningApps.Values
        .Where(app => app.Category == category)
        .ToList();
    }
    
    // App factory
    private IInteractiveApp CreateAppInstance(AppCategory category)
    {
      return category switch
      {
        AppCategory.Terminal => new TerminalApp(),
        // AppCategory.Email => new EmailApp(),
        // AppCategory.WebBrowser => new BrowserApp(),
        _ => null
      };
    }
    
    // Save/Load
    public object GetSaveData()
    {
      var appStates = new List<AppSaveData>();
      
      foreach (var app in runningApps.Values)
      {
        appStates.Add(new AppSaveData
        {
          instanceId = app.InstanceId,
          appId = app.AppId,
          category = app.Category,
          deviceHostname = app.HostDevice.Hostname,
          state = app.SerializeState()
        });
      }
      
      return new ApplicationContainerSaveData
      {
        apps = appStates,
        focusedAppInstanceId = focusedApp?.InstanceId
      };
    }
    
    public void LoadFromSave(object saveData)
    {
      if (saveData is ApplicationContainerSaveData data)
      {
        // TODO: Restore app instances
        // Need access to DeviceRegistry to lookup devices by hostname
        Debug.Log($"[ApplicationContainerService] Loaded {data.apps.Count} apps");
      }
    }
    
    [Serializable]
    private class ApplicationContainerSaveData
    {
      public List<AppSaveData> apps;
      public string focusedAppInstanceId;
    }
    
    [Serializable]
    private class AppSaveData
    {
      public string instanceId;
      public string appId;
      public AppCategory category;
      public string deviceHostname;
      public object state;
    }
  }
}
