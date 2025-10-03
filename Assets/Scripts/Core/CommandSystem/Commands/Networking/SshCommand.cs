using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class SshCommand : CommandBase
  {
    private CommandProcessor processor;

    public override string Name => "ssh";
    public override string Description => "Connect to remote systems via SSH";
    public override string Usage => "ssh [user@]hostname [-p password]";

    public SshCommand(CommandProcessor processor)
    {
      this.processor = processor;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length == 0)
      {
        WriteError(context, "Usage: ssh [user@]hostname [-p password]");
        return CommandResult.Error("Missing hostname");
      }

      // Parse arguments
      string target = args[0];
      string username = null;
      string hostname = null;
      string password = null;

      // Parse user@hostname format
      if (target.Contains("@"))
      {
        string[] parts = target.Split('@');
        username = parts[0];
        hostname = parts[1];
      }
      else
      {
        hostname = target;
      }

      // Parse password option
      for (int i = 1; i < args.Length; i++)
      {
        if (args[i] == "-p" && i + 1 < args.Length)
        {
          password = args[++i];
        }
      }

      // Try to connect
      var system = context.Network?.GetSystemByHostname(hostname);
      if (system == null)
      {
        WriteError(context, $"ssh: Could not resolve hostname {hostname}");
        return CommandResult.Error("Host not found");
      }

      // Use default username if not specified
      if (string.IsNullOrEmpty(username))
      {
        username = system.Username;
        WriteOutput(context, $"Connecting to {hostname} as {username}...");
      }
      else
      {
        WriteOutput(context, $"Connecting to {username}@{hostname}...");
      }

      // Simulate connection delay
      System.Threading.Thread.Sleep(500);

      // Check for SSH port
      if (!system.GetOpenPorts().Contains(22))
      {
        WriteError(context, $"ssh: connect to host {hostname} port 22: Connection refused");
        return CommandResult.Error("SSH port closed");
      }

      // Try authentication
      if (system.Authenticate(username, password))
      {
        // Connection successful
        context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
        WriteOutput(context, $"Successfully connected to {hostname}");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");

        // Switch context
        processor.SetCurrentSystem(system);
        processor.SetFileSystem(system.FileSystem);

        // Show welcome message
        WriteOutput(context, $"Welcome to {system.Name}");
        WriteOutput(context, $"Type: {system.Type}");
        WriteOutput(context, "");

        // Check permissions
        if (system.HasRootAccess)
        {
          context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f));
          WriteOutput(context, "# ROOT ACCESS GRANTED #");
          context.Stdout.SetColor(Color.white);
          WriteOutput(context, "");
        }
        else if (!system.HasPermission("/root"))
        {
          WriteOutput(context, "Limited access - explore and escalate privileges.");
          WriteOutput(context, "");
        }

        return CommandResult.Ok();
      }
      else
      {
        WriteError(context, $"{username}@{hostname}: Permission denied (publickey,password).");
        return CommandResult.Error("Authentication failed");
      }
    }
  }
}
