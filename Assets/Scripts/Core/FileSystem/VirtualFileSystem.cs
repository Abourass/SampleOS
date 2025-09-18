using System.Collections.Generic;
using System.Text;
using System.IO; 

public class VirtualFileSystem
{
  private VirtualNode root;
  private VirtualNode currentNode;

  public string CurrentPath { get; private set; }

  public VirtualFileSystem()
  {
    Initialize();
  }

  private void Initialize()
  {
    // Create root directory
    root = new VirtualNode("/", true);

    // Build default directory structure
    FileSystemFactory.BuildDefaultFileSystem(root);

    // Set current directory
    currentNode = root.FindNode("/home/user");
    CurrentPath = "/home/user";
  }

  public Result<List<VirtualNode>> ListDirectory(string path)
  {
    VirtualNode targetNode = ResolvePath(path);

    if (targetNode == null)
      return Result<List<VirtualNode>>.Failure($"Directory not found: {path}");

    if (!targetNode.IsDirectory)
      return Result<List<VirtualNode>>.Failure($"Not a directory: {path}");

    return Result<List<VirtualNode>>.Success(new List<VirtualNode>(targetNode.Children.Values));
  }

  public Result<bool> ChangeDirectory(string path)
  {
    VirtualNode targetNode = ResolvePath(path);

    if (targetNode == null)
      return Result<bool>.Failure($"Directory not found: {path}");

    if (!targetNode.IsDirectory)
      return Result<bool>.Failure($"Not a directory: {path}");

    currentNode = targetNode;
    CurrentPath = GetAbsolutePath(targetNode);

    return Result<bool>.Success(true);
  }

  /// <summary>
  /// Resolves a path string to a VirtualNode
  /// </summary>
  /// <param name="path">The path to resolve (absolute or relative)</param>
  /// <returns>The node at the specified path, or null if not found</returns>
  public VirtualNode ResolvePath(string path)
  {
    if (string.IsNullOrEmpty(path))
      return currentNode;

    // Handle absolute paths (starting with /)
    if (path.StartsWith("/"))
      return root.FindNode(path);

    // Handle relative paths
    return currentNode.FindNode(path);
  }

  /// <summary>
  /// Gets the absolute path of a node
  /// </summary>
  /// <param name="node">The node to get the path for</param>
  /// <returns>The absolute path as a string</returns>
  private string GetAbsolutePath(VirtualNode node)
  {
    if (node == null)
      return string.Empty;

    // Special case: root directory
    if (node.Parent == null)
      return "/";

    StringBuilder path = new StringBuilder();
    BuildPathRecursive(node, path);

    // If empty, we're at root
    if (path.Length == 0)
      return "/";

    return path.ToString();
  }

  /// <summary>
  /// Helper method to build a path recursively
  /// </summary>
  private void BuildPathRecursive(VirtualNode node, StringBuilder path)
  {
    if (node == null || node.Parent == null)
      return;

    BuildPathRecursive(node.Parent, path);

    // Don't add a slash for the root directory's name
    if (node.Parent.Parent != null || path.Length > 0)
      path.Append("/");

    path.Append(node.Name);
  }

  /// <summary>
  /// Creates a new directory at the specified path
  /// </summary>
  /// <param name="path">Path where to create the directory</param>
  /// <returns>Result indicating success or failure</returns>
  public Result<bool> CreateDirectory(string path)
  {
      // Handle root directory creation as a special case
      if (path == "/")
          return Result<bool>.Failure("Cannot create root directory");
          
      // Extract parent path and directory name
      string parentPath = Path.GetDirectoryName(path).Replace('\\', '/');
      string dirName = Path.GetFileName(path);
      
      if (string.IsNullOrEmpty(dirName))
          return Result<bool>.Failure("Invalid directory name");
          
      // Find the parent node
      VirtualNode parentNode = ResolvePath(parentPath);
      if (parentNode == null)
          return Result<bool>.Failure($"Parent directory not found: {parentPath}");
          
      if (!parentNode.IsDirectory)
          return Result<bool>.Failure($"Not a directory: {parentPath}");
          
      // Check if directory already exists
      if (parentNode.Children.ContainsKey(dirName))
          return Result<bool>.Failure($"Directory already exists: {path}");
          
      // Create the new directory
      VirtualNode newDir = new VirtualNode(dirName, true);
      parentNode.AddChild(newDir);
      return Result<bool>.Success(true);
  }
    
  /// <summary>
  /// Creates a new file at the specified path with the given content
  /// </summary>
  /// <param name="path">Path where to create the file</param>
  /// <param name="content">Content to write to the file</param>
  /// <returns>Result indicating success or failure</returns>
  public Result<bool> CreateFile(string path, string content)
  {
      // Extract parent path and file name
      string parentPath = Path.GetDirectoryName(path).Replace('\\', '/');
      string fileName = Path.GetFileName(path);
      
      if (string.IsNullOrEmpty(fileName))
          return Result<bool>.Failure("Invalid file name");
          
      // Find the parent node
      VirtualNode parentNode = ResolvePath(parentPath);
      if (parentNode == null)
          return Result<bool>.Failure($"Directory not found: {parentPath}");
          
      if (!parentNode.IsDirectory)
          return Result<bool>.Failure($"Not a directory: {parentPath}");
          
      // Check if file already exists, update it if it does
      if (parentNode.Children.TryGetValue(fileName, out VirtualNode existingNode))
      {
          if (existingNode.IsDirectory)
              return Result<bool>.Failure($"Cannot create file, directory exists: {path}");
              
          existingNode.Content = content;
          existingNode.UpdateModificationTime();
          return Result<bool>.Success(true);
      }
      
      // Create the new file
      VirtualNode newFile = new VirtualNode(fileName, false, content);
      parentNode.AddChild(newFile);
      return Result<bool>.Success(true);
  }
}
