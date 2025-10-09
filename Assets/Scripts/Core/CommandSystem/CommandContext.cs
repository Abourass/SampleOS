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
    /// Complete execution context for commands
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

        // Convenience accessors to services (lazy loaded)
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

        // Convenience accessors (delegate to services)
        public Device CurrentDevice => HackingSession?.CurrentDevice;
        public VirtualFileSystem FileSystem => CurrentDevice?.FileSystem;
        public VirtualNetwork CurrentNetwork => WorldService?.GetCurrentCity()?.CurrentNetwork;
        public bool IsRemoteSession => HackingSession?.IsOnRemoteDevice ?? false;

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
        }

        /// <summary>
        /// Create a new context with piped input
        /// </summary>
        public CommandContext WithPipedInput(string input)
        {
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

            var context = new CommandContext(
                stdoutBuffer, stderrBuffer, CancellationToken, Progress,
                PipedInput, IsInteractive, Environment);

            return (context, stdoutBuffer, stderrBuffer);
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
