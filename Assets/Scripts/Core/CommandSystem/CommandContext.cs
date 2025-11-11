using System;
using System.Collections.Generic;
using System.Threading;
using SampleOS.Core.Devices;
using SampleOS.Core.FileSystem;
using SampleOS.Core.Networking;
using SampleOS.Core.Services;
using SampleOS.Core.Terminal;

namespace SampleOS.Core.CommandSystem
{
    /// <summary>
    /// Complete execution context for commands.
    /// Now supports both global (service-based) and local (app-specific) device contexts.
    /// </summary>
    public class CommandContext
    {
        // I/O Streams
        public ITerminalStream Stdout { get; }
        public ITerminalStream Stderr { get; }

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

        // NEW: Device-specific context (for app isolation)
        // If set, these override the service-based values
        private readonly Device _localDevice;
        private readonly VirtualNode _localWorkingDirectory;
        private readonly string _localUser;
        private readonly object _sourceApp; // IInteractiveApp, but stored as object to avoid circular dependency

        // Command History (for auto-completion and history navigation)
        public List<string> CommandHistory { get; set; }

        // Lazy-loaded services
        private IPlayerStateService _playerState;
        private IHackingSessionService _hackingSession;
        private IWorldService _worldService;
        private INetworkService _networkService;

        public IPlayerStateService PlayerState =>
            _playerState ??= ServiceLocator.Instance.Get<IPlayerStateService>();

        public IHackingSessionService HackingSession =>
            _hackingSession ??= ServiceLocator.Instance.Get<IHackingSessionService>();

        public IWorldService WorldService =>
            _worldService ??= ServiceLocator.Instance.Get<IWorldService>();

        public INetworkService NetworkService =>
            _networkService ??= ServiceLocator.Instance.Get<INetworkService>();

        // Smart accessors: Use local context if available, otherwise fall back to services
        public Device CurrentDevice => _localDevice ?? HackingSession?.CurrentDevice;
        public VirtualFileSystem FileSystem => CurrentDevice?.FileSystem;
        public VirtualNode WorkingDirectory => _localWorkingDirectory ?? FileSystem?.GetNode("/");
        public string CurrentUser => _localUser ?? PlayerState?.CurrentUser ?? "user";
        public object SourceApp => _sourceApp; // The app that created this context
        
        public VirtualNetwork CurrentNetwork => WorldService?.GetCurrentCity()?.CurrentNetwork;
        public bool IsRemoteSession => HackingSession?.IsOnRemoteDevice ?? false;

        /// <summary>
        /// Standard constructor for service-based context (backward compatible)
        /// </summary>
        public CommandContext(
            ITerminalStream stdout,
            ITerminalStream stderr,
            CancellationToken cancellationToken = default,
            IProgress<CommandProgress> progress = null,
            string pipedInput = null,
            bool isInteractive = false,
            CommandEnvironment environment = null)
        {
            Stdout = stdout;
            Stderr = stderr;
            CancellationToken = cancellationToken;
            Progress = progress ?? new Progress<CommandProgress>();
            PipedInput = pipedInput;
            IsInteractive = isInteractive;
            Environment = environment ?? new CommandEnvironment();
            
            // No local device context - will use services
            _localDevice = null;
            _localWorkingDirectory = null;
            _localUser = null;
            _sourceApp = null;
        }

        /// <summary>
        /// NEW: Constructor for app-specific context (for isolated terminal instances)
        /// </summary>
        public CommandContext(
            ITerminalStream stdout,
            ITerminalStream stderr,
            Device device,
            VirtualNode workingDirectory,
            string currentUser,
            object sourceApp,
            CancellationToken cancellationToken = default,
            IProgress<CommandProgress> progress = null,
            string pipedInput = null,
            bool isInteractive = false,
            CommandEnvironment environment = null,
            List<string> commandHistory = null)
        {
            Stdout = stdout;
            Stderr = stderr;
            CancellationToken = cancellationToken;
            Progress = progress ?? new Progress<CommandProgress>();
            PipedInput = pipedInput;
            IsInteractive = isInteractive;
            Environment = environment ?? new CommandEnvironment();
            
            // Set local device context
            _localDevice = device;
            _localWorkingDirectory = workingDirectory;
            _localUser = currentUser;
            _sourceApp = sourceApp;
            CommandHistory = commandHistory ?? new List<string>();
        }

        /// <summary>
        /// Create a new context with piped input
        /// </summary>
        public CommandContext WithPipedInput(string input)
        {
            // Preserve local context if present
            if (_localDevice != null)
            {
                return new CommandContext(
                    Stdout, Stderr, _localDevice, _localWorkingDirectory, _localUser, _sourceApp,
                    CancellationToken, Progress, input, IsInteractive, Environment, CommandHistory);
            }
            
            return new CommandContext(
                Stdout, Stderr, CancellationToken, Progress,
                input, IsInteractive, Environment);
        }

        /// <summary>
        /// Create a context with buffered streams for piping
        /// </summary>
        public (CommandContext context, BufferedStream stdout, BufferedStream stderr) CreateBuffered()
        {
            var stdoutBuffer = new BufferedStream();
            var stderrBuffer = new BufferedStream();

            CommandContext context;
            
            // Preserve local context if present
            if (_localDevice != null)
            {
                context = new CommandContext(
                    stdoutBuffer, stderrBuffer, _localDevice, _localWorkingDirectory, _localUser, _sourceApp,
                    CancellationToken, Progress, PipedInput, IsInteractive, Environment, CommandHistory);
            }
            else
            {
                context = new CommandContext(
                    stdoutBuffer, stderrBuffer, CancellationToken, Progress,
                    PipedInput, IsInteractive, Environment);
            }

            return (context, stdoutBuffer, stderrBuffer);
        }

        /// <summary>
        /// Update the working directory (for cd command)
        /// </summary>
        public void SetWorkingDirectory(VirtualNode node)
        {
            // This is a bit hacky, but necessary for cd command to work
            // In a more pure functional design, cd would return a new context
            // But that would require refactoring all commands
            
            if (_localDevice != null)
            {
                // Can't directly modify readonly field, so we use reflection as a workaround
                // Alternative: Store working directory in a mutable wrapper class
                var field = typeof(CommandContext).GetField("_localWorkingDirectory", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                field?.SetValue(this, node);
            }
            else
            {
                // For service-based context, the cd command should update the HackingSession
                // This is handled by the CdCommand itself
            }
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
        public string Output { get; set; } // NEW: Store output for app to use
        public string Message { get; set; }
        public Exception Exception { get; set; }

        public bool Success => ExitCode == 0;

        public static CommandResult Ok(string message = "", string output = "") 
            => new CommandResult { ExitCode = 0, Message = message, Output = output };

        public static CommandResult Error(string message, int exitCode = 1) 
            => new CommandResult { ExitCode = exitCode, Message = message, Output = message };

        public static CommandResult FromException(Exception ex) 
            => new CommandResult { ExitCode = 1, Message = ex.Message, Exception = ex, Output = ex.Message };
        
        public static CommandResult Empty() 
            => new CommandResult { ExitCode = 0, Message = "", Output = "" };
    }
}
