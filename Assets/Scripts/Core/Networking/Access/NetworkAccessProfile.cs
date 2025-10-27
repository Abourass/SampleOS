using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Networking.Discovery;
using SampleOS.Core.Services;

namespace SampleOS.Core.Networking.Access
{
  public enum NetworkAccessType
  {
    Public,           // Always accessible (ISP networks, public WiFi)
    VPN,              // Requires VPN credentials discovered through hacking
    DirectConnection, // Requires physical network access (compromised systems)
    Compromised,      // Requires having compromised a gateway system
    Invitation        // Requires invitation/key from another player/NPC
  }

  [Serializable]
  public class NetworkAccessProfile
  {
    public string NetworkId { get; set; }
    public NetworkAccessType AccessType { get; set; }
    public List<AccessRequirement> Requirements { get; set; } = new List<AccessRequirement>();
    public DateTime LastAccessAttempt { get; set; }
    public int FailedAttempts { get; set; } = 0;
    public bool IsLocked { get; set; } = false;
    public DateTime? LockoutExpiry { get; set; }

    // Discovery requirements
    public List<DiscoveryRequirement> DiscoveryRequirements { get; set; } = new List<DiscoveryRequirement>();
    public bool IsDiscovered { get; set; } = false;

    public NetworkAccessProfile(string networkId, NetworkAccessType accessType)
    {
      NetworkId = networkId;
      AccessType = accessType;
    }

    /// <summary>
    /// Add a requirement for accessing this network
    /// </summary>
    public void AddRequirement(AccessRequirement requirement)
    {
      Requirements.Add(requirement);
    }

    /// <summary>
    /// Check if all requirements are met for access
    /// Uses PlayerStateService instead of PlayerProgressManager
    /// </summary>
    public Result<bool> ValidateAccess(NetworkCredentials credentials)
    {
      if (IsLocked && LockoutExpiry > DateTime.Now)
      {
        return Result<bool>.Failure($"Network access locked until {LockoutExpiry:HH:mm}");
      }

      // Get player state from service
      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
      {
        return Result<bool>.Failure("Player state not available");
      }

      foreach (var requirement in Requirements)
      {
        var result = requirement.Validate(credentials, playerState);
        if (!result.IsSuccess)
        {
          RecordFailedAttempt();
          return result;
        }
      }

      // Reset failed attempts on successful validation
      FailedAttempts = 0;
      return Result<bool>.Success(true);
    }

    private void RecordFailedAttempt()
    {
      FailedAttempts++;
      LastAccessAttempt = DateTime.Now;

      // Lock after 3 failed attempts
      if (FailedAttempts >= 3)
      {
        IsLocked = true;
        LockoutExpiry = DateTime.Now.AddMinutes(15);
      }
    }

    /// <summary>
    /// Check if this network has been discovered by the player
    /// Uses PlayerStateService instead of NetworkDiscoveryManager
    /// </summary>
    public Result<bool> ValidateDiscovery()
    {
      if (IsDiscovered)
        return Result<bool>.Success(true);

      // Get player state from service
      var playerState = ServiceLocator.Instance.Get<IPlayerStateService>();
      if (playerState == null)
      {
        return Result<bool>.Failure("Player state not available");
      }

      foreach (var requirement in DiscoveryRequirements)
      {
        var result = requirement.Validate(playerState);
        if (!result.IsSuccess)
          return result;
      }

      IsDiscovered = true;
      return Result<bool>.Success(true);
    }
  }

  /// <summary>
  /// Base class for access requirements
  /// Now uses IPlayerStateService instead of PlayerProgressManager
  /// </summary>
  public abstract class AccessRequirement
  {
    public string Description { get; set; }
    public abstract Result<bool> Validate(NetworkCredentials credentials, IPlayerStateService playerState);
  }

  public class VPNCredentialRequirement : AccessRequirement
  {
    public string NetworkId { get; set; }
    public string RequiredUsername { get; set; }
    public string RequiredPassword { get; set; }
    public string RequiredServer { get; set; }

    public override Result<bool> Validate(NetworkCredentials credentials, IPlayerStateService playerState)
    {
      if (credentials?.VPNCredentials == null)
        return Result<bool>.Failure("VPN credentials required");

      var vpnCred = credentials.VPNCredentials;

      if (vpnCred.Username != RequiredUsername)
        return Result<bool>.Failure("Invalid VPN username");

      if (vpnCred.Password != RequiredPassword)
        return Result<bool>.Failure("Invalid VPN password");

      if (vpnCred.ServerAddress != RequiredServer)
        return Result<bool>.Failure("Invalid VPN server");

      return Result<bool>.Success(true);
    }
  }

  public class CompromisedSystemRequirement : AccessRequirement
  {
    public string RequiredSystemHostname { get; set; }

    public override Result<bool> Validate(NetworkCredentials credentials, IPlayerStateService playerState)
    {
      if (playerState.HasCompromisedSystem(RequiredSystemHostname))
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Must compromise system '{RequiredSystemHostname}' first");
    }
  }

  /// <summary>
  /// Base class for discovery requirements
  /// Now uses IPlayerStateService instead of NetworkDiscoveryManager
  /// </summary>
  public abstract class DiscoveryRequirement
  {
    public string Description { get; set; }
    public abstract Result<bool> Validate(IPlayerStateService playerState);
  }

  public class ClueDiscoveryRequirement : DiscoveryRequirement
  {
    public DiscoveryClueType RequiredClueType { get; set; }
    public int MinimumClues { get; set; } = 1;
    public string NetworkId { get; set; }

    public override Result<bool> Validate(IPlayerStateService playerState)
    {
      var clues = playerState.GetCluesForNetwork(NetworkId)
          .Where(c => c.Type == RequiredClueType)
          .ToList();

      if (clues.Count >= MinimumClues)
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Need {MinimumClues} {RequiredClueType} clues to discover this network");
    }
  }

  /// <summary>
  /// Require compromising a specific device to discover network
  /// </summary>
  public class DeviceCompromiseRequirement : DiscoveryRequirement
  {
    public string RequiredDeviceHostname { get; set; }

    public override Result<bool> Validate(IPlayerStateService playerState)
    {
      if (playerState.HasCompromisedSystem(RequiredDeviceHostname))
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Must compromise device '{RequiredDeviceHostname}' to discover this network");
    }
  }

  /// <summary>
  /// Require being at a specific physical location to discover network
  /// </summary>
  public class PhysicalLocationRequirement : DiscoveryRequirement
  {
    public string RequiredLocationId { get; set; }

    public override Result<bool> Validate(IPlayerStateService playerState)
    {
      var worldService = ServiceLocator.Instance.Get<IWorldService>();
      if (worldService == null)
        return Result<bool>.Failure("World service not available");

      var currentLocation = worldService.GetPlayerLocation();
      if (currentLocation != null && currentLocation.LocationId == RequiredLocationId)
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Must visit location to discover this network");
    }
  }
}
