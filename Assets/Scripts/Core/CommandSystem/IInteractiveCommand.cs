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
