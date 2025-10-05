using UnityEngine;
using SampleOS.Core.Devices;
using SampleOS.Core.Session;
using SampleOS.Core.World;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class SshCommand : CommandBase
  {
    private CommandProcessor processor;
    private PlayerSession session;
    private GameWorld gameWorld;

    public override string Name => "ssh";
    public override string Description => "Connect to remote systems via SSH";
    public override string Usage => "ssh [user@]hostname [-p password]";

    public SshCommand(CommandProcessor processor, PlayerSession session, GameWorld gameWorld)
    {
      this.processor = processor;
      this.session = session;
      this.gameWorld = gameWorld;
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

      // Find the target device
      var device = gameWorld.FindDevice(hostname);
      if (device == null)
      {
        WriteError(context, $"ssh: Could not resolve hostname {hostname}");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'nmap <ip-range>' to discover devices on the current network");
        WriteOutput(context, "  - Check if you're connected to the correct network with 'networks'");
        return CommandResult.Error("Host not found");
      }

      // Check if device is online
      if (!device.IsOnline)
      {
        WriteError(context, $"ssh: {hostname} is offline or unreachable");
        return CommandResult.Error("Host offline");
      }

      // Check if SSH is available on this device
      var availableMethods = device.GetAvailableInteractionMethods();
      if (!availableMethods.Contains("ssh"))
      {
        WriteError(context, $"ssh: {hostname} does not support SSH connections");
        WriteOutput(context, $"Available methods: {string.Join(", ", availableMethods)}");
        return CommandResult.Error("SSH not available");
      }

      // Check for open SSH port (port 22)
      var sshSoftware = device.InstalledSoftware.Find(s =>
        s.Name.ToLower().Contains("ssh") || s.ListeningPorts.Contains(22));

      if (sshSoftware == null || !sshSoftware.ListeningPorts.Contains(22))
      {
        WriteError(context, $"ssh: connect to host {hostname} port 22: Connection refused");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - The SSH service may not be running");
        WriteOutput(context, "  - Use 'nmap <ip>' to check which ports are open");
        return CommandResult.Error("SSH port closed");
      }

      // Determine username if not specified
      if (string.IsNullOrEmpty(username))
      {
        // Try to find default username from credentials or use generic default
        var defaultCred = device.Credentials.Find(c => c.IsDefault);
        username = defaultCred?.Username ?? "user";
        WriteOutput(context, $"Connecting to {hostname} as {username}...");
      }
      else
      {
        WriteOutput(context, $"Connecting to {username}@{hostname}...");
      }

      // Simulate connection delay
      System.Threading.Thread.Sleep(500);

      // Try authentication
      if (!device.Authenticate(username, password))
      {
        WriteError(context, $"{username}@{hostname}: Permission denied (publickey,password).");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Try different usernames (admin, root, user)");
        WriteOutput(context, "  - Use credentials found in compromised systems");
        WriteOutput(context, "  - Check your credential database with 'creds list'");
        return CommandResult.Error("Authentication failed");
      }

      // Connection successful - create remote connection
      var connectionResult = session.ConnectToRemote(device, username, password);
      if (connectionResult.IsFailure)
      {
        WriteError(context, $"Failed to establish connection: {connectionResult.ErrorMessage}");
        return CommandResult.Error("Connection failed");
      }

      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
      WriteOutput(context, $"✓ Successfully connected to {hostname}");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Update command processor context
      processor.SetCurrentDevice(device);

      // Show welcome message
      WriteOutput(context, $"Welcome to {device.Hostname}");
      WriteOutput(context, $"Type: {device.DeviceType.Name}");
      WriteOutput(context, $"OS: {DetermineOSType(device)}");
      WriteOutput(context, "");

      // Check access level and show warnings
      var hasRootCred = device.Credentials.Exists(c =>
        c.Username == "root" && c.Username == username);

      if (hasRootCred)
      {
        context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f));
        WriteOutput(context, "# ROOT ACCESS GRANTED #");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");
      }
      else
      {
        WriteOutput(context, "Limited access - explore and escalate privileges.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'scan-vulns' to find exploitable vulnerabilities");
        WriteOutput(context, "  - Check for sensitive files with 'find' and 'grep'");
        WriteOutput(context, "  - Look for credentials in user home directories");
        WriteOutput(context, "");
      }

      // Show security warnings if device has high security
      if (device.SecurityLevel == SampleOS.Core.Networking.SecurityLevel.High)
      {
        context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
        WriteOutput(context, "⚠ Warning: This system has enhanced security monitoring");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");
      }

      return CommandResult.Ok();
    }

    private string DetermineOSType(Device device)
    {
      // Determine OS based on device category
      switch (device.DeviceType.Category)
      {
        case DeviceCategory.Workstation:
        case DeviceCategory.Server:
          return "Linux";
        case DeviceCategory.Router:
          return "RouterOS";
        case DeviceCategory.IoTDevice:
        case DeviceCategory.EmbeddedSystem:
          return "Embedded Linux";
        case DeviceCategory.MobileDevice:
          return "Android";
        case DeviceCategory.IndustrialControl:
          return "Industrial Control System";
        default:
          return "Unix-like";
      }
    }
  }
}
