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

  // Maximum number of lines to keep in the terminal (for performance)
  private const int MAX_LINES = 1000;

  public TerminalOutputHandler(TMP_Text outputText, ScrollRect scrollRect)
  {
    this.outputText = outputText;
    this.scrollRect = scrollRect;
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
    AppendText($"{path}{DEFAULT_PROMPT}");
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
    // Defer scrolling to the next frame to ensure layout has been updated
    if (scrollRect != null)
    {
      // Use Unity's coroutine system via MonoBehaviour.StartCoroutine
      if (outputText.gameObject.activeInHierarchy)
      {
        // Can't use StartCoroutine directly, so we'll use Canvas.ForceUpdateCanvases
        Canvas.ForceUpdateCanvases();
        scrollRect.verticalNormalizedPosition = 0f;
      }
    }
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
