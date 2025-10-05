using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SampleOS.Core.SoftwarePackages;
using SampleOS.Core.Devices;
using UnityEngine;
using System.Collections.Generic;

namespace SampleOS.Core.CommandSystem.Commands.Vulnerabilities
{
  /// <summary>
  /// Scans a device for vulnerabilities in its installed software
  /// </summary>
  public class VulnScanCommand : IAsyncCommand
  {
    public string Name => "vulnscan";
    public string Description => "Scan a device for software vulnerabilities";
    public string Usage => "vulnscan [target] - Scan current or specified device for vulnerabilities";

    private readonly PlayerVulnerabilityInventory vulnerabilityInventory;
    private readonly VulnerabilityDatabase vulnerabilityDatabase;
    public bool SupportsCancellation => true;

    public VulnScanCommand(PlayerVulnerabilityInventory inventory)
    {
      vulnerabilityInventory = inventory;
      vulnerabilityDatabase = new VulnerabilityDatabase();
    }

    public CommandResult Execute(string[] args, CommandContext context)
    {
      // This command must be async
      return CommandResult.Error("This command requires async execution. Use 'await' or run it properly.");
    }

    public async Task<CommandResult> ExecuteAsync(string[] args, CommandContext context)
    {
      try
      {
        // Determine target device
        Device targetDevice = context.CurrentDevice;
        string targetIdentifier = targetDevice.Hostname;

        if (args.Length > 0)
        {
          // TODO: Allow scanning remote devices if we have access
          context.Stderr.WriteLine("Remote scanning not yet implemented. Scanning current device.");
        }

        context.Stdout.WriteLine($"Scanning {targetDevice.Hostname} ({targetDevice.IPAddress}) for vulnerabilities...");
        context.Stdout.WriteLine("");

        // Simulate scan delay
        await Task.Delay(1500, context.CancellationToken);

        // Check if device has any software
        if (targetDevice.InstalledSoftware == null || targetDevice.InstalledSoftware.Count == 0)
        {
          context.Stdout.WriteLine("No software detected on this device.");
          return CommandResult.Ok();
        }

        context.Stdout.WriteLine($"Analyzing {targetDevice.InstalledSoftware.Count} installed software packages...");
        await Task.Delay(800, context.CancellationToken);
        context.Stdout.WriteLine("");

        // Scan each software package for vulnerabilities
        var foundVulnerabilities = new List<(Software software, Vulnerability vuln, int port)>();
        int totalSoftware = 0;
        int vulnerableSoftware = 0;

        foreach (var software in targetDevice.InstalledSoftware)
        {
          totalSoftware++;

          // Check if software has known vulnerabilities
          if (software.Vulnerabilities != null && software.Vulnerabilities.Count > 0)
          {
            vulnerableSoftware++;

            foreach (var vuln in software.Vulnerabilities)
            {
              // Determine port (use first listening port or 0)
              int port = software.ListeningPorts?.FirstOrDefault() ?? 0;

              foundVulnerabilities.Add((software, vuln, port));

              // Add to player's vulnerability inventory
              vulnerabilityInventory.AddVulnerability(
                  vuln,
                  targetDevice.Hostname,
                  port,
                  software.Name
              );
            }
          }

          // Simulate progressive scanning
          if (totalSoftware % 3 == 0)
          {
            await Task.Delay(200, context.CancellationToken);
          }
        }

        // Display results
        DisplayScanResults(context, targetDevice, foundVulnerabilities, totalSoftware, vulnerableSoftware);

        // Save to file on current device
        if (foundVulnerabilities.Count > 0)
        {
          vulnerabilityInventory.SaveToLocalFile(context.CurrentDevice.FileSystem);
          context.Stdout.WriteLine("");
          context.Stdout.WriteLine($"Results saved to ~/vulnerabilities.txt");
        }

        return CommandResult.Ok();
      }
      catch (OperationCanceledException)
      {
        context.Stdout.WriteLine("");
        context.Stdout.WriteLine("Scan cancelled.");
        return CommandResult.Error("Cancelled");
      }
      catch (Exception ex)
      {
        context.Stderr.WriteLine($"Scan failed: {ex.Message}");
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
      context.Stdout.WriteLine("═══════════════════════════════════════════════════════════════");
      context.Stdout.WriteLine($"VULNERABILITY SCAN REPORT: {device.Hostname}");
      context.Stdout.WriteLine("═══════════════════════════════════════════════════════════════");
      context.Stdout.WriteLine("");

      // Summary
      context.Stdout.WriteLine("SUMMARY");
      context.Stdout.WriteLine("-------");
      context.Stdout.WriteLine($"Target:              {device.Hostname} ({device.IPAddress})");
      context.Stdout.WriteLine($"Device Type:         {device.DeviceType.Name}");
      context.Stdout.WriteLine($"Security Level:      {device.SecurityLevel}");
      context.Stdout.WriteLine($"Software Scanned:    {totalSoftware}");
      context.Stdout.WriteLine($"Vulnerable Packages: {vulnerableSoftware}");
      context.Stdout.WriteLine($"Total Vulnerabilities: {vulnerabilities.Count}");
      context.Stdout.WriteLine("");

      if (vulnerabilities.Count == 0)
      {
        context.Stdout.WriteLine("✓ No vulnerabilities detected.");
        context.Stdout.WriteLine("  This device appears to be secure.");
        return;
      }

      // Severity breakdown
      var critical = vulnerabilities.Count(v => v.vuln.Severity >= 9);
      var high = vulnerabilities.Count(v => v.vuln.Severity >= 7 && v.vuln.Severity < 9);
      var medium = vulnerabilities.Count(v => v.vuln.Severity >= 4 && v.vuln.Severity < 7);
      var low = vulnerabilities.Count(v => v.vuln.Severity < 4);

      context.Stdout.WriteLine("SEVERITY BREAKDOWN");
      context.Stdout.WriteLine("------------------");
      if (critical > 0) context.Stdout.WriteLine($"🔴 Critical: {critical}");
      if (high > 0) context.Stdout.WriteLine($"🟠 High:     {high}");
      if (medium > 0) context.Stdout.WriteLine($"🟡 Medium:   {medium}");
      if (low > 0) context.Stdout.WriteLine($"🟢 Low:      {low}");
      context.Stdout.WriteLine("");

      // Detailed vulnerability list
      context.Stdout.WriteLine("VULNERABILITIES DETECTED");
      context.Stdout.WriteLine("------------------------");
      context.Stdout.WriteLine("");

      // Group by software
      var groupedBySoftware = vulnerabilities
          .GroupBy(v => v.software)
          .OrderByDescending(g => g.Max(v => v.vuln.Severity));

      foreach (var softwareGroup in groupedBySoftware)
      {
        var software = softwareGroup.Key;
        var vulns = softwareGroup.ToList();

        context.Stdout.WriteLine($"📦 {software.Name} v{software.Version}");

        if (software.ListeningPorts != null && software.ListeningPorts.Count > 0)
        {
          context.Stdout.WriteLine($"   Ports: {string.Join(", ", software.ListeningPorts)}");
        }

        context.Stdout.WriteLine("");

        foreach (var (_, vuln, port) in vulns)
        {
          string severityIcon = GetSeverityIcon(vuln.Severity);
          string severityColor = GetSeverityColor(vuln.Severity);

          context.Stdout.WriteLine($"   {severityIcon} {vuln.CVE} - {vuln.Name}");
          context.Stdout.WriteLine($"      Severity: {vuln.Severity}/10 ({severityColor})");
          context.Stdout.WriteLine($"      Type: {vuln.Type}");

          if (port > 0)
          {
            context.Stdout.WriteLine($"      Port: {port}");
          }

          context.Stdout.WriteLine($"      Description: {vuln.Description}");

          if (!string.IsNullOrEmpty(vuln.ExploitCommand))
          {
            context.Stdout.WriteLine($"      💡 Exploit: {vuln.ExploitCommand}");
          }

          context.Stdout.WriteLine("");
        }
      }

      // Recommendations
      context.Stdout.WriteLine("RECOMMENDATIONS");
      context.Stdout.WriteLine("---------------");

      if (critical > 0)
      {
        context.Stdout.WriteLine("⚠️  CRITICAL vulnerabilities detected!");
        context.Stdout.WriteLine("    This device is highly vulnerable to exploitation.");
        context.Stdout.WriteLine("");
      }

      context.Stdout.WriteLine("• Use 'vulns' to view your discovered vulnerabilities");
      context.Stdout.WriteLine("• Use 'exploit <target> <cve>' to attempt exploitation");
      context.Stdout.WriteLine("• Vulnerabilities saved to ~/vulnerabilities.txt");
      context.Stdout.WriteLine("");
      context.Stdout.WriteLine("═══════════════════════════════════════════════════════════════");
    }

    private string GetSeverityIcon(int severity)
    {
      if (severity >= 9) return "🔴";
      if (severity >= 7) return "🟠";
      if (severity >= 4) return "🟡";
      return "🟢";
    }

    private string GetSeverityColor(int severity)
    {
      if (severity >= 9) return "CRITICAL";
      if (severity >= 7) return "HIGH";
      if (severity >= 4) return "MEDIUM";
      return "LOW";
    }
  }
}
