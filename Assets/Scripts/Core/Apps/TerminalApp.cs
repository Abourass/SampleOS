using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using SampleOS.Core.CommandSystem;
using SampleOS.Core.Devices;
using SampleOS.Core.Terminal;
using UnityEngine;

namespace SampleOS.Core.Apps
{
    /// <summary>
    /// Terminal application - manages terminal state and command execution.
    /// This is the business logic layer, separate from UI rendering.
    /// </summary>
    public class TerminalApp : IInteractiveApp
    {
        // IInteractiveApp implementation
        public string InstanceId { get; private set; }
        public string AppId => "terminal";
        public string DisplayName => "Terminal";
        public AppCategory Category => AppCategory.Terminal;
        public Device HostDevice { get; private set; }
        
        // Terminal-specific state
        private CommandProcessor commandProcessor;
        private CommandContext context;
        private TerminalConfig config;
        private List<string> commandHistory;

        // Buffered streams for command output (until bound to a view)
        private BufferedStream stdout;
        private BufferedStream stderr;

        public TerminalApp()
        {
            InstanceId = Guid.NewGuid().ToString();
            commandHistory = new List<string>();
        }

        public void Initialize(Device hostDevice)
        {
            HostDevice = hostDevice;

            // Create buffered streams for command output
            stdout = new BufferedStream();
            stderr = new BufferedStream();

            // Get initial working directory
            var homeDir = hostDevice.FileSystem.ResolvePath("/home/user") ??
                         hostDevice.FileSystem.ResolvePath("/");

            // Create ISOLATED device-specific command context using the proper constructor
            context = new CommandContext(
                stdout: stdout,
                stderr: stderr,
                device: hostDevice,
                workingDirectory: homeDir,
                currentUser: hostDevice.OS?.CurrentUser ?? "user",
                sourceApp: this,
                cancellationToken: CancellationToken.None,
                progress: null,
                pipedInput: null,
                isInteractive: true,
                environment: null,
                commandHistory: commandHistory
            );

            // Create isolated command processor for this terminal
            // (Initialize is called automatically in constructor)
            commandProcessor = new CommandProcessor();

            // Load device-specific terminal config
            config = LoadTerminalConfig(hostDevice);

            Debug.Log($"[TerminalApp] Initialized on {hostDevice.Hostname} (Instance: {InstanceId}) with isolated context");
        }
        
        public void Update(float deltaTime)
        {
            // Terminal doesn't need per-frame updates currently
            // But this hook exists for future needs (animations, etc.)
        }
        
        public void Shutdown()
        {
            Debug.Log($"[TerminalApp] Shutting down instance {InstanceId} on {HostDevice.Hostname}");
            commandProcessor = null;
            context = null;
        }
        
        // Terminal-specific API
        public async Task<CommandResult> ExecuteCommandAsync(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return CommandResult.Empty();

            commandHistory.Add(input);

            // Clear previous buffered output
            stdout.Clear();
            stderr.Clear();

            // Update context before executing
            context.CommandHistory = commandHistory;

            // Execute command with ISOLATED context (not service-based)
            var result = await commandProcessor.ProcessWithContextAsync(input, context);

            // Capture buffered output from our streams
            string capturedStdout = stdout.Content;
            string capturedStderr = stderr.Content;

            // Combine output streams
            string combinedOutput = capturedStdout;
            if (!string.IsNullOrEmpty(capturedStderr))
            {
                combinedOutput += (string.IsNullOrEmpty(combinedOutput) ? "" : "\n") + capturedStderr;
            }

            // Return result with captured output
            return new CommandResult
            {
                ExitCode = result.ExitCode,
                Output = combinedOutput,
                Message = result.Message,
                Exception = result.Exception
            };
        }
        
        /// <summary>
        /// Get the terminal configuration
        /// </summary>
        public TerminalConfig GetConfig() => config;
        
        /// <summary>
        /// Get the current command context
        /// </summary>
        public CommandContext GetContext() => context;
        
        /// <summary>
        /// Get the command history
        /// </summary>
        public List<string> GetCommandHistory() => new List<string>(commandHistory);
        
        /// <summary>
        /// Get the command processor (exposed for UI to check state)
        /// </summary>
        public CommandProcessor GetCommandProcessor() => commandProcessor;
        
        private TerminalConfig LoadTerminalConfig(Device device)
        {
            // Try to load device-specific config from Resources
            var config = UnityEngine.Resources.Load<TerminalConfig>("Configs/TerminalConfig");

            if (config != null)
                return config;

            // If no config found, create a basic default programmatically
            // Note: This creates a non-persistent instance (not saved as asset)
            config = ScriptableObject.CreateInstance<TerminalConfig>();
            config.welcomeMessage = $"Welcome to {device.Hostname}\nType 'help' for available commands.";

            return config;
        }
        
        // IInteractiveApp UI hooks
        public void OnAppOpened()
        {
            Debug.Log($"[TerminalApp] Opened on {HostDevice.Hostname}");
            // Could trigger events here
        }
        
        public void OnAppClosed()
        {
            Debug.Log($"[TerminalApp] Closed on {HostDevice.Hostname}");
            // Could save state or cleanup
        }
        
        public void OnFocusGained()
        {
            // Could play focus sound, update UI, etc.
        }
        
        public void OnFocusLost()
        {
            // Could dim UI, pause animations, etc.
        }
        
        public bool CanRunOnDevice(Device device)
        {
            // Terminals can run on any device with a filesystem
            return device?.FileSystem != null;
        }
        
        // State serialization
        public object SerializeState()
        {
            // Get the working directory path from the file system
            string workingDirPath = HostDevice.FileSystem.CurrentPath;

            return new TerminalState
            {
                instanceId = InstanceId,
                deviceHostname = HostDevice.Hostname,
                workingDirectory = workingDirPath,
                commandHistory = new List<string>(commandHistory),
                currentUser = context.CurrentUser
            };
        }

        public void DeserializeState(object state)
        {
            if (state is TerminalState termState)
            {
                commandHistory = termState.commandHistory;

                // Resolve the working directory path
                var workingDir = HostDevice.FileSystem.ResolvePath(termState.workingDirectory);
                if (workingDir == null)
                {
                    // Fallback to home or root if path doesn't exist
                    workingDir = HostDevice.FileSystem.ResolvePath("/home/user") ??
                                HostDevice.FileSystem.ResolvePath("/");
                }

                // Recreate the context with restored state
                // (CommandContext properties are read-only, so we need to create a new one)
                context = new CommandContext(
                    stdout: stdout,
                    stderr: stderr,
                    device: HostDevice,
                    workingDirectory: workingDir,
                    currentUser: termState.currentUser,
                    sourceApp: this,
                    cancellationToken: CancellationToken.None,
                    progress: null,
                    pipedInput: null,
                    isInteractive: true,
                    environment: null,
                    commandHistory: commandHistory
                );

                Debug.Log($"[TerminalApp] Restored state for {InstanceId}");
            }
        }
        
        [Serializable]
        private class TerminalState
        {
            public string instanceId;
            public string deviceHostname;
            public string workingDirectory;
            public List<string> commandHistory;
            public string currentUser;
        }
    }
}
