using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// Handles displaying text output in the terminal UI.
/// </summary>
public class TerminalOutputHandler : ITerminalOutput
{
  private TMP_Text outputText;
  private ScrollRect scrollRect;
  private Color currentColor = Color.white;
  private const string DEFAULT_PROMPT = "> ";
  private PromptConfig promptConfig;
  private CommandProcessor commandProcessor;

  // Reference to a MonoBehaviour for starting coroutines
  private MonoBehaviour coroutineRunner;

  // Maximum number of lines to keep in the terminal (for performance)
  private const int MAX_LINES = 1000;

  public TerminalOutputHandler(TMP_Text outputText, ScrollRect scrollRect, PromptConfig config, CommandProcessor cmdProcessor, MonoBehaviour coroutineRunner)
  {
    this.outputText = outputText;
    this.scrollRect = scrollRect;
    this.promptConfig = config;
    this.commandProcessor = cmdProcessor;
    this.coroutineRunner = coroutineRunner;
  }

  /// <summary>
  /// Appends text to the terminal output
  /// </summary>
  public void AppendText(string text)
  {
    // Apply color formatting if not default color
    if (currentColor != Color.white)
    {
      string colorHex = ColorUtility.ToHtmlStringRGB(currentColor);
      text = $"<color=#{colorHex}>{text}</color>";
    }

    outputText.text += text;

    // Trim output if it gets too long
    TrimOutputIfNeeded();

    // Scroll to bottom after appending text
    ScrollToBottom();
  }

  /// <summary>
  /// Displays a welcome message when the terminal starts
  /// </summary>
  public void DisplayWelcomeMessage(string welcomeMessage)
  {
    Clear();
    AppendText(welcomeMessage + "\n\n");
  }

  /// <summary>
  /// Displays the command prompt with the current path
  /// </summary>
  public void DisplayPrompt(string path)
  {
    if (promptConfig != null && promptConfig.enablePowerline)
    {
      PowerlinePrompt powerline = new PowerlinePrompt();
      powerline.SetPromptColor(promptConfig.promptColor);

      // Add status badge if enabled
      if (promptConfig.showStatusBadge && commandProcessor != null)
      {
        bool lastCommandSucceeded = commandProcessor.LastCommandSucceeded;
        powerline.AddBadge(
            lastCommandSucceeded ? "✓" : "✗",
            lastCommandSucceeded ? promptConfig.successBackground : promptConfig.failureBackground,
            Color.white
        );
      }

      // Add user badge if enabled
      if (promptConfig.showUserBadge)
      {
        powerline.AddBadge(
            PowerlinePrompt.CreateUserBadge(System.Environment.UserName)
        );
      }

      // Add directory badge if enabled
      if (promptConfig.showDirectoryBadge)
      {
        // Get just the last part of the path for cleaner display
        string dirName = System.IO.Path.GetFileName(path);
        if (string.IsNullOrEmpty(dirName)) dirName = path;

        powerline.AddBadge(
            PowerlinePrompt.CreateDirectoryBadge(dirName)
        );
      }

      // Add time badge if enabled
      if (promptConfig.showTimeBadge)
      {
        powerline.AddBadge(
            PowerlinePrompt.CreateTimeBadge()
        );
      }

      // Generate and display the prompt
      string prompt = powerline.Generate(promptConfig.promptSymbol);
      AppendText(prompt);
    }
    else
    {
      // Fall back to the simple prompt style
      AppendText($"{path}{DEFAULT_PROMPT}");
    }
  }

  /// <summary>
  /// Clears all text from the terminal
  /// </summary>
  public void Clear()
  {
    outputText.text = "";
  }

  /// <summary>
  /// Sets the text color for subsequent output
  /// </summary>
  public void SetColor(Color color)
  {
    currentColor = color;
  }

  /// <summary>
  /// Ensures the scroll view shows the most recent text
  /// </summary>
  private void ScrollToBottom()
  {
    if (scrollRect == null) return;

    if (coroutineRunner != null && coroutineRunner.gameObject.activeInHierarchy)
    {
      coroutineRunner.StartCoroutine(ScrollToBottomCoroutine());
    }
    else
    {
      // Fallback if coroutine runner isn't available
      Canvas.ForceUpdateCanvases();
      scrollRect.verticalNormalizedPosition = 0f;
    }
  }

  /// <summary>
  /// Coroutine to scroll to the bottom of the content after layout updates
  /// </summary>
  private IEnumerator ScrollToBottomCoroutine()
  {
    // Wait for end of frame to ensure all layout calculations are done
    yield return new WaitForEndOfFrame();

    // Force the canvas to update
    Canvas.ForceUpdateCanvases();

    // Ensure the layout group has updated
    if (outputText.transform.parent.GetComponent<LayoutGroup>() != null)
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(outputText.transform.parent as RectTransform);
    }

    // Another frame to be extra safe
    yield return null;

    // Scroll to bottom (0 = bottom, 1 = top for vertical scroll)
    scrollRect.verticalNormalizedPosition = 0f;

    // One more update for good measure
    yield return null;
    scrollRect.verticalNormalizedPosition = 0f;
  }

  /// <summary>
  /// Trims the output text if it exceeds the maximum number of lines
  /// </summary>
  private void TrimOutputIfNeeded()
  {
    string text = outputText.text;
    string[] lines = text.Split('\n');

    if (lines.Length > MAX_LINES)
    {
      // Keep only the most recent MAX_LINES lines
      System.Text.StringBuilder sb = new System.Text.StringBuilder();
      for (int i = lines.Length - MAX_LINES; i < lines.Length; i++)
      {
        sb.AppendLine(lines[i]);
      }
      outputText.text = sb.ToString();
    }
  }
}
