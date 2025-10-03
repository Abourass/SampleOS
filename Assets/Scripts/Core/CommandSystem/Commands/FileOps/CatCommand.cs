using System.Linq;

namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
  public class CatCommand : CommandBase, IPipeableCommand
  {
    public override string Name => "cat";
    public override string Description => "Display file contents";
    public override string Usage => "cat <file> [file...]";

    public bool AcceptsPipedInput => true;

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      // Handle piped input
      if (HasPipedInput(context, out string pipedInput))
      {
        WriteOutput(context, pipedInput);
        return CommandResult.Ok();
      }

      if (args.Length == 0)
      {
        WriteError(context, "Usage: cat <file> [file...]");
        return CommandResult.Error("No files specified");
      }

      bool hadErrors = false;

      foreach (var filePath in args)
      {
        var node = context.FileSystem.ResolvePath(filePath);

        if (node == null)
        {
          WriteError(context, $"cat: {filePath}: No such file or directory");
          hadErrors = true;
          continue;
        }

        if (node.IsDirectory)
        {
          WriteError(context, $"cat: {filePath}: Is a directory");
          hadErrors = true;
          continue;
        }

        WriteOutput(context, node.Content);
      }

      return hadErrors ? CommandResult.Error("Some files could not be read", 1) : CommandResult.Ok();
    }
  }
}
