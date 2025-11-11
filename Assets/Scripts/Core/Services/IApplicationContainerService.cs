using SampleOS.Core.Apps;
using SampleOS.Core.Devices;

namespace SampleOS.Core.Services
{
    /// <summary>
    /// Service interface for managing interactive application instances.
    /// Handles launching, closing, and tracking running apps.
    /// </summary>
    public interface IApplicationContainerService
    {
        /// <summary>
        /// Launch an application on a specific device
        /// </summary>
        /// <param name="category">The category of app to launch</param>
        /// <param name="device">The device to run the app on</param>
        /// <returns>The launched app instance, or null if launch failed</returns>
        IInteractiveApp LaunchApp(AppCategory category, Device device);
        
        /// <summary>
        /// Launch an application by app ID on a specific device
        /// </summary>
        /// <param name="appId">The app ID (e.g., "terminal", "email")</param>
        /// <param name="device">The device to run the app on</param>
        /// <returns>The launched app instance, or null if launch failed</returns>
        IInteractiveApp LaunchAppById(string appId, Device device);
        
        /// <summary>
        /// Close an app instance
        /// </summary>
        /// <param name="instanceId">The unique instance ID of the app to close</param>
        void CloseApp(string instanceId);
        
        /// <summary>
        /// Get a running app instance by ID
        /// </summary>
        /// <param name="instanceId">The unique instance ID</param>
        /// <returns>The app instance, or null if not found</returns>
        IInteractiveApp GetRunningApp(string instanceId);
        
        /// <summary>
        /// Get all running apps on a specific device
        /// </summary>
        /// <param name="device">The device</param>
        /// <returns>List of running app instances</returns>
        System.Collections.Generic.List<IInteractiveApp> GetRunningAppsOnDevice(Device device);
        
        /// <summary>
        /// Get all running apps
        /// </summary>
        /// <returns>List of all running app instances</returns>
        System.Collections.Generic.List<IInteractiveApp> GetAllRunningApps();
        
        /// <summary>
        /// Close all apps on a specific device
        /// </summary>
        /// <param name="device">The device</param>
        void CloseAllAppsOnDevice(Device device);
        
        /// <summary>
        /// Check if a specific app is running on a device
        /// </summary>
        /// <param name="appId">The app ID</param>
        /// <param name="device">The device</param>
        /// <returns>True if the app is running on the device</returns>
        bool IsAppRunningOnDevice(string appId, Device device);
        
        /// <summary>
        /// Update all running apps (called each frame)
        /// </summary>
        /// <param name="deltaTime">Time since last update</param>
        void UpdateApps(float deltaTime);
    }
}
