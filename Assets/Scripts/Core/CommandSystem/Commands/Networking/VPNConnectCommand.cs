using Core.Networking.Discovery;

public class VpnConnectCommand : ICommand
{
  private VirtualCity city;
  private PlayerCredentialManager credentialManager;

  public string Name => "vpn-connect";
  public string Description => "Connect to a network via VPN";
  public string Usage => "vpn-connect <network-id> [--config <config-file>]";

  public VpnConnectCommand(VirtualCity city, PlayerCredentialManager credentialManager)
  {
    this.city = city;
    this.credentialManager = credentialManager;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    if (args.Length < 1)
    {
      output.AppendText($"Usage: {Usage}\n");
      return;
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

    output.AppendText($"Attempting VPN connection to network '{networkId}'...\n");

    // Get credentials for this network
    var credentials = credentialManager.GetCredentialsForNetwork(networkId);
    if (credentials == null)
    {
      output.AppendText("Error: No VPN credentials found for this network.\n");
      output.AppendText("Try searching compromised systems for configuration files or emails.\n");
      return;
    }

    // Create a valid NetworkCredentials object
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

    // Simulate VPN connection process
    output.AppendText($"Connecting to VPN server {credentials.ServerAddress}...\n");
    output.AppendText($"Authenticating with username '{credentials.Username}'...\n");

    var connectionResult = city.ConnectToNetwork(networkId, networkCredentials);
    if (connectionResult.IsSuccess)
    {
      output.AppendText("VPN connection established!\n");
      output.AppendText($"Connected to network: {connectionResult.Data.Metadata.Name}\n");
      output.AppendText($"Your IP address is now in range: {connectionResult.Data.Metadata.IPRange}\n");
    }
    else
    {
      output.AppendText($"VPN connection failed: {connectionResult.ErrorMessage}\n");
    }
  }
}
