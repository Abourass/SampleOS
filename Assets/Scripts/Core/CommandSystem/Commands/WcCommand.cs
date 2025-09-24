using System;
using System.Text;

public class WcCommand : ICommand, IFileSystemCommand, IPipeableCommand
{
    private VirtualFileSystem fileSystem;
    
    public string Name => "wc";
    public string Description => "Count lines, words, and characters in a file";
    public string Usage => "wc [options] [file] or pipe text into wc [options]\n  Options:\n  -l  Count lines only\n  -w  Count words only\n  -c  Count characters only";
    
    public WcCommand(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }
    
    public void Execute(string[] args, ITerminalOutput output)
    {
        if (args.Length == 0)
        {
            output.AppendText($"Usage: {Usage}\n");
            return;
        }
        
        bool countLines = true;
        bool countWords = true;
        bool countChars = true;
        string filePath = null;
        
        // Parse arguments
        foreach (string arg in args)
        {
            if (arg.StartsWith("-"))
            {
                // Reset all counts when we have a specific option
                countLines = false;
                countWords = false;
                countChars = false;
                
                if (arg.Contains("l")) countLines = true;
                if (arg.Contains("w")) countWords = true;
                if (arg.Contains("c")) countChars = true;
            }
            else
            {
                filePath = arg;
                break;
            }
        }
        
        if (filePath == null)
        {
            output.AppendText("Error: No file specified\n");
            return;
        }
        
        var fileNode = fileSystem.ResolvePath(filePath);
        if (fileNode == null)
        {
            output.AppendText($"Error: File not found: {filePath}\n");
            return;
        }
        
        if (fileNode.IsDirectory)
        {
            output.AppendText($"Error: {filePath} is a directory\n");
            return;
        }
        
        // Count and display stats
        CountAndOutput(fileNode.Content, countLines, countWords, countChars, output);
    }
    
    public void ExecuteWithInput(string[] args, ITerminalOutput output, string inputText)
    {
        bool countLines = true;
        bool countWords = true;
        bool countChars = true;
        
        // Parse arguments
        if (args.Length > 0)
        {
            foreach (string arg in args)
            {
                if (arg.StartsWith("-"))
                {
                    // Reset all counts when we have a specific option
                    countLines = false;
                    countWords = false;
                    countChars = false;
                    
                    if (arg.Contains("l")) countLines = true;
                    if (arg.Contains("w")) countWords = true;
                    if (arg.Contains("c")) countChars = true;
                }
            }
        }
        
        // Count and display stats
        CountAndOutput(inputText, countLines, countWords, countChars, output);
    }
    
    private void CountAndOutput(string text, bool countLines, bool countWords, bool countChars, ITerminalOutput output)
    {
        int lines = 0, words = 0, chars = 0;
        
        if (countLines)
        {
            lines = text.Split('\n').Length;
        }
        
        if (countWords)
        {
            words = text.Split(new[] { ' ', '\t', '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries).Length;
        }
        
        if (countChars)
        {
            chars = text.Length;
        }
        
        // Build output string based on requested counts
        StringBuilder result = new StringBuilder();
        
        if (countLines) result.Append($"  Lines: {lines}");
        if (countWords) result.Append($"  Words: {words}");
        if (countChars) result.Append($"  Chars: {chars}");
        
        output.AppendText(result.ToString() + "\n");
    }
    
    public void SetFileSystem(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }
}
