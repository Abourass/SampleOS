using UnityEngine;

[CreateAssetMenu(fileName = "TerminalConfig", menuName = "Terminal/Config")]
public class TerminalConfig : ScriptableObject
{
  [TextArea(3, 10)]
  public string welcomeMessage = "Welcome to Terminal Simulator v1.0\nType 'help' for available commands.";

  public Color backgroundColor = Color.black;
  public Color textColor = Color.white;
  public Color directoryColor = new Color(0.306f, 0.592f, 0.847f); // #4E97D8
  public Color promptColor = new Color(0.533f, 0.69f, 0.294f);     // #88B04B

  public int historySize = 50;
  public float typingSpeed = 0.02f;
  public bool useTypingEffect = false;
}
