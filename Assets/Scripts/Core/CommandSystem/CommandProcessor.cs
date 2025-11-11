using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using SampleOS.Core.Terminal;
using SampleOS.Core.CommandSystem.Commands.FileOps;
using SampleOS.Core.CommandSystem.Commands.CLI;
using SampleOS.Core.CommandSystem.Commands.Systems;
using SampleOS.Core.CommandSystem.Commands.Networking;
using SampleOS.Core.CommandSystem.Commands.Vulnerabilities;
using SampleOS.Core.Services;

namespace SampleOS.Core.CommandSystem
{
    /// <summary>
    /// Processes and executes terminal commands.
    /// NOTE: This is NOT a MonoBehaviour - it's a plain C# class that can be instantiated freely.
    /// </summary>
    public class CommandProcessor
    {
        private Dictionary<string, ICommand> commands = new();
        private Dictionary<string, string> aliases = new();

        private IPlayerStateService playerState;
        private IWorldService worldService;

        private IInteractiveCommand interactiveCommand;
        private CancellationTokenSource cancellationSource;

        public bool IsWaitingForInput => interactiveCommand?.IsWaitingForInput ?? false;
        public bool IsExecuting => cancellationSource != null;
        public bool LastCommandSucceeded { get; private set; } = true;

        public CommandProcessor()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get services instead of creating state
            playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
            worldService = ServiceLocator.Instance.Get<IWorldService>();

            if (playerState == null || worldService == null)
            {
                Debug.LogError("Required services not initialized!");
                return;
            }

            // Register commands
            RegisterCommands();
        }

        private void RegisterCommands()
        {
            // System
            Register(new AliasCommand(aliases));
            Register(new ClearCommand());
            Register(new HelpCommand(commands));
            Register(new PsCommand(this));
            Register(new QuitCommand());

            // File Operations
            Register(new CatCommand());
            Register(new CdCommand());
            Register(new GrepCommand());
            Register(new LsCommand());
            Register(new MkdirCommand());
            Register(new TouchCommand());
            Register(new WcCommand());

            // CLI
            Register(new GumCommand());

            // Networking
            Register(new NetstatCommand());
            Register(new NetworksCommand());
            Register(new SshCommand());
            Register(new NmapCommand());
            Register(new OwnedCommand());
            Register(new VpnConnectCommand());

            // Vulnerabilities
            Register(new ExploitCommand());
            Register(new VulnScanCommand());
            Register(new VulnsCommand());
        }

        private void Register(ICommand command)
        {
            commands[command.Name] = command;
        }

        /// <summary>
        /// Process command - main entry point
        /// </summary>
        public async Task<CommandResult> ProcessCommandAsync(
            string input,
            ITerminalOutput output,
            CancellationToken cancellationToken = default)
        {
            // Create streams
            var stdout = new DirectTerminalStream(output, Color.white);
            var stderr = new DirectTerminalStream(output, new Color(1f, 0.3f, 0.3f));

            // Create context
            var context = CreateContext(stdout, stderr, cancellationToken);

            return await ProcessWithContextAsync(input, context);
        }

        /// <summary>
        /// Synchronous version for non-async commands
        /// </summary>
        private CommandResult ProcessCommand(string input, ITerminalOutput output)
        {
            var stdout = new DirectTerminalStream(output, Color.white);
            var stderr = new DirectTerminalStream(output, new Color(1f, 0.3f, 0.3f));
            var context = CreateContext(stdout, stderr);

            return ProcessWithContext(input, context);
        }

        /// <summary>
        /// Handle interactive command input
        /// </summary>
        public void ProcessInteractiveInput(string input, ITerminalOutput output)
        {
            if (interactiveCommand == null || !interactiveCommand.IsWaitingForInput)
                return;

            var stdout = new DirectTerminalStream(output, Color.white);
            var stderr = new DirectTerminalStream(output, new Color(1f, 0.3f, 0.3f));
            var context = CreateContext(stdout, stderr);

            interactiveCommand.ProcessInput(input, context);

            if (!interactiveCommand.IsWaitingForInput)
            {
                interactiveCommand = null;
            }
        }

