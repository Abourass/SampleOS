using NUnit.Framework;
using SampleOS.Core.FileSystem;
using System.Linq;

namespace SampleOS.Tests.FileSystem
{
  [TestFixture]
  public class VirtualFileSystemTests
  {
    private VirtualFileSystem _fs;

    [SetUp]
    public void Setup()
    {
      _fs = new VirtualFileSystem();
      CleanTestDirectory();
    }

    [TearDown]
    public void TearDown()
    {
      _fs = null;
    }

    /// <summary>
    /// Removes all files and directories from /home/user to ensure clean test state
    /// </summary>
    private void CleanTestDirectory()
    {
      var userDir = _fs.ResolvePath("/home/user");
      if (userDir != null)
      {
        // Clear all children from the user directory
        userDir.Children.Clear();
      }
    }

    #region Directory Operations Tests

    [Test]
    public void Initialize_SetsCurrentPathToHomeUser()
    {
      // Assert
      Assert.AreEqual("/home/user", _fs.CurrentPath);
    }

    [Test]
    public void CreateDirectory_ValidPath_CreatesDirectory()
    {
      // Arrange
      var path = "/home/user/documents";

      // Act
      var result = _fs.CreateDirectory(path);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      var listResult = _fs.ListDirectory("/home/user");
      Assert.IsTrue(listResult.IsSuccess);
      Assert.IsTrue(listResult.Data.Any(n => n.Name == "documents" && n.IsDirectory));
    }

    [Test]
    public void CreateDirectory_NestedPath_CreatesDirectoryInParent()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");

      // Act
      var result = _fs.CreateDirectory("/home/user/documents/work");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      var listResult = _fs.ListDirectory("/home/user/documents");
      Assert.IsTrue(listResult.Data.Any(n => n.Name == "work"));
    }

    [Test]
    public void CreateDirectory_ParentDoesNotExist_ReturnsFailure()
    {
      // Arrange
      var path = "/nonexistent/newdir";

      // Act
      var result = _fs.CreateDirectory(path);

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public void CreateDirectory_AlreadyExists_ReturnsFailure()
    {
      // Arrange
      var path = "/home/user/documents";
      _fs.CreateDirectory(path);

      // Act
      var result = _fs.CreateDirectory(path);

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("already exists"));
    }

    [Test]
    public void CreateDirectory_RootDirectory_ReturnsFailure()
    {
      // Act
      var result = _fs.CreateDirectory("/");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Cannot create root"));
    }

    [Test]
    public void CreateDirectory_EmptyName_ReturnsFailure()
    {
      // Act
      var result = _fs.CreateDirectory("/home/user/");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Invalid directory name"));
    }

    [Test]
    public void ListDirectory_ValidPath_ReturnsChildren()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");
      _fs.CreateFile("/home/user/test.txt", "content");

