using System;
using System.Threading.Tasks;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Vulnerabilities
{
  public class VulnScanCommand : CommandBase, IAsyncCommand
  {
    private PlayerVulnerabilityInventory vulnerabilityInventory;

    public override string Name => "vuln-scan";
    public override string Description => "Scan for vulnerabilities in network services";
    public override string Usage => "vuln-scan <host> [port]";

    public bool SupportsCancellation => true;

    public VulnScanCommand(PlayerVulnerabilityInventory inventory)
    {
      this.vulnerabilityInventory = inventory;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // Synchronous fallback
      return ExecuteAsync(args, context).GetAwaiter().GetResult();
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
    {
      try
      {
        if (args.Length < 1)
        {
          WriteError(context, $"Usage: {Usage}");
          return CommandResult.Error("Missing hostname");
        }

        string target = args[0];
        int? specificPort = null;

        if (args.Length > 1 && int.TryParse(args[1], out int parsedPort))
        {
          specificPort = parsedPort;
        }

        WriteOutput(context, $"Scanning {target} for vulnerabilities...");
        WriteOutput(context, "");

        // Check network availability
        if (context.Network == null)
        {
          WriteError(context, "Network not available");
          return CommandResult.Error("Network not available");
        }

        RemoteSystem system = context.Network.GetSystemByHostname(target);
        if (system == null)
        {
          WriteError(context, $"Host {target} not found.");
          return CommandResult.Error("Host not found");
        }

        // Start scan with progress
        ReportProgress(context, 0f, "Initializing scan");
        await Task.Delay(300, context.CancellationToken);

        if (context.CancellationToken.IsCancellationRequested)
          return CommandResult.Error("Scan cancelled");

        // Generate vulnerabilities if not already done
        WriteOutput(context, "Analyzing system configuration...");
        ReportProgress(context, 0.2f, "Analyzing system");
        system.GenerateVulnerabilities();
        await Task.Delay(500, context.CancellationToken);

        if (context.CancellationToken.IsCancellationRequested)
          return CommandResult.Error("Scan cancelled");

        // Get open ports
        WriteOutput(context, "Enumerating services...");
        ReportProgress(context, 0.4f, "Enumerating services");
        var ports = system.GetOpenPorts();
        await Task.Delay(400, context.CancellationToken);

        if (context.CancellationToken.IsCancellationRequested)
          return CommandResult.Error("Scan cancelled");

        bool vulnerabilitiesFound = false;
        int portsScanned = 0;

        foreach (int portNumber in ports)
        {
          if (specificPort.HasValue && portNumber != specificPort.Value)
            continue;

          Software software = system.GetSoftwareOnPort(portNumber);
          if (software == null) continue;

          portsScanned++;
          float progress = 0.4f + (0.5f * portsScanned / ports.Count);
          ReportProgress(context, progress, $"Checking port {portNumber}");

          WriteOutput(context, $"Checking {software.Name} v{software.Version} on port {portNumber}...");

          // Simulate scan time
          await Task.Delay(300, context.CancellationToken);

          if (context.CancellationToken.IsCancellationRequested)
            return CommandResult.Error("Scan cancelled");

          if (software.HasVulnerability())
          {
            // Found at least one vulnerability!
            vulnerabilitiesFound = true;
            Color vulnColor = new Color(1f, 0.5f, 0.5f); // Red

            foreach (var vuln in software.Vulnerabilities)
            {
              context.Stdout.SetColor(vulnColor);
              WriteOutput(context, $"[VULNERABLE] {vuln.CVE}: {vuln.Name} (Severity: {vuln.Severity}/10)");
              context.Stdout.SetColor(Color.white);
              WriteOutput(context, $"  {vuln.Description}");

              // Add to player's inventory
              vulnerabilityInventory.AddVulnerability(vuln, target, portNumber, software.Name);
            }
          }
          else
          {
            WriteOutput(context, "No vulnerabilities found.");
          }

          WriteOutput(context, "");
        }

        ReportProgress(context, 0.9f, "Finalizing scan");
        await Task.Delay(200, context.CancellationToken);

        if (!vulnerabilitiesFound)
        {
          WriteOutput(context, "No vulnerabilities were found on this system.");
        }
        else
        {
          context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
          WriteOutput(context, "Vulnerabilities added to your database.");
          context.Stdout.SetColor(Color.white);
          WriteOutput(context, "Use 'vulns' command to view your vulnerability inventory.");

          // Save vulnerabilities to a file in the user's home directory
          vulnerabilityInventory.SaveToLocalFile(context.Network.GetLocalSystem().FileSystem);
        }

        ReportProgress(context, 1.0f, "Scan complete");
        return CommandResult.Ok();
      }
      catch (Exception ex)
      {
        WriteError(context, $"Error during scan: {ex.Message}");
        Debug.LogError($"VulnScanCommand error: {ex}");
        return CommandResult.FromException(ex);
      }
    }
  }
}
