public class CatCommand : ICommand, IFileSystemCommand, IPipeableCommand
{
  private VirtualFileSystem fileSystem;

  public string Name => "cat";
  public string Description => "Display the contents of a file";
  public string Usage => "cat <filename>";

  public CatCommand(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    if (args.Length == 0)
    {
      output.AppendText($"Usage: {Usage}\n");
      return;
    }

    foreach (string path in args)
    {
      var fileNode = fileSystem.ResolvePath(path);

      if (fileNode == null)
      {
        output.AppendText($"Error: File not found: {path}\n");
        continue;
      }

      if (fileNode.IsDirectory)
      {
        output.AppendText($"Error: {path} is a directory\n");
        continue;
      }

      // Display the file content
      output.AppendText($"{fileNode.Content}\n");
    }
  }

  public void ExecuteWithInput(string[] args, ITerminalOutput output, string inputText)
  {
    // If args provided, behave like normal cat with files
    if (args.Length > 0)
    {
      Execute(args, output);
      return;
    }
    
    // If no args, just output the piped input
    output.AppendText(inputText);
  }

  public void SetFileSystem(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }
}
