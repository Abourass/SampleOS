using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Networking
{
  public class OwnedCommand : CommandBase
  {
    public override string Name => "owned";
    public override string Description => "List systems you have compromised";
    public override string Usage => "owned [-v|--verbose]";

    // No constructor dependencies!
    public OwnedCommand()
    {
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      bool verbose = args.Length > 0 && (args[0] == "-v" || args[0] == "--verbose");

      // Get owned systems from PlayerProgress via PlayerStateService
      var ownedSystems = context.PlayerState.GetOwnedSystems();

      if (ownedSystems.Count == 0)
      {
        WriteOutput(context, "You haven't compromised any systems yet.");
        WriteOutput(context, "");
        WriteOutput(context, "Hints:");
        WriteOutput(context, "  - Use 'nmap' to discover open ports on systems");
        WriteOutput(context, "  - Use 'vulnscan' to find vulnerabilities");
        WriteOutput(context, "  - Use 'exploit' to gain access");
        WriteOutput(context, "  - Try default credentials with 'ssh'");
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
      else
      {
        WriteOutput(context, "Use 'owned -v' for detailed statistics");
        WriteOutput(context, "");
      }

      return CommandResult.Ok();
    }

    private void DisplayOwnedSystem(SampleOS.Core.Player.OwnedSystemInfo system, CommandContext context, bool verbose)
    {
      // System name with access level indicator
      string accessIndicator = system.HasRootAccess ? "# ROOT" : "$ USER";
      Color accessColor = system.HasRootAccess
          ? new Color(1f, 0.2f, 0.2f)
          : new Color(1f, 0.7f, 0.2f);

      context.Stdout.SetColor(accessColor);
      WriteOutput(context, $"  [{accessIndicator}] {system.Hostname}");
      context.Stdout.SetColor(Color.white);

      WriteOutput(context, $"    IP: {system.IPAddress}");
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

        if (system.LastAccessTime.HasValue)
        {
          var timeSinceAccess = System.DateTime.Now - system.LastAccessTime.Value;
          WriteOutput(context, $"    Last Access: {FormatTimeSpan(timeSinceAccess)} ago");
        }

        if (system.IsCurrentlyConnected)
        {
          context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
          WriteOutput(context, "    [CURRENTLY CONNECTED]");
          context.Stdout.SetColor(Color.white);
        }
      }

      WriteOutput(context, "");
    }

    private void DisplayStatistics(System.Collections.Generic.List<SampleOS.Core.Player.OwnedSystemInfo> systems, CommandContext context)
    {
      context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      WriteOutput(context, "                      STATISTICS                            ");
      WriteOutput(context, "═══════════════════════════════════════════════════════════");
      context.Stdout.SetColor(Color.white);
      WriteOutput(context, "");

      // Get statistics from PlayerProgress
      var stats = context.PlayerState.Progress.GetStatistics();

      WriteOutput(context, $"  Root Access:      {stats.SystemsWithRootAccess} systems");
      WriteOutput(context, $"  User Access:      {stats.TotalSystemsCompromised - stats.SystemsWithRootAccess} systems");
      WriteOutput(context, $"  Vulnerabilities:  {stats.TotalVulnerabilitiesFound} discovered");
      WriteOutput(context, $"  Unique Exploits:  {stats.UniqueExploitsUsed}");
      WriteOutput(context, "");

      // Show total data exfiltrated
      long totalData = systems.Sum(s => s.DataExfiltrated);
      if (totalData > 0)
      {
        WriteOutput(context, $"  Data Stolen:      {FormatBytes(totalData)}");
      }

      // Show total flags captured
      int totalFlags = systems.Sum(s => s.FlagsFound.Count);
      if (totalFlags > 0)
      {
        WriteOutput(context, $"  Flags Captured:   {totalFlags}");
      }

      WriteOutput(context, "");

      // Show unique exploits and credentials used
      var allExploits = systems.SelectMany(s => s.ExploitsUsed).Distinct().ToList();

      if (allExploits.Count > 0)
      {
        WriteOutput(context, $"  Exploits Used:");
        foreach (var exploit in allExploits.Take(5))
        {
          WriteOutput(context, $"    - {exploit}");
        }
        if (allExploits.Count > 5)
          WriteOutput(context, $"    ... and {allExploits.Count - 5} more");
      }

      WriteOutput(context, "");

      // Network penetration overview
      var uniqueNetworks = systems.Select(s => s.NetworkName).Distinct().Count();
      WriteOutput(context, $"  Networks Penetrated: {uniqueNetworks}");
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

    private string FormatTimeSpan(System.TimeSpan span)
    {
      if (span.TotalMinutes < 1)
        return "less than a minute";
      if (span.TotalMinutes < 60)
        return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes >= 2 ? "s" : "")}";
      if (span.TotalHours < 24)
        return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")}";
      return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "")}";
    }
  }
}
