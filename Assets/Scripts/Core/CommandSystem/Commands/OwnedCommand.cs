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

        output.AppendText($"{paddedHostname} {paddedIP} {accessLevel}\n");
      }
    }
  }
}
