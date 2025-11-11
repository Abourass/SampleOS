using System.Linq;
using System.Collections.Generic;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class NetworksCommand : CommandBase
  {
    public override string Name => "networks";
    public override string Description => "List available networks and connection status";
    public override string Usage => "networks [--available|--connected|--discovered]";

    // No constructor dependencies anymore!
    public NetworksCommand()
    {
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      bool showAvailable = args.Contains("--available");
      bool showConnected = args.Contains("--connected");
      bool showDiscovered = args.Contains("--discovered");

      if (!showAvailable && !showConnected && !showDiscovered)
      {
        // Default: show all information
        showAvailable = true;
        showConnected = true;
        showDiscovered = true;
      }

      // Display header
      context.Stdout.SetColor(new Color(0.3f, 0.7f, 1f)); // Light blue
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                    NETWORK STATUS                          ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Show current network information
      if (showConnected)
      {
        DisplayCurrentNetwork(context);
      }

      // Show available/connected networks
      if (showAvailable)
      {
        DisplayAvailableNetworks(context);
      }

      // Show discovered networks
      if (showDiscovered)
      {
        DisplayDiscoveredNetworks(context);
      }

      return CommandResult.Ok();
    }

    private void DisplayCurrentNetwork(CommandContext context)
    {
      // Get current network from WorldService
      var currentCity = context.WorldService.GetCurrentCity();
      if (currentCity == null)
      {
        WriteError(context, "Error: No city context available");
        return;
      }

      var currentNetwork = currentCity.CurrentNetwork;

      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
      WriteOutput(context, "CURRENT NETWORK:");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, new string('─', 60));

      WriteOutput(context, $"Name:         {currentNetwork.Metadata.Name}");
      WriteOutput(context, $"Network ID:   {currentNetwork.NetworkId}");
      WriteOutput(context, $"IP Range:     {currentNetwork.Metadata.IPRange}");
      WriteOutput(context, $"Organization: {currentNetwork.Metadata.Organization}");
      WriteOutput(context, $"Type:         {currentNetwork.Metadata.Type}");
      WriteOutput(context, $"Security:     {currentNetwork.SecurityProfile.DefaultSecurityLevel}");
      WriteOutput(context, "");
      WriteOutput(context, $"Description:  {currentNetwork.Metadata.Description}");
      WriteOutput(context, "");

      // Show device count
      var devices = context.NetworkService.GetDevicesInNetwork(currentNetwork.NetworkId);
      WriteOutput(context, $"Devices:      {devices.Count} total");
      WriteOutput(context, "");
    }

    private void DisplayAvailableNetworks(CommandContext context)
    {
      var currentCity = context.WorldService.GetCurrentCity();
      if (currentCity == null) return;

      var currentNetwork = currentCity.CurrentNetwork;
      var activeConnections = context.NetworkService.GetActiveConnections();

      context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f)); // Light blue
      WriteOutput(context, "ACCESSIBLE NETWORKS:");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, new string('─', 60));

      // Current network is always accessible
      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
      WriteOutput(context, $"[CONNECTED] {currentNetwork.Metadata.Name}");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, $"            {currentNetwork.Metadata.Description}");
      WriteOutput(context, $"            Network ID: {currentNetwork.NetworkId}");
      WriteOutput(context, "");

      // Show other networks with active connections
      foreach (var connection in activeConnections)
      {
        if (connection.TargetNetworkId != currentNetwork.NetworkId)
        {
          var networkInfoResult = currentCity.GetNetworkInfo(connection.TargetNetworkId);

          context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
          WriteOutput(context, $"[AVAILABLE] {connection.TargetNetworkId}");
          context.Stdout.SetColor(Color.white);

          if (networkInfoResult.IsSuccess)
          {
            var metadata = networkInfoResult.Data;
            WriteOutput(context, $"            {metadata.Name} - {metadata.Description}");
            WriteOutput(context, $"            Type: {metadata.Type}");
          }

          WriteOutput(context, $"            Connection: {connection.Type} ({connection.Status})");
          WriteOutput(context, $"            Quality: {connection.GetQualityScore()}%");
          WriteOutput(context, "");
        }
      }

      if (activeConnections.Count == 0 ||
          activeConnections.All(c => c.TargetNetworkId == currentNetwork.NetworkId))
      {
        WriteOutput(context, "No other networks currently connected.");
        WriteOutput(context, "");
      }
    }

    private void DisplayDiscoveredNetworks(CommandContext context)
    {
      var currentCity = context.WorldService.GetCurrentCity();
      if (currentCity == null) return;

      var currentNetwork = currentCity.CurrentNetwork;

      // Get discovered network IDs from NetworkService
      List<string> discoveredNetworkIds = context.NetworkService.GetAccessibleNetworks();

      // Remove the current network from the list (it's shown separately)
      discoveredNetworkIds.RemoveAll(id => id == currentNetwork?.NetworkId);

      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
      WriteOutput(context, "DISCOVERED NETWORKS:");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, new string('─', 60));

      if (discoveredNetworkIds.Count == 0)
      {
        WriteOutput(context, "No additional networks discovered yet.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Compromise systems and scan their files");
        WriteOutput(context, "  - Look for VPN configurations and network diagrams");
        WriteOutput(context, "  - Check email for network references");
        WriteOutput(context, "  - Use 'cat' on configuration files in /etc/openvpn/");
        WriteOutput(context, "");
        return;
      }

      // Get active connections to check if networks are already connected
      var activeConnections = context.NetworkService.GetActiveConnections();

      foreach (string networkId in discoveredNetworkIds)
      {
        // Skip the current network as it's already shown above
        if (networkId == currentNetwork.NetworkId)
          continue;

        // Check if this network has an active connection
        var isConnected = activeConnections.Any(c => c.TargetNetworkId == networkId);

        if (!isConnected)
        {
          var networkInfoResult = currentCity.GetNetworkInfo(networkId);

          if (networkInfoResult.IsSuccess)
          {
            var metadata = networkInfoResult.Data;

            context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f)); // Gray
            WriteOutput(context, $"[LOCKED] {metadata.Name}");
            context.Stdout.SetColor(Color.white);
            WriteOutput(context, $"         Network ID: {networkId}");
            WriteOutput(context, $"         Type: {metadata.Type}");
            WriteOutput(context, $"         Organization: {metadata.Organization}");
            WriteOutput(context, $"         Description: {metadata.Description}");

            // Show connected networks (gateways)
            if (metadata.ConnectedNetworks != null && metadata.ConnectedNetworks.Count > 0)
            {
              WriteOutput(context, $"         Gateway Networks: {string.Join(", ", metadata.ConnectedNetworks)}");
            }

            WriteOutput(context, "         Status: Requires credentials or compromised gateway");

            // Show if player has VPN credentials
            bool hasVpnCreds = context.PlayerState.Credentials.HasVPNCredentialsFor(networkId);
            if (hasVpnCreds)
            {
              context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
              WriteOutput(context, "         [VPN CREDENTIALS AVAILABLE]");
              context.Stdout.SetColor(Color.white);
            }
          }
          else
          {
            context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f)); // Gray
            WriteOutput(context, $"[LOCKED] Network {networkId}");
            context.Stdout.SetColor(Color.white);
            WriteOutput(context, "         Details unknown - network partially discovered");
          }

          WriteOutput(context, "");
        }
      }

      // Show usage hint
      WriteOutput(context, "Use 'vpn-connect <network-id>' to connect to a discovered network.");
      WriteOutput(context, "Note: You may need VPN credentials or a compromised gateway system.");
      WriteOutput(context, "");
    }
  }
}
