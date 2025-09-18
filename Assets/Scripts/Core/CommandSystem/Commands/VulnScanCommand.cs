using System;
using UnityEngine;

public class VulnScanCommand : ICommand
{
  private VirtualNetwork network;
  private PlayerVulnerabilityInventory vulnerabilityInventory;

  public string Name => "vuln-scan";
  public string Description => "Scan for vulnerabilities in network services";
  public string Usage => "vuln-scan <host> [port]";

  public VulnScanCommand(VirtualNetwork network, PlayerVulnerabilityInventory inventory)
  {
    this.network = network;
    this.vulnerabilityInventory = inventory;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    try
    {
      if (args.Length < 1)
      {
        output.AppendText($"Usage: {Usage}\n");
        return;
      }

      string target = args[0];
      int? specificPort = null;

      if (args.Length > 1 && int.TryParse(args[1], out int parsedPort))
      {
        specificPort = parsedPort;
      }

      output.AppendText($"Scanning {target} for vulnerabilities...\n\n");

      RemoteSystem system = network.GetSystemByHostname(target);
      if (system == null)
      {
        output.AppendText($"Host {target} not found.\n");
        return;
      }

      // Generate vulnerabilities if not already done
      system.GenerateVulnerabilities();

      // Get open ports
      var ports = system.GetOpenPorts();
      bool vulnerabilitiesFound = false;

      foreach (int portNumber in ports)
      {
        if (specificPort.HasValue && portNumber != specificPort.Value)
          continue;

        Software software = system.GetSoftwareOnPort(portNumber);
        if (software == null) continue;

        output.AppendText($"Checking {software.Name} v{software.Version} on port {portNumber}...\n");

        if (software.HasVulnerability())
        {
          // Found at least one vulnerability!
          vulnerabilitiesFound = true;
          Color defaultColor = new Color(1f, 1f, 1f);
          Color vulnColor = new Color(1f, 0.5f, 0.5f); // Red

          foreach (var vuln in software.Vulnerabilities)
          {
            output.SetColor(vulnColor);
            output.AppendText($"[VULNERABLE] {vuln.CVE}: {vuln.Name} (Severity: {vuln.Severity}/10)\n");
            output.SetColor(defaultColor);
            output.AppendText($"  {vuln.Description}\n");

            // Add to player's inventory
            vulnerabilityInventory.AddVulnerability(vuln, target, portNumber, software.Name);
          }
        }
        else
        {
          output.AppendText("No vulnerabilities found.\n");
        }

        output.AppendText("\n");
      }

      if (!vulnerabilitiesFound)
      {
        output.AppendText("No vulnerabilities were found on this system.\n");
      }
      else
      {
        output.AppendText("Vulnerabilities added to your database.\n");
        output.AppendText("Use 'vulns' command to view your vulnerability inventory.\n");

        // Save vulnerabilities to a file in the user's home directory
        vulnerabilityInventory.SaveToLocalFile(network.GetLocalSystem().FileSystem);
      }
    }
    catch (Exception ex)
    {
      output.AppendText($"Error during scan: {ex.Message}\n");
      Debug.LogError($"VulnScanCommand error: {ex}");
    }
  }
}
