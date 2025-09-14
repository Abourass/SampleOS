using System.Collections.Generic;

public class CommandProcessor
{
  private Dictionary<string, ICommand> commands = new Dictionary<string, ICommand>();
  private VirtualFileSystem fileSystem;
  private VirtualNetwork network;

  public CommandProcessor()
  {
    fileSystem = new VirtualFileSystem();
    network = new VirtualNetwork();

    // Register commands
    RegisterCommand(new LsCommand(fileSystem));
    RegisterCommand(new CdCommand(fileSystem));
    RegisterCommand(new MkdirCommand(fileSystem));
    RegisterCommand(new TouchCommand(fileSystem));
    RegisterCommand(new CatCommand(fileSystem));
    RegisterCommand(new SshCommand(network, this));
    RegisterCommand(new HelpCommand(commands));
    RegisterCommand(new ClearCommand());
  }

  public void ProcessCommand(string input, ITerminalOutput output)
  {
    string[] parts = input.Trim().Split(' ');
    if (parts.Length == 0) return;

    string commandName = parts[0].ToLower();
    string[] args = new string[parts.Length - 1];
    System.Array.Copy(parts, 1, args, 0, parts.Length - 1);

    if (commands.TryGetValue(commandName, out ICommand command))
    {
      command.Execute(args, output);
    }
    else
    {
      output.AppendText($"Command not found: {commandName}\n");
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
}
