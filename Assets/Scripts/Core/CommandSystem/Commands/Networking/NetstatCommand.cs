using System.Collections.Generic;
using System.Linq;
using System.Text;
using SampleOS.Core.Networking;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
    public class NetstatCommand : CommandBase
    {
        public override string Name => "netstat";
        public override string Description => "Display network devices and connections";
        public override string Usage => "netstat [-a] [-d] [-c] [-n] [-t] [-l]";

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            // Validate service availability
            if (context.CurrentNetwork == null)
            {
                WriteError(context, "Network not available");
                return CommandResult.Error("Network not available");
            }

            if (context.CurrentDevice == null)
            {
                WriteError(context, "Device context not available");
                return CommandResult.Error("Device not available");
            }

            // Parse options
            bool showAll = false;
            bool showDevices = false;
            bool showConnections = false;
            bool numericOnly = false;
            bool tcpOnly = false;
            bool listeningOnly = false;

            if (args.Length == 0)
            {
                // Default: show devices
                showDevices = true;
            }
            else
            {
                foreach (var arg in args)
                {
                    switch (arg)
                    {
                        case "-a":
                            showAll = true;
                            break;
                        case "-d":
                            showDevices = true;
                            break;
                        case "-c":
                            showConnections = true;
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
                        case "-h":
                        case "--help":
                            DisplayHelp(context);
                            return CommandResult.Ok();
                        default:
                            WriteError(context, $"Unknown option: {arg}");
                            DisplayHelp(context);
                            return CommandResult.Error($"Unknown option: {arg}");
                    }
                }
            }

            // Show all means both devices and connections
            if (showAll)
            {
                showDevices = true;
                showConnections = true;
            }

            // Display devices
            if (showDevices)
            {
                DisplayNetworkDevices(context);

                if (showConnections)
                {
                    WriteOutput(context, ""); // Add spacing
                }
            }

            // Display connections
            if (showConnections)
            {
                DisplayConnections(context, numericOnly, tcpOnly, listeningOnly);
            }

            return CommandResult.Ok();
        }

        private void DisplayNetworkDevices(CommandContext context)
        {
            // Display a header
            context.Stdout.SetColor(new Color(0.3f, 0.7f, 1f));
            WriteOutput(context, "NETWORK DEVICES");
            WriteOutput(context, "===============");
            context.Stdout.SetColor(Color.white);

            // Get devices from the current network via NetworkService
            var networkId = context.CurrentNetwork.NetworkId;
            var devices = context.NetworkService.GetDevicesInNetwork(networkId);

            if (devices.Count == 0)
            {
                WriteOutput(context, "No devices found on the network.");
                return;
            }

            // Format and display each device
            StringBuilder table = new StringBuilder();

            // Table header
            table.AppendLine("HOST            IP ADDRESS         STATUS    TYPE");
            table.AppendLine("--------------------------------------------------------");

            foreach (var device in devices)
            {
                string hostname = device.Hostname ?? "N/A";
                hostname = hostname.PadRight(15).Substring(0, 15);

                string ipAddress = device.IPAddress ?? "N/A";
                ipAddress = ipAddress.PadRight(18).Substring(0, 18);

                string status = device.IsOnline ? "online" : "offline";
                string type = device.DeviceType?.Name ?? "unknown";

                // Highlight compromised devices
                if (context.PlayerState.HasCompromisedSystem(device.Hostname))
                {
                    context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
                    table.AppendLine($"{hostname} {ipAddress} {status.PadRight(9)} {type} [OWNED]");
                    context.Stdout.SetColor(Color.white);
                }
                else
                {
                    table.AppendLine($"{hostname} {ipAddress} {status.PadRight(9)} {type}");
                }
            }

            WriteOutput(context, table.ToString().TrimEnd());
            WriteOutput(context, "");
            WriteOutput(context, $"Total devices: {devices.Count}");
        }

        private void DisplayConnections(CommandContext context, bool numericOnly, bool tcpOnly, bool listeningOnly)
        {
            context.Stdout.SetColor(new Color(0.3f, 0.7f, 1f));
            WriteOutput(context, "ACTIVE CONNECTIONS");
            WriteOutput(context, "==================");
            context.Stdout.SetColor(Color.white);

            // Get connections from current device and player state
            var connections = GetConnections(context, listeningOnly);

            if (connections.Count == 0)
            {
                WriteOutput(context, "No active connections.");
                return;
            }

            // Display header
            WriteOutput(context, "Proto Recv-Q Send-Q Local Address           Foreign Address         State       Process");
            WriteOutput(context, "----------------------------------------------------------------------------------------------------");

            // Display connections
            foreach (var conn in connections)
            {
                if (tcpOnly && conn.Protocol != "tcp")
                    continue;

                string localAddr = numericOnly ? conn.LocalAddress : ResolveAddress(conn.LocalAddress, context);
                string foreignAddr = numericOnly ? conn.ForeignAddress : ResolveAddress(conn.ForeignAddress, context);

                string line = string.Format("{0,-5} {1,6} {2,6} {3,-23} {4,-23} {5,-11} {6}",
                    conn.Protocol,
                    conn.RecvQueue,
                    conn.SendQueue,
                    localAddr,
                    foreignAddr,
                    conn.State,
                    conn.Process);

                // Color code by state
                if (conn.State == "ESTABLISHED")
                {
                    context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
                }
                else if (conn.State == "LISTEN")
                {
                    context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
                }

                WriteOutput(context, line);
                context.Stdout.SetColor(Color.white);
            }

            WriteOutput(context, "");
            WriteOutput(context, $"Total connections: {connections.Count}");
        }

        private List<NetworkConnection> GetConnections(CommandContext context, bool listeningOnly)
        {
            var connections = new List<NetworkConnection>();
            var currentDevice = context.CurrentDevice;
            string localIP = currentDevice.IPAddress ?? "127.0.0.1";

            // 1. Add listening services from installed software
            if (currentDevice.InstalledSoftware != null)
            {
                foreach (var software in currentDevice.InstalledSoftware)
                {
                    if (software.IsRunning && software.ListeningPorts != null)
                    {
                        foreach (var port in software.ListeningPorts)
                        {
                            connections.Add(new NetworkConnection
                            {
                                Protocol = "tcp",
                                LocalAddress = $"{localIP}:{port}",
                                ForeignAddress = "0.0.0.0:*",
                                State = "LISTEN",
                                RecvQueue = 0,
                                SendQueue = 0,
                                Process = software.Name
                            });
                        }
                    }
                }
            }

            // Don't show established connections if listening-only flag is set
            if (listeningOnly)
                return connections;

            // 2. Show active SSH connections (from PlayerState)
            var activeConnections = context.PlayerState.ActiveConnections;
            if (activeConnections != null && activeConnections.Count > 0)
            {
                foreach (var remoteConn in activeConnections)
                {
                    // Outbound connection
                    connections.Add(new NetworkConnection
                    {
                        Protocol = "tcp",
                        LocalAddress = $"{localIP}:{UnityEngine.Random.Range(50000, 60000)}", // Ephemeral port
                        ForeignAddress = $"{remoteConn.TargetDevice.IPAddress}:22",
                        State = "ESTABLISHED",
                        RecvQueue = 0,
                        SendQueue = 0,
                        Process = "ssh"
                    });
                }
            }

            // 3. Show connections to other devices on the network (simulated background traffic)
            if (currentDevice.IsOnline)
            {
                // DNS lookups
                connections.Add(new NetworkConnection
                {
                    Protocol = "udp",
                    LocalAddress = $"{localIP}:{UnityEngine.Random.Range(50000, 60000)}",
                    ForeignAddress = "8.8.8.8:53",
                    State = "",
                    RecvQueue = 0,
                    SendQueue = 0,
                    Process = "systemd-resolve"
                });

                // DHCP client
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

                // Random background connection (if device has internet access)
                var network = context.CurrentNetwork;
                if (network?.Metadata.Type == NetworkType.Corporate ||
                    network?.Metadata.Type == NetworkType.ISP)
                {
                    connections.Add(new NetworkConnection
                    {
                        Protocol = "tcp",
                        LocalAddress = $"{localIP}:{UnityEngine.Random.Range(50000, 60000)}",
                        ForeignAddress = $"{UnityEngine.Random.Range(1, 255)}.{UnityEngine.Random.Range(1, 255)}.{UnityEngine.Random.Range(1, 255)}.{UnityEngine.Random.Range(1, 255)}:443",
                        State = "ESTABLISHED",
                        RecvQueue = 0,
                        SendQueue = 0,
                        Process = "firefox"
                    });
                }
            }

            return connections;
        }

        private string ResolveAddress(string address, CommandContext context)
        {
            // Simple hostname resolution using NetworkService
            if (address.Contains("127.0.0.1"))
                return address.Replace("127.0.0.1", "localhost");

            if (address.Contains("0.0.0.0"))
                return address;

            // Try to resolve IP to hostname
            var parts = address.Split(':');
            if (parts.Length >= 1)
            {
                string ip = parts[0];
                var network = context.CurrentNetwork;

                if (network != null)
                {
                    var device = network.GetDeviceByIP(ip);
                    if (device != null)
                    {
                        return address.Replace(ip, device.Hostname);
                    }
                }
            }

            return address;
        }

        private void DisplayHelp(CommandContext context)
        {
            WriteOutput(context, $"Usage: {Usage}");
            WriteOutput(context, "");
            WriteOutput(context, "Options:");
            WriteOutput(context, "  -a    Show all information (devices and connections)");
            WriteOutput(context, "  -d    Show network devices (default)");
            WriteOutput(context, "  -c    Show active connections");
            WriteOutput(context, "  -n    Show numeric addresses only");
            WriteOutput(context, "  -t    Show TCP connections only");
            WriteOutput(context, "  -l    Show listening connections only");
            WriteOutput(context, "  -h    Display this help message");
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
