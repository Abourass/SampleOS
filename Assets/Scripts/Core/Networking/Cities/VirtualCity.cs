using System.Collections.Generic;
using System.Linq;
using Core.Networking.Connections;
using Core.Networking.Discovery;

public class VirtualCity
{
  private Dictionary<string, VirtualNetwork> networks = new Dictionary<string, VirtualNetwork>();
  private ConnectionManager connectionManager;
  private NetworkAccessManager accessManager;
  private NetworkDiscoveryManager discoveryManager;
  private PlayerCredentialManager credentialManager;
  private PlayerProgressManager progressManager;

  public VirtualNetwork CurrentNetwork { get; private set; }

  public VirtualCity()
  {
    // Initialize managers
    accessManager = new NetworkAccessManager();
    discoveryManager = new NetworkDiscoveryManager();
    connectionManager = new ConnectionManager();
    credentialManager = new PlayerCredentialManager();
    progressManager = new PlayerProgressManager(null); // Set default network later

    // Link managers
    accessManager.SetCredentialManager(credentialManager);
    accessManager.SetDiscoveryManager(discoveryManager);

    // Setup event handlers
    connectionManager.OnConnectionEstablished += HandleConnectionEstablished;
    connectionManager.OnConnectionLost += HandleConnectionLost;

    // Initialize networks
    InitializeCityNetworks();

    // Set initial network to public
    CurrentNetwork = networks["public"];
    progressManager.SetNetwork(CurrentNetwork);
  }

  private void InitializeCityNetworks()
  {
    // Public/ISP network (always accessible)
    networks.Add("public", CityNetworkFactory.CreatePublicNetwork());

    // Corporate networks (require VPN)
    networks.Add("corp_megacorp", CityNetworkFactory.CreateCorporateNetwork("MegaCorp Industries"));
    networks.Add("corp_techstart", CityNetworkFactory.CreateCorporateNetwork("TechStart Inc"));

    // Government networks (require special access)
    networks.Add("gov_cityhall", CityNetworkFactory.CreateGovernmentNetwork("City Hall"));
    networks.Add("gov_police", CityNetworkFactory.CreateGovernmentNetwork("Police Department"));

    // Underground/criminal networks (require dark web access)
    networks.Add("dark_underground", CityNetworkFactory.CreateDarkNetwork("Underground Market"));

    // Home/residential networks
    networks.Add("res_maple", CityNetworkFactory.CreateResidentialNetwork("Maple Street"));

    // Register all networks with the discovery manager
    foreach (var network in networks)
    {
      RegisterNetworkWithDiscoveryManager(network.Key, network.Value);
    }

    // Create connections between networks
    SetupNetworkConnections();
  }

  /// <summary>
  /// Connect to a specific network using appropriate credentials
  /// </summary>
  public Result<VirtualNetwork> ConnectToNetwork(string networkId, NetworkCredentials credentials = null)
  {
    // Check if network exists
    if (!networks.TryGetValue(networkId, out VirtualNetwork targetNetwork))
      return Result<VirtualNetwork>.Failure($"Network '{networkId}' not found");

    // Check if network has been discovered
    if (!discoveryManager.IsNetworkDiscovered(networkId))
      return Result<VirtualNetwork>.Failure($"Network '{networkId}' not discovered yet");

    // Check access permissions
    var accessResult = accessManager.CheckAccess(networkId, credentials);
    if (!accessResult.IsSuccess)
      return Result<VirtualNetwork>.Failure(accessResult.ErrorMessage);

    // Establish connection
    var connectionResult = connectionManager.EstablishConnection(
      CurrentNetwork.NetworkId,
      networkId,
      DetermineConnectionType(CurrentNetwork, targetNetwork),
      credentials
    );

    if (!connectionResult.IsSuccess)
      return Result<VirtualNetwork>.Failure(connectionResult.ErrorMessage);

    // Switch to the new network
    CurrentNetwork = targetNetwork;
    progressManager.SetNetwork(CurrentNetwork);

    return Result<VirtualNetwork>.Success(CurrentNetwork);
  }

  /// <summary>
  /// Scan current system for network clues
  /// </summary>
  public Result<List<NetworkClue>> ScanForNetworkClues(string systemHostname)
  {
    if (CurrentNetwork == null)
      return Result<List<NetworkClue>>.Failure("No active network");

    var system = CurrentNetwork.GetSystemByHostname(systemHostname);
    if (system == null)
      return Result<List<NetworkClue>>.Failure($"System '{systemHostname}' not found");

    // Scan the system's files for network clues
    var scanner = new CredentialScanner();
    var results = scanner.ScanSystemForCredentials(system);

    if (!results.Success)
      return Result<List<NetworkClue>>.Failure(results.ErrorMessage);

    // Store discovered credentials
    credentialManager.StoreCredentialScanResults(results);

    // Convert discovery clues to network clues
    List<NetworkClue> clues = new List<NetworkClue>();
    foreach (var clue in results.DiscoveredClues)
    {
      discoveryManager.AddClue(ConvertToNetworkClue(clue));
    }

    return Result<List<NetworkClue>>.Success(clues);
  }

  /// <summary>
  /// Get all discovered networks
  /// </summary>
  public List<string> GetDiscoveredNetworks()
  {
    return discoveryManager.GetDiscoveredNetworks();
  }

  /// <summary>
  /// Get information about a specific network
  /// </summary>
  public Result<NetworkMetadata> GetNetworkInfo(string networkId)
  {
    if (!networks.TryGetValue(networkId, out VirtualNetwork network))
      return Result<NetworkMetadata>.Failure($"Network '{networkId}' not found");

    if (!discoveryManager.IsNetworkDiscovered(networkId))
      return Result<NetworkMetadata>.Failure($"Network '{networkId}' not discovered yet");

    return Result<NetworkMetadata>.Success(network.Metadata);
  }

