using UnityEngine;

/// <summary>
/// Interface for classes that can display output in a terminal.
/// Used by commands to show results without knowing the underlying implementation.
/// </summary>
public interface ITerminalOutput
{
  /// <summary>
  /// Appends text to the terminal output
  /// </summary>
  /// <param name="text">Text to append</param>
  void AppendText(string text);

  /// <summary>
  /// Displays the command prompt with the current path
  /// </summary>
  /// <param name="path">Current directory path</param>
  void DisplayPrompt(string path);

  /// <summary>
  /// Clears all text from the terminal
  /// </summary>
  void Clear();

  /// <summary>
  /// Sets the text color for subsequent output
  /// </summary>
  /// <param name="color">Color to use</param>
  void SetColor(Color color);
}
