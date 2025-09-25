using System;
using System.Collections.Generic;

namespace Core.Networking.Connections
{
  [Serializable]
  public class NetworkConnection
  {
    public string ConnectionId { get; set; }
    public string SourceNetworkId { get; set; }
    public string TargetNetworkId { get; set; }
    public ConnectionType Type { get; set; }
    public ConnectionStatus Status { get; set; }
    public DateTime EstablishedTime { get; set; }
    public DateTime LastActivity { get; set; }

    // Connection parameters
    public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();

    // Performance metrics
    public float Latency { get; set; } // in milliseconds
    public float Bandwidth { get; set; } // in Mbps
    public int PacketLoss { get; set; } // percentage

    // Security information
    public bool IsEncrypted { get; set; }
    public string EncryptionType { get; set; }
    public bool IsAuthenticated { get; set; }

    // Gateway information
    public string GatewaySystemId { get; set; }
    public List<string> RouteHops { get; set; } = new List<string>();

    public NetworkConnection(string sourceNetwork, string targetNetwork, ConnectionType type)
    {
      ConnectionId = Guid.NewGuid().ToString();
      SourceNetworkId = sourceNetwork;
      TargetNetworkId = targetNetwork;
      Type = type;
      Status = ConnectionStatus.Connecting;
      EstablishedTime = DateTime.Now;
      LastActivity = DateTime.Now;
    }

    /// <summary>
    /// Update connection activity timestamp
    /// </summary>
    public void UpdateActivity()
    {
      LastActivity = DateTime.Now;
    }

    /// <summary>
    /// Check if connection has timed out
    /// </summary>
    public bool IsTimedOut(TimeSpan timeout)
    {
      return DateTime.Now - LastActivity > timeout;
    }

    /// <summary>
    /// Calculate connection quality score (0-100)
    /// </summary>
    public int GetQualityScore()
    {
      int score = 100;

      // Deduct for high latency
      if (Latency > 100) score -= 20;
      else if (Latency > 50) score -= 10;

      // Deduct for packet loss
      score -= PacketLoss * 2;

      // Bonus for encryption
      if (IsEncrypted) score += 5;

      return Math.Max(0, Math.Min(100, score));
    }

    /// <summary>
    /// Get human-readable connection description
    /// </summary>
    public string GetDescription()
    {
      string security = IsEncrypted ? "encrypted" : "unencrypted";
      string auth = IsAuthenticated ? "authenticated" : "unauthenticated";
      return $"{Type} connection from {SourceNetworkId} to {TargetNetworkId} ({security}, {auth})";
    }
  }

  public enum ConnectionType
  {
    Direct,         // Direct network connection
    VPN,           // VPN tunnel
    SSH,           // SSH tunnel
    Proxy,         // HTTP/SOCKS proxy
    Tor,           // Tor hidden service
    Bounce,        // Connection through compromised system
    Bridge         // Network bridge connection
  }

  public enum ConnectionStatus
  {
    Disconnected,
    Connecting,
    Connected,
    Reconnecting,
    Failed,
    Timeout,
    Denied
  }
}
