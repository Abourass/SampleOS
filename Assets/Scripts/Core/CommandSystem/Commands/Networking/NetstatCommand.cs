using System.Collections.Generic;
using System.Linq;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
    public class NetstatCommand : CommandBase
    {
        public override string Name => "netstat";
        public override string Description => "Display network connections and statistics";
        public override string Usage => "netstat [-a] [-n] [-t] [-l]";

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            if (context.Network == null)
            {
                WriteError(context, "Network not available");
                return CommandResult.Error("Network not available");
            }

            // Parse options
            bool showAll = false;
            bool numericOnly = false;
            bool tcpOnly = false;
            bool listeningOnly = false;

            foreach (var arg in args)
            {
                switch (arg)
                {
                    case "-a":
                        showAll = true;
                        break;
                    case "-n":
                        numericOnly = true;
                        break;
                    case "-t":
                        tcpOnly = true;
                        break;
                    case "-l":
                        listeningOnly = true;
                        break;
                    default:
                        WriteError(context, $"Unknown option: {arg}");
                        WriteError(context, Usage);
                        return CommandResult.Error($"Unknown option: {arg}");
                }
            }

            // Get connections
            var connections = GetConnections(context, showAll, listeningOnly);

            // Display header
            WriteOutput(context, "Proto Recv-Q Send-Q Local Address           Foreign Address         State");

            // Display connections
            foreach (var conn in connections)
            {
                if (tcpOnly && conn.Protocol != "tcp")
                    continue;

                string localAddr = numericOnly ? conn.LocalAddress : ResolveAddress(conn.LocalAddress);
                string foreignAddr = numericOnly ? conn.ForeignAddress : ResolveAddress(conn.ForeignAddress);

                string line = string.Format("{0,-5} {1,6} {2,6} {3,-23} {4,-23} {5}",
                    conn.Protocol,
                    conn.RecvQueue,
                    conn.SendQueue,
                    localAddr,
                    foreignAddr,
                    conn.State);

                WriteOutput(context, line);
            }

            return CommandResult.Ok();
        }

        private List<NetworkConnection> GetConnections(CommandContext context, bool showAll, bool listeningOnly)
        {
            var connections = new List<NetworkConnection>();

            // Get local system
            var localSystem = context.CurrentSystem ?? context.Network.GetLocalSystem();
            string localIP = localSystem?.IPAddress ?? "127.0.0.1";

            // Add listening services
            if (localSystem != null)
            {
                foreach (var port in localSystem.GetOpenPorts())
                {
                    var software = localSystem.GetSoftwareOnPort(port);
                    connections.Add(new NetworkConnection
                    {
                        Protocol = "tcp",
                        LocalAddress = $"{localIP}:{port}",
                        ForeignAddress = "0.0.0.0:*",
                        State = "LISTEN",
                        RecvQueue = 0,
                        SendQueue = 0,
                        Process = software?.Name ?? "unknown"
                    });
                }
            }

            // Add established connections if showing all
            if (showAll && !listeningOnly)
            {
                // Check if connected to any systems via SSH
                if (context.CurrentSystem != null && context.CurrentSystem != context.Network.GetLocalSystem())
                {
                    connections.Add(new NetworkConnection
                    {
                        Protocol = "tcp",
                        LocalAddress = $"{localIP}:56789",
                        ForeignAddress = $"{context.CurrentSystem.IPAddress}:22",
                        State = "ESTABLISHED",
                        RecvQueue = 0,
                        SendQueue = 0,
                        Process = "ssh"
                    });
                }

                // Add some typical background connections
                connections.Add(new NetworkConnection
                {
                    Protocol = "tcp",
                    LocalAddress = $"{localIP}:45123",
                    ForeignAddress = "8.8.8.8:53",
                    State = "ESTABLISHED",
                    RecvQueue = 0,
                    SendQueue = 0,
                    Process = "systemd-resolve"
                });

                connections.Add(new NetworkConnection
                {
                    Protocol = "udp",
                    LocalAddress = $"{localIP}:68",
                    ForeignAddress = "0.0.0.0:*",
                    State = "",
                    RecvQueue = 0,
                    SendQueue = 0,
                    Process = "dhclient"
                });
            }

            return connections;
        }

        private string ResolveAddress(string address)
        {
            // Simple hostname resolution
            if (address.Contains("127.0.0.1"))
                return address.Replace("127.0.0.1", "localhost");
            if (address.Contains("0.0.0.0"))
                return address;

            // Could expand this to resolve other known hosts
            return address;
        }

        private class NetworkConnection
        {
            public string Protocol { get; set; }
            public string LocalAddress { get; set; }
            public string ForeignAddress { get; set; }
            public string State { get; set; }
            public int RecvQueue { get; set; }
            public int SendQueue { get; set; }
            public string Process { get; set; }
        }
    }
}
