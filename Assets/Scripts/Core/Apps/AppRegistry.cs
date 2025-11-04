using System.Collections.Generic;
using UnityEngine;

namespace SampleOS.Core.Apps
{
    /// <summary>
    /// Registry that maps app IDs to their OS-specific UI prefab paths.
    /// This allows different operating systems to have different visual styles
    /// for the same app (e.g., Linux terminal vs Windows CMD).
    /// </summary>
    public class AppRegistry
    {
        // Map: AppId -> (OSName -> PrefabPath)
        private readonly Dictionary<string, Dictionary<string, string>> appUIPrefabs;
        
        // Fallback OS if specific OS prefab not found
        private const string FALLBACK_OS = "Linux";

        public AppRegistry()
        {
            appUIPrefabs = new Dictionary<string, Dictionary<string, string>>
            {
                // Terminal app prefabs
                ["terminal"] = new Dictionary<string, string>
                {
                    ["Linux"] = "UI/Apps/Linux/TerminalUI",
                    ["Windows"] = "UI/Apps/Windows/TerminalUI",
                    ["MacOS"] = "UI/Apps/MacOS/TerminalUI",
                    ["Mobile"] = "UI/Apps/Mobile/TerminalUI"
                },
                
                // Email app prefabs (for future implementation)
                ["email"] = new Dictionary<string, string>
                {
                    ["Linux"] = "UI/Apps/Linux/EmailUI",
                    ["Windows"] = "UI/Apps/Windows/EmailUI",
                    ["MacOS"] = "UI/Apps/MacOS/EmailUI",
                    ["Mobile"] = "UI/Apps/Mobile/EmailUI"
                },
                
                // Web browser prefabs (for future implementation)
                ["browser"] = new Dictionary<string, string>
                {
                    ["Linux"] = "UI/Apps/Linux/BrowserUI",
                    ["Windows"] = "UI/Apps/Windows/BrowserUI",
                    ["MacOS"] = "UI/Apps/MacOS/BrowserUI",
                    ["Mobile"] = "UI/Apps/Mobile/BrowserUI"
                },
                
                // File manager prefabs (for future implementation)
                ["filemanager"] = new Dictionary<string, string>
                {
                    ["Linux"] = "UI/Apps/Linux/FileManagerUI",
                    ["Windows"] = "UI/Apps/Windows/FileManagerUI",
                    ["MacOS"] = "UI/Apps/MacOS/FileManagerUI",
                    ["Mobile"] = "UI/Apps/Mobile/FileManagerUI"
                },
                
                // IRC client prefabs (for future implementation)
                ["irc"] = new Dictionary<string, string>
                {
                    ["Linux"] = "UI/Apps/Linux/IRCUI",
                    ["Windows"] = "UI/Apps/Windows/IRCUI",
                    ["MacOS"] = "UI/Apps/MacOS/IRCUI",
                    ["Mobile"] = "UI/Apps/Mobile/IRCUI"
                }
            };
        }

        /// <summary>
        /// Get the prefab path for a specific app and OS
        /// </summary>
        /// <param name="appId">The app ID (e.g., "terminal", "email")</param>
        /// <param name="osName">The OS name (e.g., "Linux", "Windows")</param>
        /// <returns>Path to the prefab in Resources folder</returns>
        public string GetPrefabPath(string appId, string osName)
        {
            // Check if app exists
            if (!appUIPrefabs.TryGetValue(appId, out var osPrefabs))
            {
                Debug.LogWarning($"[AppRegistry] No prefab mapping found for app: {appId}");
                return null;
            }

            // Try to get OS-specific prefab
            if (osPrefabs.TryGetValue(osName, out var prefabPath))
            {
                return prefabPath;
            }

            // Fall back to default OS
            if (osPrefabs.TryGetValue(FALLBACK_OS, out var fallbackPath))
            {
                Debug.LogWarning($"[AppRegistry] No prefab found for {appId} on {osName}, using {FALLBACK_OS} version");
                return fallbackPath;
            }

            Debug.LogError($"[AppRegistry] No prefab found for {appId} (tried {osName} and fallback {FALLBACK_OS})");
            return null;
        }

        /// <summary>
        /// Register a new app prefab mapping
        /// </summary>
        /// <param name="appId">The app ID</param>
        /// <param name="osName">The OS name</param>
        /// <param name="prefabPath">Path to the prefab in Resources</param>
        public void RegisterAppPrefab(string appId, string osName, string prefabPath)
        {
            if (!appUIPrefabs.ContainsKey(appId))
            {
                appUIPrefabs[appId] = new Dictionary<string, string>();
            }

            appUIPrefabs[appId][osName] = prefabPath;
            Debug.Log($"[AppRegistry] Registered prefab for {appId} on {osName}: {prefabPath}");
        }

        /// <summary>
        /// Check if an app has a prefab registered
        /// </summary>
        /// <param name="appId">The app ID</param>
        /// <returns>True if the app has at least one prefab registered</returns>
        public bool HasAppPrefab(string appId)
        {
            return appUIPrefabs.ContainsKey(appId);
        }

        /// <summary>
        /// Get all registered OS names for a specific app
        /// </summary>
        /// <param name="appId">The app ID</param>
        /// <returns>List of OS names that have prefabs for this app</returns>
        public List<string> GetRegisteredOSesForApp(string appId)
        {
            if (appUIPrefabs.TryGetValue(appId, out var osPrefabs))
            {
                return new List<string>(osPrefabs.Keys);
            }

            return new List<string>();
        }

        /// <summary>
        /// Get all registered app IDs
        /// </summary>
        /// <returns>List of all registered app IDs</returns>
        public List<string> GetAllAppIds()
        {
            return new List<string>(appUIPrefabs.Keys);
        }
    }
}
