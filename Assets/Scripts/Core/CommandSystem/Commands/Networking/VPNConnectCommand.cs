using Core.Networking.Discovery;
using UnityEngine;
using System.Threading;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class VpnConnectCommand : CommandBase
  {
    private VirtualCity city;
    private PlayerCredentialManager credentialManager;
    private CommandProcessor processor;

    public override string Name => "vpn-connect";
    public override string Description => "Connect to a network via VPN";
    public override string Usage => "vpn-connect <network-id> [--config <config-file>]";

    public VpnConnectCommand(VirtualCity city, PlayerCredentialManager credentialManager, CommandProcessor processor)
    {
      this.city = city;
      this.credentialManager = credentialManager;
      this.processor = processor;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length < 1)
      {
        WriteError(context, $"Usage: {Usage}");
        return CommandResult.Error("Missing network ID");
      }

      string networkId = args[0];
      string configFile = null;

      // Parse config file option
      for (int i = 1; i < args.Length; i++)
      {
        if (args[i] == "--config" && i + 1 < args.Length)
        {
          configFile = args[i + 1];
          break;
        }
      }

      WriteOutput(context, $"Attempting VPN connection to network '{networkId}'...");
      WriteOutput(context, "");

      // Get credentials for this network from the credential manager
      var credentials = credentialManager.GetCredentialsForNetwork(networkId);
      if (credentials == null)
      {
        WriteError(context, "Error: No VPN credentials found for this network.");
        WriteError(context, "Try searching compromised systems for configuration files or emails.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'cat' to read files on compromised systems");
        WriteOutput(context, "  - Look for .ovpn files in /etc/openvpn/ or user home directories");
        WriteOutput(context, "  - Check email folders for VPN setup instructions");
        WriteOutput(context, "  - Search documents for network credentials");
        return CommandResult.Error("No VPN credentials found");
      }

      // Create a valid NetworkCredentials object with the VPN credentials
      var networkCredentials = new NetworkCredentials(networkId)
      {
        VPNCredentials = new VPNCredential
        {
          NetworkId = networkId,
          NetworkName = credentials.NetworkName,
          Username = credentials.Username,
          Password = credentials.Password,
          ServerAddress = credentials.ServerAddress,
          Protocol = credentials.Protocol
        }
      };

      // Simulate VPN connection process with visual feedback
      context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f)); // Light blue
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                  VPN CONNECTION                            ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      WriteOutput(context, $"Server:   {credentials.ServerAddress}");
      WriteOutput(context, $"Protocol: {credentials.Protocol}");
      WriteOutput(context, $"Username: {credentials.Username}");
      WriteOutput(context, "");

      // Simulate connection stages
      WriteOutput(context, $"Connecting to VPN server {credentials.ServerAddress}...");
      Thread.Sleep(500);

      WriteOutput(context, "Establishing encrypted tunnel...");
      Thread.Sleep(400);

      WriteOutput(context, $"Authenticating with username '{credentials.Username}'...");
      Thread.Sleep(600);

      WriteOutput(context, "Negotiating connection parameters...");
      Thread.Sleep(300);

      // Attempt to connect to the network using the city's ConnectToNetwork method
      var connectionResult = city.ConnectToNetwork(networkId, networkCredentials);

      if (connectionResult.IsSuccess)
      {
        WriteOutput(context, "");
        context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
        WriteOutput(context, "✓ VPN connection established!");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");

        var newNetwork = connectionResult.Data;

        WriteOutput(context, $"Connected to network: {newNetwork.Metadata.Name}");
        WriteOutput(context, $"Organization: {newNetwork.Metadata.Organization}");
        WriteOutput(context, $"Network Type: {newNetwork.Metadata.Type}");
        WriteOutput(context, $"IP Range: {newNetwork.Metadata.IPRange}");
        WriteOutput(context, "");

        // Show security profile information
        if (newNetwork.SecurityProfile != null)
        {
          var securityLevel = newNetwork.SecurityProfile.DefaultSecurityLevel;
          Color secColor = securityLevel == SecurityLevel.High ? new Color(1f, 0.3f, 0.3f) :
                          securityLevel == SecurityLevel.Medium ? new Color(1f, 0.7f, 0.2f) :
                          new Color(0.3f, 1f, 0.3f);

          context.Stdout.SetColor(secColor);
          WriteOutput(context, $"Security Level: {securityLevel}");
          context.Stdout.SetColor(Color.white);

          // if (newNetwork.SecurityProfile.HasIDS)
          // {
          //   context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
          //   WriteOutput(context, "⚠ Warning: Intrusion Detection System detected");
          //   context.Stdout.SetColor(Color.white);
          // }

          WriteOutput(context, "");
        }

        WriteOutput(context, "Your IP address is now in range: " + newNetwork.Metadata.IPRange);
        WriteOutput(context, "");
        WriteOutput(context, "Use 'nmap <ip-range>' to discover systems on this network.");
        WriteOutput(context, "Use 'networks' to view network information.");

        // Update the command processor's network reference
        processor.UpdateCurrentNetwork();

        return CommandResult.Ok();
      }
      else
      {
        WriteOutput(context, "");
        context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f)); // Red
        WriteOutput(context, "✗ VPN connection failed");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");

        WriteError(context, $"Error: {connectionResult.ErrorMessage}");
        WriteOutput(context, "");

        // Provide helpful troubleshooting information
        WriteOutput(context, "Possible reasons:");
        WriteOutput(context, "  - Network has not been discovered yet");
        WriteOutput(context, "  - Invalid or expired credentials");
        WriteOutput(context, "  - Network requires additional access requirements");
        WriteOutput(context, "  - VPN server is unreachable");
        WriteOutput(context, "");
        WriteOutput(context, "Try:");
        WriteOutput(context, "  - Use 'networks --discovered' to see available networks");
        WriteOutput(context, "  - Scan more systems for updated credentials");
        WriteOutput(context, "  - Check if additional systems need to be compromised");

        return CommandResult.Error(connectionResult.ErrorMessage);
      }
    }
  }
}
