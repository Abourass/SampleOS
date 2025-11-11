using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SampleOS.Core.Apps;
using SampleOS.Core.Devices;
using SampleOS.Core.UI;

namespace SampleOS.Core.Services
{
    /// <summary>
    /// Manages interactive application instances and their UI views.
    /// Handles launching apps, creating UI prefabs, and lifecycle management.
    /// </summary>
    public class ApplicationContainerService : IApplicationContainerService
    {
        // Registry for mapping apps to prefabs
        private readonly AppRegistry appRegistry;
        
        // Running apps and their views
        private readonly Dictionary<string, IInteractiveApp> runningApps;
        private readonly Dictionary<string, IAppView> appViews;
        
        // UI container for instantiated app UIs
        private Transform uiContainer;
        
        // Flag for service initialization
        private bool isInitialized;

        public ApplicationContainerService()
        {
            appRegistry = new AppRegistry();
            runningApps = new Dictionary<string, IInteractiveApp>();
            appViews = new Dictionary<string, IAppView>();
        }

        #region IGameService Implementation

        public void Initialize()
        {
            if (isInitialized)
            {
                Debug.LogWarning("[ApplicationContainerService] Already initialized");
                return;
            }

            // Find or create Canvas for UI rendering
            Canvas canvas = GameObject.FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[ApplicationContainerService] No Canvas found in scene! UI elements require a Canvas to render. Please add a Canvas to your scene.");
                return;
            }

            // Find or create the UI container under Canvas
            GameObject containerObj = GameObject.Find("AppUIContainer");
            if (containerObj == null)
            {
                containerObj = new GameObject("AppUIContainer");
                containerObj.transform.SetParent(canvas.transform, false);
                GameObject.DontDestroyOnLoad(containerObj);

                // Add RectTransform for proper UI parenting
                RectTransform rectTransform = containerObj.AddComponent<RectTransform>();
                rectTransform.anchorMin = Vector2.zero;
                rectTransform.anchorMax = Vector2.one;
                rectTransform.sizeDelta = Vector2.zero;
                rectTransform.anchoredPosition = Vector2.zero;
            }
            else if (containerObj.transform.parent != canvas.transform)
            {
                // If container exists but not under canvas, reparent it
                containerObj.transform.SetParent(canvas.transform, false);
            }

            uiContainer = containerObj.transform;

            isInitialized = true;
            Debug.Log("[ApplicationContainerService] Initialized with Canvas");
        }

        public void Shutdown()
        {
            // Close all running apps
            var appIds = runningApps.Keys.ToList();
            foreach (var appId in appIds)
            {
                CloseApp(appId);
            }
            
            runningApps.Clear();
            appViews.Clear();
            
            isInitialized = false;
            Debug.Log("[ApplicationContainerService] Shutdown complete");
        }

        #endregion

        #region IApplicationContainerService Implementation

        public IInteractiveApp LaunchApp(AppCategory category, Device device)
        {
            if (!isInitialized)
            {
                Debug.LogError("[ApplicationContainerService] Cannot launch app - service not initialized");
                return null;
            }

            if (device == null)
            {
                Debug.LogError("[ApplicationContainerService] Cannot launch app - device is null");
                return null;
            }

            // Create the app instance
            IInteractiveApp app = CreateAppInstance(category);
            if (app == null)
            {
                Debug.LogError($"[ApplicationContainerService] Failed to create app instance for category: {category}");
                return null;
            }

            return LaunchAppInstance(app, device);
        }

        public IInteractiveApp LaunchAppById(string appId, Device device)
        {
            if (!isInitialized)
            {
                Debug.LogError("[ApplicationContainerService] Cannot launch app - service not initialized");
                return null;
            }

            if (device == null)
            {
                Debug.LogError("[ApplicationContainerService] Cannot launch app - device is null");
                return null;
            }

            // Create the app instance by ID
            IInteractiveApp app = CreateAppInstanceById(appId);
            if (app == null)
            {
                Debug.LogError($"[ApplicationContainerService] Failed to create app instance for ID: {appId}");
                return null;
            }

            return LaunchAppInstance(app, device);
        }

