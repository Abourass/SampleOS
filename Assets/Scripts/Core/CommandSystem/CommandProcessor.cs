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
  public bool LastCommandSucceeded { get; private set; } = true;

  public CommandProcessor()
  {
    fileSystem = new VirtualFileSystem();
    network = new VirtualNetwork();
    vulnerabilityInventory = new PlayerVulnerabilityInventory();
    progressManager = new PlayerProgressManager(network);

    // Register commands
    RegisterCommand(new LsCommand(fileSystem));
    RegisterCommand(new CdCommand(fileSystem));
    RegisterCommand(new MkdirCommand(fileSystem));
    RegisterCommand(new TouchCommand(fileSystem));
    RegisterCommand(new CatCommand(fileSystem));
    RegisterCommand(new SshCommand(network, this));
    RegisterCommand(new NetstatCommand(network));
    RegisterCommand(new NmapCommand(network));
    RegisterCommand(new PsCommand(this));
    RegisterCommand(new VulnScanCommand(network, vulnerabilityInventory));
    RegisterCommand(new VulnsCommand(vulnerabilityInventory));
    RegisterCommand(new ExploitCommand(network, this, vulnerabilityInventory, progressManager));
    RegisterCommand(new HelpCommand(commands));
    RegisterCommand(new ClearCommand());
    RegisterCommand(new AliasCommand(aliases));
    RegisterCommand(new OwnedCommand(progressManager, network));
  }

  public void ProcessCommand(string input, ITerminalOutput output)
  {
    string[] parts = input.Trim().Split(' ');
    if (parts.Length == 0) return;

    string commandName = parts[0].ToLower();
    string[] args = new string[parts.Length - 1];
    System.Array.Copy(parts, 1, args, 0, parts.Length - 1);

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
        command.Execute(args, output);
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

  public RemoteSystem GetCurrentSystem()
  {
    return currentSystem;
  }

  public void SetCurrentSystem(RemoteSystem system)
  {
    currentSystem = system;
  }
}
