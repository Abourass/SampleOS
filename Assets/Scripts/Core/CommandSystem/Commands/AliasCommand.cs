using System.Collections.Generic;
using System.Text.RegularExpressions;

public class AliasCommand : ICommand
{
  private Dictionary<string, string> aliases;

  public string Name => "alias";
  public string Description => "Create or list command aliases";
  public string Usage => "alias [name=command] [-r name]";

  public AliasCommand(Dictionary<string, string> aliasStore)
  {
    aliases = aliasStore;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    // If no arguments, list all aliases
    if (args.Length == 0)
    {
      ListAliases(output);
      return;
    }

    // Check for removal flag
    if (args[0] == "-r" && args.Length > 1)
    {
      RemoveAlias(args[1], output);
      return;
    }

    // Join all arguments in case there are spaces within the command definition
    string fullCommand = string.Join(" ", args);

    // Parse alias definition (name=command)
    var match = Regex.Match(fullCommand, @"(\w+)=(?:'(.*?)'|""(.*?)""|(.+))");
    if (match.Success)
    {
      string aliasName = match.Groups[1].Value;
      string aliasCommand = match.Groups[2].Value.Length > 0 ? match.Groups[2].Value :
                          match.Groups[3].Value.Length > 0 ? match.Groups[3].Value :
                          match.Groups[4].Value;

      CreateAlias(aliasName, aliasCommand, output);
      return;
    }

    // Invalid syntax
    output.AppendText($"Usage: {Usage}\n");
  }

  private void ListAliases(ITerminalOutput output)
  {
    if (aliases.Count == 0)
    {
      output.AppendText("No aliases defined.\n");
      return;
    }

    foreach (var alias in aliases)
    {
      output.AppendText($"{alias.Key}='{alias.Value}'\n");
    }
  }

  private void CreateAlias(string name, string command, ITerminalOutput output)
  {
    aliases[name] = command;
    output.AppendText($"Alias created: {name}='{command}'\n");
  }

  private void RemoveAlias(string name, ITerminalOutput output)
  {
    if (aliases.ContainsKey(name))
    {
      aliases.Remove(name);
      output.AppendText($"Alias removed: {name}\n");
    }
    else
    {
      output.AppendText($"Alias not found: {name}\n");
    }
  }
}
