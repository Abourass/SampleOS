using System.Linq;
using System.Collections.Generic;

public class NetworksCommand : ICommand
{
  private VirtualCity city;

  public string Name => "networks";
  public string Description => "List available networks and connection status";
  public string Usage => "networks [--available|--connected|--discovered]";

  public NetworksCommand(VirtualCity city)
  {
    this.city = city;
  }

  public void Execute(string[] args, ITerminalOutput output)
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

    // Get discovered network IDs
    List<string> discoveredNetworkIds = city.GetDiscoveredNetworks();

    output.AppendText("NETWORK STATUS\n");
    output.AppendText("==============\n\n");

    if (showConnected)
    {
      var currentNetwork = city.CurrentNetwork;
      output.AppendText($"Current Network: {currentNetwork.Metadata.Name} ({currentNetwork.NetworkId})\n");
      output.AppendText($"IP Range: {currentNetwork.Metadata.IPRange}\n");
      output.AppendText($"Organization: {currentNetwork.Metadata.Organization}\n");
      output.AppendText($"Security Level: {currentNetwork.SecurityProfile.DefaultSecurityLevel}\n\n");
    }

    if (showAvailable)
    {
      // Get connection manager for connection status
      var connectionManager = city.GetConnectionManager();
      var activeConnections = connectionManager.GetActiveConnections();

      output.AppendText("ACCESSIBLE NETWORKS:\n");

      // First list the currently connected network
      output.AppendText($"[CONNECTED] {city.CurrentNetwork.Metadata.Name} - {city.CurrentNetwork.Metadata.Description}\n");

      // Then list other networks with active connections
      foreach (var connection in activeConnections)
      {
        if (connection.TargetNetworkId != city.CurrentNetwork.NetworkId)
        {
          output.AppendText($"[Available] Network {connection.TargetNetworkId} - Connected via {connection.Type}\n");
        }
      }
      output.AppendText("\n");
    }

    if (showDiscovered)
    {
      output.AppendText("DISCOVERED NETWORKS (Credentials Required):\n");

      foreach (string networkId in discoveredNetworkIds)
      {
        // Skip the current network as it's already shown above
        if (networkId == city.CurrentNetwork.NetworkId)
          continue;

        // Skip networks that have active connections as they're shown above
        var isConnected = city.GetConnectionManager().GetActiveConnections()
            .Any(c => c.TargetNetworkId == networkId);

        if (!isConnected)
        {
          var networkInfoResult = city.GetNetworkInfo(networkId);
          if (networkInfoResult.IsSuccess)
          {
            var metadata = networkInfoResult.Data;
            output.AppendText($"[LOCKED] {metadata.Name} - {metadata.Description}\n");
            output.AppendText($"         Network Type: {metadata.Type}\n");
          }
          else
          {
            output.AppendText($"[LOCKED] Network {networkId} - Details unknown\n");
          }
        }
      }
    }
  }
}
