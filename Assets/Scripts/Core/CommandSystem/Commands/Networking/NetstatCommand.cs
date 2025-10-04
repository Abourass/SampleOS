using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
    public class NetstatCommand : CommandBase
    {
        public override string Name => "netstat";
        public override string Description => "Display network devices and connections";
        public override string Usage => "netstat [-a] [-d] [-c] [-n] [-t] [-l]";

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            if (context.CurrentNetwork == null)
            {
                WriteError(context, "Network not available");
                return CommandResult.Error("Network not available");
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
            WriteOutput(context, "NETWORK DEVICES");
            WriteOutput(context, "===============");

            // Get devices from the network
            var devices = context.CurrentNetwork.GetAllDevices();

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

                string status = "online";
                string type = device.DeviceType.Name ?? "unknown";

                table.AppendLine($"{hostname} {ipAddress} {status.PadRight(9)} {type}");
            }

            WriteOutput(context, table.ToString().TrimEnd());
        }

        private void DisplayConnections(CommandContext context, bool numericOnly, bool tcpOnly, bool listeningOnly)
        {
            WriteOutput(context, "ACTIVE CONNECTIONS");
            WriteOutput(context, "==================");

            // Get connections
            var connections = GetConnections(context, listeningOnly);

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
        }

        private List<NetworkConnection> GetConnections(CommandContext context, bool listeningOnly)
        {
            var connections = new List<NetworkConnection>();

            // Get current device/system
            var currentDevice = context.CurrentDevice;
            string localIP = currentDevice?.IPAddress ?? "127.0.0.1";

            // Add listening services if we have access to the current system
            if (currentDevice != null)
            {
                // Get open ports from installed software
                foreach (var software in currentDevice.InstalledSoftware)
                {
                    var openPorts = software.ListeningPorts;
                    foreach (var port in openPorts)
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

            // Add established connections if not listening-only
            if (!listeningOnly)
            {
                // Check if connected to any systems via SSH
                var devices = context.CurrentNetwork.GetAllDevices();
                var localDevice = devices.FirstOrDefault(d => d.IPAddress == localIP);

                if (localDevice != null && currentDevice != null && localDevice.DeviceId != currentDevice.DeviceId)
                {
                    connections.Add(new NetworkConnection
                    {
                        Protocol = "tcp",
                        LocalAddress = $"{localIP}:56789",
                        ForeignAddress = $"{currentDevice.IPAddress}:22",
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
