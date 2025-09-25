using System;
using System.Text;
using System.Text.RegularExpressions;

public class GrepCommand : ICommand, IFileSystemCommand, IPipeableCommand
{
    private VirtualFileSystem fileSystem;
    
    public string Name => "grep";
    public string Description => "Search for patterns in text";
    public string Usage => "grep <pattern> [file] or pipe text into grep <pattern>";
    
    public GrepCommand(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }
    
    public void Execute(string[] args, ITerminalOutput output)
    {
        if (args.Length < 2)
        {
            output.AppendText($"Usage: {Usage}\n");
            return;
        }
        
        string pattern = args[0];
        string filePath = args[1];
        
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
        
        // Process the file content line by line
        FilterText(fileNode.Content, pattern, output);
    }
    
    public void ExecuteWithInput(string[] args, ITerminalOutput output, string inputText)
    {
        if (args.Length < 1)
        {
            output.AppendText("Error: No pattern specified. Usage: grep <pattern>\n");
            return;
        }
        
        string pattern = args[0];
        FilterText(inputText, pattern, output);
    }
    
    private void FilterText(string text, string pattern, ITerminalOutput output)
    {
        try
        {
            StringBuilder result = new StringBuilder();
            string[] lines = text.Split('\n');
            
            Regex regex = new Regex(pattern);
            
            foreach (string line in lines)
            {
                if (regex.IsMatch(line))
                {
                    result.AppendLine(line);
                }
            }
            
            output.AppendText(result.ToString());
        }
        catch (ArgumentException ex)
        {
            output.AppendText($"Invalid regular expression pattern: {ex.Message}\n");
        }
    }
    
    public void SetFileSystem(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }
}
