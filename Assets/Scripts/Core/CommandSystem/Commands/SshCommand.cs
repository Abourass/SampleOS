using System;

public class SshCommand : ICommand
{
  private VirtualNetwork network;
  private CommandProcessor commandProcessor;

  public string Name => "ssh";
  public string Description => "Connect to a remote system";
  public string Usage => "ssh [username@]hostname";

  public SshCommand(VirtualNetwork network, CommandProcessor commandProcessor)
  {
    this.network = network;
    this.commandProcessor = commandProcessor;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    if (args.Length == 0)
    {
      output.AppendText($"Usage: {Usage}\n");
      return;
    }

    string target = args[0];
    string username = "user";
    string host = target;

    // Parse username@host format
    if (target.Contains("@"))
    {
      string[] parts = target.Split('@');
      username = parts[0];
      host = parts[1];
    }

    // For now, no password handling
    string password = "";

    output.AppendText($"Connecting to {host} as {username}...\n");

    var result = network.Connect(host, username, password);

    if (result.IsSuccess)
    {
      RemoteSystem system = result.Data;
      output.AppendText($"Connected to {system.Name}\n");

      // Switch to the remote system's filesystem
      commandProcessor.SetFileSystem(system.FileSystem);
    }
    else
    {
      output.AppendText($"Connection failed: {result.ErrorMessage}\n");
    }
  }
}