  /// <summary>
  /// Get the connection manager for UI display
  /// </summary>
  public ConnectionManager GetConnectionManager()
  {
    return connectionManager;
  }

  // Private helper methods
  private void RegisterNetworkWithDiscoveryManager(string networkId, VirtualNetwork network)
  {
    // Register network systems for discovery
    foreach (var device in network.GetNetworkDevices())
    {
      if (device.Type != "local")  // Don't register localhost
      {
        discoveryManager.AddSystemToNetwork(networkId, device.Hostname);
      }
    }
  }

  private void SetupNetworkConnections()
  {
    // Define logical connections between networks
    CreateNetworkConnection("public", "corp_megacorp", GatewayType.VPNServer);
    CreateNetworkConnection("public", "corp_techstart", GatewayType.VPNServer);
    CreateNetworkConnection("public", "gov_cityhall", GatewayType.VPNServer);
    CreateNetworkConnection("public", "gov_police", GatewayType.VPNServer);
    CreateNetworkConnection("public", "dark_underground", GatewayType.ProxyServer);
    CreateNetworkConnection("public", "res_maple", GatewayType.Router);
  }

  private void CreateNetworkConnection(string sourceNetworkId, string targetNetworkId, GatewayType gatewayType)
  {
    // Create a bidirectional connection between networks
    if (networks.TryGetValue(sourceNetworkId, out VirtualNetwork sourceNetwork) &&
        networks.TryGetValue(targetNetworkId, out VirtualNetwork targetNetwork))
    {
      // Add connection information to network metadata
      sourceNetwork.Metadata.ConnectedNetworks.Add(targetNetworkId);
      targetNetwork.Metadata.ConnectedNetworks.Add(sourceNetworkId);

      // Find appropriate gateway systems
      string sourceGatewayHost = FindGatewaySystem(sourceNetwork);
      string targetGatewayHost = FindGatewaySystem(targetNetwork);

      // Create gateways
      if (!string.IsNullOrEmpty(sourceGatewayHost) && !string.IsNullOrEmpty(targetGatewayHost))
      {
        var gatewayId = $"{sourceNetworkId}_to_{targetNetworkId}";
        sourceNetwork.AddGateway(new NetworkGateway(gatewayId, sourceGatewayHost, targetNetworkId, gatewayType));

        var reverseGatewayId = $"{targetNetworkId}_to_{sourceNetworkId}";
        targetNetwork.AddGateway(new NetworkGateway(reverseGatewayId, targetGatewayHost, sourceNetworkId, gatewayType));
      }
    }
  }

  private string FindGatewaySystem(VirtualNetwork network)
  {
    // Find a system suitable for gateway based on type
    var devices = network.GetNetworkDevices();

    // First look for routers or VPN servers
    var router = devices.FirstOrDefault(d => d.Type == "router");
    if (router != null)
      return router.Hostname;

    // Otherwise use any available server
    var server = devices.FirstOrDefault(d => d.Type == "server");
    if (server != null)
      return server.Hostname;

    // If all else fails, use the first device
    if (devices.Count > 1)  // Skip localhost
      return devices[1].Hostname;

    return null;
  }

  private ConnectionType DetermineConnectionType(VirtualNetwork sourceNetwork, VirtualNetwork targetNetwork)
  {
    // Determine appropriate connection type based on network types
    if (targetNetwork.Metadata.Type == NetworkType.Criminal)
      return ConnectionType.Tor;

    if (targetNetwork.SecurityProfile.RequiresVPN)
      return ConnectionType.VPN;

    if (targetNetwork.Metadata.Type == NetworkType.Residential)
      return ConnectionType.Direct;

    // Default to direct connection
    return ConnectionType.Direct;
  }

  private NetworkClue ConvertToNetworkClue(DiscoveryClue discoveryClue)
  {
    var networkClue = new NetworkClue
    {
      NetworkId = discoveryClue.NetworkId,
      ClueType = ConvertClueType(discoveryClue.Type),
      ClueContent = discoveryClue.Content,
      SourceSystem = discoveryClue.SourceSystemId,
      SourceFile = discoveryClue.FilePath
    };

    return networkClue;
  }

  private DiscoveryClueType ConvertClueType(Core.Networking.Discovery.DiscoveryClueType type)
  {
    // Convert between clue type enums
    switch (type)
    {
      case Core.Networking.Discovery.DiscoveryClueType.VPNConfiguration:
        return DiscoveryClueType.VPNCredentials;
      case Core.Networking.Discovery.DiscoveryClueType.DomainReference:
        return DiscoveryClueType.SystemHostname;
      case Core.Networking.Discovery.DiscoveryClueType.IPAddressReference:
        return DiscoveryClueType.IPAddress;
      case Core.Networking.Discovery.DiscoveryClueType.EmailReference:
        return DiscoveryClueType.EmailReference;
      case Core.Networking.Discovery.DiscoveryClueType.DocumentMention:
        return DiscoveryClueType.Document;
      case Core.Networking.Discovery.DiscoveryClueType.BrowserBookmark:
        return DiscoveryClueType.BrowserHistory;
      default:
        return DiscoveryClueType.Document;
    }
  }

  // Event handlers
  private void HandleConnectionEstablished(NetworkConnection connection)
  {
    // Update player's knowledge of the network
    discoveryManager.MarkNetworkDiscovered(connection.TargetNetworkId);
  }

  private void HandleConnectionLost(NetworkConnection connection)
  {
    // Handle disconnection events, possibly revert to public network
    if (CurrentNetwork.NetworkId == connection.TargetNetworkId)
    {
      CurrentNetwork = networks["public"];
      progressManager.SetNetwork(CurrentNetwork);
    }
  }
}
