using System.Threading;
using System.Threading.Tasks;
using SampleOS.Core.Terminal;

namespace SampleOS.Core.CommandSystem
{
  public interface ICommand
  {
    string Name { get; }
    string Description { get; }
    string Usage { get; }
    void Execute(string[] args, ITerminalOutput output);
  }

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

  public interface IInteractiveCommand : ICommand
  {
    /// <summary>
    /// Whether the command is currently waiting for user input
    /// </summary>
    bool IsWaitingForInput { get; }

    /// <summary>
    /// Process input provided by the user during an interactive session
    /// </summary>
    /// <param name="input">User input text</param>
    /// <param name="output">Terminal output handler</param>
    void ProcessInput(string input, ITerminalOutput output);

    /// <summary>
    /// Request input from the user with a specific prompt
    /// </summary>
    /// <param name="prompt">The prompt to display to the user</param>
    /// <param name="output">Terminal output handler</param>
    void RequestInput(string prompt, ITerminalOutput output);
  }

  /// <summary>
  /// Interface for commands that can accept piped input from another command
  /// </summary>
  public interface IPipeableCommand : ICommand
  {
    /// <summary>
    /// Executes the command with piped input from a previous command
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <param name="output">Terminal output handler</param>
    /// <param name="inputText">Text received from previous command in pipe</param>
    void ExecuteWithInput(string[] args, ITerminalOutput output, string inputText);
  }

  public interface IAsyncCommand : ICommand
  {
    /// <summary>
    /// Execute command asynchronously with cancellation support
    /// </summary>
    Task<Result<string>> ExecuteAsync(
        CommandExecutionContext context,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Reports progress for long-running operations (0.0 to 1.0)
    /// </summary>
    event System.Action<float, string> OnProgress;

    /// <summary>
    /// Indicates if command supports cancellation (Ctrl+C)
    /// </summary>
    bool SupportsCancellation { get; }
  }

  public interface IStreamingCommand : IAsyncCommand
  {
    /// <summary>
    /// Stream output as it's generated instead of buffering
    /// </summary>
    event System.Action<string, OutputStreamType> OnStreamOutput;
  }
}


