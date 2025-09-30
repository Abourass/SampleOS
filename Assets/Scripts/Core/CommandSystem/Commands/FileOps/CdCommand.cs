namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
  public class CdCommand : CommandBase
  {
    public override string Name => "cd";
    public override string Description => "Change directory";
    public override string Usage => "cd <path>";

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (args.Length == 0)
      {
        // Go to home directory
        var result = context.FileSystem.ChangeDirectory("/home/user");
        return result.IsSuccess
            ? CommandResult.Ok()
            : CommandResult.Error(result.ErrorMessage);
      }

      string path = args[0];
      var changeResult = context.FileSystem.ChangeDirectory(path);

      if (!changeResult.IsSuccess)
      {
        WriteError(context, changeResult.ErrorMessage);
        return CommandResult.Error(changeResult.ErrorMessage);
      }

      return CommandResult.Ok();
    }
  }
}
