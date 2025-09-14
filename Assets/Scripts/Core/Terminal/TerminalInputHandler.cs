using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TerminalInputHandler
{
  private TMP_InputField inputField;
  private TerminalHistory history;
  private Action<string> onCommandSubmitted;

  private int currentHistoryIndex = -1;

  public TerminalInputHandler(TMP_InputField inputField, TerminalHistory history, Action<string> onCommandSubmitted)
  {
    this.inputField = inputField;
    this.history = history;
    this.onCommandSubmitted = onCommandSubmitted;

    SetupInputField();
  }

  private void SetupInputField()
  {
    // Handle submission of commands when Enter is pressed
    inputField.onSubmit.AddListener(SubmitCommand);

    // Capture special key events for history navigation
    inputField.onValidateInput += HandleKeyInput;
  }

  public void FocusInput()
  {
    inputField.ActivateInputField();
    inputField.Select();

    // Reset history index when starting new input
    currentHistoryIndex = -1;
  }

  private void SubmitCommand(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      // Just show a new prompt if empty
      onCommandSubmitted?.Invoke("");
      inputField.text = "";
      return;
    }

    // Add to history and process
    history.AddCommand(input);
    onCommandSubmitted?.Invoke(input);
    inputField.text = "";
  }

  private char HandleKeyInput(string text, int charIndex, char addedChar)
  {
    // Process up/down arrow for history navigation using the new Input System
    var keyboard = Keyboard.current;
    if (keyboard != null)
    {
      // Check for up arrow
      if (keyboard.upArrowKey.wasPressedThisFrame)
      {
        ShowPreviousCommand();
        // Return \0 to prevent the arrow character from being inserted
        return '\0';
      }
      // Check for down arrow
      else if (keyboard.downArrowKey.wasPressedThisFrame)
      {
        ShowNextCommand();
        return '\0';
      }
    }

    // Tab completion could be added here

    // Allow the character to be added normally
    return addedChar;
  }

  private void ShowPreviousCommand()
  {
    if (history.CommandCount == 0) return;

    currentHistoryIndex = Mathf.Min(currentHistoryIndex + 1, history.CommandCount - 1);
    inputField.text = history.GetCommand(currentHistoryIndex);
    inputField.caretPosition = inputField.text.Length;
  }

  private void ShowNextCommand()
  {
    if (history.CommandCount == 0) return;

    currentHistoryIndex = Mathf.Max(currentHistoryIndex - 1, -1);

    if (currentHistoryIndex == -1)
    {
      inputField.text = "";
    }
    else
    {
      inputField.text = history.GetCommand(currentHistoryIndex);
    }

    inputField.caretPosition = inputField.text.Length;
  }
}
