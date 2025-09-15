using UnityEngine;

/// <summary>
/// Represents a single badge/segment in a powerline-style prompt
/// </summary>
public class PowerlineBadge
{
  // Font name to use in rich text tags - should match your Nerd Font asset name
  public const string FONT_NAME = "FiraCodeRetina SDF";

  // Powerline symbols from Nerd Font
  public const string SEPARATOR_FILLED_RIGHT = "\uE0B0";   // 
  public const string SEPARATOR_THIN_RIGHT = "\uE0B1";     // 
  public const string SEPARATOR_FILLED_LEFT = "\uE0B2";    // 
  public const string SEPARATOR_THIN_LEFT = "\uE0B3";      // 

  // Common Nerd Font icons
  public const string ICON_FOLDER = "\uF07B";              // 
  public const string ICON_USER = "\uF007";                // 
  public const string ICON_CLOCK = "\uF017";               // 
  public const string ICON_CHECK = "\uF00C";               // 
  public const string ICON_TIMES = "\uF00D";               // 

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

    // Don't separate the icon - keep everything in one set of tags
    string iconPart = string.IsNullOrEmpty(Icon) ? "" : Icon + " ";

    // Use the font tag on the outside, then mark, then color - based on working test case 7
    string badge = $"<font=\"{FONT_NAME}\"><mark=#{bgHex} padding=\"4,4,2,2\"><color=#{textHex}> {iconPart}{Text} </color></mark></font>";

    // Add separator if there's a next badge
    if (nextBadge != null)
    {
      string nextBgHex = ColorUtility.ToHtmlStringRGB(nextBadge.BackgroundColor);
      // Similar structure for the separator - font tag first, then color
      badge += $"<font=\"{FONT_NAME}\"><color=#{bgHex}>{SEPARATOR_FILLED_RIGHT}</color></font>";
    }

    return badge;
  }
}
