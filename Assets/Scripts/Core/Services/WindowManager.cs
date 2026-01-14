using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SampleOS.Core.UI.Window;

namespace SampleOS.Core.Services
{
    /// <summary>
    /// Manages window state, z-order, and focus for application windows.
    /// </summary>
    public class WindowManager : IWindowManager
    {
        private readonly Dictionary<string, WindowInfo> windows = new();
        private readonly List<string> zOrderStack = new(); // First = frontmost
        private string focusedWindowId;

        // Events
        public event Action<string> OnWindowFocused;
        public event Action<string> OnWindowUnfocused;
        public event Action<string> OnWindowMinimized;
        public event Action<string> OnWindowMaximized;
        public event Action<string> OnWindowRestored;

        public void RegisterWindow(string instanceId, WindowChrome chrome)
        {
            if (string.IsNullOrEmpty(instanceId) || chrome == null)
            {
                Debug.LogWarning("[WindowManager] Cannot register window with null instanceId or chrome");
                return;
            }

            if (windows.ContainsKey(instanceId))
            {
                Debug.LogWarning($"[WindowManager] Window {instanceId} already registered");
                return;
            }

            var info = new WindowInfo
            {
                InstanceId = instanceId,
                Chrome = chrome,
                State = WindowState.Normal,
                NormalBounds = new Rect(
                    chrome.WindowRect.anchoredPosition,
                    chrome.WindowRect.sizeDelta
                )
            };

            windows[instanceId] = info;
            zOrderStack.Insert(0, instanceId); // Add to front

            // Update sibling indices to reflect z-order
            UpdateSiblingIndices();

            // Auto-focus newly created window
            BringToFront(instanceId);

            Debug.Log($"[WindowManager] Registered window: {instanceId}");
        }

        public void UnregisterWindow(string instanceId)
        {
            if (!windows.ContainsKey(instanceId))
                return;

            windows.Remove(instanceId);
            zOrderStack.Remove(instanceId);

            // If this was the focused window, focus the next one
            if (focusedWindowId == instanceId)
            {
                focusedWindowId = null;
                if (zOrderStack.Count > 0)
                {
                    BringToFront(zOrderStack[0]);
                }
            }

            UpdateSiblingIndices();
            Debug.Log($"[WindowManager] Unregistered window: {instanceId}");
        }

        public void BringToFront(string instanceId)
        {
            if (!windows.TryGetValue(instanceId, out var info))
                return;

            // Don't bring minimized windows to front
            if (info.State == WindowState.Minimized)
                return;

            // Update z-order stack
            zOrderStack.Remove(instanceId);
            zOrderStack.Insert(0, instanceId);

            // Update sibling indices
            UpdateSiblingIndices();

            // Handle focus change
            string previousFocused = focusedWindowId;
            if (previousFocused != instanceId)
            {
                // Unfocus previous window
                if (!string.IsNullOrEmpty(previousFocused) && windows.TryGetValue(previousFocused, out var prevInfo))
                {
                    prevInfo.Chrome?.SetFocused(false);
                    OnWindowUnfocused?.Invoke(previousFocused);
                }

                // Focus new window
                focusedWindowId = instanceId;
                info.Chrome?.SetFocused(true);
                OnWindowFocused?.Invoke(instanceId);

                // Trigger game event
                GameEvents.Instance.Trigger(GameEventType.WindowFocused, instanceId);
            }
        }

        public void SendToBack(string instanceId)
        {
            if (!windows.ContainsKey(instanceId))
                return;

            zOrderStack.Remove(instanceId);
            zOrderStack.Add(instanceId); // Add to back

            UpdateSiblingIndices();

            // If this was focused, focus the new front window
            if (focusedWindowId == instanceId && zOrderStack.Count > 0)
            {
                BringToFront(zOrderStack[0]);
            }
        }

        public string GetFocusedWindowId() => focusedWindowId;

        public void MinimizeWindow(string instanceId)
        {
            if (!windows.TryGetValue(instanceId, out var info))
                return;

            if (info.State == WindowState.Minimized)
                return;

            // Store current bounds if normal
            if (info.State == WindowState.Normal)
            {
                info.NormalBounds = new Rect(
                    info.Chrome.WindowRect.anchoredPosition,
                    info.Chrome.WindowRect.sizeDelta
                );
            }

            info.State = WindowState.Minimized;
            info.Chrome?.OnMinimize();

            OnWindowMinimized?.Invoke(instanceId);
            GameEvents.Instance.Trigger(GameEventType.WindowMinimized, instanceId);

            // Focus next non-minimized window
            if (focusedWindowId == instanceId)
            {
                focusedWindowId = null;
                var nextWindow = zOrderStack.FirstOrDefault(id =>
                    windows.TryGetValue(id, out var w) && w.State != WindowState.Minimized);
                if (!string.IsNullOrEmpty(nextWindow))
                {
                    BringToFront(nextWindow);
                }
            }
        }

        public void MaximizeWindow(string instanceId)
        {
            if (!windows.TryGetValue(instanceId, out var info))
                return;

            if (info.State == WindowState.Maximized)
            {
                // Already maximized, restore instead
                RestoreWindow(instanceId);
                return;
            }

            // Store current bounds if normal
            if (info.State == WindowState.Normal)
            {
                info.NormalBounds = new Rect(
                    info.Chrome.WindowRect.anchoredPosition,
                    info.Chrome.WindowRect.sizeDelta
                );
            }

            info.State = WindowState.Maximized;
            info.Chrome?.OnMaximize();

            OnWindowMaximized?.Invoke(instanceId);
            GameEvents.Instance.Trigger(GameEventType.WindowMaximized, instanceId);

            BringToFront(instanceId);
        }

        public void RestoreWindow(string instanceId)
        {
            if (!windows.TryGetValue(instanceId, out var info))
                return;

            if (info.State == WindowState.Normal)
                return;

            var previousState = info.State;
            info.State = WindowState.Normal;
            info.Chrome?.OnRestore(info.NormalBounds);

            OnWindowRestored?.Invoke(instanceId);
            GameEvents.Instance.Trigger(GameEventType.WindowRestored, instanceId);

            BringToFront(instanceId);
        }

        public int GetWindowZOrder(string instanceId)
        {
            int index = zOrderStack.IndexOf(instanceId);
            // Convert to z-order where higher = more in front
            return index >= 0 ? zOrderStack.Count - 1 - index : -1;
        }

        public List<string> GetWindowsByZOrder() => new List<string>(zOrderStack);

        public bool IsWindowMinimized(string instanceId)
        {
            return windows.TryGetValue(instanceId, out var info) && info.State == WindowState.Minimized;
        }

        public bool IsWindowMaximized(string instanceId)
        {
            return windows.TryGetValue(instanceId, out var info) && info.State == WindowState.Maximized;
        }

        public WindowState GetWindowState(string instanceId)
        {
            return windows.TryGetValue(instanceId, out var info) ? info.State : WindowState.Normal;
        }

        private void UpdateSiblingIndices()
        {
            // Higher sibling index = rendered on top
            // zOrderStack[0] should have highest sibling index
            for (int i = 0; i < zOrderStack.Count; i++)
            {
                if (windows.TryGetValue(zOrderStack[i], out var info) && info.Chrome != null)
                {
                    int siblingIndex = zOrderStack.Count - 1 - i;
                    info.Chrome.transform.SetSiblingIndex(siblingIndex);
                }
            }
        }

        private class WindowInfo
        {
            public string InstanceId;
            public WindowChrome Chrome;
            public WindowState State;
            public Rect NormalBounds; // Stored for restore after maximize/minimize
        }
    }
}
