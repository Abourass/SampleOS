using System;
using System.Collections.Generic;
using System.Linq;

namespace Core.Networking.Access
{
  [Serializable]
  public class NetworkSecurityProfile
  {
    public SecurityLevel DefaultSecurityLevel { get; set; } = SecurityLevel.Medium;
    public bool RequiresVPN { get; set; } = false;
    public bool HasFirewall { get; set; } = true;
    public bool NetworkSegmentation { get; set; } = false;
    public bool RequiresMultiFactor { get; set; } = false;
    public bool AllowsGuestAccess { get; set; } = false;

    // Intrusion detection and monitoring
    public bool HasIntrusionDetection { get; set; } = false;
    public bool LogsConnections { get; set; } = true;
    public bool RequiresEncryption { get; set; } = false;

    // Access restrictions
    public List<string> AllowedSourceNetworks { get; set; } = new List<string>();
    public List<string> BlockedSourceNetworks { get; set; } = new List<string>();
    public List<TimeRange> AllowedAccessTimes { get; set; } = new List<TimeRange>();

    // Certificate requirements
    public bool RequiresClientCertificate { get; set; } = false;
    public string RequiredCertificateAuthority { get; set; }

    public NetworkSecurityProfile()
    {
      // Default to always-accessible
      AllowedAccessTimes.Add(new TimeRange(TimeSpan.Zero, TimeSpan.FromHours(24)));
    }

    /// <summary>
    /// Calculate security score from 0-100 based on enabled features
    /// </summary>
    public int CalculateSecurityScore()
    {
      int score = (int)DefaultSecurityLevel * 10; // Base score from security level

      if (RequiresVPN) score += 15;
      if (HasFirewall) score += 10;
      if (NetworkSegmentation) score += 15;
      if (RequiresMultiFactor) score += 20;
      if (HasIntrusionDetection) score += 15;
      if (RequiresEncryption) score += 10;
      if (RequiresClientCertificate) score += 15;

      return Math.Min(100, score);
    }

    /// <summary>
    /// Check if access is allowed from a source network at current time
    /// </summary>
    public bool IsAccessAllowed(string sourceNetwork, DateTime accessTime)
    {
      // Check blocked networks first
      if (BlockedSourceNetworks.Contains(sourceNetwork))
        return false;

      // Check allowed networks (empty list means all allowed)
      if (AllowedSourceNetworks.Count > 0 && !AllowedSourceNetworks.Contains(sourceNetwork))
        return false;

      // Check time restrictions
      TimeSpan currentTime = accessTime.TimeOfDay;
      return AllowedAccessTimes.Any(range =>
          currentTime >= range.StartTime && currentTime <= range.EndTime);
    }
  }

  [Serializable]
  public class TimeRange
  {
    public TimeSpan StartTime { get; set; }
    public TimeSpan EndTime { get; set; }

    public TimeRange(TimeSpan start, TimeSpan end)
    {
      StartTime = start;
      EndTime = end;
    }
  }
}
