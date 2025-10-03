namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
  public class TouchCommand : CommandBase
  {
    public override string Name => "touch";
    public override string Description => "Create an empty file";
    public override string Usage => "touch <file>";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length == 0)
      {
        WriteError(context, "Usage: touch <file>");
        return CommandResult.Error("No file specified");
      }

      string filePath = args[0];
      var result = context.FileSystem.CreateFile(filePath, "");

      if (!result.IsSuccess)
      {
        WriteError(context, result.ErrorMessage);
        return CommandResult.Error(result.ErrorMessage);
      }

      return CommandResult.Ok();
    }
  }
}
