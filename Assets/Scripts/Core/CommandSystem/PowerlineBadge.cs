using UnityEngine;

/// <summary>
/// Represents a single badge/segment in a powerline-style prompt
/// </summary>
public class PowerlineBadge
{
  // Content of the badge
  public string Text { get; set; }

  // Badge styling
  public Color BackgroundColor { get; set; }
  public Color TextColor { get; set; }

  // Optional icon or symbol to display before text
  public string Icon { get; set; }

  // Constructor
  public PowerlineBadge(string text, Color bgColor, Color textColor, string icon = "")
  {
    Text = text;
    BackgroundColor = bgColor;
    TextColor = textColor;
    Icon = icon;
  }

  /// <summary>
  /// Renders the badge with rich text formatting
  /// </summary>
  public string Render(PowerlineBadge nextBadge = null)
  {
    string bgHex = ColorUtility.ToHtmlStringRGB(BackgroundColor);
    string textHex = ColorUtility.ToHtmlStringRGB(TextColor);

    // Badge content with text coloring
    string content = $"<color=#{textHex}>{(string.IsNullOrEmpty(Icon) ? "" : Icon + " ")}{Text}</color>";

    // Add background color
    string badge = $"<mark=#{bgHex}> {content} </mark>";

    // Add separator if there's a next badge
    if (nextBadge != null)
    {
      string nextBgHex = ColorUtility.ToHtmlStringRGB(nextBadge.BackgroundColor);
      // Create an arrow separator with current bg color as the text color
      badge += $"<color=#{bgHex}><mark=#{nextBgHex}>▶</mark></color>";
    }

    return badge;
  }
}
