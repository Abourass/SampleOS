using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Threading;
using SampleOS.Core.CommandSystem;
using SampleOS.Core.FileSystem;
using SampleOS.Core.Apps;
using SampleOS.Core.Services;

namespace SampleOS.Core.UI
{
    /// <summary>
    /// Terminal UI View - handles rendering and input for a TerminalApp instance.
    /// This is the MonoBehaviour component that gets instantiated from a prefab.
    /// </summary>
    public class TerminalAppView : MonoBehaviour, IAppView
    {
        [Header("UI References")]
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TerminalInputHandler inputHandler;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Visual Settings")]
        [SerializeField] private TMP_FontAsset nerdFontAsset;
        
        // The app this view is bound to
        private TerminalApp terminalApp;
        
        // UI Handlers
        private TerminalOutputHandler outputHandler;
        private TerminalHistory history;
        private CancellationTokenSource commandCancellation;
        
        // IAppView implementation
        public bool IsBound => terminalApp != null;
        public IInteractiveApp BoundApp => terminalApp;

        private void Awake()
        {
            ApplyFontSettings();
            
            // Ensure we have a canvas group for show/hide
            if (canvasGroup == null)
            {
                canvasGroup = GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = gameObject.AddComponent<CanvasGroup>();
                }
            }
        }

        private void OnDestroy()
        {
            commandCancellation?.Cancel();
            commandCancellation?.Dispose();
        }

        #region IAppView Implementation

        public void BindToApp(IInteractiveApp app)
        {
            if (!(app is TerminalApp termApp))
            {
                Debug.LogError($"[TerminalAppView] Cannot bind to app of type {app.GetType().Name}. Expected TerminalApp.");
                return;
            }

            terminalApp = termApp;
            
            // Initialize history
            history = new TerminalHistory();
            history.RestoreHistory(terminalApp.GetCommandHistory());
            
            // Create output handler
            outputHandler = new TerminalOutputHandler(
                outputText,
                scrollRect,
                terminalApp.GetConfig().promptConfig,
                terminalApp.GetCommandProcessor(),
                this
            );
            
            // Initialize input handler
            if (inputHandler != null)
            {
                inputHandler.Initialize(
                    history,
                    ProcessCommand,
                    terminalApp.GetCommandProcessor(),
                    terminalApp.HostDevice.FileSystem
                );
            }
            else
            {
                Debug.LogError("[TerminalAppView] TerminalInputHandler reference is missing!");
            }
            
            // Display initial state
            RefreshView();
            
            Debug.Log($"[TerminalAppView] Bound to TerminalApp {terminalApp.InstanceId} on {terminalApp.HostDevice.Hostname}");
        }

        public void UnbindFromApp()
        {
            if (terminalApp != null)
            {
                Debug.Log($"[TerminalAppView] Unbinding from TerminalApp {terminalApp.InstanceId}");
                terminalApp = null;
                outputHandler = null;
                history = null;
            }
        }

        public void Show()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.interactable = true;
                canvasGroup.blocksRaycasts = true;
            }
            
            gameObject.SetActive(true);
            
            if (inputHandler != null)
            {
                inputHandler.FocusInput();
            }
        }

        public void Hide()
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = 0f;
                canvasGroup.interactable = false;
                canvasGroup.blocksRaycasts = false;
            }
        }

        public void RefreshView()
        {
            if (!IsBound) return;
            
            // Display welcome message
            var config = terminalApp.GetConfig();
            if (config != null && !string.IsNullOrEmpty(config.welcomeMessage))
            {
                outputHandler.DisplayWelcomeMessage(config.welcomeMessage);
            }
            
            // Update prompt
            UpdatePrompt();
            
            // Focus input
            if (inputHandler != null)
            {
                inputHandler.FocusInput();
            }
        }

        #endregion

        #region Terminal Operations

        private async void ProcessCommand(string input)
        {
            if (!IsBound)
            {
                Debug.LogWarning("[TerminalAppView] Cannot process command - not bound to an app");
                return;
            }

            // Check if we're in interactive mode
            bool isInteractiveMode = terminalApp.GetCommandProcessor().IsWaitingForInput;

            // Don't echo navigation commands from interactive mode
            if (!isInteractiveMode ||
                (input != "up" && input != "down" && input != "enter" && input != "escape"))
            {
                outputHandler.AppendText(input + "\n");
            }

            // Execute command through the app
            var result = await terminalApp.ExecuteCommandAsync(input);

            // Display result
            if (!string.IsNullOrEmpty(result.Output))
            {
                outputHandler.AppendText(result.Output);
            }

            // Update prompt if not in interactive mode
            if (!terminalApp.GetCommandProcessor().IsWaitingForInput)
            {
                UpdatePrompt();
            }

            // Focus input
            if (inputHandler != null)
            {
                inputHandler.FocusInput();
            }
        }

        private void UpdatePrompt()
        {
            if (!IsBound) return;

            var context = terminalApp.GetContext();
            string currentPath = context.WorkingDirectory?.FullPath ?? "/";
            
            // Check if we're connected remotely (via SSH)
            // This would be determined by checking the context's current device
            bool isRemote = context.CurrentDevice != null && 
                            context.CurrentDevice.Id != terminalApp.HostDevice.Id;

            if (isRemote)
            {
                string hostname = context.CurrentDevice.Hostname;
                outputHandler.DisplayRemotePrompt(hostname, currentPath);
            }
            else
            {
                outputHandler.DisplayPrompt(currentPath);
            }
        }

        /// <summary>
        /// Clear the terminal output
        /// </summary>
        public void ClearTerminal()
        {
            if (outputHandler != null)
            {
                outputHandler.Clear();
                UpdatePrompt();
            }
        }

        /// <summary>
        /// Display a system message (not from a command)
        /// </summary>
        public void DisplaySystemMessage(string message, Color? color = null)
        {
            if (outputHandler != null)
            {
                if (color.HasValue)
                {
                    outputHandler.AppendColoredText(message + "\n", color.Value);
                }
                else
                {
                    outputHandler.AppendText(message + "\n");
                }
            }
        }

        #endregion

        #region Helpers

        private void ApplyFontSettings()
        {
            if (nerdFontAsset != null && outputText != null)
            {
                outputText.font = nerdFontAsset;

                if (inputField != null && inputField.textComponent != null)
                {
                    inputField.textComponent.font = nerdFontAsset;
                }
            }
        }

        #endregion
    }
}
