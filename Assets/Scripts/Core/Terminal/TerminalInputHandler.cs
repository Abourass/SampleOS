using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Controls;

public class TerminalInputHandler : MonoBehaviour
{
  [SerializeField] private TMP_InputField inputField;
  [SerializeField] private TMP_Text placeholderText;  // For showing completions
  
  private TerminalHistory history;
  private Action<string> onCommandSubmitted;
  private CommandProcessor commandProcessor;
  private VirtualFileSystem fileSystem;
  private TabCompletionSystem completionSystem;

  private int currentHistoryIndex = -1;
  private string currentCompletion = null;
  
  // Track last frame's key states to detect new presses
  private bool wasUpPressed;
  private bool wasDownPressed;
  private bool wasEscapePressed;
  private bool wasTabPressed;
  
  // Add a delay to prevent immediate input after entering interactive mode
  private float interactiveModeStartTime;
  private const float INPUT_DELAY = 0.3f; // 300ms delay
  private bool hasEnteredInteractiveMode = false;

  public void Initialize(TerminalHistory history, Action<string> onCommandSubmitted, CommandProcessor commandProcessor, VirtualFileSystem fileSystem)
  {
    this.history = history;
    this.onCommandSubmitted = onCommandSubmitted;
    this.commandProcessor = commandProcessor;
    this.fileSystem = fileSystem;
    
    // Initialize completion system
    this.completionSystem = new TabCompletionSystem(fileSystem, history, commandProcessor);
    
    SetupInputField();
    SetupPlaceholderText();
  }

  private void SetupInputField()
  {
    // Handle submission of commands when Enter is pressed
    inputField.onSubmit.AddListener(SubmitCommand);

    // Handle text changes for completion suggestions
    inputField.onValueChanged.AddListener(OnTextChanged);

    // Capture special key events for history navigation
    inputField.onValidateInput += HandleKeyInput;
  }

  private void SetupPlaceholderText()
  {
    if (placeholderText == null)
    {
      // Create placeholder text if it doesn't exist
      GameObject placeholderGO = new GameObject("CompletionPlaceholder");
      placeholderGO.transform.SetParent(inputField.transform, false);
      
      placeholderText = placeholderGO.AddComponent<TMP_Text>();
      placeholderText.text = "";
      placeholderText.color = new Color(0.7f, 0.7f, 0.7f, 0.8f); // Light gray
      
      // Get the input field's text component to match its settings
      var inputTextComponent = inputField.textComponent;
      if (inputTextComponent != null)
      {
        // Match the important text properties but not positioning
        placeholderText.font = inputTextComponent.font;
        placeholderText.fontSize = inputTextComponent.fontSize;
        placeholderText.alignment = TextAlignmentOptions.Left; // Force left alignment
      }
      
      // Position it to fill the input field area, like the original approach
      RectTransform placeholderRect = placeholderText.GetComponent<RectTransform>();
      placeholderRect.anchorMin = Vector2.zero;
      placeholderRect.anchorMax = Vector2.one;
      placeholderRect.anchoredPosition = Vector2.zero;
      placeholderRect.sizeDelta = Vector2.zero;
      
      // Match the input field's content padding
      var inputFieldRect = inputField.GetComponent<RectTransform>();
      if (inputField.textViewport != null)
      {
        // Use the viewport's padding if available
        var viewport = inputField.textViewport;
        placeholderRect.offsetMin = viewport.offsetMin;
        placeholderRect.offsetMax = viewport.offsetMax;
      }
      else
      {
        // Add some standard padding to match typical input field margins
        placeholderRect.offsetMin = new Vector2(5, 2); // left, bottom padding
        placeholderRect.offsetMax = new Vector2(-5, -2); // right, top padding
      }
      
      // Ensure it renders behind the input text
      placeholderGO.transform.SetAsFirstSibling();
    }
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

    // Update completion when focusing
    UpdateCompletion();
  }

  private void OnTextChanged(string newText)
  {
    // Don't show completions during interactive command mode
    if (commandProcessor != null && commandProcessor.IsWaitingForCommandInput)
    {
      ClearCompletion();
      return;
    }

    // Add small delay to avoid excessive updates while typing
    if (gameObject.activeInHierarchy)
    {
      StopCoroutine(nameof(DelayedUpdateCompletion));
      StartCoroutine(nameof(DelayedUpdateCompletion));
    }
  }

  /// <summary>
  /// Delays completion update slightly to reduce visual flickering during fast typing
  /// </summary>
  private System.Collections.IEnumerator DelayedUpdateCompletion()
  {
    yield return new WaitForSeconds(0.05f); // 50ms delay
    UpdateCompletion();
  }

