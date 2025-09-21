using System.Collections.Generic;

public class NetworkGateway
{
  public string GatewayId { get; set; }
  public string SystemHostname { get; set; }
  public string TargetNetworkId { get; set; }
  public GatewayType Type { get; set; }
  public bool IsActive { get; set; } = true;
  public Dictionary<string, object> ConnectionProperties { get; set; } = new Dictionary<string, object>();

  public NetworkGateway(string id, string systemHost, string targetNetwork, GatewayType type)
  {
    GatewayId = id;
    SystemHostname = systemHost;
    TargetNetworkId = targetNetwork;
    Type = type;
  }
}

public enum GatewayType
{
  VPNServer,        // VPN endpoint requiring credentials
  Router,           // Network router with routing tables
  ProxyServer,      // HTTP/SOCKS proxy
  JumpBox,          // SSH jump host
  VPNClient,        // System with VPN client installed
  TunnelEndpoint,   // Special tunnel connections
  Firewall          // Firewall with rules and exceptions
}
