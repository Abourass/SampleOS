using NUnit.Framework;
using SampleOS.Core.FileSystem;
using System;
using System.Linq;

namespace SampleOS.Tests.FileSystem
{
  [TestFixture]
  public class VirtualNodeTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_Directory_CreatesDirectoryNode()
    {
      // Act
      var node = new VirtualNode("testdir", true);

      // Assert
      Assert.AreEqual("testdir", node.Name);
      Assert.IsTrue(node.IsDirectory);
      Assert.IsNull(node.Content);
      Assert.IsNotNull(node.Children);
      Assert.AreEqual(0, node.Children.Count);
      Assert.AreEqual(0, node.Size);
    }

    [Test]
    public void Constructor_File_CreatesFileNode()
    {
      // Act
      var node = new VirtualNode("testfile.txt", false, "Hello World");

      // Assert
      Assert.AreEqual("testfile.txt", node.Name);
      Assert.IsFalse(node.IsDirectory);
      Assert.AreEqual("Hello World", node.Content);
      Assert.IsNull(node.Children);
      Assert.AreEqual(11, node.Size); // "Hello World" is 11 characters
    }

    [Test]
    public void Constructor_FileWithoutContent_CreatesEmptyFile()
    {
      // Act
      var node = new VirtualNode("empty.txt", false);

      // Assert
      Assert.IsFalse(node.IsDirectory);
      Assert.IsNull(node.Content);
      Assert.AreEqual(0, node.Size);
    }

    [Test]
    public void Constructor_DirectoryWithContent_IgnoresContent()
    {
      // Act
      var node = new VirtualNode("dir", true, "This should be ignored");

      // Assert
      Assert.IsTrue(node.IsDirectory);
      Assert.IsNull(node.Content);
      Assert.AreEqual(0, node.Size);
    }

    [Test]
    public void Constructor_SetsTimestamps()
    {
      // Arrange
      var beforeCreation = DateTime.UtcNow;

      // Act
      var node = new VirtualNode("test", false);
      var afterCreation = DateTime.UtcNow;

      // Assert
      Assert.GreaterOrEqual(node.CreationTime, beforeCreation);
      Assert.LessOrEqual(node.CreationTime, afterCreation);
      Assert.AreEqual(node.CreationTime, node.ModificationTime);
    }

    [Test]
    public void Constructor_SetsDefaultOwnerAndPermissions()
    {
      // Act
      var node = new VirtualNode("test", false);

      // Assert
      Assert.AreEqual("user", node.Owner);
      Assert.AreEqual("rwxr--r--", node.Permissions);
    }

    #endregion

    #region Property Tests

    [Test]
    public void Size_EmptyFile_ReturnsZero()
    {
      // Arrange
      var node = new VirtualNode("empty.txt", false, "");

      // Act & Assert
      Assert.AreEqual(0, node.Size);
    }

    [Test]
    public void Size_FileWithContent_ReturnsContentLength()
    {
      // Arrange
      var content = "This is a test file with some content.";
      var node = new VirtualNode("test.txt", false, content);

      // Act & Assert
      Assert.AreEqual(content.Length, node.Size);
    }

    [Test]
    public void Size_Directory_AlwaysReturnsZero()
    {
      // Arrange
      var dir = new VirtualNode("dir", true);
      var file = new VirtualNode("file.txt", false, "content");
      dir.AddChild(file);

      // Act & Assert
      Assert.AreEqual(0, dir.Size);
    }

    [Test]
    public void Content_CanBeModified()
    {
      // Arrange
      var node = new VirtualNode("test.txt", false, "Original");

      // Act
      node.Content = "Modified";

      // Assert
      Assert.AreEqual("Modified", node.Content);
      Assert.AreEqual(8, node.Size);
    }

    [Test]
    public void Owner_CanBeModified()
    {
      // Arrange
      var node = new VirtualNode("test", false);

      // Act
      node.Owner = "root";

      // Assert
      Assert.AreEqual("root", node.Owner);
    }

    [Test]
    public void Permissions_CanBeModified()
    {
      // Arrange
      var node = new VirtualNode("test", false);

      // Act
      node.Permissions = "rw-rw-rw-";

      // Assert
      Assert.AreEqual("rw-rw-rw-", node.Permissions);
    }