        private CommandContext CreateContext(
            ITerminalStream stdout,
            ITerminalStream stderr,
            CancellationToken cancellationToken = default)
        {
            return new CommandContext(
                stdout,
                stderr,
                cancellationToken,
                new Progress<CommandProgress>(p =>
                    Debug.Log($"Progress: {p.Percentage:P0} - {p.Message}")),
                null, // pipedInput
                false, // isInteractive
                null // environment (uses default)
            );
        }

        /// <summary>
        /// Process command with an externally-provided context (for app-specific contexts)
        /// </summary>
        public async Task<CommandResult> ProcessWithContextAsync(string input, CommandContext context)
        {
            // Split by conditional operators
            var segments = SplitByConditionalOperators(input);
            CommandResult lastResult = CommandResult.Ok();

            foreach (var segment in segments)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return CommandResult.Error("Cancelled");

                // Check conditional logic
                if (segment.requiresSuccess && !lastResult.Success) continue;
                if (segment.requiresFailure && lastResult.Success) continue;

                lastResult = await ProcessPipelineAsync(segment.command, context);
            }

            LastCommandSucceeded = lastResult.Success;
            return lastResult;
        }

        private CommandResult ProcessWithContext(string input, CommandContext context)
        {
            var segments = SplitByConditionalOperators(input);
            CommandResult lastResult = CommandResult.Ok();

            foreach (var segment in segments)
            {
                if (segment.requiresSuccess && !lastResult.Success) continue;
                if (segment.requiresFailure && lastResult.Success) continue;

                lastResult = ProcessPipeline(segment.command, context);
            }

            LastCommandSucceeded = lastResult.Success;
            return lastResult;
        }

        private async Task<CommandResult> ProcessPipelineAsync(string commandLine, CommandContext context)
        {
            var commands = commandLine.Split('|').Select(c => c.Trim()).ToArray();

            if (commands.Length == 1)
                return await ExecuteCommandAsync(commands[0], context);

            // Process pipe chain
            string pipedData = null;
            CommandResult lastResult = CommandResult.Ok();

            for (int i = 0; i < commands.Length; i++)
            {
                var (pipeContext, stdoutBuffer, stderrBuffer) = context.CreateBuffered();
                pipeContext = pipeContext.WithPipedInput(pipedData);

                lastResult = await ExecuteCommandAsync(commands[i], pipeContext);

                // Last command outputs to terminal
                if (i == commands.Length - 1)
                {
                    if (stdoutBuffer.HasContent)
                        context.Stdout.Write(stdoutBuffer.Content);
                    if (stderrBuffer.HasContent)
                        context.Stderr.Write(stderrBuffer.Content);
                }
                else if (!lastResult.Success)
                {
                    context.Stderr.WriteLine($"Pipe broken: {lastResult.Message}");
                    return lastResult;
                }
                else
                {
                    pipedData = stdoutBuffer.Content;
                }
            }

            return lastResult;
        }

        private CommandResult ProcessPipeline(string commandLine, CommandContext context)
        {
            var commands = commandLine.Split('|').Select(c => c.Trim()).ToArray();

            if (commands.Length == 1)
                return ExecuteCommand(commands[0], context);

            string pipedData = null;
            CommandResult lastResult = CommandResult.Ok();

            for (int i = 0; i < commands.Length; i++)
            {
                var (pipeContext, stdoutBuffer, stderrBuffer) = context.CreateBuffered();
                pipeContext = pipeContext.WithPipedInput(pipedData);

                lastResult = ExecuteCommand(commands[i], pipeContext);

                if (i == commands.Length - 1)
                {
                    if (stdoutBuffer.HasContent)
                        context.Stdout.Write(stdoutBuffer.Content);
                    if (stderrBuffer.HasContent)
                        context.Stderr.Write(stderrBuffer.Content);
                }
                else if (!lastResult.Success)
                {
                    context.Stderr.WriteLine($"Pipe broken: {lastResult.Message}");
                    return lastResult;
                }
                else
                {
                    pipedData = stdoutBuffer.Content;
                }
            }

            return lastResult;
        }

        private async Task<CommandResult> ExecuteCommandAsync(string commandText, CommandContext context)
        {
            var (commandName, args) = ParseCommand(commandText);

            if (!commands.TryGetValue(commandName, out var command))
            {
                context.Stderr.WriteLine($"Command not found: {commandName}");
                return CommandResult.Error($"Command not found: {commandName}");
            }

            try
            {
                // Check if it's async
                if (command is IAsyncCommand asyncCommand)
                {
                    cancellationSource = CancellationTokenSource.CreateLinkedTokenSource(context.CancellationToken);

                    try
                    {
                        var linkedContext = new CommandContext(
                            context.Stdout,
                            context.Stderr,
                            cancellationSource.Token,
                            context.Progress,
                            context.PipedInput,
                            context.IsInteractive,
                            context.Environment
                        );

                        return await asyncCommand.ExecuteAsync(args, linkedContext);
                    }
                    finally
                    {
                        cancellationSource?.Dispose();
                        cancellationSource = null;
                    }
                }
                else
                {
                    // For sync commands, run them directly (they're fast)
                    var result = command.Execute(args, context);

                    // Track interactive commands
                    if (command is IInteractiveCommand interactive && interactive.IsWaitingForInput)
                    {
                        interactiveCommand = interactive;
                    }

                    return result;
                }
            }
            catch (OperationCanceledException)
            {
                return CommandResult.Error("Cancelled");
            }
            catch (System.Exception ex)
            {
                context.Stderr.WriteLine($"Error: {ex.Message}");
                return CommandResult.FromException(ex);
            }
        }

        private CommandResult ExecuteCommand(string commandText, CommandContext context)
        {
            var (commandName, args) = ParseCommand(commandText);

            if (!commands.TryGetValue(commandName, out var command))
            {
                context.Stderr.WriteLine($"Command not found: {commandName}");
                return CommandResult.Error($"Command not found: {commandName}");
            }

            try
            {
                var result = command.Execute(args, context);

                // Track interactive commands
                if (command is IInteractiveCommand interactive && interactive.IsWaitingForInput)
                {
                    interactiveCommand = interactive;
                }

                return result;
            }
            catch (System.Exception ex)
            {
                context.Stderr.WriteLine($"Error: {ex.Message}");
                return CommandResult.FromException(ex);
            }
        }

        private (string commandName, string[] args) ParseCommand(string commandText)
        {
            var parts = commandText.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0)
                return ("", new string[0]);

            string commandName = parts[0].ToLower();

            // Handle aliases
            if (aliases.TryGetValue(commandName, out var aliasCommand))
            {
                var aliasParts = aliasCommand.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
                commandName = aliasParts[0].ToLower();

                var aliasArgs = aliasParts.Skip(1).ToArray();
                var commandArgs = parts.Skip(1).ToArray();
                var mergedArgs = aliasArgs.Concat(commandArgs).ToArray();

                return (commandName, mergedArgs);
            }

            var args = parts.Skip(1).ToArray();
            return (commandName, args);
        }

        private List<(string command, bool requiresSuccess, bool requiresFailure)> SplitByConditionalOperators(string input)
        {
            var segments = new List<(string, bool, bool)>();
            var current = new System.Text.StringBuilder();
            bool nextRequiresSuccess = false;
            bool nextRequiresFailure = false;

            for (int i = 0; i < input.Length; i++)
            {
                if (i < input.Length - 1)
                {
                    if (input[i] == '&' && input[i + 1] == '&')
                    {
                        segments.Add((current.ToString().Trim(), nextRequiresSuccess, nextRequiresFailure));
                        current.Clear();
                        nextRequiresSuccess = true;
                        nextRequiresFailure = false;
                        i++;
                        continue;
                    }
                    else if (input[i] == '|' && input[i + 1] == '|')
                    {
                        segments.Add((current.ToString().Trim(), nextRequiresSuccess, nextRequiresFailure));
                        current.Clear();
                        nextRequiresSuccess = false;
                        nextRequiresFailure = true;
                        i++;
                        continue;
                    }
                }
                current.Append(input[i]);
            }

            if (current.Length > 0)
            {
                segments.Add((current.ToString().Trim(), nextRequiresSuccess, nextRequiresFailure));
            }

            return segments;
        }

        public void CancelCurrentCommand()
        {
            cancellationSource?.Cancel();
        }
    }
}


