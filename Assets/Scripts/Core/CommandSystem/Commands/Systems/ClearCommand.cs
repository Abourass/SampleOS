using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
  public class ClearCommand : CommandBase
  {
    public override string Name => "clear";
    public override string Description => "Clears the terminal screen";
    public override string Usage => "clear";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // Note: This will need special handling in TerminalController
      // as we can't directly clear from here anymore
      WriteOutput(context, "\x1b[2J\x1b[H"); // ANSI clear screen codes
      return CommandResult.Ok();
    }
  }
}
