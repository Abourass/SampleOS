using Core.Networking.Discovery;
using UnityEngine;
using System.Threading;
using SampleOS.Core.World;
using SampleOS.Core.Session;
using Core.Networking.Connections;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class VpnConnectCommand : CommandBase
  {
    private City city;
    private PlayerCredentialManager credentialManager;
    private CommandProcessor processor;
    private PlayerSession session;
    private GameWorld gameWorld;

    public override string Name => "vpn-connect";
    public override string Description => "Connect to a network via VPN";
    public override string Usage => "vpn-connect <network-id> [--config <config-file>]";

    public VpnConnectCommand(
      City city,
      PlayerCredentialManager credentialManager,
      CommandProcessor processor,
      PlayerSession session,
      GameWorld gameWorld)
    {
      this.city = city;
      this.credentialManager = credentialManager;
      this.processor = processor;
      this.session = session;
      this.gameWorld = gameWorld;
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

      // Check if network is discovered
      if (!city.DiscoveryManager.IsNetworkDiscovered(networkId))
      {
        WriteError(context, $"Network '{networkId}' has not been discovered yet.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'networks --discovered' to see available networks");
        WriteOutput(context, "  - Compromise systems to find clues about new networks");
        WriteOutput(context, "  - Scan files for VPN configurations and network references");
        return CommandResult.Error("Network not discovered");
      }

      // Get the network
      var network = city.GetNetwork(networkId);
      if (network == null)
      {
        WriteError(context, $"Network '{networkId}' not found in this city.");
        return CommandResult.Error("Network not found");
      }

      // Get VPN credentials
      var vpnCredentials = credentialManager.GetCredentialsForNetwork(networkId);
      if (vpnCredentials == null)
      {
        WriteError(context, "Error: No VPN credentials found for this network.");
        WriteOutput(context, "");
        WriteOutput(context, "You need to find VPN credentials first:");
        WriteOutput(context, "  - Search compromised systems for .ovpn files");
        WriteOutput(context, "  - Look in /etc/openvpn/ or user home directories");
        WriteOutput(context, "  - Check email folders for VPN setup instructions");
        WriteOutput(context, "  - Use 'scan-creds' on compromised systems");
        WriteOutput(context, "");
        WriteOutput(context, "Once you have credentials, they'll be stored automatically.");
        return CommandResult.Error("No VPN credentials found");
      }

      // Create network credentials object
      var networkCredentials = new NetworkCredentials(networkId)
      {
        VPNCredentials = vpnCredentials
      };

      // Display connection UI
      context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f)); // Light blue
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                  VPN CONNECTION                            ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      WriteOutput(context, $"Network:  {network.Metadata.Name}");
      WriteOutput(context, $"Org:      {network.Metadata.Organization}");
      WriteOutput(context, $"Server:   {vpnCredentials.ServerAddress}");
      WriteOutput(context, $"Protocol: {vpnCredentials.Protocol}");
      WriteOutput(context, $"Username: {vpnCredentials.Username}");
      WriteOutput(context, "");

      // Simulate connection stages
      WriteOutput(context, $"[1/5] Resolving VPN server {vpnCredentials.ServerAddress}...");
      Thread.Sleep(300);

      WriteOutput(context, "[2/5] Establishing encrypted tunnel...");
      Thread.Sleep(400);

      WriteOutput(context, $"[3/5] Authenticating with username '{vpnCredentials.Username}'...");
      Thread.Sleep(500);

      WriteOutput(context, "[4/5] Negotiating connection parameters...");
      Thread.Sleep(300);

      WriteOutput(context, "[5/5] Configuring routing tables...");
      Thread.Sleep(400);

      // Get current network (source)
      string currentNetworkId = city.CurrentNetwork?.NetworkId ?? "public";

      // Use ConnectionManager to establish VPN connection
      var connectionManager = city.GetConnectionManager();
      var connectionResult = connectionManager.EstablishConnection(
        currentNetworkId,
        networkId,
        ConnectionType.VPN,
        networkCredentials,
        new System.Collections.Generic.Dictionary<string, object>
        {
          { "VPNServer", vpnCredentials.ServerAddress },
          { "VPNProtocol", vpnCredentials.Protocol },
          { "Port", vpnCredentials.Port }
        }
      );

      if (connectionResult.IsFailure)
      {
        WriteOutput(context, "");
        context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f)); // Red
        WriteOutput(context, "✗ VPN connection failed");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");
        WriteError(context, $"Error: {connectionResult.ErrorMessage}");
        return CommandResult.Error(connectionResult.ErrorMessage);
      }

      // Switch to the new network
      city.SetCurrentNetwork(networkId);

      // Connection successful
      var connection = connectionResult.Data;
      WriteOutput(context, "");
      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
      WriteOutput(context, "✓ VPN connection established!");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Display network information
      WriteOutput(context, $"Connected to: {network.Metadata.Name}");
      WriteOutput(context, $"Organization: {network.Metadata.Organization}");
      WriteOutput(context, $"Network Type: {network.Metadata.Type}");
      WriteOutput(context, $"IP Range: {network.Metadata.IPRange}");
      WriteOutput(context, "");

      // Display connection quality
      WriteOutput(context, "Connection Quality:");
      WriteOutput(context, $"  Latency: {connection.Latency:F1}ms");
      WriteOutput(context, $"  Bandwidth: {connection.Bandwidth:F1} Mbps");
      WriteOutput(context, $"  Quality Score: {connection.GetQualityScore()}/100");
      WriteOutput(context, $"  Encryption: {connection.EncryptionType}");
      WriteOutput(context, "");

      // Show security profile information
      if (network.SecurityProfile != null)
      {
        var securityLevel = network.SecurityProfile.DefaultSecurityLevel;
        Color secColor = securityLevel == SampleOS.Core.Networking.SecurityLevel.High ?
          new Color(1f, 0.3f, 0.3f) :
          securityLevel == SampleOS.Core.Networking.SecurityLevel.Medium ?
          new Color(1f, 0.7f, 0.2f) :
          new Color(0.3f, 1f, 0.3f);

        context.Stdout.SetColor(secColor);
        WriteOutput(context, $"Security Level: {securityLevel}");
        context.Stdout.SetColor(Color.white);

        if (network.SecurityProfile.HasIntrusionDetection)
        {
          context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
          WriteOutput(context, "⚠ Warning: Intrusion Detection System active");
          context.Stdout.SetColor(Color.white);
        }

        if (network.SecurityProfile.RequiresMultiFactor)
        {
          context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
          WriteOutput(context, "⚠ Warning: Multi-factor authentication required for some systems");
          context.Stdout.SetColor(Color.white);
        }

        WriteOutput(context, "");
      }

      // Show next steps
      WriteOutput(context, "Your IP address is now in range: " + network.Metadata.IPRange);
      WriteOutput(context, "");
      WriteOutput(context, "Next steps:");
      WriteOutput(context, "  • Use 'nmap <ip-range>' to discover systems on this network");
      WriteOutput(context, "  • Use 'networks' to view detailed network information");
      WriteOutput(context, "  • Use 'connections' to see active network connections");

      // Show number of known devices on this network
      var devicesOnNetwork = gameWorld.GetDeviceNetwork(session.CurrentDevice);
      if (devicesOnNetwork != null)
      {
        var deviceCount = city.DiscoveryManager.GetNetworkDevices(networkId).Count;
        if (deviceCount > 0)
        {
          WriteOutput(context, $"  • {deviceCount} device(s) already discovered on this network");
        }
      }

      return CommandResult.Ok();
    }
  }
}
