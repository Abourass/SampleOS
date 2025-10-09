using UnityEngine;
using System.Threading.Tasks;
using System.Linq;
using SampleOS.Core.Services;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Networking.Connections;
using SampleOS.Core.Networking.Access;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class VpnConnectCommand : CommandBase, IAsyncCommand
  {
    public override string Name => "vpn-connect";
    public override string Description => "Connect to a network via VPN";
    public override string Usage => "vpn-connect <network-id> [--config <config-file>]";
    public bool SupportsCancellation => true;

    public VpnConnectCommand()
    {
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      return CommandResult.Error("This command requires async execution.");
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
    {
      if (args.Length < 1)
      {
        WriteError(context, $"Usage: {Usage}");
        return CommandResult.Error("Missing network ID");
      }

      string networkId = args[0];

      WriteOutput(context, $"Attempting VPN connection to network '{networkId}'...");
      WriteOutput(context, "");

      // Get services via context
      var currentCity = context.WorldService.GetCurrentCity();
      if (currentCity == null)
      {
        WriteError(context, "Error: No city context available");
        return CommandResult.Error("No city context");
      }

      // Check if network is discovered (via PlayerStateService)
      if (!context.PlayerState.IsNetworkDiscovered(networkId))
      {
        WriteError(context, $"Network '{networkId}' has not been discovered yet.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Compromise systems to find clues about new networks");
        WriteOutput(context, "  - Scan files for VPN configurations and network references");
        WriteOutput(context, "  - Use 'scan-creds' on compromised systems");
        return CommandResult.Error("Network not discovered");
      }

      // Get the network
      var network = currentCity.GetNetwork(networkId);
      if (network == null)
      {
        WriteError(context, $"Network '{networkId}' not found in this city.");
        return CommandResult.Error("Network not found");
      }

      // Get VPN credentials from PlayerCredentials
      var vpnCredentials = context.PlayerState.Credentials.GetVPNCredentials(networkId);

      if (vpnCredentials == null)
      {
        WriteError(context, "Error: No VPN credentials found for this network.");
        WriteOutput(context, "");
        WriteOutput(context, "You need to find VPN credentials first:");
        WriteOutput(context, "  - Search compromised systems for .ovpn files");
        WriteOutput(context, "  - Look in /etc/openvpn/ or user home directories");
        WriteOutput(context, "  - Check email folders for VPN setup instructions");
        WriteOutput(context, "  - Use 'scan-creds' on compromised systems");
        return CommandResult.Error("No VPN credentials found");
      }

      // Display connection UI
      context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f));
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                  VPN CONNECTION                            ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      WriteOutput(context, $"Network:  {network.Metadata.Name}");
      WriteOutput(context, $"Org:      {network.Metadata.Organization}");
      WriteOutput(context, $"Server:   {vpnCredentials.Server}");
      WriteOutput(context, $"Protocol: {vpnCredentials.Protocol}");
      WriteOutput(context, $"Username: {vpnCredentials.Username}");
      WriteOutput(context, "");

      // Simulate connection stages
      ReportProgress(context, 0.2f, "Resolving VPN server");
      WriteOutput(context, $"[1/5] Resolving VPN server {vpnCredentials.Server}...");
      await Task.Delay(300, context.CancellationToken);

      ReportProgress(context, 0.4f, "Establishing encrypted tunnel");
      WriteOutput(context, "[2/5] Establishing encrypted tunnel...");
      await Task.Delay(400, context.CancellationToken);

      ReportProgress(context, 0.6f, "Authenticating");
      WriteOutput(context, $"[3/5] Authenticating with username '{vpnCredentials.Username}'...");
      await Task.Delay(500, context.CancellationToken);

      ReportProgress(context, 0.8f, "Negotiating parameters");
      WriteOutput(context, "[4/5] Negotiating connection parameters...");
      await Task.Delay(300, context.CancellationToken);

      ReportProgress(context, 0.9f, "Configuring routing");
      WriteOutput(context, "[5/5] Configuring routing tables...");
      await Task.Delay(400, context.CancellationToken);

      // Get current network (source)
      string currentNetworkId = currentCity.CurrentNetwork?.NetworkId ?? "public";

      // Create network credentials
      var networkCredentials = new NetworkCredentials(networkId)
      {
        VPNCredentials = new VPNCredential
        {
          NetworkId = vpnCredentials.NetworkId,
          NetworkName = network.Metadata.Name,
          Username = vpnCredentials.Username,
          Password = vpnCredentials.Password,
          ServerAddress = vpnCredentials.Server,
          Port = vpnCredentials.Port,
          Protocol = vpnCredentials.Protocol
        }
      };

      // Use NetworkService to establish VPN connection
      var connectionResult = context.NetworkService.EstablishConnection(
          currentNetworkId,
          networkId,
          ConnectionType.VPN,
          new System.Collections.Generic.Dictionary<string, object>
          {
                    { "VPNServer", vpnCredentials.Server },
                    { "VPNProtocol", vpnCredentials.Protocol },
                    { "Port", vpnCredentials.Port },
                    { "Credentials", networkCredentials }
          }
      );

      if (connectionResult.IsFailure)
      {
        WriteOutput(context, "");
        context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f));
        WriteOutput(context, "✗ VPN connection failed");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "");
        WriteError(context, $"Error: {connectionResult.ErrorMessage}");
        return CommandResult.Error(connectionResult.ErrorMessage);
      }

      // Switch to the new network
      currentCity.SetCurrentNetwork(networkId);

      // Trigger network change event
      GameEvents.Instance.Trigger(GameEventType.NetworkChanged, networkId);

      // Connection successful
      var connection = connectionResult.Data;
      WriteOutput(context, "");
      ReportProgress(context, 1.0f, "Connected!");
      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
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

      // Show security profile
      if (network.SecurityProfile != null)
      {
        var securityLevel = network.SecurityProfile.DefaultSecurityLevel;
        Color secColor = securityLevel == SampleOS.Core.Networking.SecurityLevel.High
            ? new Color(1f, 0.3f, 0.3f)
            : securityLevel == SampleOS.Core.Networking.SecurityLevel.Medium
            ? new Color(1f, 0.7f, 0.2f)
            : new Color(0.3f, 1f, 0.3f);

        context.Stdout.SetColor(secColor);
        WriteOutput(context, $"Security Level: {securityLevel}");
        context.Stdout.SetColor(Color.white);

        if (network.SecurityProfile.HasIntrusionDetection)
        {
          context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
          WriteOutput(context, "⚠ Warning: Intrusion Detection System active");
          context.Stdout.SetColor(Color.white);
        }
      }

      WriteOutput(context, "");
      WriteOutput(context, "Your IP address is now in range: " + network.Metadata.IPRange);
      WriteOutput(context, "");
      WriteOutput(context, "Next steps:");
      WriteOutput(context, "  • Use 'nmap <ip-range>' to discover systems");
      WriteOutput(context, "  • Use 'networks' to view network details");
      WriteOutput(context, "  • Use 'connections' to see active connections");

      // Show discovered devices count
      var devices = context.NetworkService.GetDevicesInNetwork(networkId);
      if (devices.Count > 0)
      {
        WriteOutput(context, $"  • {devices.Count} device(s) on this network");
      }

      return CommandResult.Ok();
    }
  }
}
