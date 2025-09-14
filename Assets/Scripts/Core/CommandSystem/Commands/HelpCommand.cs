using System.Collections.Generic;
using System.Text;

public class HelpCommand : ICommand
{
  private Dictionary<string, ICommand> commands;

  public string Name => "help";
  public string Description => "Display help information about available commands";
  public string Usage => "help [command]";

  public HelpCommand(Dictionary<string, ICommand> commandDictionary)
  {
    commands = commandDictionary;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    if (args.Length == 0)
    {
      // Display help for all commands
      output.AppendText("Available commands:\n\n");

      // Get all commands and sort them alphabetically
      List<ICommand> sortedCommands = new List<ICommand>(commands.Values);
      sortedCommands.Sort((a, b) => string.Compare(a.Name, b.Name));

      foreach (var cmd in sortedCommands)
      {
        output.AppendText($"<color=#4E97D8>{cmd.Name}</color> - {cmd.Description}\n");
      }

      output.AppendText("\nType 'help <command>' for more information about a specific command.\n");
    }
    else
    {
      // Display help for a specific command
      string commandName = args[0].ToLower();

      if (commands.TryGetValue(commandName, out ICommand command))
      {
        StringBuilder helpText = new StringBuilder();

        helpText.AppendLine($"<color=#4E97D8>{command.Name}</color>");
        helpText.AppendLine($"  {command.Description}");
        helpText.AppendLine($"  Usage: {command.Usage}");

        output.AppendText(helpText.ToString());
      }
      else
      {
        output.AppendText($"Unknown command: {commandName}\n");
        output.AppendText("Type 'help' to see a list of all available commands.\n");
      }
    }
  }
}
