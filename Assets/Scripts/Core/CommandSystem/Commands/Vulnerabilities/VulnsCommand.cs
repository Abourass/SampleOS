using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Vulnerabilities
{
  public class VulnsCommand : CommandBase
  {
    public override string Name => "vulns";
    public override string Description => "Display your vulnerability database";
    public override string Usage => "vulns [--sort=severity|date|cve]";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // Parse sort option
      string sortBy = "date";
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

      // Get discovered vulnerabilities from PlayerProgress
      var discoveredVulns = context.PlayerState.Progress.GetAllVulnerabilities();

      if (discoveredVulns.Count == 0)
      {
        WriteOutput(context, "No vulnerabilities in database. Use 'vulnscan' to find vulnerabilities.");
        return CommandResult.Ok();
      }

      // Resolve vulnerability details for all discovered vulns
      var enrichedVulns = discoveredVulns
          .Select(dv => new
          {
            Discovered = dv,
            Details = context.PlayerState.ResolveVulnerability(dv)
          })
          .Where(v => v.Details != null)
          .ToList();

      // Sort
      switch (sortBy)
      {
        case "severity":
          enrichedVulns = enrichedVulns
              .OrderByDescending(v => v.Details.Severity)
              .ToList();
          break;
        case "cve":
          enrichedVulns = enrichedVulns
              .OrderBy(v => v.Details.CVE)
              .ToList();
          break;
        case "date":
        default:
          enrichedVulns = enrichedVulns
              .OrderByDescending(v => v.Discovered.DiscoveredAt)
              .ToList();
          break;
      }

      // Display
      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
      WriteOutput(context, "VULNERABILITY DATABASE");
      WriteOutput(context, "=====================");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");
      WriteOutput(context, "CVE             | SEVERITY | TARGET           | SOFTWARE        | NAME                      | STATUS");
      WriteOutput(context, "----------------+----------+------------------+-----------------+---------------------------+-----------");

      foreach (var vuln in enrichedVulns)
      {
        string target = $"{vuln.Discovered.Hostname}:{vuln.Discovered.Port}";
        string status = vuln.Discovered.HasBeenExploited ? "[EXPLOITED]" : "";

        Color severityColor = GetSeverityColor(vuln.Details.Severity);
        context.Stdout.SetColor(severityColor);

        string line = string.Format("{0,-15} | {1,-8} | {2,-16} | {3,-15} | {4,-25} | {5}",
            vuln.Details.CVE,
            vuln.Details.Severity,
            target,
            vuln.Discovered.SoftwareName,
            vuln.Details.Name,
            status);

        WriteOutput(context, line);
        context.Stdout.SetColor(Color.white);
      }

      WriteOutput(context, "");
      WriteOutput(context, $"Total vulnerabilities: {enrichedVulns.Count}");
      WriteOutput(context, $"Exploited: {enrichedVulns.Count(v => v.Discovered.HasBeenExploited)}");
      WriteOutput(context, $"Sorted by: {sortBy}");

      return CommandResult.Ok();
    }

    private Color GetSeverityColor(int severity)
    {
      if (severity >= 9) return new Color(1f, 0.2f, 0.2f);
      if (severity >= 7) return new Color(1f, 0.5f, 0.2f);
      if (severity >= 5) return new Color(1f, 0.9f, 0.3f);
      return new Color(0.7f, 0.7f, 0.7f);
    }
  }
}
