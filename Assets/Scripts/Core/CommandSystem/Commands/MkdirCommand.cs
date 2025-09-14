using System.IO;

public class MkdirCommand : ICommand, IFileSystemCommand
{
  private VirtualFileSystem fileSystem;

  public string Name => "mkdir";
  public string Description => "Create a new directory";
  public string Usage => "mkdir <directory_name>";

  public MkdirCommand(VirtualFileSystem fs)
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
      var result = CreateDirectory(path);

      if (!result.IsSuccess)
      {
        output.AppendText($"Error: {result.ErrorMessage}\n");
      }
    }
  }

  private Result<bool> CreateDirectory(string path)
  {
    // Get the parent directory path and new directory name
    string dirName = Path.GetFileName(path);
    string parentPath = Path.GetDirectoryName(path);

    if (string.IsNullOrEmpty(dirName))
    {
      return Result<bool>.Failure("Invalid directory name");
    }

    // Resolve the parent directory node
    VirtualNode parentNode = string.IsNullOrEmpty(parentPath)
        ? fileSystem.ResolvePath(".")  // Current directory
        : fileSystem.ResolvePath(parentPath);

    if (parentNode == null)
    {
      return Result<bool>.Failure($"Parent directory not found: {parentPath}");
    }

    if (!parentNode.IsDirectory)
    {
      return Result<bool>.Failure($"Not a directory: {parentPath}");
    }

    // Check if directory already exists
    if (parentNode.Children.ContainsKey(dirName))
    {
      return Result<bool>.Failure($"Directory already exists: {dirName}");
    }

    // Create the new directory
    VirtualNode newDir = new VirtualNode(dirName, true);
    parentNode.AddChild(newDir);
    return Result<bool>.Success(true);
  }

  public void SetFileSystem(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }
}
