using System.Text;
using UnityEngine;

/// <summary>
/// Implementation of ITerminalOutput that captures text instead of displaying it
/// Used for command piping
/// </summary>
public class CapturedOutput : ITerminalOutput
{
    private StringBuilder buffer = new StringBuilder();
    
    public string CapturedText => buffer.ToString();

    public void AppendText(string text)
    {
        buffer.Append(text);
    }

    public void DisplayPrompt(string path)
    {
        // Do nothing when capturing output
    }

    public void Clear()
    {
        buffer.Clear();
    }

    public void SetColor(Color color)
    {
        // Color information is ignored in captured output
    }
}
