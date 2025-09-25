using System.Collections.Generic;

public class CommandProcessor
{
  private Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
  private Dictionary<string, string> aliases = new Dictionary<string, string>();
  private VirtualFileSystem fileSystem;
  private VirtualNetwork network;
  private RemoteSystem currentSystem;
  private PlayerVulnerabilityInventory vulnerabilityInventory;
  private PlayerProgressManager progressManager;
  private VirtualCity city;
  private PlayerCredentialManager credentialManager;
  private IInteractiveCommand pendingCommand;
  /// <summary>
  /// Whether there's a command waiting for user input
  /// </summary>
  public bool IsWaitingForCommandInput => pendingCommand != null && pendingCommand.IsWaitingForInput;

  public bool LastCommandSucceeded { get; private set; } = true;

  public CommandProcessor()
  {
    fileSystem = new VirtualFileSystem();
    city = new VirtualCity();
    network = city.CurrentNetwork;
    credentialManager = new PlayerCredentialManager();
    vulnerabilityInventory = new PlayerVulnerabilityInventory();
    progressManager = new PlayerProgressManager(network);

    // Register commands
    RegisterCommand(new LsCommand(fileSystem));
    RegisterCommand(new CdCommand(fileSystem));
    RegisterCommand(new MkdirCommand(fileSystem));
    RegisterCommand(new TouchCommand(fileSystem));
    RegisterCommand(new CatCommand(fileSystem));
    RegisterCommand(new GrepCommand(fileSystem));
    RegisterCommand(new WcCommand(fileSystem));
    RegisterCommand(new GumCommand(fileSystem));
    // Networking Commands
    RegisterCommand(new SshCommand(network, this));
    RegisterCommand(new NetstatCommand(network));
    RegisterCommand(new NmapCommand(network));
    RegisterCommand(new NetworksCommand(city));
    RegisterCommand(new VpnConnectCommand(city, credentialManager));

    RegisterCommand(new PsCommand(this));
    RegisterCommand(new VulnScanCommand(network, vulnerabilityInventory));
    RegisterCommand(new VulnsCommand(vulnerabilityInventory));
    RegisterCommand(new ExploitCommand(network, this, vulnerabilityInventory, progressManager));
    RegisterCommand(new HelpCommand(commands));
    RegisterCommand(new ClearCommand());
    RegisterCommand(new AliasCommand(aliases));
    RegisterCommand(new OwnedCommand(progressManager, network));

    RegisterCommand(new QuitCommand(progressManager));
  }

  public void ProcessCommand(string input, ITerminalOutput output)
  {
      // If we're waiting for input for an interactive command, route input there
      if (IsWaitingForCommandInput)
      {
          pendingCommand.ProcessInput(input, output);

          // If command is done waiting for input, clear pending state
          if (!pendingCommand.IsWaitingForInput)
          {
              pendingCommand = null;
          }

          LastCommandSucceeded = true;
          return;
      }

      // Split the input into parts based on conditional operators
      string[] parts = input.Split(new[] { "&&", "||", "and", "or" }, System.StringSplitOptions.None);
      string[] operators = ExtractOperators(input);

      for (int i = 0; i < parts.Length; i++)
      {
          string commandPart = parts[i].Trim();

          // Skip execution based on the previous command's success/failure
          if (i > 0)
          {
              string op = operators[i - 1];
              if ((op == "&&" || op == "and") && !LastCommandSucceeded)
              {
                  continue; // Skip this command if the last one failed
              }
              if ((op == "||" || op == "or") && LastCommandSucceeded)
              {
                  continue; // Skip this command if the last one succeeded
              }
          }

         // Process the current command (which may contain pipes)
          ProcessPipedCommands(commandPart, output);
      }
  }
  
  private void ProcessPipedCommands(string commandLine, ITerminalOutput output)
    {
        // Split by pipe symbol
        string[] pipeCommands = commandLine.Split('|');
        
        // If there are no pipes, execute normally
        if (pipeCommands.Length == 1)
        {
            ExecuteSingleCommand(pipeCommands[0].Trim(), null, output);
            return;
        }

        // There are pipes, execute commands in sequence
        string pipeInput = null;
        
        for (int i = 0; i < pipeCommands.Length; i++)
        {
            string cmdText = pipeCommands[i].Trim();
            
            // Last command outputs to terminal
            if (i == pipeCommands.Length - 1)
            {
                ExecuteSingleCommand(cmdText, pipeInput, output);
            }
            // Intermediate commands have output captured
            else
            {
                var capturedOutput = new CapturedOutput();
                ExecuteSingleCommand(cmdText, pipeInput, capturedOutput);
                pipeInput = capturedOutput.CapturedText;
            }
        }
    }

