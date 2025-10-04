using System;
using System.Collections.Generic;
using System.Threading;
using SampleOS.Core.Devices;
using SampleOS.Core.FileSystem;
using SampleOS.Core.Networking;
using SampleOS.Core.Terminal;

namespace SampleOS.Core.CommandSystem
{
    /// <summary>
    /// Complete execution context for commands
    /// </summary>
    public class CommandContext
    {
        // I/O Streams
        public ITerminalStream Stdout { get; }
        public ITerminalStream Stderr { get; }

        // Device & Network Context
        public Device CurrentDevice { get; }           // The device we're currently executing on
        public VirtualNetwork CurrentNetwork { get; }   // The network we're connected to

        // Execution Control
        public CancellationToken CancellationToken { get; }
        public IProgress<CommandProgress> Progress { get; }

        // Piping
        public string PipedInput { get; }
        public bool HasPipedInput => !string.IsNullOrEmpty(PipedInput);

        // Interactive Mode
        public bool IsInteractive { get; }

        // Environment
        public CommandEnvironment Environment { get; }

        // Convenience accessors
        public VirtualFileSystem FileSystem => CurrentDevice?.FileSystem;
        // public string CurrentPath => Environment?.GetVariable("PWD") ?? "/";
        public bool IsRemoteConnection => CurrentDevice != null && CurrentDevice.GetType() == typeof(RemoteDevice);

        public CommandContext(
            ITerminalStream stdout,
            ITerminalStream stderr,
            Device currentDevice,
            VirtualNetwork currentNetwork,
            CancellationToken cancellationToken = default,
            IProgress<CommandProgress> progress = null,
            string pipedInput = null,
            bool isInteractive = false,
            CommandEnvironment environment = null)
        {
            Stdout = stdout;
            Stderr = stderr;
            CurrentDevice = currentDevice;
            CurrentNetwork = currentNetwork;
            CancellationToken = cancellationToken;
            Progress = progress ?? new Progress<CommandProgress>();
            PipedInput = pipedInput;
            IsInteractive = isInteractive;
            Environment = environment ?? new CommandEnvironment();
        }

        /// <summary>
        /// Create a new context with piped input
        /// </summary>
        public CommandContext WithPipedInput(string input)
        {
            return new CommandContext(
                Stdout, Stderr, CurrentDevice, CurrentNetwork,
                CancellationToken, Progress, input, IsInteractive, Environment);
        }

        /// <summary>
        /// Create a context with buffered streams for piping
        /// </summary>
        public (CommandContext context, BufferedStream stdout, BufferedStream stderr) CreateBuffered()
        {
            var stdoutBuffer = new BufferedStream();
            var stderrBuffer = new BufferedStream();

            var context = new CommandContext(
                stdoutBuffer, stderrBuffer, CurrentDevice, CurrentNetwork,
                CancellationToken, Progress, PipedInput, IsInteractive, Environment);

            return (context, stdoutBuffer, stderrBuffer);
        }

        /// <summary>
        /// Create a new context for a different device (e.g., after SSH)
        /// </summary>
        public CommandContext WithDevice(Device device)
        {
            return new CommandContext(
                Stdout, Stderr, device, CurrentNetwork,
                CancellationToken, Progress, PipedInput, IsInteractive, Environment);
        }

        /// <summary>
        /// Create a new context for a different network (e.g., after VPN connect)
        /// </summary>
        public CommandContext WithNetwork(VirtualNetwork network)
        {
            return new CommandContext(
                Stdout, Stderr, CurrentDevice, network,
                CancellationToken, Progress, PipedInput, IsInteractive, Environment);
        }

        /// <summary>
        /// Checks if we're currently connected to a remote device
        /// </summary>
        public bool IsRemoteSession => CurrentDevice is RemoteDevice;

        /// <summary>
        /// Checks if we're on the player's local device
        /// </summary>
        public bool IsLocalSession => CurrentDevice is PlayerDevice;

        /// <summary>
        /// Gets the current device as a RemoteDevice (if applicable)
        /// </summary>
        public RemoteDevice GetRemoteDevice()
        {
            return CurrentDevice as RemoteDevice;
        }

        /// <summary>
        /// Gets the current device as a PlayerDevice (if applicable)
        /// </summary>
        public PlayerDevice GetPlayerDevice()
        {
            return CurrentDevice as PlayerDevice;
        }
    }

    /// <summary>
    /// Environment variables and settings
    /// </summary>
    public class CommandEnvironment
    {
        private readonly Dictionary<string, string> variables
            = new Dictionary<string, string>();

        public string Get(string key, string defaultValue = "")
        {
            return variables.TryGetValue(key, out var value) ? value : defaultValue;
        }

        public void Set(string key, string value)
        {
            variables[key] = value;
        }

        public bool Has(string key)
        {
            return variables.ContainsKey(key);
        }

        public Dictionary<string, string> GetAll()
        {
            return new Dictionary<string, string>(variables);
        }
    }

    /// <summary>
    /// Progress information for long-running commands
    /// </summary>
    public struct CommandProgress
    {
        public float Percentage { get; set; }
        public string Message { get; set; }
        public string CurrentItem { get; set; }
        public int ItemsProcessed { get; set; }
        public int TotalItems { get; set; }
    }

    /// <summary>
    /// Result of command execution
    /// </summary>
    public class CommandResult
    {
        public int ExitCode { get; set; }
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public bool Success => ExitCode == 0;

        public static CommandResult Ok(string message = "") 
            => new CommandResult { ExitCode = 0, Message = message };

        public static CommandResult Error(string message, int exitCode = 1) 
            => new CommandResult { ExitCode = exitCode, Message = message };

        public static CommandResult FromException(Exception ex) 
            => new CommandResult { ExitCode = 1, Message = ex.Message, Exception = ex };
    }
}
