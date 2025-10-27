using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using SampleOS.Core.CommandSystem;
using SampleOS.Core.FileSystem;
using SampleOS.Core.Devices;
using SampleOS.Core.Services;
using System.Threading;

namespace SampleOS.Core.Terminal
{
    public class TerminalController : MonoBehaviour
    {
        [SerializeField] private TMP_Text outputText;
        [SerializeField] private TMP_InputField inputField;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private TerminalConfig config;
        [SerializeField] private PromptConfig promptConfig;
        [SerializeField] private TerminalInputHandler inputHandler;

        [Header("Font Settings")]
        [SerializeField] private TMP_FontAsset nerdFontAsset;

        private TerminalOutputHandler outputHandler;
        private TerminalHistory history;
        private CommandProcessor commandProcessor;

        // Service references
        private IPlayerStateService playerState;
        private IHackingSessionService hackingSession;
        private IWorldService worldService;

        // Current device context (cached from services)
        private Device currentDevice;
        private VirtualFileSystem currentFileSystem;

        // Interactive mode tracking
        private bool isInteractiveMode;
        private CancellationTokenSource commandCancellation;

        #region Initialization

        private void ApplyFontSettings()
        {
            if (nerdFontAsset != null)
            {
                outputText.font = nerdFontAsset;

                if (inputField.textComponent != null)
                {
                    inputField.textComponent.font = nerdFontAsset;
                }
            }
        }

        private bool ValidateServices()
        {
            // Check if core services are registered
            return ServiceLocator.Instance.IsRegistered<IPlayerStateService>() &&
                    ServiceLocator.Instance.IsRegistered<IHackingSessionService>() &&
                    ServiceLocator.Instance.IsRegistered<IWorldService>();
        }

        private void InitializeTerminal()
        {
            outputHandler.DisplayWelcomeMessage(config.welcomeMessage);
            UpdatePrompt();
            inputHandler.FocusInput();

            // Trigger terminal opened event
            GameEvents.Instance.Trigger(GameEventType.TerminalOpened, this);
        }

        #endregion

        private void Awake()
        {
            // Apply the Nerd Font to text components
            ApplyFontSettings();

            // Wait for services to be initialized
            if (!ValidateServices())
            {
                Debug.LogError("Required services not initialized! Make sure GameStateManager starts first.");
                enabled = false;
                return;
            }

            // Get service references
            playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
            hackingSession = ServiceLocator.Instance.Get<IHackingSessionService>();
            worldService = ServiceLocator.Instance.Get<IWorldService>();

            // Create command processor
            commandProcessor = new CommandProcessor();

            outputHandler = new TerminalOutputHandler(
                outputText,
                scrollRect,
                promptConfig,
                commandProcessor,
                this
            );
            history = new TerminalHistory();

            // Get current device context from services
            UpdateDeviceContext();

            // Initialize the input handler with the required references including file system
            if (inputHandler == null)
            {
                Debug.LogError("TerminalInputHandler reference is missing!");
                return;
            }

            inputHandler.Initialize(
                history,
                ProcessCommand,
                commandProcessor,
                currentFileSystem
            );

            // Subscribe to device changes
            SubscribeToDeviceChanges();
        }

        private void Start()
        {
            InitializeTerminal();
        }

        private void OnDestroy()
        {
            UnsubscribeFromDeviceChanges();
            commandCancellation?.Cancel();
            commandCancellation?.Dispose();
        }

        #region Command Processing

        private async void ProcessCommand(string input)
        {
            // Check if we're in interactive mode
            bool isInteractiveMode = commandProcessor.IsWaitingForInput;

            // Don't echo navigation commands from interactive mode
            if (!isInteractiveMode ||
                (input != "up" && input != "down" && input != "enter" && input != "escape"))
            {
                outputHandler.AppendText(input + "\n");
            }

            // Process the command
            await commandProcessor.ProcessCommandAsync(input, outputHandler);

            // Check if device context changed (e.g., SSH, exploit)
            UpdateDeviceContext();

            // Only display the standard prompt if we're not waiting for input
            if (!commandProcessor.IsWaitingForInput)
            {
                UpdatePrompt();
            }

            inputHandler.FocusInput();
        }

        #endregion

        #region Device Context Management
        /// <summary>
        /// Updates the device context if it changed
        /// </summary>
        private void UpdateDeviceContext()
        {
            var newDevice = hackingSession.CurrentDevice;

            if (newDevice != currentDevice)
            {
                currentDevice = newDevice;
                currentFileSystem = currentDevice?.FileSystem;

                // Update input handler with new file system for tab completion
                if (currentFileSystem != null)
                {
                    inputHandler.UpdateFileSystem(currentFileSystem);
                }

                // Show connection status change
                if (commandProcessor.IsOnRemoteDevice())
                {
                    outputHandler.AppendColoredText(
                        $"[Connected to {currentDevice.Hostname}]\n",
                        new Color(0.3f, 1f, 0.3f) // Green for connection
                    );
                }
                else
                {
                    outputHandler.AppendColoredText(
                        "[Returned to local device]\n",
                        new Color(0.7f, 0.7f, 1f) // Blue for local
                    );
                }

                UpdatePrompt();
            }
        }

        /// <summary>
        /// Updates the prompt display based on current device and directory
        /// </summary>
        private void UpdatePrompt()
        {
            if (currentDevice == null || currentFileSystem == null)
                return;

            string currentPath = currentFileSystem.CurrentPath;
            string hostname = currentDevice.Hostname;
            bool isRemote = commandProcessor.IsOnRemoteDevice();

            // Show different prompt style for remote vs local
            if (isRemote)
            {
                outputHandler.DisplayRemotePrompt(hostname, currentPath);
            }
            else
            {
                outputHandler.DisplayPrompt(currentPath);
            }
        }

        /// <summary>
        /// Subscribe to events that might change device context
        /// </summary>
        private void SubscribeToDeviceChanges()
        {
            // If you have events in CommandProcessor or Session, subscribe here
            // For now, we rely on UpdateDeviceContext() being called after each command
        }

        private void UnsubscribeFromDeviceChanges()
        {
            // Unsubscribe from any events
        }

        #endregion

        /// <summary>
        /// Get the current device being used
        /// </summary>
        public Device GetCurrentDevice()
        {
            return currentDevice;
        }

        /// <summary>
        /// Get the current file system
        /// </summary>
        public VirtualFileSystem GetCurrentFileSystem()
        {
            return currentFileSystem;
        }

        /// <summary>
        /// Force a prompt refresh (useful after device changes)
        /// </summary>
        public void RefreshPrompt()
        {
            UpdateDeviceContext();
            UpdatePrompt();
        }

        /// <summary>
        /// Clear the terminal output
        /// </summary>
        public void ClearTerminal()
        {
            outputHandler.Clear();
            UpdatePrompt();
        }

        /// <summary>
        /// Display a system message (not from a command)
        /// </summary>
        public void DisplaySystemMessage(string message, Color? color = null)
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

        /// <summary>
        /// Get command history for saving/loading
        /// </summary>
        public List<string> GetCommandHistory()
        {
            return new List<string>(history.GetAllCommands());
        }

        /// <summary>
        /// Set command history (for loading saved sessions)
        /// </summary>
        public void SetCommandHistory(List<string> commands)
        {
            history.Clear();
            foreach (var cmd in commands)
            {
                history.AddCommand(cmd);
            }
        }
    }
}
