using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
  public class AliasCommand : CommandBase
  {
    private Dictionary<string, string> aliases;

    public override string Name => "alias";
    public override string Description => "Create or view command aliases";
    public override string Usage => "alias [name[=value]]";

    public AliasCommand(Dictionary<string, string> aliases)
    {
      this.aliases = aliases;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // No arguments - list all aliases
      if (args.Length == 0)
      {
        return ListAllAliases(context);
      }

      // Join all args back together to handle spaces in alias definitions
      string fullInput = string.Join(" ", args);

      // Check if this is an assignment (contains '=')
      if (fullInput.Contains("="))
      {
        return CreateOrUpdateAlias(fullInput, context);
      }
      else
      {
        // Show specific alias
        return ShowAlias(args[0], context);
      }
    }

    private CommandResult ListAllAliases(CommandContext context)
    {
      if (aliases.Count == 0)
      {
        WriteOutput(context, "No aliases defined");
        return CommandResult.Ok();
      }

      // Sort aliases alphabetically
      var sortedAliases = aliases.OrderBy(kvp => kvp.Key);

      foreach (var alias in sortedAliases)
      {
        WriteOutput(context, $"alias {alias.Key}='{alias.Value}'");
      }

      return CommandResult.Ok();
    }

    private CommandResult ShowAlias(string name, CommandContext context)
    {
      if (aliases.TryGetValue(name, out string value))
      {
        WriteOutput(context, $"alias {name}='{value}'");
        return CommandResult.Ok();
      }
      else
      {
        WriteError(context, $"alias: {name}: not found");
        return CommandResult.Error($"Alias not found: {name}");
      }
    }

    private CommandResult CreateOrUpdateAlias(string definition, CommandContext context)
    {
      // Parse the alias definition
      int equalsIndex = definition.IndexOf('=');
      if (equalsIndex == -1)
      {
        WriteError(context, "alias: invalid format. Use: alias name='command'");
        return CommandResult.Error("Invalid format");
      }

      string aliasName = definition.Substring(0, equalsIndex).Trim();
      string aliasValue = definition.Substring(equalsIndex + 1).Trim();

      // Validate alias name
      if (string.IsNullOrWhiteSpace(aliasName))
      {
        WriteError(context, "alias: name cannot be empty");
        return CommandResult.Error("Empty alias name");
      }

      // Check for invalid characters in alias name
      if (aliasName.Any(c => char.IsWhiteSpace(c) || c == '=' || c == '\'' || c == '"'))
      {
        WriteError(context, $"alias: invalid alias name: {aliasName}");
        return CommandResult.Error("Invalid alias name");
      }

      // Remove surrounding quotes if present
      if ((aliasValue.StartsWith("'") && aliasValue.EndsWith("'")) ||
          (aliasValue.StartsWith("\"") && aliasValue.EndsWith("\"")))
      {
        aliasValue = aliasValue.Substring(1, aliasValue.Length - 2);
      }

      // Validate alias value
      if (string.IsNullOrWhiteSpace(aliasValue))
      {
        WriteError(context, "alias: value cannot be empty");
        return CommandResult.Error("Empty alias value");
      }

      // Check for circular alias references
      if (WouldCreateCircularReference(aliasName, aliasValue))
      {
        WriteError(context, $"alias: would create circular reference: {aliasName}");
        return CommandResult.Error("Circular reference");
      }

      // Update or create the alias
      bool isUpdate = aliases.ContainsKey(aliasName);
      aliases[aliasName] = aliasValue;

      if (isUpdate)
      {
        WriteOutput(context, $"Updated alias: {aliasName}='{aliasValue}'");
      }
      else
      {
        WriteOutput(context, $"Created alias: {aliasName}='{aliasValue}'");
      }

      return CommandResult.Ok();
    }

    private bool WouldCreateCircularReference(string aliasName, string aliasValue)
    {
      // Extract the command from the alias value
      string[] parts = aliasValue.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
        return false;

      string targetCommand = parts[0];

      // If the alias points to itself, it's circular
      if (targetCommand == aliasName)
        return true;

      // Check if it points to another alias that eventually points back
      HashSet<string> visited = new HashSet<string> { aliasName };
      return CheckCircularReference(targetCommand, visited);
    }

    private bool CheckCircularReference(string command, HashSet<string> visited)
    {
      // If we've already visited this, we found a cycle
      if (visited.Contains(command))
        return true;

      // If it's not an alias, no cycle
      if (!aliases.TryGetValue(command, out string aliasValue))
        return false;

      // Extract the target command
      string[] parts = aliasValue.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
      if (parts.Length == 0)
        return false;

      string targetCommand = parts[0];

      // Add to visited and recurse
      visited.Add(command);
      return CheckCircularReference(targetCommand, visited);
    }
  }
}