    private void ExecuteSingleCommand(string commandText, string inputText, ITerminalOutput output)
    {
        string[] commandParts = commandText.Split(' ');
        if (commandParts.Length == 0) return;

        string commandName = commandParts[0].ToLower();
        string[] args = new string[commandParts.Length - 1];
        if (commandParts.Length > 1)
        {
            System.Array.Copy(commandParts, 1, args, 0, commandParts.Length - 1);
        }

        // Check if the command is an alias
        if (aliases.TryGetValue(commandName, out string aliasCommand))
        {
            // Split the alias command into parts
            string[] aliasParts = aliasCommand.Split(' ');

            // Replace the command with the alias target
            commandName = aliasParts[0].ToLower();

            // Combine alias args with original args
            if (aliasParts.Length > 1)
            {
                string[] aliasArgs = new string[aliasParts.Length - 1];
                System.Array.Copy(aliasParts, 1, aliasArgs, 0, aliasParts.Length - 1);

                // Combine the alias args and the original args
                string[] combinedArgs = new string[aliasArgs.Length + args.Length];
                aliasArgs.CopyTo(combinedArgs, 0);
                args.CopyTo(combinedArgs, aliasArgs.Length);

                args = combinedArgs;
            }
        }

        if (commands.TryGetValue(commandName, out ICommand command))
        {
            try
            {
                // If we have piped input and the command supports it
                if (inputText != null && command is IPipeableCommand pipeableCommand)
                {
                    pipeableCommand.ExecuteWithInput(args, output, inputText);
                }
                else if (inputText != null)
                {
                    // Command doesn't support piped input
                    output.AppendText($"Error: Command '{commandName}' doesn't support piped input\n");
                    LastCommandSucceeded = false;
                    return;
                }
                else
                {
                    // Normal command execution
                    command.Execute(args, output);
                }

                // Check if this is an interactive command now waiting for input
                if (command is IInteractiveCommand interactiveCommand && interactiveCommand.IsWaitingForInput)
                {
                    pendingCommand = interactiveCommand;
                }

                LastCommandSucceeded = true;
            }
            catch (System.Exception ex)
            {
                output.AppendText($"Command error: {ex.Message}\n");
                LastCommandSucceeded = false;
            }
        }
        else
        {
            output.AppendText($"Command not found: {commandName}\n");
            LastCommandSucceeded = false;
        }
    }

  private string[] ExtractOperators(string input)
  {
    // Extract the operators (&&, ||, and, or) from the input string
    List<string> operators = new List<string>();
    int index = 0;

    while (index < input.Length)
    {
      if (input.Substring(index).StartsWith("&&"))
      {
        operators.Add("&&");
        index += 2;
      }
      else if (input.Substring(index).StartsWith("||"))
      {
        operators.Add("||");
        index += 2;
      }
      else if (input.Substring(index).StartsWith("and"))
      {
        operators.Add("and");
        index += 3;
      }
      else if (input.Substring(index).StartsWith("or"))
      {
        operators.Add("or");
        index += 2;
      }
      else
      {
        index++;
      }
    }

    return operators.ToArray();
  }

  private void RegisterCommand(ICommand command)
  {
    commands[command.Name] = command;
  }

  public string GetCurrentPath()
  {
    return fileSystem.CurrentPath;
  }

  public void SetFileSystem(VirtualFileSystem newFileSystem)
  {
    fileSystem = newFileSystem;
    // Update commands with new file system
    foreach (var entry in commands)
    {
      if (entry.Value is IFileSystemCommand fsCommand)
      {
        fsCommand.SetFileSystem(fileSystem);
      }
    }
  }

  public VirtualFileSystem GetFileSystem()
  {
      return fileSystem;
  }

  public RemoteSystem GetCurrentSystem()
  {
    return currentSystem;
  }

  public void SetCurrentSystem(RemoteSystem system)
  {
    currentSystem = system;
  }

  public void UpdateCurrentNetwork()
  {
    network = city.CurrentNetwork;
  }
}