    #endregion

    #region AddChild Tests

    [Test]
    public void AddChild_ToDirectory_AddsChild()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", false);

      // Act
      var result = parent.AddChild(child);

      // Assert
      Assert.IsTrue(result);
      Assert.AreEqual(1, parent.Children.Count);
      Assert.IsTrue(parent.Children.ContainsKey("child"));
      Assert.AreEqual(child, parent.Children["child"]);
    }

    [Test]
    public void AddChild_SetsParentReference()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", false);

      // Act
      parent.AddChild(child);

      // Assert
      Assert.AreEqual(parent, child.Parent);
    }

    [Test]
    public void AddChild_ToFile_ReturnsFalse()
    {
      // Arrange
      var file = new VirtualNode("file.txt", false);
      var child = new VirtualNode("child", false);

      // Act
      var result = file.AddChild(child);

      // Assert
      Assert.IsFalse(result);
      Assert.IsNull(child.Parent);
    }

    [Test]
    public void AddChild_DuplicateName_ReturnsFalse()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child1 = new VirtualNode("duplicate", false);
      var child2 = new VirtualNode("duplicate", false);

      // Act
      var result1 = parent.AddChild(child1);
      var result2 = parent.AddChild(child2);

      // Assert
      Assert.IsTrue(result1);
      Assert.IsFalse(result2);
      Assert.AreEqual(1, parent.Children.Count);
      Assert.AreEqual(child1, parent.Children["duplicate"]);
    }

    [Test]
    public void AddChild_MultipleChildren_AddsAllChildren()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child1 = new VirtualNode("child1", false);
      var child2 = new VirtualNode("child2", true);
      var child3 = new VirtualNode("child3", false);

      // Act
      parent.AddChild(child1);
      parent.AddChild(child2);
      parent.AddChild(child3);

      // Assert
      Assert.AreEqual(3, parent.Children.Count);
      Assert.IsTrue(parent.Children.ContainsKey("child1"));
      Assert.IsTrue(parent.Children.ContainsKey("child2"));
      Assert.IsTrue(parent.Children.ContainsKey("child3"));
    }

    #endregion

    #region RemoveChild Tests

    [Test]
    public void RemoveChild_ExistingChild_RemovesChild()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", false);
      parent.AddChild(child);

      // Act
      var result = parent.RemoveChild("child");

      // Assert
      Assert.IsTrue(result);
      Assert.AreEqual(0, parent.Children.Count);
      Assert.IsFalse(parent.Children.ContainsKey("child"));
    }

    [Test]
    public void RemoveChild_NonExistentChild_ReturnsFalse()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);

      // Act
      var result = parent.RemoveChild("nonexistent");

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void RemoveChild_FromFile_ReturnsFalse()
    {
      // Arrange
      var file = new VirtualNode("file.txt", false);

      // Act
      var result = file.RemoveChild("anything");

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void RemoveChild_OneOfMultiple_RemovesOnlySpecifiedChild()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child1 = new VirtualNode("child1", false);
      var child2 = new VirtualNode("child2", false);
      var child3 = new VirtualNode("child3", false);
      parent.AddChild(child1);
      parent.AddChild(child2);
      parent.AddChild(child3);

      // Act
      var result = parent.RemoveChild("child2");

      // Assert
      Assert.IsTrue(result);
      Assert.AreEqual(2, parent.Children.Count);
      Assert.IsTrue(parent.Children.ContainsKey("child1"));
      Assert.IsFalse(parent.Children.ContainsKey("child2"));
      Assert.IsTrue(parent.Children.ContainsKey("child3"));
    }

    #endregion

    #region FindNode Tests - Absolute Paths

    [Test]
    public void FindNode_RootPath_ReturnsRoot()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      root.AddChild(home);

      // Act
      var result = root.FindNode("/");

      // Assert
      Assert.AreEqual(root, result);
    }

    [Test]
    public void FindNode_AbsolutePath_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);
      var file = new VirtualNode("test.txt", false);

      root.AddChild(home);
      home.AddChild(user);
      user.AddChild(file);

      // Act
      var result = root.FindNode("/home/user/test.txt");

      // Assert
      Assert.AreEqual(file, result);
      Assert.AreEqual("test.txt", result.Name);
    }

    [Test]
    public void FindNode_AbsolutePathFromChild_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);
      var docs = new VirtualNode("documents", true);

      root.AddChild(home);
      home.AddChild(user);
      user.AddChild(docs);

      // Act - Start search from 'user' but use absolute path
      var result = user.FindNode("/home");

      // Assert
      Assert.AreEqual(home, result);
    }

    [Test]
    public void FindNode_NonExistentAbsolutePath_ReturnsNull()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      root.AddChild(home);

      // Act
      var result = root.FindNode("/nonexistent/path");

      // Assert
      Assert.IsNull(result);
    }

    #endregion

    #region FindNode Tests - Relative Paths

    [Test]
    public void FindNode_RelativePath_ReturnsCorrectNode()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child1 = new VirtualNode("child1", true);
      var child2 = new VirtualNode("child2", false);

      parent.AddChild(child1);
      parent.AddChild(child2);

      // Act
      var result = parent.FindNode("child1");

      // Assert
      Assert.AreEqual(child1, result);
    }

    [Test]
    public void FindNode_NestedRelativePath_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("root", true);
      var level1 = new VirtualNode("level1", true);
      var level2 = new VirtualNode("level2", true);
      var file = new VirtualNode("file.txt", false);

      root.AddChild(level1);
      level1.AddChild(level2);
      level2.AddChild(file);

      // Act
      var result = root.FindNode("level1/level2/file.txt");

      // Assert
      Assert.AreEqual(file, result);
    }

    [Test]
    public void FindNode_NonExistentRelativePath_ReturnsNull()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", true);
      parent.AddChild(child);

      // Act
      var result = parent.FindNode("nonexistent");

      // Assert
      Assert.IsNull(result);
    }

    #endregion

    #region FindNode Tests - Special Path Components

    [Test]
    public void FindNode_EmptyPath_ReturnsCurrentNode()
    {
      // Arrange
      var node = new VirtualNode("test", true);

      // Act
      var result = node.FindNode("");

      // Assert
      Assert.AreEqual(node, result);
    }

    [Test]
    public void FindNode_NullPath_ReturnsCurrentNode()
    {
      // Arrange
      var node = new VirtualNode("test", true);

      // Act
      var result = node.FindNode(null);

      // Assert
      Assert.AreEqual(node, result);
    }

    [Test]
    public void FindNode_CurrentDirectory_ReturnsCurrentNode()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", true);
      parent.AddChild(child);

      // Act
      var result = child.FindNode(".");

      // Assert
      Assert.AreEqual(child, result);
    }

    [Test]
    public void FindNode_ParentDirectory_ReturnsParentNode()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", true);
      parent.AddChild(child);

      // Act
      var result = child.FindNode("..");

      // Assert
      Assert.AreEqual(parent, result);
    }

    [Test]
    public void FindNode_ParentDirectoryFromRoot_ReturnsRoot()
    {
      // Arrange
      var root = new VirtualNode("/", true);

      // Act
      var result = root.FindNode("..");

      // Assert
      Assert.AreEqual(root, result);
    }

    [Test]
    public void FindNode_ComplexPathWithDotDot_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);
      var docs = new VirtualNode("documents", true);
      var downloads = new VirtualNode("downloads", true);
      var file = new VirtualNode("file.txt", false);

      root.AddChild(home);
      home.AddChild(user);
      user.AddChild(docs);
      user.AddChild(downloads);
      downloads.AddChild(file);

      // Act - From docs, go ../downloads/file.txt
      var result = docs.FindNode("../downloads/file.txt");

      // Assert
      Assert.AreEqual(file, result);
    }

    [Test]
    public void FindNode_PathWithMultipleDots_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);
      var deep1 = new VirtualNode("deep1", true);
      var deep2 = new VirtualNode("deep2", true);

      root.AddChild(home);
      home.AddChild(user);
      user.AddChild(deep1);
      deep1.AddChild(deep2);

      // Act - From deep2, go ../../user
      var result = deep2.FindNode("../..");

      // Assert
      Assert.AreEqual(user, result);
    }

    [Test]
    public void FindNode_PathWithCurrentDirectory_ReturnsCorrectNode()
    {
      // Arrange
      var parent = new VirtualNode("parent", true);
      var child = new VirtualNode("child", true);
      var file = new VirtualNode("file.txt", false);

      parent.AddChild(child);
      child.AddChild(file);

      // Act
      var result = parent.FindNode("./child/./file.txt");

      // Assert
      Assert.AreEqual(file, result);
    }

    [Test]
    public void FindNode_PathWithTrailingSlash_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);

      root.AddChild(home);
      home.AddChild(user);

      // Act
      var result = root.FindNode("/home/user/");

      // Assert
      Assert.AreEqual(user, result);
    }

    [Test]
    public void FindNode_PathWithMultipleSlashes_ReturnsCorrectNode()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);

      root.AddChild(home);
      home.AddChild(user);

      // Act
      var result = root.FindNode("///home///user///");

      // Assert
      Assert.AreEqual(user, result);
    }

    #endregion

    #region UpdateModificationTime Tests

    [Test]
    public void UpdateModificationTime_UpdatesTimestamp()
    {
      // Arrange
      var node = new VirtualNode("test.txt", false, "content");
      var originalModTime = node.ModificationTime;

      // Wait a tiny bit to ensure time difference
      System.Threading.Thread.Sleep(10);

      // Act
      node.UpdateModificationTime();

      // Assert
      Assert.Greater(node.ModificationTime, originalModTime);
      Assert.AreEqual(node.CreationTime, originalModTime); // Creation time shouldn't change
    }

    [Test]
    public void UpdateModificationTime_MultipleUpdates_KeepsUpdating()
    {
      // Arrange
      var node = new VirtualNode("test.txt", false, "content");
      var time1 = node.ModificationTime;

      System.Threading.Thread.Sleep(50);
      node.UpdateModificationTime();
      var time2 = node.ModificationTime;

      System.Threading.Thread.Sleep(50);
      node.UpdateModificationTime();
      var time3 = node.ModificationTime;

      // Assert
      Assert.Greater(time2, time1);
      Assert.Greater(time3, time2);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void ComplexTree_NavigationWorks()
    {
      // Arrange - Build a complex tree structure
      var root = new VirtualNode("/", true);
      var home = new VirtualNode("home", true);
      var user = new VirtualNode("user", true);
      var documents = new VirtualNode("documents", true);
      var pictures = new VirtualNode("pictures", true);
      var file1 = new VirtualNode("doc1.txt", false, "Document 1");
      var file2 = new VirtualNode("pic1.jpg", false, "Picture data");

      root.AddChild(home);
      home.AddChild(user);
      user.AddChild(documents);
      user.AddChild(pictures);
      documents.AddChild(file1);
      pictures.AddChild(file2);

      // Act & Assert - Various navigation scenarios
      Assert.AreEqual(file1, documents.FindNode("doc1.txt"));
      Assert.AreEqual(file2, documents.FindNode("../pictures/pic1.jpg"));
      Assert.AreEqual(user, file1.FindNode("../.."));
      Assert.AreEqual(root, file1.FindNode("/"));
      Assert.AreEqual(pictures, root.FindNode("/home/user/pictures"));
    }

    [Test]
    public void FileHierarchy_ParentReferencesWork()
    {
      // Arrange
      var root = new VirtualNode("/", true);
      var level1 = new VirtualNode("level1", true);
      var level2 = new VirtualNode("level2", true);
      var level3 = new VirtualNode("level3", true);

      root.AddChild(level1);
      level1.AddChild(level2);
      level2.AddChild(level3);

      // Act & Assert
      Assert.IsNull(root.Parent);
      Assert.AreEqual(root, level1.Parent);
      Assert.AreEqual(level1, level2.Parent);
      Assert.AreEqual(level2, level3.Parent);

      // Navigate up the tree
      Assert.AreEqual(level2, level3.Parent);
      Assert.AreEqual(level1, level3.Parent.Parent);
      Assert.AreEqual(root, level3.Parent.Parent.Parent);
    }

    #endregion
  }
}
