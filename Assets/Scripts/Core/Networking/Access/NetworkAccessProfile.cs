using System;
using System.Collections.Generic;
using Core.Networking.Discovery;

namespace Core.Networking.Access
{
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
    /// </summary>
    public Result<bool> ValidateAccess(NetworkCredentials credentials, PlayerProgressManager progress)
    {
      if (IsLocked && LockoutExpiry > DateTime.Now)
      {
        return Result<bool>.Failure($"Network access locked until {LockoutExpiry:HH:mm}");
      }

      foreach (var requirement in Requirements)
      {
        var result = requirement.Validate(credentials, progress);
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
    /// </summary>
    public Result<bool> ValidateDiscovery(NetworkDiscoveryManager discoveryManager)
    {
      if (IsDiscovered)
        return Result<bool>.Success(true);

      foreach (var requirement in DiscoveryRequirements)
      {
        var result = requirement.Validate(discoveryManager);
        if (!result.IsSuccess)
          return result;
      }

      IsDiscovered = true;
      return Result<bool>.Success(true);
    }
  }

  public abstract class AccessRequirement
  {
    public string Description { get; set; }
    public abstract Result<bool> Validate(NetworkCredentials credentials, PlayerProgressManager progress);
  }

  public class VPNCredentialRequirement : AccessRequirement
  {
    public string RequiredUsername { get; set; }
    public string RequiredPassword { get; set; }
    public string RequiredServer { get; set; }

    public override Result<bool> Validate(NetworkCredentials credentials, PlayerProgressManager progress)
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

    public override Result<bool> Validate(NetworkCredentials credentials, PlayerProgressManager progress)
    {
      if (progress.HasCompromisedSystem(RequiredSystemHostname))
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Must compromise system '{RequiredSystemHostname}' first");
    }
  }

  public abstract class DiscoveryRequirement
  {
    public string Description { get; set; }
    public abstract Result<bool> Validate(NetworkDiscoveryManager discoveryManager);
  }

  public class ClueDiscoveryRequirement : DiscoveryRequirement
  {
    public DiscoveryClueType RequiredClueType { get; set; }
    public int MinimumClues { get; set; } = 1;

    public override Result<bool> Validate(NetworkDiscoveryManager discoveryManager)
    {
      var clues = discoveryManager.GetCluesOfType(RequiredClueType);
      if (clues.Count >= MinimumClues)
        return Result<bool>.Success(true);

      return Result<bool>.Failure($"Need {MinimumClues} {RequiredClueType} clues to discover this network");
    }
  }
}