        private IInteractiveApp LaunchAppInstance(IInteractiveApp app, Device device)
        {
            // Check if app can run on this device
            if (!app.CanRunOnDevice(device))
            {
                Debug.LogWarning($"[ApplicationContainerService] App {app.AppId} cannot run on device {device.Hostname}");
                return null;
            }

            // Initialize the app with the device
            app.Initialize(device);

            // Load and instantiate the UI prefab
            string osName = device.OS?.Name ?? "Linux";
            string prefabPath = appRegistry.GetPrefabPath(app.AppId, osName);
            
            if (string.IsNullOrEmpty(prefabPath))
            {
                Debug.LogError($"[ApplicationContainerService] No prefab path found for {app.AppId} on {osName}");
                app.Shutdown();
                return null;
            }

            GameObject uiPrefab = Resources.Load<GameObject>(prefabPath);
            if (uiPrefab == null)
            {
                Debug.LogError($"[ApplicationContainerService] Failed to load prefab at path: {prefabPath}");
                app.Shutdown();
                return null;
            }

            GameObject uiInstance = UnityEngine.Object.Instantiate(uiPrefab, uiContainer);
            uiInstance.name = $"{app.AppId}_{app.InstanceId}";

            // Get the view component
            IAppView view = uiInstance.GetComponent<IAppView>();
            if (view == null)
            {
                Debug.LogError($"[ApplicationContainerService] Prefab at {prefabPath} does not have IAppView component");
                UnityEngine.Object.Destroy(uiInstance);
                app.Shutdown();
                return null;
            }

            // Bind view to app
            view.BindToApp(app);

            // Track instances
            runningApps[app.InstanceId] = app;
            appViews[app.InstanceId] = view;

            // Register app with device
            if (device is PlayerDevice playerDevice)
            {
                playerDevice.RegisterApp(app);
            }

            // Trigger app opened lifecycle
            app.OnAppOpened();
            view.Show();

            Debug.Log($"[ApplicationContainerService] Launched {app.DisplayName} (Instance: {app.InstanceId}) on {device.Hostname}");
            
            return app;
        }

        public void CloseApp(string instanceId)
        {
            if (!runningApps.TryGetValue(instanceId, out var app))
            {
                Debug.LogWarning($"[ApplicationContainerService] Cannot close app - instance not found: {instanceId}");
                return;
            }

            Debug.Log($"[ApplicationContainerService] Closing {app.DisplayName} (Instance: {instanceId})");

            // Trigger app closed lifecycle
            app.OnAppClosed();
            
            // Cleanup view
            if (appViews.TryGetValue(instanceId, out var view))
            {
                view.UnbindFromApp();
                
                if (view is MonoBehaviour viewMono)
                {
                    UnityEngine.Object.Destroy(viewMono.gameObject);
                }
                
                appViews.Remove(instanceId);
            }

            // Shutdown app
            app.Shutdown();
            runningApps.Remove(instanceId);
        }

        public IInteractiveApp GetRunningApp(string instanceId)
        {
            runningApps.TryGetValue(instanceId, out var app);
            return app;
        }

        public List<IInteractiveApp> GetRunningAppsOnDevice(Device device)
        {
            if (device == null) return new List<IInteractiveApp>();

            return runningApps.Values
                .Where(app => app.HostDevice.DeviceId == device.DeviceId)
                .ToList();
        }

        public List<IInteractiveApp> GetAllRunningApps()
        {
            return runningApps.Values.ToList();
        }

        public void CloseAllAppsOnDevice(Device device)
        {
            if (device == null) return;

            var appsToClose = GetRunningAppsOnDevice(device);
            foreach (var app in appsToClose)
            {
                CloseApp(app.InstanceId);
            }
        }

        public bool IsAppRunningOnDevice(string appId, Device device)
        {
            if (device == null) return false;

            return runningApps.Values.Any(app =>
                app.AppId == appId &&
                app.HostDevice.DeviceId == device.DeviceId);
        }

        public void UpdateApps(float deltaTime)
        {
            foreach (var app in runningApps.Values.ToList())
            {
                try
                {
                    app.Update(deltaTime);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ApplicationContainerService] Error updating app {app.InstanceId}: {ex.Message}");
                }
            }
        }

        #endregion

        #region App Instance Creation

        private IInteractiveApp CreateAppInstance(AppCategory category)
        {
            return category switch
            {
                AppCategory.Terminal => new TerminalApp(),
                AppCategory.Email => throw new NotImplementedException("Email app not yet implemented"),
                AppCategory.WebBrowser => throw new NotImplementedException("Web browser app not yet implemented"),
                AppCategory.FileManager => throw new NotImplementedException("File manager app not yet implemented"),
                AppCategory.IRC => throw new NotImplementedException("IRC app not yet implemented"),
                _ => throw new ArgumentException($"Unknown app category: {category}")
            };
        }

        private IInteractiveApp CreateAppInstanceById(string appId)
        {
            return appId.ToLower() switch
            {
                "terminal" => new TerminalApp(),
                "email" => throw new NotImplementedException("Email app not yet implemented"),
                "browser" => throw new NotImplementedException("Web browser app not yet implemented"),
                "filemanager" => throw new NotImplementedException("File manager app not yet implemented"),
                "irc" => throw new NotImplementedException("IRC app not yet implemented"),
                _ => throw new ArgumentException($"Unknown app ID: {appId}")
            };
        }

        #endregion

        #region Public Utility Methods

        /// <summary>
        /// Register a custom app prefab mapping (for mods or custom apps)
        /// </summary>
        public void RegisterCustomAppPrefab(string appId, string osName, string prefabPath)
        {
            appRegistry.RegisterAppPrefab(appId, osName, prefabPath);
        }

        /// <summary>
        /// Get the app registry (useful for debugging or editor tools)
        /// </summary>
        public AppRegistry GetAppRegistry()
        {
            return appRegistry;
        }

        #endregion
    }
}
