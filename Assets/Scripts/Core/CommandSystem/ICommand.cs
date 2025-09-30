using System.Threading;
using System.Threading.Tasks;

namespace SampleOS.Core.CommandSystem
{
  /// <summary>
  /// Base command interface - all commands implement this
  /// </summary>
  public interface ICommand
  {
    string Name { get; }
    string Description { get; }
    string Usage { get; }

    /// <summary>
    /// Execute the command
    /// </summary>
    CommandResult Execute(string[] args, CommandContext context);
  }

  /// <summary>
  /// Commands that support async execution
  /// </summary>
  public interface IAsyncCommand : ICommand
  {
    Task<CommandResult> ExecuteAsync(string[] args, CommandContext context);
    bool SupportsCancellation { get; }
  }

  /// <summary>
  /// Commands that can receive piped input
  /// </summary>
  public interface IPipeableCommand : ICommand
  {
    bool AcceptsPipedInput { get; }
  }

  /// <summary>
  /// Commands that need interactive input
  /// </summary>
  public interface IInteractiveCommand : ICommand
  {
    bool IsWaitingForInput { get; }
    void ProcessInput(string input, CommandContext context);
  }

  /// <summary>
  /// Base class with helper methods
  /// </summary>
  public abstract class CommandBase : ICommand
  {
    public abstract string Name { get; }
    public abstract string Description { get; }
    public abstract string Usage { get; }

    public abstract CommandResult Execute(string[] args, CommandContext context);

    // Helper methods
    protected void WriteOutput(CommandContext context, string message)
    {
      context.Stdout.WriteLine(message);
    }

    protected void WriteError(CommandContext context, string message)
    {
      context.Stderr.WriteLine(message);
    }

    protected bool HasPipedInput(CommandContext context, out string input)
    {
      input = context.PipedInput;
      return !string.IsNullOrEmpty(input);
    }

    protected void ReportProgress(CommandContext context, float percentage, string message)
    {
      context.Progress?.Report(new CommandProgress
      {
        Percentage = percentage,
        Message = message
      });
    }
  }
}


