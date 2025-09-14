using System.Collections.Generic;
using UnityEngine;
using System.Text;

/// <summary>
/// Generates powerline-style prompts with multiple badges
/// </summary>
public class PowerlinePrompt
{
  private List<PowerlineBadge> badges = new List<PowerlineBadge>();
  private Color promptColor = Color.white;

  /// <summary>
  /// Adds a badge to the prompt
  /// </summary>
  public void AddBadge(PowerlineBadge badge)
  {
    badges.Add(badge);
  }

  /// <summary>
  /// Adds a badge with the specified parameters
  /// </summary>
  public void AddBadge(string text, Color bgColor, Color textColor, string icon = "")
  {
    badges.Add(new PowerlineBadge(text, bgColor, textColor, icon));
  }

  /// <summary>
  /// Sets the color for the command input prompt
  /// </summary>
  public void SetPromptColor(Color color)
  {
    promptColor = color;
  }

  /// <summary>
  /// Clears all badges
  /// </summary>
  public void ClearBadges()
  {
    badges.Clear();
  }

  /// <summary>
  /// Generates the complete prompt with all badges
  /// </summary>
  public string Generate(string promptSymbol = ">")
  {
    StringBuilder result = new StringBuilder();

    // Render each badge
    for (int i = 0; i < badges.Count; i++)
    {
      PowerlineBadge nextBadge = (i < badges.Count - 1) ? badges[i + 1] : null;
      result.Append(badges[i].Render(nextBadge));
    }

    // Add the prompt symbol
    string promptHex = ColorUtility.ToHtmlStringRGB(promptColor);
    result.Append($" <color=#{promptHex}>{promptSymbol}</color> ");

    return result.ToString();
  }

  /// <summary>
  /// Creates a standard directory badge
  /// </summary>
  public static PowerlineBadge CreateDirectoryBadge(string path)
  {
    // Extract just the current directory name for cleaner display
    string dirName = System.IO.Path.GetFileName(path);
    if (string.IsNullOrEmpty(dirName)) dirName = path;

    return new PowerlineBadge(
        dirName,
        new Color(0.2f, 0.4f, 0.6f), // Blue background
        Color.white,                  // White text
        "📁"                           // Folder icon
    );
  }

  /// <summary>
  /// Creates a user badge
  /// </summary>
  public static PowerlineBadge CreateUserBadge(string username)
  {
    return new PowerlineBadge(
        username,
        new Color(0.3f, 0.6f, 0.3f), // Green background
        Color.white,                  // White text
        "👤"                          // User icon
    );
  }

  /// <summary>
  /// Creates a status badge (success/failure)
  /// </summary>
  public static PowerlineBadge CreateStatusBadge(bool success)
  {
    return new PowerlineBadge(
        success ? "✓" : "✗",
        success ? new Color(0.2f, 0.7f, 0.3f) : new Color(0.8f, 0.2f, 0.2f), // Green or red
        Color.white,
        ""
    );
  }

  /// <summary>
  /// Creates a time badge
  /// </summary>
  public static PowerlineBadge CreateTimeBadge()
  {
    string time = System.DateTime.Now.ToString("HH:mm");
    return new PowerlineBadge(
        time,
        new Color(0.5f, 0.5f, 0.5f), // Gray background
        Color.white,                  // White text
        "⏱"                          // Clock icon
    );
  }
}
