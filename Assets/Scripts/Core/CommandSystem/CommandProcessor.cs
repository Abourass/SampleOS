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
using SampleOS.Core.Networking;
using SampleOS.Core.World;
using SampleOS.Core.Devices;
using SampleOS.Core.Player;
using SampleOS.Core.Session;

namespace SampleOS.Core.CommandSystem
{
    public class CommandProcessor : MonoBehaviour
    {
        private Dictionary<string, ICommand> commands = new();
        private Dictionary<string, string> aliases = new();

        private PlayerDevice playerDevice;           // The player's laptop/phone
        private Device currentDevice;                 // Currently connected device (player's or remote)
        private CommandEnvironment environment;

        private IInteractiveCommand interactiveCommand;
        private CancellationTokenSource cancellationSource;

        // Dependencies
        private GameWorld gameWorld;
        private PlayerSession session;
        private PlayerVulnerabilityInventory vulnerabilityInventory;
        private PlayerProgressManager progressManager;
        private City city;
        private PlayerCredentialManager credentialManager;

        public bool IsWaitingForInput => interactiveCommand?.IsWaitingForInput ?? false;
        public bool IsExecuting => cancellationSource != null;
        public bool LastCommandSucceeded { get; private set; } = true;

        public CommandProcessor()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Initialize game world first
            gameWorld = new GameWorld();
            session = new PlayerSession();

            // Initialize world and network
            city = new City("metropolis", "Metropolis");
            environment = new CommandEnvironment();
            gameWorld.RegisterCity(city);

            environment = new CommandEnvironment();

            // Create player's device
            playerDevice = DeviceFactory.CreatePlayerDevice(
                "player_laptop",
                "localhost",
                PlayerDevice.DeviceForm.Laptop
            );

            gameWorld.RegisterDevice(playerDevice);

            // Start on player's device
            currentDevice = playerDevice;

            // Initialize player systems
            credentialManager = new PlayerCredentialManager();
            vulnerabilityInventory = new PlayerVulnerabilityInventory();
            progressManager = new PlayerProgressManager(city.CurrentNetwork);

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
            Register(new QuitCommand(progressManager));

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
            Register(new NetworksCommand(city));
            Register(new SshCommand(this, session, gameWorld));
            Register(new NmapCommand());
            Register(new OwnedCommand(progressManager));
            Register(new VpnConnectCommand(city, credentialManager, this, session, gameWorld));

            // Vulnerabilities
            Register(new ExploitCommand(this, vulnerabilityInventory, progressManager));
            Register(new VulnScanCommand(vulnerabilityInventory));
            Register(new VulnsCommand(vulnerabilityInventory));
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
                stdout, stderr,
                currentDevice,          // Current device (player's or remote)
                city.CurrentNetwork, // Current network
                cancellationToken,
                new Progress<CommandProgress>(p =>
                    Debug.Log($"Progress: {p.Percentage:P0} - {p.Message}")),
                null, false, environment
            );
        }

        private async Task<CommandResult> ProcessWithContextAsync(string input, CommandContext context)
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
                            context.Stdout, context.Stderr,
                            context.CurrentDevice, context.CurrentNetwork,
                            cancellationSource.Token, context.Progress,
                            context.PipedInput, context.IsInteractive, context.Environment);

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

        /// <summary>
        /// Sets the current device context (for SSH, exploits, etc.)
        /// </summary>
        public void SetCurrentDevice(Device device)
        {
            if (device == null)
            {
                Debug.LogWarning("Attempted to set null device");
                return;
            }

            currentDevice = device;

            Debug.Log($"Switched to device: {device.Hostname} ({device.IPAddress})");
        }

        /// <summary>
        /// Gets the current device
        /// </summary>
        public Device GetCurrentDevice()
        {
            return currentDevice;
        }

        /// <summary>
        /// Gets the player's personal device
        /// </summary>
        public PlayerDevice GetPlayerDevice()
        {
            return playerDevice;
        }

        /// <summary>
        /// Gets the current network
        /// </summary>
        public VirtualNetwork GetCurrentNetwork()
        {
            return city.CurrentNetwork;
        }

        /// <summary>
        /// Returns to the player's device
        /// </summary>
        public void ReturnToPlayerDevice()
        {
            SetCurrentDevice(playerDevice);
            Debug.Log("Returned to local device");
        }

        /// <summary>
        /// Checks if currently on a remote device
        /// </summary>
        public bool IsOnRemoteDevice()
        {
            return currentDevice != playerDevice;
        }

        /// <summary>
        /// Gets the city context
        /// </summary>
        public City GetCity()
        {
            return city;
        }

        /// <summary>
        /// Gets credential manager
        /// </summary>
        public PlayerCredentialManager GetCredentialManager()
        {
            return credentialManager;
        }

        /// <summary>
        /// Gets vulnerability inventory
        /// </summary>
        public PlayerVulnerabilityInventory GetVulnerabilityInventory()
        {
            return vulnerabilityInventory;
        }

        /// <summary>
        /// Gets progress manager
        /// </summary>
        public PlayerProgressManager GetProgressManager()
        {
            return progressManager;
        }
    }
}