  private void UpdateCompletion()
  {
    if (inputField == null || placeholderText == null || completionSystem == null)
      return;

    string currentInput = inputField.text;
    int caretPos = inputField.caretPosition;

    // Get completion suggestion
    currentCompletion = completionSystem.GetCompletionSuggestion(currentInput, caretPos);

    if (!string.IsNullOrEmpty(currentCompletion) && currentCompletion.Length > currentInput.Length)
    {
      // Get the completion part that needs to be shown
      string completionPart = currentCompletion.Substring(currentInput.Length);
      
      // Create invisible text for the typed portion, then show the completion
      string invisiblePrefix = CreateInvisibleText(currentInput);
      placeholderText.text = invisiblePrefix + "<color=#808080><i>" + completionPart + "</i></color>";
      
      // Ensure basic alignment properties match
      AlignPlaceholderWithInputText();
    }
    else
    {
      ClearCompletion();
    }
  }

  /// <summary>
  /// Creates invisible text that takes up the same space as the input text
  /// This pushes the visible completion to start where the input ends
  /// </summary>
  private string CreateInvisibleText(string inputText)
  {
    if (string.IsNullOrEmpty(inputText))
      return "";
    
    // Use completely transparent color (alpha = 0) to make text invisible but still take up space
    // We need to be careful about special characters that might affect layout
    string escapedText = inputText.Replace("<", "&lt;").Replace(">", "&gt;");
    return "<color=#ffffff00>" + escapedText + "</color>";
  }

  /// <summary>
  /// Ensures the placeholder text uses the same basic properties as the input field
  /// </summary>
  private void AlignPlaceholderWithInputText()
  {
    if (placeholderText == null || inputField == null)
      return;
    
    // Only match the essential text properties, not positioning
    var inputTextComponent = inputField.textComponent;
    if (inputTextComponent != null)
    {
      placeholderText.fontSize = inputTextComponent.fontSize;
      placeholderText.font = inputTextComponent.font;
      placeholderText.characterSpacing = inputTextComponent.characterSpacing;
      placeholderText.wordSpacing = inputTextComponent.wordSpacing;
      // Keep left alignment regardless of input field alignment
      placeholderText.alignment = TextAlignmentOptions.Left;
    }
  }

  /// <summary>
  /// Forces an update of text layout to ensure proper positioning
  /// </summary>
  private void ForceTextLayoutUpdate()
  {
    if (placeholderText != null)
    {
      // Force the text mesh to update immediately
      placeholderText.ForceMeshUpdate();
      
      // Ensure canvas updates
      Canvas.ForceUpdateCanvases();
    }
  }

  private void ClearCompletion()
  {
    if (placeholderText != null)
    {
      placeholderText.text = "";
    }
    currentCompletion = null;
  }

  // This Update method handles special keys for interactive commands
  private void Update()
  {
    var keyboard = Keyboard.current;
    if (keyboard == null) return;

    // Handle Tab key for completion
    if (keyboard.tabKey.isPressed && !wasTabPressed)
    {
      AcceptCompletion();
      wasTabPressed = true;
    }
    else if (!keyboard.tabKey.isPressed)
    {
      wasTabPressed = false;
    }

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

  private void AcceptCompletion()
  {
    if (!string.IsNullOrEmpty(currentCompletion) && 
        !commandProcessor.IsWaitingForCommandInput &&
        inputField.isFocused)
    {
      inputField.text = currentCompletion;
      inputField.caretPosition = currentCompletion.Length;
      
      // Clear the completion display
      ClearCompletion();
      
      // Update completion again in case there are further suggestions
      UpdateCompletion();
    }
  }

  private void SubmitCommand(string input)
  {
    if (string.IsNullOrWhiteSpace(input))
    {
      // Just show a new prompt if empty
      onCommandSubmitted?.Invoke("");
      inputField.text = "";
      ClearCompletion();
      return;
    }

    // Add to history and process
    history.AddCommand(input);
    onCommandSubmitted?.Invoke(input);
    inputField.text = "";
    ClearCompletion();

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
        // Check for tab key - prevent it from being inserted as text
        else if (keyboard.tabKey.wasPressedThisFrame)
        {
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
    
    // Clear completion when navigating history
    ClearCompletion();
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
    
    // Clear completion when navigating history
    ClearCompletion();
  }

  /// <summary>
  /// Updates the file system reference when it changes (e.g., SSH connections)
  /// </summary>
  public void UpdateFileSystem(VirtualFileSystem newFileSystem)
  {
    fileSystem = newFileSystem;
    if (completionSystem != null)
    {
      completionSystem.UpdateFileSystem(newFileSystem);
    }
  }
}
