using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages the history of commands entered in the terminal.
/// Allows navigating through previous commands using up/down arrows.
/// </summary>
public class TerminalHistory
{
  // List to store command history
  private List<string> commandHistory = new List<string>();

  // Maximum number of commands to store in history
  private const int MAX_HISTORY_SIZE = 100;

  /// <summary>
  /// The number of commands stored in history
  /// </summary>
  public int CommandCount => commandHistory.Count;

  /// <summary>
  /// Adds a command to the history.
  /// </summary>
  /// <param name="command">The command to add</param>
  public void AddCommand(string command)
  {
    // Don't add empty commands or duplicates of the most recent command
    if (string.IsNullOrWhiteSpace(command) ||
        (commandHistory.Count > 0 && commandHistory[0] == command))
    {
      return;
    }

    // Add to the beginning of the list (most recent first)
    commandHistory.Insert(0, command);

    // Trim history if it exceeds the maximum size
    if (commandHistory.Count > MAX_HISTORY_SIZE)
    {
      commandHistory.RemoveAt(commandHistory.Count - 1);
    }
  }

  /// <summary>
  /// Gets a command from history by index.
  /// Index 0 is the most recent command.
  /// </summary>
  /// <param name="index">The index of the command to retrieve</param>
  /// <returns>The command at the specified index</returns>
  public string GetCommand(int index)
  {
    if (index >= 0 && index < commandHistory.Count)
    {
      return commandHistory[index];
    }

    return string.Empty;
  }

  /// <summary>
  /// Clears the command history
  /// </summary>
  public void Clear()
  {
    commandHistory.Clear();
  }

  /// <summary>
  /// Gets all commands in the history, with most recent first
  /// </summary>
  /// <returns>Array of command strings</returns>
  public string[] GetAllCommands()
  {
    return commandHistory.ToArray();
  }
}
