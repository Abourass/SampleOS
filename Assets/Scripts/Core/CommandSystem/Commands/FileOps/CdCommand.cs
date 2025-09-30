using SampleOS.Core.CommandSystem;

namespace SampleOS.Core.CommandSystem.Commands.FileOps
{

  /// <summary>
  /// Command to change the current directory.
  /// Usage: cd <directory>
  /// If no directory is provided, it defaults to the home directory.
  /// </summary>

  public class CdCommand : ICommand, IFileSystemCommand
  {
    private VirtualFileSystem fileSystem;

    public string Name => "cd";
    public string Description => "Change the current directory";
    public string Usage => "cd <directory>";

    public CdCommand(VirtualFileSystem fs)
    {
      fileSystem = fs;
    }

    public void Execute(string[] args, ITerminalOutput output)
    {
      // Default to home directory if no args
      string path = args.Length == 0 ? "/home/user" : args[0];

      var result = fileSystem.ChangeDirectory(path);

      if (!result.IsSuccess)
      {
        output.AppendText($"Error: {result.ErrorMessage}\n");
      }
    }

    public void SetFileSystem(VirtualFileSystem fs)
    {
      fileSystem = fs;
    }
  }
}
