namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
  public class MkdirCommand : CommandBase
  {
    public override string Name => "mkdir";
    public override string Description => "Create a directory";
    public override string Usage => "mkdir <directory>";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length == 0)
      {
        WriteError(context, "Usage: mkdir <directory>");
        return CommandResult.Error("No directory specified");
      }

      string dirPath = args[0];
      var result = context.FileSystem.CreateDirectory(dirPath);

      if (!result.IsSuccess)
      {
        WriteError(context, result.ErrorMessage);
        return CommandResult.Error(result.ErrorMessage);
      }

      return CommandResult.Ok();
    }
  }
}
