using UnityEngine;
using SampleOS.Core.Apps;
using SampleOS.Core.Services;

namespace SampleOS.Core.Testing
{
    /// <summary>
    /// Simple test script to launch a terminal app instance.
    /// Add this to any GameObject in a scene to test the terminal.
    /// Press 'T' to launch a terminal on the player's current device.
    /// </summary>
    public class TerminalLauncher : MonoBehaviour
    {
        [Header("Settings")]
        [Tooltip("Key to press to launch the terminal")]
        public KeyCode launchKey = KeyCode.T;

        [Tooltip("Launch terminal automatically on start")]
        public bool launchOnStart = false;

        private IApplicationContainerService appService;
        private IPlayerStateService playerState;
        private IInteractiveApp currentTerminal;

        private void Start()
        {
            // Get service references
            appService = ServiceLocator.Instance.Get<IApplicationContainerService>();
            playerState = ServiceLocator.Instance.Get<IPlayerStateService>();

            if (appService == null)
            {
                Debug.LogError("[TerminalLauncher] ApplicationContainerService not found! Make sure GameStateManager is in the scene.");
                enabled = false;
                return;
            }

            if (playerState == null)
            {
                Debug.LogError("[TerminalLauncher] PlayerStateService not found! Make sure GameStateManager is in the scene.");
                enabled = false;
                return;
            }

            if (launchOnStart)
            {
                LaunchTerminal();
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(launchKey))
            {
                if (currentTerminal == null)
                {
                    LaunchTerminal();
                }
                else
                {
                    CloseTerminal();
                }
            }
        }

        /// <summary>
        /// Launch a terminal on the player's current device
        /// </summary>
        public void LaunchTerminal()
        {
            var device = playerState.CurrentDevice;

            if (device == null)
            {
                Debug.LogError("[TerminalLauncher] No current device! Cannot launch terminal.");
                return;
            }

            Debug.Log($"[TerminalLauncher] Launching terminal on device: {device.Hostname}");

            currentTerminal = appService.LaunchAppById("terminal", device);

            if (currentTerminal == null)
            {
                Debug.LogError("[TerminalLauncher] Failed to launch terminal!");
                return;
            }

            Debug.Log($"[TerminalLauncher] Terminal launched successfully! Press '{launchKey}' again to close.");
        }

        /// <summary>
        /// Close the current terminal
        /// </summary>
        public void CloseTerminal()
        {
            if (currentTerminal == null)
            {
                Debug.LogWarning("[TerminalLauncher] No terminal to close.");
                return;
            }

            Debug.Log("[TerminalLauncher] Closing terminal...");

            appService.CloseApp(currentTerminal.InstanceId);
            currentTerminal = null;

            Debug.Log($"[TerminalLauncher] Terminal closed. Press '{launchKey}' to launch again.");
        }

        private void OnDestroy()
        {
            // Clean up if the launcher is destroyed
            if (currentTerminal != null)
            {
                CloseTerminal();
            }
        }
    }
}
