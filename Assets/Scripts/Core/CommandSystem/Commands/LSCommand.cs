public class LsCommand : ICommand, IFileSystemCommand
{
  private VirtualFileSystem fileSystem;

  public string Name => "ls";
  public string Description => "List directory contents";
  public string Usage => "ls [directory]";

  public LsCommand(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    string path = args.Length > 0 ? args[0] : ".";
    var result = fileSystem.ListDirectory(path);

    if (result.IsSuccess)
    {
      foreach (var entry in result.Data)
      {
        if (entry.IsDirectory)
        {
          output.AppendText($"<color=#4E97D8>[DIR] {entry.Name}</color>\n");
        }
        else
        {
          output.AppendText($"<color=#A9B7C6>{entry.Name}</color>\n");
        }
      }
    }
    else
    {
      output.AppendText($"Error: {result.ErrorMessage}\n");
    }
  }

  public void SetFileSystem(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }
}
