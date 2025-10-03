using System.Linq;
using SampleOS.Core.Player;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class OwnedCommand : CommandBase
  {
    private PlayerProgressManager progressManager;

    public override string Name => "owned";
    public override string Description => "List systems you have compromised";
    public override string Usage => "owned [-v|--verbose]";

    public OwnedCommand(PlayerProgressManager progressManager)
    {
      this.progressManager = progressManager;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      bool verbose = args.Length > 0 && (args[0] == "-v" || args[0] == "--verbose");

      var ownedSystems = progressManager.GetOwnedSystems();

      if (ownedSystems.Count == 0)
      {
        WriteOutput(context, "You haven't compromised any systems yet.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'nmap' to discover open ports on systems");
        WriteOutput(context, "  - Use 'vuln-scan' to find vulnerabilities");
        WriteOutput(context, "  - Use 'exploit' to gain access");
        return CommandResult.Ok();
      }

      // Header
      context.Stdout.SetColor(new Color(1f, 0.3f, 0.3f)); // Red (hacker theme)
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                  COMPROMISED SYSTEMS                       ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      WriteOutput(context, $"Total Systems Owned: {ownedSystems.Count}");
      WriteOutput(context, "");

      // Group by network
      var systemsByNetwork = ownedSystems.GroupBy(s => s.NetworkName);

      foreach (var networkGroup in systemsByNetwork.OrderBy(g => g.Key))
      {
        // Network header
        context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f));
        WriteOutput(context, $"Network: {networkGroup.Key}");
        context.Stdout.SetColor(Color.white);
        WriteOutput(context, new string('─', 60));

        foreach (var system in networkGroup.OrderBy(s => s.Hostname))
        {
          DisplayOwnedSystem(system, context, verbose);
        }

        WriteOutput(context, "");
      }

      // Summary statistics
      if (verbose)
      {
        DisplayStatistics(ownedSystems, context);
      }

      return CommandResult.Ok();
    }

    private void DisplayOwnedSystem(OwnedSystemInfo system, CommandContext context, bool verbose)
    {
      // System name with access level indicator
      string accessIndicator = system.HasRootAccess ? "# ROOT" : "$ USER";
      Color accessColor = system.HasRootAccess
          ? new Color(1f, 0.2f, 0.2f)
          : new Color(1f, 0.7f, 0.2f);

      context.Stdout.SetColor(accessColor);
      WriteOutput(context, $"  [{accessIndicator}]");
      context.Stdout.SetColor(Color.white);

      WriteOutput(context, $"  {system.Hostname} ({system.IPAddress})");
      WriteOutput(context, $"    Type: {system.Type}");
      WriteOutput(context, $"    Compromised: {system.CompromiseDate:yyyy-MM-dd HH:mm:ss}");

      if (verbose)
      {
        WriteOutput(context, $"    Method: {system.CompromiseMethod}");

        if (system.CredentialsUsed.Count > 0)
        {
          WriteOutput(context, $"    Credentials: {string.Join(", ", system.CredentialsUsed)}");
        }

        if (system.ExploitsUsed.Count > 0)
        {
          WriteOutput(context, $"    Exploits: {string.Join(", ", system.ExploitsUsed)}");
        }

        if (system.FlagsFound.Count > 0)
        {
          WriteOutput(context, $"    Flags: {system.FlagsFound.Count} found");
        }

        if (system.DataExfiltrated > 0)
        {
          WriteOutput(context, $"    Data Exfiltrated: {FormatBytes(system.DataExfiltrated)}");
        }
      }

      WriteOutput(context, "");
    }

    private void DisplayStatistics(System.Collections.Generic.List<OwnedSystemInfo> systems, CommandContext context)
    {
      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                      STATISTICS                            ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      int rootSystems = systems.Count(s => s.HasRootAccess);
      int userSystems = systems.Count - rootSystems;
      int totalFlags = systems.Sum(s => s.FlagsFound.Count);
      long totalData = systems.Sum(s => s.DataExfiltrated);

      WriteOutput(context, $"  Root Access:      {rootSystems} systems");
      WriteOutput(context, $"  User Access:      {userSystems} systems");
      WriteOutput(context, $"  Flags Captured:   {totalFlags}");
      WriteOutput(context, $"  Data Stolen:      {FormatBytes(totalData)}");
      WriteOutput(context, "");

      // Show unique exploits and credentials used
      var allExploits = systems.SelectMany(s => s.ExploitsUsed).Distinct().ToList();
      var allCredentials = systems.SelectMany(s => s.CredentialsUsed).Distinct().ToList();

      if (allExploits.Count > 0)
      {
        WriteOutput(context, $"  Exploits Used:    {allExploits.Count} unique");
        foreach (var exploit in allExploits.Take(5))
        {
          WriteOutput(context, $"    - {exploit}");
        }
        if (allExploits.Count > 5)
          WriteOutput(context, $"    ... and {allExploits.Count - 5} more");
      }

      WriteOutput(context, "");
    }

    private string FormatBytes(long bytes)
    {
      string[] sizes = { "B", "KB", "MB", "GB", "TB" };
      double len = bytes;
      int order = 0;

      while (len >= 1024 && order < sizes.Length - 1)
      {
        order++;
        len /= 1024;
      }

      return $"{len:0.##} {sizes[order]}";
    }
  }
}
