using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Vulnerabilities
{
  public class VulnsCommand : CommandBase
  {
    private PlayerVulnerabilityInventory vulnerabilityInventory;

    public override string Name => "vulns";
    public override string Description => "Display your vulnerability database";
    public override string Usage => "vulns [--sort=severity|date|cve]";

    public VulnsCommand(PlayerVulnerabilityInventory inventory)
    {
      this.vulnerabilityInventory = inventory;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // Parse sort option
      string sortBy = "date"; // default
      if (args.Length > 0)
      {
        foreach (string arg in args)
        {
          if (arg.StartsWith("--sort="))
          {
            sortBy = arg.Substring(7).ToLower();
          }
        }
      }

      var vulnerabilities = vulnerabilityInventory.GetAllVulnerabilities();

      if (vulnerabilities.Count == 0)
      {
        WriteOutput(context, "No vulnerabilities in database. Use 'vuln-scan' to find vulnerabilities.");
        return CommandResult.Ok();
      }

      // Sort based on option
      switch (sortBy)
      {
        case "severity":
          vulnerabilities.Sort((a, b) => b.Vulnerability.Severity.CompareTo(a.Vulnerability.Severity));
          break;
        case "cve":
          vulnerabilities.Sort((a, b) => a.Vulnerability.CVE.CompareTo(b.Vulnerability.CVE));
          break;
        case "date":
        default:
          vulnerabilities.Sort((a, b) => b.DiscoveryDate.CompareTo(a.DiscoveryDate));
          break;
      }

      // Display header
      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f)); // Orange
      WriteOutput(context, "VULNERABILITY DATABASE");
      WriteOutput(context, "=====================");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");
      WriteOutput(context, "CVE             | SEVERITY | TARGET           | SOFTWARE        | NAME");
      WriteOutput(context, "----------------+----------+------------------+-----------------+---------------------------");

      // Display vulnerabilities with clear column separators
      foreach (var vuln in vulnerabilities)
      {
        string target = $"{vuln.HostIP}:{vuln.Port}";

        // Color code by severity
        Color severityColor = GetSeverityColor(vuln.Vulnerability.Severity);
        context.Stdout.SetColor(severityColor);

        string line = string.Format("{0,-15} | {1,-8} | {2,-16} | {3,-15} | {4}",
            vuln.Vulnerability.CVE,
            vuln.Vulnerability.Severity,
            target,
            vuln.SoftwareName,
            vuln.Vulnerability.Name);

        WriteOutput(context, line);
        context.Stdout.SetColor(Color.white);
      }

      WriteOutput(context, "");
      WriteOutput(context, $"Total vulnerabilities: {vulnerabilities.Count}");
      WriteOutput(context, $"Sorted by: {sortBy}");

      return CommandResult.Ok();
    }

    private Color GetSeverityColor(int severity)
    {
      if (severity >= 9)
        return new Color(1f, 0.2f, 0.2f); // Critical - Bright red
      else if (severity >= 7)
        return new Color(1f, 0.5f, 0.2f); // High - Orange
      else if (severity >= 5)
        return new Color(1f, 0.9f, 0.3f); // Medium - Yellow
      else
        return new Color(0.7f, 0.7f, 0.7f); // Low - Gray
    }
  }
}
