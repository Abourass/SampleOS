using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Core.Networking.Discovery;
using UnityEngine;

namespace Core.Networking.Connections
{
  public class ConnectionManager
  {
    private Dictionary<string, NetworkConnection> activeConnections = new Dictionary<string, NetworkConnection>();
    private List<NetworkConnection> connectionHistory = new List<NetworkConnection>();
    private TimeSpan connectionTimeout = TimeSpan.FromMinutes(30);

    public event Action<NetworkConnection> OnConnectionEstablished;
    public event Action<NetworkConnection> OnConnectionLost;
    public event Action<NetworkConnection> OnConnectionFailed;

    /// <summary>
    /// Establish a new network connection
    /// </summary>
    public Result<NetworkConnection> EstablishConnection(
        string sourceNetwork,
        string targetNetwork,
        ConnectionType type,
        NetworkCredentials credentials = null,
        Dictionary<string, object> parameters = null)
    {
      try
      {
        // Check if connection already exists
        var existingConnection = GetActiveConnection(sourceNetwork, targetNetwork);
        if (existingConnection != null)
        {
          existingConnection.UpdateActivity();
          return Result<NetworkConnection>.Success(existingConnection);
        }

        // Create new connection
        var connection = new NetworkConnection(sourceNetwork, targetNetwork, type);

        if (parameters != null)
        {
          foreach (var param in parameters)
            connection.Parameters[param.Key] = param.Value;
        }

        // Simulate connection establishment based on type
        var establishResult = SimulateConnectionEstablishment(connection, credentials);
        if (!establishResult.IsSuccess)
        {
          connection.Status = ConnectionStatus.Failed;
          connectionHistory.Add(connection);
          OnConnectionFailed?.Invoke(connection);
          return Result<NetworkConnection>.Failure(establishResult.ErrorMessage);
        }

        // Connection successful
        connection.Status = ConnectionStatus.Connected;
        connection.Latency = establishResult.Data.Latency;
        connection.Bandwidth = establishResult.Data.Bandwidth;
        connection.IsEncrypted = establishResult.Data.IsEncrypted;
        connection.IsAuthenticated = establishResult.Data.IsAuthenticated;

        activeConnections[connection.ConnectionId] = connection;
        connectionHistory.Add(connection);

        OnConnectionEstablished?.Invoke(connection);

        return Result<NetworkConnection>.Success(connection);
      }
      catch (Exception ex)
      {
        return Result<NetworkConnection>.Failure($"Connection error: {ex.Message}");
      }
    }

    /// <summary>
    /// Disconnect from a network
    /// </summary>
    public Result<bool> DisconnectFromNetwork(string connectionId)
    {
      if (!activeConnections.TryGetValue(connectionId, out NetworkConnection connection))
      {
        return Result<bool>.Failure("Connection not found");
      }

      connection.Status = ConnectionStatus.Disconnected;
      activeConnections.Remove(connectionId);

      OnConnectionLost?.Invoke(connection);

      return Result<bool>.Success(true);
    }

    /// <summary>
    /// Get active connection between two networks
    /// </summary>
    public NetworkConnection GetActiveConnection(string sourceNetwork, string targetNetwork)
    {
      return activeConnections.Values.FirstOrDefault(c =>
          c.SourceNetworkId == sourceNetwork &&
          c.TargetNetworkId == targetNetwork &&
          c.Status == ConnectionStatus.Connected);
    }

    /// <summary>
    /// Get all active connections
    /// </summary>
    public List<NetworkConnection> GetActiveConnections()
    {
      return new List<NetworkConnection>(activeConnections.Values);
    }

    /// <summary>
    /// Clean up timed out connections
    /// </summary>
    public void CleanupConnections()
    {
      var timedOutConnections = activeConnections.Values
          .Where(c => c.IsTimedOut(connectionTimeout))
          .ToList();

      foreach (var connection in timedOutConnections)
      {
        connection.Status = ConnectionStatus.Timeout;
        activeConnections.Remove(connection.ConnectionId);
        OnConnectionLost?.Invoke(connection);
      }
    }

    /// <summary>
    /// Simulate the technical aspects of establishing a connection
    /// </summary>
    private Result<ConnectionMetrics> SimulateConnectionEstablishment(
        NetworkConnection connection,
        NetworkCredentials credentials)
    {
      var metrics = new ConnectionMetrics();

      switch (connection.Type)
      {
        case ConnectionType.VPN:
          return SimulateVPNConnection(connection, credentials, metrics);

        case ConnectionType.SSH:
          return SimulateSSHConnection(connection, credentials, metrics);

        case ConnectionType.Proxy:
          return SimulateProxyConnection(connection, credentials, metrics);

        case ConnectionType.Direct:
          return SimulateDirectConnection(connection, metrics);

        default:
          return Result<ConnectionMetrics>.Failure("Unsupported connection type");
      }
    }

