using System;
using System.Threading;
using SampleOS.Core.Terminal;

namespace SampleOS.Core.CommandSystem
{
    /// <summary>
    /// Complete execution context for commands
    /// </summary>
    public class CommandContext
    {
        // Streams
        public ITerminalStream Stdout { get; }
        public ITerminalStream Stderr { get; }

        // Resources
        public VirtualFileSystem FileSystem { get; }
        public VirtualNetwork Network { get; }
        public RemoteSystem CurrentSystem { get; }

        // State
        public CancellationToken CancellationToken { get; }
        public IProgress<CommandProgress> Progress { get; }
        public string PipedInput { get; }
        public bool IsInteractive { get; }

        // Environment
        public CommandEnvironment Environment { get; }

        public CommandContext(
            ITerminalStream stdout,
            ITerminalStream stderr,
            VirtualFileSystem fileSystem,
            VirtualNetwork network,
            RemoteSystem currentSystem = null,
            CancellationToken cancellationToken = default,
            IProgress<CommandProgress> progress = null,
            string pipedInput = null,
            bool isInteractive = false,
            CommandEnvironment environment = null)
        {
            Stdout = stdout;
            Stderr = stderr;
            FileSystem = fileSystem;
            Network = network;
            CurrentSystem = currentSystem;
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
                Stdout, Stderr, FileSystem, Network, CurrentSystem,
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
                stdoutBuffer, stderrBuffer, FileSystem, Network, CurrentSystem,
                CancellationToken, Progress, PipedInput, IsInteractive, Environment);
            
            return (context, stdoutBuffer, stderrBuffer);
        }
    }

    /// <summary>
    /// Environment variables and settings
    /// </summary>
    public class CommandEnvironment
    {
        private readonly System.Collections.Generic.Dictionary<string, string> variables 
            = new System.Collections.Generic.Dictionary<string, string>();

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
