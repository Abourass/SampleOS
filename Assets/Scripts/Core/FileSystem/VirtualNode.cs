using System;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Represents a node in the virtual file system, which can be either a file or directory.
/// </summary>
public class VirtualNode
{
  /// <summary>
  /// Name of the file or directory
  /// </summary>
  public string Name { get; private set; }

  /// <summary>
  /// Whether this node is a directory (true) or a file (false)
  /// </summary>
  public bool IsDirectory { get; private set; }

  /// <summary>
  /// Content of the file (null if this is a directory)
  /// </summary>
  public string Content { get; set; }

  /// <summary>
  /// Time when the node was created
  /// </summary>
  public DateTime CreationTime { get; private set; }

  /// <summary>
  /// Time when the node was last modified
  /// </summary>
  public DateTime ModificationTime { get; private set; }

  /// <summary>
  /// Size of the file in bytes (0 for directories)
  /// </summary>
  public int Size => IsDirectory ? 0 : (Content?.Length ?? 0);

  /// <summary>
  /// Owner of the file or directory
  /// </summary>
  public string Owner { get; set; } = "user";

  /// <summary>
  /// Permissions for the file or directory (simplified)
  /// </summary>
  public string Permissions { get; set; } = "rwxr--r--";

  /// <summary>
  /// Parent directory of this node (null for root)
  /// </summary>
  public VirtualNode Parent { get; private set; }

  /// <summary>
  /// Child nodes if this is a directory
  /// </summary>
  public Dictionary<string, VirtualNode> Children { get; private set; }

  /// <summary>
  /// Creates a new file or directory node
  /// </summary>
  /// <param name="name">Name of the node</param>
  /// <param name="isDirectory">Whether it's a directory</param>
  /// <param name="content">Content if it's a file</param>
  public VirtualNode(string name, bool isDirectory, string content = null)
  {
    Name = name;
    IsDirectory = isDirectory;
    Content = isDirectory ? null : content;
    CreationTime = DateTime.Now;
    ModificationTime = DateTime.Now;

    if (isDirectory)
    {
      Children = new Dictionary<string, VirtualNode>();
    }
  }

  /// <summary>
  /// Adds a child node to this directory
  /// </summary>
  /// <param name="node">Child node to add</param>
  /// <returns>True if successful, false if not a directory or child already exists</returns>
  public bool AddChild(VirtualNode node)
  {
    if (!IsDirectory) return false;

    if (Children.ContainsKey(node.Name))
      return false;

    Children[node.Name] = node;
    node.Parent = this;
    return true;
  }

  /// <summary>
  /// Removes a child node from this directory
  /// </summary>
  /// <param name="name">Name of the child to remove</param>
  /// <returns>True if successful</returns>
  public bool RemoveChild(string name)
  {
    if (!IsDirectory) return false;

    if (Children.ContainsKey(name))
    {
      Children.Remove(name);
      return true;
    }

    return false;
  }

  /// <summary>
  /// Finds a node by path starting from this node
  /// </summary>
  /// <param name="path">Path to the target node</param>
  /// <returns>The found node or null</returns>
  public VirtualNode FindNode(string path)
  {
    // Handle empty path
    if (string.IsNullOrEmpty(path))
      return this;

    // Split path into components
    string[] components = path.Split('/');

    // Handle absolute paths
    if (path.StartsWith("/"))
    {
      // Find the root node
      VirtualNode root = this;
      while (root.Parent != null)
      {
        root = root.Parent;
      }

      // Start from root but skip the empty first component
      return root.FindNodeRelative(components.Skip(1).ToArray());
    }

    // Handle relative paths
    return FindNodeRelative(components);
  }

  /// <summary>
  /// Helper for finding a node using relative path components
  /// </summary>
  private VirtualNode FindNodeRelative(string[] pathComponents)
  {
    if (pathComponents.Length == 0)
      return this;

    VirtualNode current = this;

    foreach (var component in pathComponents)
    {
      // Skip empty components
      if (string.IsNullOrEmpty(component))
        continue;

      // Handle "." (current directory)
      if (component == ".")
        continue;

      // Handle ".." (parent directory)
      if (component == "..")
      {
        current = current.Parent ?? current;
        continue;
      }

      // Handle normal directory/file names
      if (!current.IsDirectory || !current.Children.ContainsKey(component))
        return null;

      current = current.Children[component];
    }

    return current;
  }

  /// <summary>
  /// Updates the modification time
  /// </summary>
  public void UpdateModificationTime()
  {
    ModificationTime = DateTime.Now;
  }
}