    private Result<ConnectionMetrics> SimulateVPNConnection(
        NetworkConnection connection,
        NetworkCredentials credentials,
        ConnectionMetrics metrics)
    {
      if (credentials?.VPNCredentials == null)
        return Result<ConnectionMetrics>.Failure("VPN credentials required");

      var vpnCred = credentials.VPNCredentials;

      // Simulate VPN handshake delay
      metrics.Latency = UnityEngine.Random.Range(50f, 200f);
      metrics.Bandwidth = UnityEngine.Random.Range(10f, 100f);
      metrics.IsEncrypted = true;
      metrics.IsAuthenticated = true;

      // Add VPN overhead
      metrics.Latency += 20f;
      metrics.Bandwidth *= 0.8f;

      connection.EncryptionType = vpnCred.Protocol;
      connection.Parameters["VPNServer"] = vpnCred.ServerAddress;
      connection.Parameters["VPNProtocol"] = vpnCred.Protocol;

      return Result<ConnectionMetrics>.Success(metrics);
    }

    private Result<ConnectionMetrics> SimulateSSHConnection(
        NetworkConnection connection,
        NetworkCredentials credentials,
        ConnectionMetrics metrics)
    {
      // SSH tunneling simulation
      metrics.Latency = UnityEngine.Random.Range(30f, 150f);
      metrics.Bandwidth = UnityEngine.Random.Range(5f, 50f);
      metrics.IsEncrypted = true;
      metrics.IsAuthenticated = true;

      connection.EncryptionType = "SSH-2";

      return Result<ConnectionMetrics>.Success(metrics);
    }

    private Result<ConnectionMetrics> SimulateProxyConnection(
        NetworkConnection connection,
        NetworkCredentials credentials,
        ConnectionMetrics metrics)
    {
      // Proxy connection simulation
      metrics.Latency = UnityEngine.Random.Range(100f, 500f);
      metrics.Bandwidth = UnityEngine.Random.Range(1f, 25f);
      metrics.IsEncrypted = false;
      metrics.IsAuthenticated = false;

      return Result<ConnectionMetrics>.Success(metrics);
    }

    private Result<ConnectionMetrics> SimulateDirectConnection(
        NetworkConnection connection,
        ConnectionMetrics metrics)
    {
      // Direct connection (best performance)
      metrics.Latency = UnityEngine.Random.Range(1f, 50f);
      metrics.Bandwidth = UnityEngine.Random.Range(50f, 1000f);
      metrics.IsEncrypted = false;
      metrics.IsAuthenticated = false;

      return Result<ConnectionMetrics>.Success(metrics);
    }

    /// <summary>
    /// Generate connection report
    /// </summary>
    public string GenerateConnectionReport()
    {
      var report = new StringBuilder();
      report.AppendLine("ACTIVE NETWORK CONNECTIONS");
      report.AppendLine("=========================");
      report.AppendLine();

      if (!activeConnections.Any())
      {
        report.AppendLine("No active connections.");
        return report.ToString();
      }

      foreach (var connection in activeConnections.Values)
      {
        report.AppendLine($"Connection: {connection.ConnectionId}");
        report.AppendLine($"  Route: {connection.SourceNetworkId} -> {connection.TargetNetworkId}");
        report.AppendLine($"  Type: {connection.Type}");
        report.AppendLine($"  Status: {connection.Status}");
        report.AppendLine($"  Latency: {connection.Latency:F1}ms");
        report.AppendLine($"  Bandwidth: {connection.Bandwidth:F1} Mbps");
        report.AppendLine($"  Quality: {connection.GetQualityScore()}/100");
        report.AppendLine($"  Encrypted: {(connection.IsEncrypted ? "Yes" : "No")}");
        if (connection.IsEncrypted)
          report.AppendLine($"  Encryption: {connection.EncryptionType}");
        report.AppendLine($"  Connected: {connection.EstablishedTime:HH:mm:ss}");
        report.AppendLine($"  Last Activity: {connection.LastActivity:HH:mm:ss}");
        report.AppendLine();
      }

      return report.ToString();
    }

    private class ConnectionMetrics
    {
      public float Latency { get; set; }
      public float Bandwidth { get; set; }
      public bool IsEncrypted { get; set; }
      public bool IsAuthenticated { get; set; }
    }
  }
}
