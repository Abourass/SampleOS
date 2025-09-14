using System;

public class TouchCommand : ICommand, IFileSystemCommand
{
  private VirtualFileSystem fileSystem;

  public string Name => "touch";
  public string Description => "Create a new empty file or update file timestamp";
  public string Usage => "touch <filename>";

  public TouchCommand(VirtualFileSystem fs)
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
      var result = CreateOrUpdateFile(path);

      if (!result.IsSuccess)
      {
        output.AppendText($"Error: {result.ErrorMessage}\n");
      }
    }
  }

  private Result<bool> CreateOrUpdateFile(string path)
  {
    // Get the directory path and filename
    string filename = System.IO.Path.GetFileName(path);
    string directory = System.IO.Path.GetDirectoryName(path);

    if (string.IsNullOrEmpty(filename))
    {
      return Result<bool>.Failure("Invalid filename");
    }

    // Resolve the directory node
    VirtualNode dirNode = string.IsNullOrEmpty(directory)
        ? fileSystem.ResolvePath(".")  // Current directory
        : fileSystem.ResolvePath(directory);

    if (dirNode == null)
    {
      return Result<bool>.Failure($"Directory not found: {directory}");
    }

    if (!dirNode.IsDirectory)
    {
      return Result<bool>.Failure($"Not a directory: {directory}");
    }

    // Check if the file already exists
    if (dirNode.Children.TryGetValue(filename, out VirtualNode existingFile))
    {
      // File exists, update the modification time
      existingFile.UpdateModificationTime();
      return Result<bool>.Success(true);
    }
    else
    {
      // Create a new empty file
      VirtualNode newFile = new VirtualNode(filename, false, "");
      dirNode.AddChild(newFile);
      return Result<bool>.Success(true);
    }
  }

  public void SetFileSystem(VirtualFileSystem fs)
  {
    fileSystem = fs;
  }
}
