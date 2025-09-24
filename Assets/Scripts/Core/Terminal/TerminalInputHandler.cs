using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TerminalInputHandler : MonoBehaviour
{
  [SerializeField] private TMP_InputField inputField;
  private TerminalHistory history;
  private Action<string> onCommandSubmitted;
  private CommandProcessor commandProcessor;

  private int currentHistoryIndex = -1;
  
  // Track last frame's key states to detect new presses
  private bool wasUpPressed;
  private bool wasDownPressed;
  private bool wasEscapePressed;
  
  // Add a delay to prevent immediate input after entering interactive mode
  private float interactiveModeStartTime;
  private const float INPUT_DELAY = 0.3f; // 300ms delay
  private bool hasEnteredInteractiveMode = false;

  public void Initialize(TerminalHistory history, Action<string> onCommandSubmitted, CommandProcessor commandProcessor)
  {
    this.history = history;
    this.onCommandSubmitted = onCommandSubmitted;
    this.commandProcessor = commandProcessor;

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
    
    // Check if we've just entered interactive mode
    if (commandProcessor != null && commandProcessor.IsWaitingForCommandInput && !hasEnteredInteractiveMode)
    {
      hasEnteredInteractiveMode = true;
      interactiveModeStartTime = Time.time;
    }
    
    // If we're no longer in interactive mode, reset the flag
    if (commandProcessor != null && !commandProcessor.IsWaitingForCommandInput)
    {
      hasEnteredInteractiveMode = false;
    }
  }

  // This Update method handles special keys for interactive commands
  private void Update()
  {
    var keyboard = Keyboard.current;
    if (keyboard == null) return;

    // Only handle special keys when an interactive command is active
    if (commandProcessor != null && commandProcessor.IsWaitingForCommandInput)
    {
      // If we just entered interactive mode, add a delay before accepting input
      if (hasEnteredInteractiveMode && (Time.time - interactiveModeStartTime < INPUT_DELAY))
      {
        // Ignore inputs during the delay period
        return;
      }

      // Handle Up Arrow
      if (keyboard.upArrowKey.isPressed && !wasUpPressed)
      {
        onCommandSubmitted?.Invoke("up");
        wasUpPressed = true;
      }
      else if (!keyboard.upArrowKey.isPressed)
      {
        wasUpPressed = false;
      }

      // Handle Down Arrow
      if (keyboard.downArrowKey.isPressed && !wasDownPressed)
      {
        onCommandSubmitted?.Invoke("down");
        wasDownPressed = true;
      }
      else if (!keyboard.downArrowKey.isPressed)
      {
        wasDownPressed = false;
      }

      // Handle Enter separately since it's used for input submission
      if (keyboard.enterKey.wasPressedThisFrame)
      {
        onCommandSubmitted?.Invoke("enter");
        // Clear input field to avoid regular submission
        inputField.text = "";
      }

      // Handle Escape
      if (keyboard.escapeKey.isPressed && !wasEscapePressed)
      {
        onCommandSubmitted?.Invoke("escape");
        wasEscapePressed = true;
      }
      else if (!keyboard.escapeKey.isPressed)
      {
        wasEscapePressed = false;
      }
    }
    // Handle history navigation in regular mode
    else if (inputField.isFocused)
    {
      // Handle Up Arrow for history navigation in normal mode
      if (keyboard.upArrowKey.wasPressedThisFrame)
      {
        ShowPreviousCommand();
      }
      // Handle Down Arrow for history navigation in normal mode
      else if (keyboard.downArrowKey.wasPressedThisFrame)
      {
        ShowNextCommand();
      }
    }
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

    // Reset history navigation index after submitting a command
    currentHistoryIndex = -1;
  }

  private char HandleKeyInput(string text, int charIndex, char addedChar)
  {
    // Normal command history navigation
    if (!commandProcessor.IsWaitingForCommandInput)
    {
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
    }

    // Allow the character to be added normally
    return addedChar;
  }

  private void ShowPreviousCommand()
  {
    if (history.CommandCount == 0) return;

    currentHistoryIndex = Mathf.Min(currentHistoryIndex + 1, history.CommandCount - 1);
    inputField.text = history.GetCommand(currentHistoryIndex);
    
    // Set caret at the end of the text
    inputField.caretPosition = inputField.text.Length;
    
    // Make sure we re-focus the input field
    inputField.ActivateInputField();
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
    
    // Set caret at the end of the text
    inputField.caretPosition = inputField.text.Length;
    
    // Make sure we re-focus the input field
    inputField.ActivateInputField();
  }
}
