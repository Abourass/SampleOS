using System.Collections.Generic;
using System.Linq;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
  public class HelpCommand : CommandBase
  {
    private Dictionary<string, ICommand> commands;

    public override string Name => "help";
    public override string Description => "Shows available commands";
    public override string Usage => "help [command]";

    public HelpCommand(Dictionary<string, ICommand> commands)
    {
      this.commands = commands;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length > 0)
      {
        return ShowCommandHelp(args[0], context);
      }

      return ShowAllCommands(context);
    }

    private CommandResult ShowAllCommands(CommandContext context)
    {
      WriteOutput(context, "Available commands:\n");

      var sortedCommands = commands.Values
          .OrderBy(cmd => cmd.Name)
          .ToList();

      int maxNameLength = sortedCommands.Max(cmd => cmd.Name.Length);

      foreach (var cmd in sortedCommands)
      {
        string paddedName = cmd.Name.PadRight(maxNameLength + 2);
        WriteOutput(context, $"  {paddedName}{cmd.Description}");
      }

      WriteOutput(context, "\nType 'help <command>' for more information on a specific command.");
      return CommandResult.Ok();
    }

    private CommandResult ShowCommandHelp(string commandName, CommandContext context)
    {
      if (!commands.TryGetValue(commandName.ToLower(), out var command))
      {
        WriteError(context, $"Unknown command: {commandName}");
        return CommandResult.Error($"Unknown command: {commandName}");
      }

      WriteOutput(context, $"{command.Name} - {command.Description}");
      WriteOutput(context, $"Usage: {command.Usage}");
      return CommandResult.Ok();
    }
  }
}
