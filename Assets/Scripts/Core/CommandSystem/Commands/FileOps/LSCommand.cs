using System.Linq;
using SampleOS.Core.FileSystem;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
  public class LsCommand : CommandBase
  {
    public override string Name => "ls";
    public override string Description => "List directory contents";
    public override string Usage => "ls [-l] [-a] [path]";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      bool longFormat = false;
      bool showHidden = false;
      string path = null;

      // Parse arguments
      foreach (var arg in args)
      {
        if (arg == "-l")
          longFormat = true;
        else if (arg == "-a")
          showHidden = true;
        else if (!arg.StartsWith("-"))
          path = arg;
      }

      path = path ?? context.FileSystem.CurrentPath;

      var result = context.FileSystem.ListDirectory(path);
      if (!result.IsSuccess)
      {
        WriteError(context, result.ErrorMessage);
        return CommandResult.Error(result.ErrorMessage);
      }

      var items = result.Data
          .Where(node => showHidden || !node.Name.StartsWith("."))
          .OrderBy(node => node.Name)
          .ToList();

      if (items.Count == 0)
      {
        return CommandResult.Ok();
      }

      if (longFormat)
      {
        DisplayLongFormat(items, context);
      }
      else
      {
        DisplayShortFormat(items, context);
      }

      return CommandResult.Ok();
    }

    private void DisplayShortFormat(System.Collections.Generic.List<VirtualNode> items, CommandContext context)
    {
      foreach (var item in items)
      {
        if (item.IsDirectory)
        {
          context.Stdout.SetColor(new Color(0.3f, 0.6f, 1f)); // Blue
          WriteOutput(context, item.Name + "/");
          context.Stdout.SetColor(Color.white);
        }
        else
        {
          WriteOutput(context, item.Name);
        }
      }
    }

    private void DisplayLongFormat(System.Collections.Generic.List<VirtualNode> items, CommandContext context)
    {
      foreach (var item in items)
      {
        string permissions = item.Permissions;
        string owner = item.Owner;
        string size = item.Size.ToString().PadLeft(8);
        string date = item.ModificationTime.ToString("MMM dd HH:mm");
        string name = item.IsDirectory ? item.Name + "/" : item.Name;

        if (item.IsDirectory)
          context.Stdout.SetColor(new Color(0.3f, 0.6f, 1f));

        WriteOutput(context, $"{permissions} {owner} {size} {date} {name}");

        if (item.IsDirectory)
          context.Stdout.SetColor(Color.white);
      }
    }
  }
}
