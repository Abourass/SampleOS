using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;
using System.Linq;

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

  /// <summary>
  /// Finds files and directories that match the specified glob pattern
  /// </summary>
  /// <param name="pattern">The glob pattern to match against (e.g., "*.txt", "doc*", "?.md")</param>
  /// <param name="startPath">The directory to start the search from (defaults to current directory)</param>
  /// <param name="recursive">Whether to search recursively through subdirectories</param>
  /// <returns>A list of nodes matching the pattern</returns>
  public Result<List<VirtualNode>> FindByPattern(string pattern, string startPath = null, bool recursive = true)
  {
    if (string.IsNullOrEmpty(pattern))
      return Result<List<VirtualNode>>.Failure("Pattern cannot be empty");

    // Default to current directory if no start path is provided
    startPath = startPath ?? CurrentPath;

    // Get the starting node
    VirtualNode startNode = ResolvePath(startPath);
    if (startNode == null)
      return Result<List<VirtualNode>>.Failure($"Directory not found: {startPath}");

    if (!startNode.IsDirectory)
      return Result<List<VirtualNode>>.Failure($"Not a directory: {startPath}");

    // Convert the glob pattern to a regex pattern
    string regexPattern = GlobToRegex(pattern);
    Regex regex = new Regex(regexPattern, RegexOptions.IgnoreCase);

    // Find matching nodes
    List<VirtualNode> results = new List<VirtualNode>();
    FindMatchingNodes(startNode, regex, results, recursive);

    return Result<List<VirtualNode>>.Success(results);
  }

  /// <summary>
  /// Finds only files (not directories) that match the specified glob pattern
  /// </summary>
  /// <param name="pattern">The glob pattern to match against (e.g., "*.txt", "doc*", "?.md")</param>
  /// <param name="startPath">The directory to start the search from (defaults to current directory)</param>
  /// <param name="recursive">Whether to search recursively through subdirectories</param>
  /// <returns>A list of file nodes matching the pattern</returns>
  public Result<List<VirtualNode>> FindFilesByPattern(string pattern, string startPath = null, bool recursive = true)
  {
    // Use the existing FindByPattern method to get all matching nodes
    var allMatchesResult = FindByPattern(pattern, startPath, recursive);

    if (!allMatchesResult.IsSuccess)
      return Result<List<VirtualNode>>.Failure(allMatchesResult.ErrorMessage);

    // Filter the results to only include files
    var files = allMatchesResult.Data.Where(node => !node.IsDirectory).ToList();

    return Result<List<VirtualNode>>.Success(files);
  }

  /// <summary>
  /// Recursively finds nodes that match the pattern
  /// </summary>
  private void FindMatchingNodes(VirtualNode currentNode, Regex pattern, List<VirtualNode> results, bool recursive)
  {
    foreach (var child in currentNode.Children.Values)
    {
      // Check if the child's name matches the pattern
      if (pattern.IsMatch(child.Name))
      {
        results.Add(child);
      }

      // If recursive and the child is a directory, search it too
      if (recursive && child.IsDirectory)
      {
        FindMatchingNodes(child, pattern, results, recursive);
      }
    }
  }

  /// <summary>
  /// Converts a glob pattern to a regex pattern
  /// </summary>
  /// <param name="glob">The glob pattern</param>
  /// <returns>Equivalent regex pattern</returns>
  private string GlobToRegex(string glob)
  {
    StringBuilder regex = new StringBuilder("^");

    foreach (char c in glob)
    {
      switch (c)
      {
        case '*':
          regex.Append(".*");
          break;
        case '?':
          regex.Append(".");
          break;
        case '.':
          regex.Append("\\.");
          break;
        case '\\':
          regex.Append("\\\\");
          break;
        case '[':
        case ']':
        case '(':
        case ')':
        case '{':
        case '}':
        case '+':
        case '^':
        case '$':
        case '|':
          regex.Append('\\').Append(c);
          break;
        default:
          regex.Append(c);
          break;
      }
    }

    regex.Append("$");
    return regex.ToString();
  }
}