      // Act
      var result = _fs.ListDirectory("/home/user");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.GreaterOrEqual(result.Data.Count, 2);
      Assert.IsTrue(result.Data.Any(n => n.Name == "documents"));
      Assert.IsTrue(result.Data.Any(n => n.Name == "test.txt"));
    }

    [Test]
    public void ListDirectory_NonExistentPath_ReturnsFailure()
    {
      // Act
      var result = _fs.ListDirectory("/nonexistent");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public void ListDirectory_FilePath_ReturnsFailure()
    {
      // Arrange
      _fs.CreateFile("/home/user/test.txt", "content");

      // Act
      var result = _fs.ListDirectory("/home/user/test.txt");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Not a directory"));
    }

    [Test]
    public void ChangeDirectory_ValidAbsolutePath_ChangesCurrentPath()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");

      // Act
      var result = _fs.ChangeDirectory("/home/user/documents");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual("/home/user/documents", _fs.CurrentPath);
    }

    [Test]
    public void ChangeDirectory_ValidRelativePath_ChangesCurrentPath()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");
      _fs.ChangeDirectory("/home/user");

      // Act
      var result = _fs.ChangeDirectory("documents");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual("/home/user/documents", _fs.CurrentPath);
    }

    [Test]
    public void ChangeDirectory_ParentDirectory_ChangesCurrentPath()
    {
      // Arrange
      _fs.ChangeDirectory("/home/user");

      // Act
      var result = _fs.ChangeDirectory("..");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual("/home", _fs.CurrentPath);
    }

    [Test]
    public void ChangeDirectory_Root_ChangesCurrentPath()
    {
      // Act
      var result = _fs.ChangeDirectory("/");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual("/", _fs.CurrentPath);
    }

    [Test]
    public void ChangeDirectory_NonExistentPath_ReturnsFailure()
    {
      // Act
      var result = _fs.ChangeDirectory("/nonexistent");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public void ChangeDirectory_FilePath_ReturnsFailure()
    {
      // Arrange
      _fs.CreateFile("/home/user/test.txt", "content");

      // Act
      var result = _fs.ChangeDirectory("/home/user/test.txt");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Not a directory"));
    }

    #endregion

    #region File Operations Tests

    [Test]
    public void CreateFile_ValidPath_CreatesFile()
    {
      // Arrange
      var path = "/home/user/test.txt";
      var content = "Hello, World!";

      // Act
      var result = _fs.CreateFile(path, content);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      var listResult = _fs.ListDirectory("/home/user");
      var file = listResult.Data.FirstOrDefault(n => n.Name == "test.txt");
      Assert.IsNotNull(file);
      Assert.IsFalse(file.IsDirectory);
      Assert.AreEqual(content, file.Content);
    }

    [Test]
    public void CreateFile_FileAlreadyExists_UpdatesContent()
    {
      // Arrange
      var path = "/home/user/test.txt";
      _fs.CreateFile(path, "Original content");

      // Act
      var result = _fs.CreateFile(path, "Updated content");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      var node = _fs.ResolvePath(path);
      Assert.AreEqual("Updated content", node.Content);
    }

    [Test]
    public void CreateFile_ParentDoesNotExist_ReturnsFailure()
    {
      // Arrange
      var path = "/nonexistent/file.txt";

      // Act
      var result = _fs.CreateFile(path, "content");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public void CreateFile_DirectoryExistsWithSameName_ReturnsFailure()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/mydir");

      // Act
      var result = _fs.CreateFile("/home/user/mydir", "content");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("directory exists"));
    }

    [Test]
    public void CreateFile_EmptyName_ReturnsFailure()
    {
      // Act
      var result = _fs.CreateFile("/home/user/", "content");

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Invalid file name"));
    }

    [Test]
    public void CreateFile_EmptyContent_CreatesFileWithEmptyContent()
    {
      // Arrange
      var path = "/home/user/empty.txt";

      // Act
      var result = _fs.CreateFile(path, "");

      // Assert
      Assert.IsTrue(result.IsSuccess);
      var node = _fs.ResolvePath(path);
      Assert.IsNotNull(node);
      Assert.AreEqual("", node.Content);
    }

    #endregion

    #region Path Resolution Tests

    [Test]
    public void ResolvePath_AbsolutePath_ReturnsCorrectNode()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");

      // Act
      var node = _fs.ResolvePath("/home/user/documents");

      // Assert
      Assert.IsNotNull(node);
      Assert.AreEqual("documents", node.Name);
      Assert.IsTrue(node.IsDirectory);
    }

    [Test]
    public void ResolvePath_RelativePath_ReturnsCorrectNode()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");
      _fs.ChangeDirectory("/home/user");

      // Act
      var node = _fs.ResolvePath("documents");

      // Assert
      Assert.IsNotNull(node);
      Assert.AreEqual("documents", node.Name);
    }

    [Test]
    public void ResolvePath_EmptyPath_ReturnsCurrentNode()
    {
      // Arrange
      _fs.ChangeDirectory("/home");

      // Act
      var node = _fs.ResolvePath("");

      // Assert
      Assert.IsNotNull(node);
      Assert.AreEqual("home", node.Name);
    }

    [Test]
    public void ResolvePath_NullPath_ReturnsCurrentNode()
    {
      // Act
      var node = _fs.ResolvePath(null);

      // Assert
      Assert.IsNotNull(node);
      Assert.AreEqual("user", node.Name);
    }

    [Test]
    public void ResolvePath_NonExistentPath_ReturnsNull()
    {
      // Act
      var node = _fs.ResolvePath("/nonexistent/path");

      // Assert
      Assert.IsNull(node);
    }

    #endregion

    #region Pattern Matching Tests

    [Test]
    public void FindByPattern_WildcardExtension_ReturnsMatchingFiles()
    {
      // Arrange
      _fs.CreateFile("/home/user/file1.txt", "");
      _fs.CreateFile("/home/user/file2.txt", "");
      _fs.CreateFile("/home/user/file3.log", "");

      // Act
      var result = _fs.FindByPattern("*.txt", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(2, result.Data.Count);
      Assert.IsTrue(result.Data.All(n => n.Name.EndsWith(".txt")));
    }

    [Test]
    public void FindByPattern_WildcardPrefix_ReturnsMatchingFiles()
    {
      // Arrange
      _fs.CreateFile("/home/user/test_file.txt", "");
      _fs.CreateFile("/home/user/test_doc.txt", "");
      _fs.CreateFile("/home/user/other.txt", "");

      // Act
      var result = _fs.FindByPattern("test_*", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(2, result.Data.Count);
      Assert.IsTrue(result.Data.All(n => n.Name.StartsWith("test_")));
    }

    [Test]
    public void FindByPattern_QuestionMark_ReturnsMatchingFiles()
    {
      // Arrange
      _fs.CreateFile("/home/user/file1.txt", "");
      _fs.CreateFile("/home/user/file2.txt", "");
      _fs.CreateFile("/home/user/file10.txt", "");

      // Act
      var result = _fs.FindByPattern("file?.txt", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(2, result.Data.Count);
      Assert.IsTrue(result.Data.Any(n => n.Name == "file1.txt"));
      Assert.IsTrue(result.Data.Any(n => n.Name == "file2.txt"));
      Assert.IsFalse(result.Data.Any(n => n.Name == "file10.txt"));
    }

    [Test]
    public void FindByPattern_Recursive_SearchesSubdirectories()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");
      _fs.CreateDirectory("/home/user/downloads");
      _fs.CreateFile("/home/user/root.txt", "");
      _fs.CreateFile("/home/user/documents/doc.txt", "");
      _fs.CreateFile("/home/user/downloads/download.txt", "");

      // Act
      var result = _fs.FindByPattern("*.txt", "/home/user", true);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(3, result.Data.Count);
    }

    [Test]
    public void FindByPattern_NonRecursive_SearchesOnlyCurrentDirectory()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/documents");
      _fs.CreateFile("/home/user/root.txt", "");
      _fs.CreateFile("/home/user/documents/doc.txt", "");

      // Act
      var result = _fs.FindByPattern("*.txt", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(1, result.Data.Count);
      Assert.AreEqual("root.txt", result.Data[0].Name);
    }

    [Test]
    public void FindByPattern_NoMatches_ReturnsEmptyList()
    {
      // Act
      var result = _fs.FindByPattern("*.xyz", "/home/user", true);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(0, result.Data.Count);
    }

    [Test]
    public void FindByPattern_EmptyPattern_ReturnsFailure()
    {
      // Act
      var result = _fs.FindByPattern("", "/home/user", true);

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("cannot be empty"));
    }

    [Test]
    public void FindByPattern_NonExistentStartPath_ReturnsFailure()
    {
      // Act
      var result = _fs.FindByPattern("*.txt", "/nonexistent", true);

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("not found"));
    }

    [Test]
    public void FindByPattern_StartPathIsFile_ReturnsFailure()
    {
      // Arrange
      _fs.CreateFile("/home/user/test.txt", "");

      // Act
      var result = _fs.FindByPattern("*.txt", "/home/user/test.txt", true);

      // Assert
      Assert.IsFalse(result.IsSuccess);
      Assert.That(result.ErrorMessage, Does.Contain("Not a directory"));
    }

    [Test]
    public void FindByPattern_MatchesDirectoriesAndFiles()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/test_dir");
      _fs.CreateFile("/home/user/test_file.txt", "");

      // Act
      var result = _fs.FindByPattern("test_*", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(2, result.Data.Count);
      Assert.IsTrue(result.Data.Any(n => n.Name == "test_dir" && n.IsDirectory));
      Assert.IsTrue(result.Data.Any(n => n.Name == "test_file.txt" && !n.IsDirectory));
    }

    [Test]
    public void FindFilesByPattern_OnlyReturnsFiles()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/test_dir");
      _fs.CreateFile("/home/user/test_file.txt", "");

      // Act
      var result = _fs.FindFilesByPattern("test_*", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(1, result.Data.Count);
      Assert.AreEqual("test_file.txt", result.Data[0].Name);
      Assert.IsFalse(result.Data[0].IsDirectory);
    }

    [Test]
    public void FindFilesByPattern_NoFiles_ReturnsEmptyList()
    {
      // Arrange
      _fs.CreateDirectory("/home/user/test_dir");

      // Act
      var result = _fs.FindFilesByPattern("test_*", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(0, result.Data.Count);
    }

    [Test]
    public void FindByPattern_ComplexPattern_ReturnsCorrectMatches()
    {
      // Arrange
      _fs.CreateFile("/home/user/config.txt", "");
      _fs.CreateFile("/home/user/config.xml", "");
      _fs.CreateFile("/home/user/config.json", "");
      _fs.CreateFile("/home/user/settings.txt", "");

      // Act
      var result = _fs.FindByPattern("config.*", "/home/user", false);

      // Assert
      Assert.IsTrue(result.IsSuccess);
      Assert.AreEqual(3, result.Data.Count);
      Assert.IsTrue(result.Data.All(n => n.Name.StartsWith("config.")));
    }

    #endregion
  }
}
