using System;
using System.Collections.Generic;
using UnityEngine;

public class VulnsCommand : ICommand
{
  private PlayerVulnerabilityInventory vulnerabilityInventory;

  public string Name => "vulns";
  public string Description => "Display your vulnerability database";
  public string Usage => "vulns [--sort=severity|date|cve]";

  public VulnsCommand(PlayerVulnerabilityInventory inventory)
  {
    this.vulnerabilityInventory = inventory;
  }

  public void Execute(string[] args, ITerminalOutput output)
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
      output.AppendText("No vulnerabilities in database. Use 'vuln-scan' to find vulnerabilities.\n");
      return;
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
    output.AppendText("VULNERABILITY DATABASE\n");
    output.AppendText("=====================\n\n");
    output.AppendText("CVE             | SEVERITY | TARGET           | SOFTWARE        | NAME\n");
    output.AppendText("----------------+---------+-----------------+----------------+---------------------------\n");

    // Display vulnerabilities with clear column separators
    foreach (var vuln in vulnerabilities)
    {
      string target = $"{vuln.HostIP}:{vuln.Port}";
      output.AppendText(
          $"{vuln.Vulnerability.CVE,-15} | {vuln.Vulnerability.Severity,-8} | {target,-15} | {vuln.SoftwareName,-14} | {vuln.Vulnerability.Name}\n"
      );
    }
  }
}
