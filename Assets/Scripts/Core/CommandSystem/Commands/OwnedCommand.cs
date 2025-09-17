using System.Collections.Generic;
using UnityEngine;

public class OwnedCommand : ICommand
{
  private PlayerProgressManager progressManager;
  private VirtualNetwork network;

  public string Name => "owned";
  public string Description => "Show systems you've gained root access to";
  public string Usage => "owned";

  public OwnedCommand(PlayerProgressManager progress, VirtualNetwork network)
  {
    this.progressManager = progress;
    this.network = network;  // Store the reference
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    List<string> compromisedSystems = progressManager.GetCompromisedSystems();

    if (compromisedSystems.Count == 0)
    {
      output.AppendText("You haven't compromised any systems yet.\n");
      output.AppendText("Use 'vuln-scan' to find vulnerabilities and 'exploit' to gain access.\n");
      return;
    }

    // Display header with fancy formatting
    Color titleColor = new Color(0.2f, 1f, 0.2f); // Green
    Color headerColor = new Color(0.5f, 0.8f, 1f); // Light blue

    output.SetColor(titleColor);
    output.AppendText("COMPROMISED SYSTEMS\n");
    output.AppendText("==================\n\n");

    output.SetColor(headerColor);
    output.AppendText("HOSTNAME             IP ADDRESS         ACCESS LEVEL\n");
    output.AppendText("------------------------------------------------------\n");

    output.SetColor(Color.white);

    foreach (string hostname in compromisedSystems)
    {
      RemoteSystem system = network.GetSystemByHostname(hostname);
      if (system != null)
      {
        string paddedHostname = hostname.PadRight(20).Substring(0, 20);
        string paddedIP = system.IPAddress.PadRight(18).Substring(0, 18);
        string accessLevel = system.HasRootAccess ? "ROOT" : "USER";

        // Get security level with color coding
        Color securityColor;
        switch (system.SecurityLevel)
        {
          case SecurityLevel.VeryLow:
            securityColor = new Color(0.0f, 0.8f, 0.0f); // Green
            break;
          case SecurityLevel.Low:
            securityColor = new Color(0.5f, 0.8f, 0.0f); // Yellow-Green
            break;
          case SecurityLevel.Medium:
            securityColor = new Color(0.8f, 0.8f, 0.0f); // Yellow
            break;
          case SecurityLevel.High:
            securityColor = new Color(0.9f, 0.5f, 0.0f); // Orange
            break;
          case SecurityLevel.VeryHigh:
            securityColor = new Color(1.0f, 0.2f, 0.2f); // Red
            break;
          default:
            securityColor = Color.white;
            break;
        }

        output.AppendText($"{paddedHostname} {paddedIP} ");

        // Change color for security level
        output.SetColor(securityColor);
        output.AppendText($"{system.SecurityLevel,-12}");

        // Back to default color for access level
        output.SetColor(Color.white);
        output.AppendText($"{accessLevel}\n");
      }
    }
  }
}
