using System;
using System.Collections.Generic;

namespace SampleOS.Core.Services
{
    /// <summary>
    /// Manages window state, z-order, and focus for application windows.
    /// </summary>
    public interface IWindowManager
    {
        /// <summary>
        /// Brings a window to the front and gives it focus.
        /// </summary>
        void BringToFront(string instanceId);

        /// <summary>
        /// Sends a window to the back of the z-order.
        /// </summary>
        void SendToBack(string instanceId);

        /// <summary>
        /// Gets the instance ID of the currently focused window.
        /// </summary>
        string GetFocusedWindowId();

        /// <summary>
        /// Minimizes a window (hides it but keeps it in memory).
        /// </summary>
        void MinimizeWindow(string instanceId);

        /// <summary>
        /// Maximizes a window to fill the container.
        /// </summary>
        void MaximizeWindow(string instanceId);

        /// <summary>
        /// Restores a window from minimized or maximized state.
        /// </summary>
        void RestoreWindow(string instanceId);

        /// <summary>
        /// Gets the z-order index of a window (higher = more in front).
        /// </summary>
        int GetWindowZOrder(string instanceId);

        /// <summary>
        /// Gets all window instance IDs in z-order (front to back).
        /// </summary>
        List<string> GetWindowsByZOrder();

        /// <summary>
        /// Checks if a window is minimized.
        /// </summary>
        bool IsWindowMinimized(string instanceId);

        /// <summary>
        /// Checks if a window is maximized.
        /// </summary>
        bool IsWindowMaximized(string instanceId);

        /// <summary>
        /// Gets the current state of a window.
        /// </summary>
        WindowState GetWindowState(string instanceId);

        /// <summary>
        /// Registers a window with the manager. Called by ApplicationContainerService.
        /// </summary>
        void RegisterWindow(string instanceId, UI.Window.WindowChrome chrome);

        /// <summary>
        /// Unregisters a window. Called when the window is closed.
        /// </summary>
        void UnregisterWindow(string instanceId);

        // Events
        event Action<string> OnWindowFocused;
        event Action<string> OnWindowUnfocused;
        event Action<string> OnWindowMinimized;
        event Action<string> OnWindowMaximized;
        event Action<string> OnWindowRestored;
    }

    /// <summary>
    /// Represents the current state of a window.
    /// </summary>
    public enum WindowState
    {
        Normal,
        Minimized,
        Maximized
    }
}
