using UnityEngine;

[CreateAssetMenu(fileName = "PromptConfig", menuName = "Terminal/Prompt Configuration")]
public class PromptConfig : ScriptableObject
{
  [Header("Powerline Settings")]
  public bool enablePowerline = true;
  public string promptSymbol = ">";

  [Header("Badge Configuration")]
  public bool showUserBadge = true;
  public bool showDirectoryBadge = true;
  public bool showStatusBadge = true;
  public bool showTimeBadge = false;

  [Header("Colors")]
  public Color userBadgeBackground = new Color(0.3f, 0.6f, 0.3f);
  public Color userBadgeText = Color.white;
  public Color dirBadgeBackground = new Color(0.2f, 0.4f, 0.6f);
  public Color dirBadgeText = Color.white;
  public Color successBackground = new Color(0.2f, 0.7f, 0.3f);
  public Color failureBackground = new Color(0.8f, 0.2f, 0.2f);
  public Color promptColor = Color.white;

  [Header("Icons")]
  public string userIcon = "👤";
  public string dirIcon = "📁";
  public string timeIcon = "⏱";
}
