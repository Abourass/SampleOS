using System.Collections.Generic;
using System.Linq;
using Core.Networking.Access;
using Core.Networking.Discovery;

public class NetworkAccessManager
{
  private Dictionary<string, NetworkAccessProfile> accessProfiles = new Dictionary<string, NetworkAccessProfile>();
  private PlayerCredentialManager credentialManager;

  private NetworkDiscoveryManager discoveryManager;

  public Result<bool> CheckAccess(string networkId, NetworkCredentials credentials)
  {
    // First check if network is discovered
    if (discoveryManager != null && !discoveryManager.IsNetworkDiscovered(networkId))
      return Result<bool>.Failure("Network not discovered yet");

    if (!accessProfiles.TryGetValue(networkId, out NetworkAccessProfile profile))
    {
      // Default to VPN access if no profile exists
      profile = new NetworkAccessProfile(networkId, NetworkAccessType.VPN);
      accessProfiles[networkId] = profile;
    }

    switch (profile.AccessType)
    {
      case NetworkAccessType.Public:
        return Result<bool>.Success(true);

      case NetworkAccessType.VPN:
        return ValidateVPNAccess(profile, credentials);

      case NetworkAccessType.DirectConnection:
        return ValidateDirectConnection(profile, credentials);

      case NetworkAccessType.Compromised:
        return ValidateCompromisedAccess(profile);

      default:
        return Result<bool>.Failure("Access denied");
    }
  }

  private Result<bool> ValidateVPNAccess(NetworkAccessProfile profile, NetworkCredentials credentials)
  {
    if (credentials == null || credentials.VPNCredentials == null)
      return Result<bool>.Failure("VPN credentials required");

    foreach (var requirement in profile.Requirements.OfType<VPNCredentialRequirement>())
    {
      if (credentials.VPNCredentials.Username != requirement.RequiredUsername)
        return Result<bool>.Failure("Invalid VPN username");

      if (credentials.VPNCredentials.Password != requirement.RequiredPassword)
        return Result<bool>.Failure("Invalid VPN password");

      if (credentials.VPNCredentials.ServerAddress != requirement.RequiredServer)
        return Result<bool>.Failure("Invalid VPN server");
    }

    return Result<bool>.Success(true);
  }

  private Result<bool> ValidateDirectConnection(NetworkAccessProfile profile, NetworkCredentials credentials)
  {
    // Check if direct connection is through a gateway system
    // This would typically check the current network connection status

    return Result<bool>.Success(true);
  }

  private Result<bool> ValidateCompromisedAccess(NetworkAccessProfile profile)
  {
    // Check if one of the gateway systems has been compromised
    foreach (var requirement in profile.Requirements.OfType<CompromisedSystemRequirement>())
    {
      if (!credentialManager.HasRootAccess(requirement.RequiredSystemHostname))
        return Result<bool>.Failure($"Must compromise system '{requirement.RequiredSystemHostname}' first");
    }

    return Result<bool>.Success(true);
  }

  // Add a profile for a network
  public void AddAccessProfile(NetworkAccessProfile profile)
  {
    accessProfiles[profile.NetworkId] = profile;
  }

  // Set credential manager
  public void SetCredentialManager(PlayerCredentialManager manager)
  {
    credentialManager = manager;
  }

  public void SetDiscoveryManager(NetworkDiscoveryManager manager)
  {
    discoveryManager = manager;
  }
}

public enum NetworkAccessType
{
  Public,           // Always accessible (ISP networks, public WiFi)
  VPN,              // Requires VPN credentials discovered through hacking
  DirectConnection, // Requires physical network access (compromised systems)
  Compromised,      // Requires having compromised a gateway system
  Invitation        // Requires invitation/key from another player/NPC
}
