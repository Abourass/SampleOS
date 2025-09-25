
using System.Collections.Generic;
using System.IO;
using System.Threading;
using UnityEngine;

/// <summary>
/// Enhanced execution context that separates stdout and stderr streams
/// </summary>
public class CommandExecutionContext
{
    public TextWriter Stdout { get; set; }
    public TextWriter Stderr { get; set; }
    public Dictionary<string, object> Variables { get; set; } = new Dictionary<string, object>();
    public CancellationToken CancellationToken { get; set; }
    public int ExitCode { get; set; } = 0;
    public bool IsInteractive { get; set; } = false;

    public CommandExecutionContext(TextWriter stdout = null, TextWriter stderr = null)
    {
        Stdout = stdout ?? new StringWriter();
        Stderr = stderr ?? new StringWriter();
    }
}

/// <summary>
/// Enhanced output handler that captures both stdout and stderr
/// </summary>
public class StreamCapture : ITerminalOutput
{
    private StringWriter stdoutWriter = new StringWriter();
    private StringWriter stderrWriter = new StringWriter();
    
    public string StdoutContent => stdoutWriter.ToString();
    public string StderrContent => stderrWriter.ToString();
    
    public CommandExecutionContext CreateContext()
    {
        return new CommandExecutionContext(stdoutWriter, stderrWriter);
    }

    public void AppendText(string text)
    {
        stdoutWriter.Write(text);
    }

    public void DisplayPrompt(string path)
    {
        // Prompts go to stdout
        stdoutWriter.Write($"{path}> ");
    }

    public void Clear()
    {
        stdoutWriter.GetStringBuilder().Clear();
        stderrWriter.GetStringBuilder().Clear();
    }

    public void SetColor(Color color)
    {
        // Color information stored as metadata if needed
    }
}
