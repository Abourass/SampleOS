using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using SampleOS.Core.Devices;
using SampleOS.Core.Networking;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
    /// <summary>
    /// Network scanner - discovers devices on current or specified network
    /// </summary>
    public class NmapCommand : CommandBase, IAsyncCommand
    {
        public override string Name => "nmap";
        public override string Description => "Scan network for active devices and open ports";
        public override string Usage => "nmap [target] [-p ports] [--aggressive]";
        public bool SupportsCancellation => true;

        // No constructor dependencies!
        public NmapCommand() { }

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            return CommandResult.Error("This command requires async execution");
        }

        public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
        {
            try
            {
                // Parse arguments
                var scanOptions = ParseArguments(args);

                // Get current network from WorldService
                var currentNetwork = context.CurrentNetwork;
                if (currentNetwork == null)
                {
                    WriteError(context, "No network available");
                    return CommandResult.Error("No network");
                }

                // Determine target network
                VirtualNetwork targetNetwork = currentNetwork;

                if (!string.IsNullOrEmpty(scanOptions.TargetNetwork))
                {
                    // Check if player has access to scan other networks
                    var allNetworks = context.WorldService.GetAllNetworks();
                    targetNetwork = allNetworks.FirstOrDefault(n =>
                        n.NetworkId == scanOptions.TargetNetwork ||
                        n.Metadata.Name.Contains(scanOptions.TargetNetwork, StringComparison.OrdinalIgnoreCase));

                    if (targetNetwork == null)
                    {
                        WriteError(context, $"Network '{scanOptions.TargetNetwork}' not found or not accessible");
                        return CommandResult.Error("Network not found");
                    }

                    // Check if player has VPN connection or gateway access
                    if (targetNetwork.NetworkId != currentNetwork.NetworkId)
                    {
                        if (!CanAccessNetwork(context, targetNetwork))
                        {
                            WriteError(context, $"Cannot scan {targetNetwork.Metadata.Name} - no route to network");
                            WriteError(context, "Hint: You may need VPN credentials or a compromised gateway");
                            return CommandResult.Error("Access denied");
                        }
                    }
                }

                // Display scan header
                DisplayScanHeader(context, targetNetwork, scanOptions);

                // Perform the scan
                var discoveredDevices = await PerformScan(
                    context,
                    targetNetwork,
                    scanOptions
                );

                // Display results
                DisplayScanResults(context, discoveredDevices, scanOptions);

                return CommandResult.Ok();
            }
            catch (OperationCanceledException)
            {
                WriteOutput(context, "");
                WriteOutput(context, "Scan cancelled by user.");
                return CommandResult.Error("Cancelled");
            }
            catch (Exception ex)
            {
                WriteError(context, $"Scan failed: {ex.Message}");
                return CommandResult.FromException(ex);
            }
        }

        private ScanOptions ParseArguments(string[] args)
        {
            var options = new ScanOptions
            {
                Ports = new List<int> { 21, 22, 23, 25, 80, 443, 3306, 5432, 8080 }, // Default ports
                IsAggressive = false,
                TargetNetwork = null
            };

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "-p" && i + 1 < args.Length)
                {
                    // Parse port specification
                    options.Ports = ParsePorts(args[i + 1]);
                    i++;
                }
                else if (args[i] == "--aggressive" || args[i] == "-A")
                {
                    options.IsAggressive = true;
                }
                else if (!args[i].StartsWith("-"))
                {
                    // Target network/subnet
                    options.TargetNetwork = args[i];
                }
            }

            return options;
        }

        private List<int> ParsePorts(string portSpec)
        {
            var ports = new List<int>();

            if (portSpec.Contains("-"))
            {
                // Port range: 80-100
                var parts = portSpec.Split('-');
                if (int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int port = start; port <= end && port <= 65535; port++)
                    {
                        ports.Add(port);
                    }
                }
            }
            else if (portSpec.Contains(","))
            {
                // Port list: 80,443,8080
                foreach (var portStr in portSpec.Split(','))
                {
                    if (int.TryParse(portStr.Trim(), out int port))
                    {
                        ports.Add(port);
                    }
                }
            }
            else if (int.TryParse(portSpec, out int singlePort))
            {
                ports.Add(singlePort);
            }

            return ports;
        }

        private bool CanAccessNetwork(CommandContext context, VirtualNetwork targetNetwork)
        {
            var currentNetwork = context.CurrentNetwork;

            // Can always scan current network
            if (targetNetwork.NetworkId == currentNetwork.NetworkId)
                return true;

            // Check for VPN credentials
            if (context.PlayerState.Credentials.HasVPNCredentialsFor(targetNetwork.NetworkId))
                return true;

            // Check for compromised gateway devices that connect networks
            var gateways = currentNetwork.GetActiveGatewayDevices();
            foreach (var gateway in gateways)
            {
                // Check if this gateway connects to target network
                if (gateway.NetworkId == targetNetwork.NetworkId ||
                    IsGatewayBetweenNetworks(context, gateway, currentNetwork.NetworkId, targetNetwork.NetworkId))
                {
                    // Check if player has compromised this gateway
                    if (context.PlayerState.HasCompromisedSystem(gateway.Hostname))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        private bool IsGatewayBetweenNetworks(CommandContext context, Device gateway, string sourceNetworkId, string targetNetworkId)
        {
            // Check if gateway has routes to both networks
            // This would require additional network topology data
            // For now, simplified check
            return gateway.DeviceType.Category == DeviceCategory.Router;
        }

        private async Task<List<DiscoveredDevice>> PerformScan(
            CommandContext context,
            VirtualNetwork network,
            ScanOptions options)
        {
            var discovered = new List<DiscoveredDevice>();

            // Get all devices in target network from NetworkService
            var devices = context.NetworkService.GetDevicesInNetwork(network.NetworkId);

            WriteOutput(context, $"Starting Nmap scan on {network.Metadata.IPRange}");
            WriteOutput(context, $"Scanning {devices.Count} potential hosts...");
            WriteOutput(context, "");

            int scannedCount = 0;
            int totalDevices = devices.Count;

            foreach (var device in devices)
            {
                // Check cancellation
                context.CancellationToken.ThrowIfCancellationRequested();

                scannedCount++;
                ReportProgress(context, (float)scannedCount / totalDevices, $"Scanning {device.IPAddress}");

                // Simulate scan delay based on network latency
                int scanDelay = options.IsAggressive ? 50 : 200;
                await Task.Delay(scanDelay, context.CancellationToken);

                // Only discover online devices
                if (!device.IsOnline)
                    continue;

                // Determine what information we can gather based on device security
                var discoveredDevice = new DiscoveredDevice
                {
                    Device = device,
                    IsResponding = true,
                    OpenPorts = new List<PortInfo>()
                };

                // Scan ports
                if (device.InstalledSoftware != null)
                {
                    foreach (var software in device.InstalledSoftware)
                    {
                        if (software.ListeningPorts != null)
                        {
                            foreach (var port in software.ListeningPorts)
                            {
                                // Only show ports we're scanning for
                                if (options.Ports.Contains(port))
                                {
                                    var portInfo = new PortInfo
                                    {
                                        Port = port,
                                        State = "open",
                                        Service = software.Name,
                                        Version = options.IsAggressive ? software.Version.ToString() : null
                                    };

                                    discoveredDevice.OpenPorts.Add(portInfo);
                                }
                            }
                        }
                    }
                }

                // Aggressive scan reveals more information
                if (options.IsAggressive)
                {
                    discoveredDevice.OperatingSystem = "Linux"; // Simplified
                    discoveredDevice.Hostname = device.Hostname;
                }
                else
                {
                    // Normal scan might only show IP
                    discoveredDevice.Hostname = null;
                }

                discovered.Add(discoveredDevice);
            }

            return discovered;
        }

        private void DisplayScanHeader(CommandContext context, VirtualNetwork network, ScanOptions options)
        {
            context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
            WriteOutput(context, "═══════════════════════════════════════════════════════════");
            WriteOutput(context, $"                    NMAP SCAN                             ");
            WriteOutput(context, "═══════════════════════════════════════════════════════════");
            context.Stdout.SetColor(Color.white);
            WriteOutput(context, "");
            WriteOutput(context, $"Target Network: {network.Metadata.Name} ({network.Metadata.IPRange})");
            WriteOutput(context, $"Scan Type:      {(options.IsAggressive ? "Aggressive (-A)" : "Normal")}");
            WriteOutput(context, $"Ports:          {string.Join(", ", options.Ports.Take(10))}{(options.Ports.Count > 10 ? "..." : "")}");
            WriteOutput(context, "");
        }

        private void DisplayScanResults(CommandContext context, List<DiscoveredDevice> devices, ScanOptions options)
        {
            WriteOutput(context, "");
            context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
            WriteOutput(context, "═══════════════════════════════════════════════════════════");
            WriteOutput(context, "                    SCAN RESULTS                           ");
            WriteOutput(context, "═══════════════════════════════════════════════════════════");
            context.Stdout.SetColor(Color.white);
            WriteOutput(context, "");

            if (devices.Count == 0)
            {
                WriteOutput(context, "No active hosts found on this network.");
                return;
            }

            WriteOutput(context, $"Found {devices.Count} active host(s):");
            WriteOutput(context, "");

            foreach (var discovered in devices)
            {
                var device = discovered.Device;

                // Device header
                context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f)); // Light blue
                if (!string.IsNullOrEmpty(discovered.Hostname))
                {
                    WriteOutput(context, $"Nmap scan report for {discovered.Hostname} ({device.IPAddress})");
                }
                else
                {
                    WriteOutput(context, $"Nmap scan report for {device.IPAddress}");
                }
                context.Stdout.SetColor(Color.white);

                WriteOutput(context, $"Host is up");

                // Show OS if aggressive scan
                if (options.IsAggressive && !string.IsNullOrEmpty(discovered.OperatingSystem))
                {
                    WriteOutput(context, $"OS: {discovered.OperatingSystem}");
                }

                WriteOutput(context, "");

                // Port information
                if (discovered.OpenPorts.Count > 0)
                {
                    WriteOutput(context, "PORT     STATE    SERVICE         VERSION");
                    WriteOutput(context, "--------------------------------------------------------");

                    foreach (var portInfo in discovered.OpenPorts.OrderBy(p => p.Port))
                    {
                        string portStr = $"{portInfo.Port}/tcp".PadRight(9);
                        string stateStr = portInfo.State.PadRight(9);
                        string serviceStr = portInfo.Service.PadRight(16);
                        string versionStr = portInfo.Version ?? "";

                        // Color based on common vulnerable ports
                        if (IsCommonlyVulnerablePort(portInfo.Port))
                        {
                            context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
                        }

                        WriteOutput(context, $"{portStr}{stateStr}{serviceStr}{versionStr}");
                        context.Stdout.SetColor(Color.white);
                    }
                }
                else
                {
                    WriteOutput(context, "No open ports detected on scanned range.");
                }

                WriteOutput(context, "");
            }

            // Summary
            int totalOpenPorts = devices.Sum(d => d.OpenPorts.Count);
            WriteOutput(context, $"Nmap done: {devices.Count} IP address(es) scanned");
            WriteOutput(context, $"Total open ports found: {totalOpenPorts}");

            // Hint about next steps
            if (devices.Any(d => d.OpenPorts.Any()))
            {
                WriteOutput(context, "");
                context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f)); // Gray
                WriteOutput(context, "Hint: Use 'vulnscan' on a device to find vulnerabilities in discovered services.");
                WriteOutput(context, "      Use 'ssh <username>@<host>' to attempt connection.");
                context.Stdout.SetColor(Color.white);
            }
        }

        private bool IsCommonlyVulnerablePort(int port)
        {
            // Ports that often have vulnerabilities
            return port == 21 ||  // FTP
                   port == 23 ||  // Telnet
                   port == 445 || // SMB
                   port == 3389;  // RDP
        }

        // Helper classes
        private class ScanOptions
        {
            public List<int> Ports { get; set; }
            public bool IsAggressive { get; set; }
            public string TargetNetwork { get; set; }
        }

        private class DiscoveredDevice
        {
            public Device Device { get; set; }
            public bool IsResponding { get; set; }
            public string Hostname { get; set; }
            public string OperatingSystem { get; set; }
            public List<PortInfo> OpenPorts { get; set; }
        }

        private class PortInfo
        {
            public int Port { get; set; }
            public string State { get; set; }
            public string Service { get; set; }
            public string Version { get; set; }
        }
    }
}
