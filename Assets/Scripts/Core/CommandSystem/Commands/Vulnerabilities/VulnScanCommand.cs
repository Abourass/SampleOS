using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using SampleOS.Core.SoftwarePackages;
using SampleOS.Core.Devices;
using SampleOS.Core.Services;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Vulnerabilities
{
  /// <summary>
  /// Scans a device for vulnerabilities in its installed software
  /// </summary>
  public class VulnScanCommand : CommandBase, IAsyncCommand
  {
    public override string Name => "vulnscan";
    public override string Description => "Scan a device for software vulnerabilities";
    public override string Usage => "vulnscan [target] - Scan current or specified device for vulnerabilities";
    public bool SupportsCancellation => true;

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      return CommandResult.Error("This command requires async execution");
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
    {
      try
      {
        // Get current device from HackingSession service via context
        var targetDevice = context.CurrentDevice;
        if (targetDevice == null)
        {
          WriteError(context, "No device context available");
          return CommandResult.Error("No device");
        }

        if (args.Length > 0)
        {
          WriteError(context, "Remote scanning not yet implemented. Scanning current device.");
        }

        WriteOutput(context, $"Scanning {targetDevice.Hostname} ({targetDevice.IPAddress}) for vulnerabilities...");
        WriteOutput(context, "");

        // Simulate scan delay
        await Task.Delay(1500, context.CancellationToken);

        // Check if device has any software
        if (targetDevice.InstalledSoftware == null || targetDevice.InstalledSoftware.Count == 0)
        {
          WriteOutput(context, "No software detected on this device.");
          return CommandResult.Ok();
        }

        WriteOutput(context, $"Analyzing {targetDevice.InstalledSoftware.Count} installed software packages...");
        await Task.Delay(800, context.CancellationToken);
        WriteOutput(context, "");

        // Get vulnerability database service
        var vulnDb = ServiceLocator.Instance.Get<IVulnerabilityDatabaseService>();
        if (vulnDb == null)
        {
          WriteError(context, "Vulnerability database not available");
          return CommandResult.Error("Service unavailable");
        }

        // Scan each software package for vulnerabilities
        var foundVulnerabilities = new List<(Software software, Vulnerability vuln, int port)>();
        int totalSoftware = 0;
        int vulnerableSoftware = 0;

        foreach (var software in targetDevice.InstalledSoftware)
        {
          totalSoftware++;

          // Find vulnerabilities that affect this software version
          var vulns = vulnDb.GetVulnerabilitiesForSoftware(software);

          if (vulns.Count > 0)
          {
            vulnerableSoftware++;

            foreach (var vuln in vulns)
            {
              // Determine port (use first listening port or 0)
              int port = software.ListeningPorts?.FirstOrDefault() ?? 0;

              foundVulnerabilities.Add((software, vuln, port));

              // Add to player's progress via PlayerStateService
              context.PlayerState.Progress.AddDiscoveredVulnerability(
                  targetDevice.Hostname,
                  software.Name,
                  vuln,
                  port
              );
            }
          }

          // Simulate progressive scanning with progress reporting
          if (totalSoftware % 3 == 0)
          {
            float progress = (float)totalSoftware / targetDevice.InstalledSoftware.Count;
            ReportProgress(context, progress, $"Scanned {totalSoftware}/{targetDevice.InstalledSoftware.Count} packages");
            await Task.Delay(200, context.CancellationToken);
          }
        }

        // Display results
        DisplayScanResults(context, targetDevice, foundVulnerabilities, totalSoftware, vulnerableSoftware);

        // Save to file on current device using context.FileSystem
        if (foundVulnerabilities.Count > 0)
        {
          var report = context.PlayerState.GenerateVulnerabilityReport();
          context.FileSystem.CreateFile("/home/user/vulnerabilities.txt", report);

          WriteOutput(context, "");
          WriteOutput(context, "Results saved to ~/vulnerabilities.txt");
        }

        return CommandResult.Ok();
      }
      catch (OperationCanceledException)
      {
        WriteOutput(context, "");
        WriteOutput(context, "Scan cancelled.");
        return CommandResult.Error("Cancelled");
      }
      catch (Exception ex)
      {
        WriteError(context, $"Scan failed: {ex.Message}");
        return CommandResult.FromException(ex);
      }
    }

    private void DisplayScanResults(
        CommandContext context,
        Device device,
        List<(Software software, Vulnerability vuln, int port)> vulnerabilities,
        int totalSoftware,
        int vulnerableSoftware)
    {
      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
      WriteOutput(context, "═══════════════════════════════════════════════════════════════");
      WriteOutput(context, $"VULNERABILITY SCAN REPORT: {device.Hostname}");
      WriteOutput(context, "═══════════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Summary
      WriteOutput(context, "SUMMARY");
      WriteOutput(context, "-------");
      WriteOutput(context, $"Target:              {device.Hostname} ({device.IPAddress})");
      WriteOutput(context, $"Device Type:         {device.DeviceType.Name}");
      WriteOutput(context, $"Security Level:      {device.SecurityLevel}");
      WriteOutput(context, $"Software Scanned:    {totalSoftware}");
      WriteOutput(context, $"Vulnerable Packages: {vulnerableSoftware}");
      WriteOutput(context, $"Total Vulnerabilities: {vulnerabilities.Count}");
      WriteOutput(context, "");

      if (vulnerabilities.Count == 0)
      {
        context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
        WriteOutput(context, "✓ No vulnerabilities detected.");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "  This device appears to be secure.");
        return;
      }

      // Severity breakdown
      var critical = vulnerabilities.Count(v => v.vuln.Severity >= 9);
      var high = vulnerabilities.Count(v => v.vuln.Severity >= 7 && v.vuln.Severity < 9);
      var medium = vulnerabilities.Count(v => v.vuln.Severity >= 4 && v.vuln.Severity < 7);
      var low = vulnerabilities.Count(v => v.vuln.Severity < 4);

      WriteOutput(context, "SEVERITY BREAKDOWN");
      WriteOutput(context, "------------------");

      if (critical > 0)
      {
        context.Stdout.SetColor(new Color(1f, 0.2f, 0.2f));
        WriteOutput(context, $"🔴 Critical: {critical}");
      }
      if (high > 0)
      {
        context.Stdout.SetColor(new Color(1f, 0.5f, 0.2f));
        WriteOutput(context, $"🟠 High:     {high}");
      }
      if (medium > 0)
      {
        context.Stdout.SetColor(new Color(1f, 0.9f, 0.3f));
        WriteOutput(context, $"🟡 Medium:   {medium}");
      }
      if (low > 0)
      {
        context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f));
        WriteOutput(context, $"🟢 Low:      {low}");
      }

      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Detailed vulnerability list
      WriteOutput(context, "VULNERABILITIES DETECTED");
      WriteOutput(context, "------------------------");
      WriteOutput(context, "");

      // Group by software
      var groupedBySoftware = vulnerabilities
          .GroupBy(v => v.software)
          .OrderByDescending(g => g.Max(v => v.vuln.Severity));

      foreach (var softwareGroup in groupedBySoftware)
      {
        var software = softwareGroup.Key;
        var vulns = softwareGroup.ToList();

        context.Stdout.SetColor(new Color(0.8f, 0.8f, 1f));
        WriteOutput(context, $"📦 {software.Name} v{software.Version}");
        context.Stdout.SetColor(Color.white);

        if (software.ListeningPorts != null && software.ListeningPorts.Count > 0)
        {
          WriteOutput(context, $"   Ports: {string.Join(", ", software.ListeningPorts)}");
        }

        WriteOutput(context, "");

        foreach (var (_, vuln, port) in vulns)
        {
          string severityIcon = GetSeverityIcon(vuln.Severity);
          Color severityColor = GetSeverityColorValue(vuln.Severity);

          context.Stdout.SetColor(severityColor);
          WriteOutput(context, $"   {severityIcon} {vuln.CVE} - {vuln.Name}");
          context.Stdout.SetColor(Color.white);

          WriteOutput(context, $"      Severity: {vuln.Severity}/10 ({GetSeverityLabel(vuln.Severity)})");
          WriteOutput(context, $"      Type: {vuln.Type}");

          if (port > 0)
          {
            WriteOutput(context, $"      Port: {port}");
          }

          WriteOutput(context, $"      Description: {vuln.Description}");

          if (!string.IsNullOrEmpty(vuln.ExploitCommand))
          {
            context.Stdout.SetColor(new Color(1f, 1f, 0.3f));
            WriteOutput(context, $"      💡 Exploit: {vuln.ExploitCommand}");
            context.Stdout.SetColor(Color.white);
          }

          WriteOutput(context, "");
        }
      }

      // Recommendations
      context.Stdout.SetColor(new Color(0.3f, 0.8f, 1f));
      WriteOutput(context, "RECOMMENDATIONS");
      WriteOutput(context, "---------------");
      context.Stdout.SetColor(Color.white);

      if (critical > 0)
      {
        context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f));
        WriteOutput(context, "⚠️  CRITICAL vulnerabilities detected!");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, "    This device is highly vulnerable to exploitation.");
        WriteOutput(context, "");
      }

      WriteOutput(context, "• Use 'vulns' to view your discovered vulnerabilities");
      WriteOutput(context, "• Use 'exploit <cve> <host> <port>' to attempt exploitation");
      WriteOutput(context, "• Vulnerabilities saved to ~/vulnerabilities.txt");
      WriteOutput(context, "");

      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
      WriteOutput(context, "═══════════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
    }

    private string GetSeverityIcon(int severity)
    {
      if (severity >= 9) return "🔴";
      if (severity >= 7) return "🟠";
      if (severity >= 4) return "🟡";
      return "🟢";
    }

    private string GetSeverityLabel(int severity)
    {
      if (severity >= 9) return "CRITICAL";
      if (severity >= 7) return "HIGH";
      if (severity >= 4) return "MEDIUM";
      return "LOW";
    }

    private Color GetSeverityColorValue(int severity)
    {
      if (severity >= 9) return new Color(1f, 0.2f, 0.2f);
      if (severity >= 7) return new Color(1f, 0.5f, 0.2f);
      if (severity >= 4) return new Color(1f, 0.9f, 0.3f);
      return new Color(0.7f, 0.7f, 0.7f);
    }
  }
}
