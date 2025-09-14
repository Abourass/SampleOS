/// <summary>
/// Interface for commands that interact with the file system.
/// Commands implementing this interface can have their file system reference updated.
/// </summary>
public interface IFileSystemCommand
{
  /// <summary>
  /// Updates the file system reference used by this command
  /// </summary>
  /// <param name="fs">The new file system to use</param>
  void SetFileSystem(VirtualFileSystem fs);
}
