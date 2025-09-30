using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
    public class NmapCommand : CommandBase, IAsyncCommand
    {
        public override string Name => "nmap";
        public override string Description => "Network exploration and port scanning";
        public override string Usage => "nmap <target> [-p ports] [-sV]";

        public bool SupportsCancellation => true;

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            // Synchronous execution for backward compatibility
            return ExecuteAsync(args, context).GetAwaiter().GetResult();
        }

        public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
        {
            if (args.Length == 0)
            {
                WriteError(context, "Usage: nmap <target> [-p ports] [-sV]");
                return CommandResult.Error("Missing target");
            }

            string target = args[0];
            string portRange = "1-1000";  // Default
            bool serviceVersion = false;

            // Parse options
            for (int i = 1; i < args.Length; i++)
            {
                if (args[i] == "-p" && i + 1 < args.Length)
                {
                    portRange = args[++i];
                }
                else if (args[i] == "-sV")
                {
                    serviceVersion = true;
                }
            }

            // Resolve target
            var targetSystem = context.Network?.GetSystemByHostname(target);
            if (targetSystem == null)
            {
                WriteError(context, $"Failed to resolve host: {target}");
                return CommandResult.Error("Host not found");
            }

            // Start scan
            WriteOutput(context, $"Starting Nmap scan on {targetSystem.Hostname} ({targetSystem.IPAddress})");
            WriteOutput(context, "");

            // Simulate scanning delay
            ReportProgress(context, 0f, "Initiating scan");
            await Task.Delay(500, context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested)
                return CommandResult.Error("Scan cancelled");

            ReportProgress(context, 0.2f, "Discovering open ports");
            await Task.Delay(800, context.CancellationToken);

            if (context.CancellationToken.IsCancellationRequested)
                return CommandResult.Error("Scan cancelled");

            // Get open ports
            var openPorts = targetSystem.GetOpenPorts();
            var scanResults = new List<PortScanResult>();

            // Parse port range
            var portsToScan = ParsePortRange(portRange);
            int totalPorts = portsToScan.Count;
            int scanned = 0;

            foreach (var port in portsToScan)
            {
                if (context.CancellationToken.IsCancellationRequested)
                    return CommandResult.Error("Scan cancelled");

                scanned++;
                ReportProgress(context, 0.2f + (0.6f * scanned / totalPorts), $"Scanning port {port}");

                if (openPorts.Contains(port))
                {
                    var software = targetSystem.GetSoftwareOnPort(port);
                    scanResults.Add(new PortScanResult
                    {
                        Port = port,
                        State = "open",
                        Service = software?.Category ?? "unknown",
                        Version = serviceVersion && software != null ? $"{software.Name} {software.Version}" : ""
                    });

                    // Simulate some scanning time for open ports
                    await Task.Delay(100, context.CancellationToken);
                }

                // Small delay for realism
                if (scanned % 50 == 0)
                {
                    await Task.Delay(50, context.CancellationToken);
                }
            }

            ReportProgress(context, 0.9f, "Finalizing scan results");
            await Task.Delay(300, context.CancellationToken);

            // Display results
            WriteOutput(context, $"Nmap scan report for {targetSystem.Hostname} ({targetSystem.IPAddress})");
            WriteOutput(context, $"Host is up (latency: {Random.Range(0.001f, 0.05f):F3}s)");

            if (scanResults.Count == 0)
            {
                WriteOutput(context, "All scanned ports are closed");
            }
            else
            {
                WriteOutput(context, $"Not shown: {totalPorts - scanResults.Count} closed ports");
                WriteOutput(context, "");
                WriteOutput(context, "PORT      STATE   SERVICE" + (serviceVersion ? "     VERSION" : ""));

                foreach (var result in scanResults.OrderBy(r => r.Port))
                {
                    string line = $"{result.Port}/tcp".PadRight(10) +
                                  result.State.PadRight(8) +
                                  result.Service.PadRight(12);

                    if (serviceVersion && !string.IsNullOrEmpty(result.Version))
                    {
                        line += result.Version;
                    }

                    WriteOutput(context, line);
                }
            }

            WriteOutput(context, "");
            WriteOutput(context, "Nmap done: 1 IP address (1 host up) scanned");

            ReportProgress(context, 1.0f, "Scan complete");
            return CommandResult.Ok();
        }

        private List<int> ParsePortRange(string range)
        {
            var ports = new List<int>();

            if (range.Contains(","))
            {
                // Comma-separated ports
                foreach (var part in range.Split(','))
                {
                    ports.AddRange(ParsePortRange(part.Trim()));
                }
            }
            else if (range.Contains("-"))
            {
                // Port range
                var parts = range.Split('-');
                if (parts.Length == 2 && int.TryParse(parts[0], out int start) && int.TryParse(parts[1], out int end))
                {
                    for (int i = start; i <= end && i <= 65535; i++)
                    {
                        ports.Add(i);
                    }
                }
            }
            else if (int.TryParse(range, out int port))
            {
                // Single port
                ports.Add(port);
            }

            return ports;
        }

        private class PortScanResult
        {
            public int Port { get; set; }
            public string State { get; set; }
            public string Service { get; set; }
            public string Version { get; set; }
        }
    }
}
