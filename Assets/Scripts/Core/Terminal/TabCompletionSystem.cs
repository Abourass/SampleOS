using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Handles tab completion suggestions for the terminal
/// </summary>
public class TabCompletionSystem
{
    private VirtualFileSystem fileSystem;
    private TerminalHistory history;
    private CommandProcessor commandProcessor;
    
    // Cache for performance
    private Dictionary<string, List<string>> commandCache = new Dictionary<string, List<string>>();
    private string lastCacheKey = "";

    public TabCompletionSystem(VirtualFileSystem fs, TerminalHistory hist, CommandProcessor cmdProc)
    {
        fileSystem = fs;
        history = hist;
        commandProcessor = cmdProc;
        
        // Pre-populate known commands
        RefreshCommandCache();
    }

    /// <summary>
    /// Gets the best completion suggestion for the current input
    /// </summary>
    /// <param name="currentInput">What the user has typed so far</param>
    /// <param name="caretPosition">Position of cursor in the input</param>
    /// <returns>Suggested completion or null if none found</returns>
    public string GetCompletionSuggestion(string currentInput, int caretPosition)
    {
        if (string.IsNullOrEmpty(currentInput) || caretPosition < 0)
            return null;

        // Get the word being typed at the cursor position
        var wordInfo = GetCurrentWord(currentInput, caretPosition);
        if (string.IsNullOrEmpty(wordInfo.word))
            return null;

        // Try different completion sources in order of priority
        string suggestion = null;

        // 1. Try command completion (if it's the first word)
        if (wordInfo.isFirstWord)
        {
            suggestion = GetCommandCompletion(wordInfo.word);
            if (suggestion != null)
                return BuildFullSuggestion(currentInput, wordInfo, suggestion);
        }

        // 2. Try file/directory completion
        suggestion = GetFileCompletion(wordInfo.word);
        if (suggestion != null)
            return BuildFullSuggestion(currentInput, wordInfo, suggestion);

        // 3. Try history completion
        suggestion = GetHistoryCompletion(currentInput);
        if (suggestion != null && suggestion.Length > currentInput.Length)
            return suggestion;

        return null;
    }

    /// <summary>
    /// Information about the word being completed
    /// </summary>
    private struct WordInfo
    {
        public string word;
        public int startIndex;
        public int endIndex;
        public bool isFirstWord;
    }

    /// <summary>
    /// Gets information about the word at the cursor position
    /// </summary>
    private WordInfo GetCurrentWord(string input, int caretPosition)
    {
        var wordInfo = new WordInfo();
        
        // Find word boundaries
        wordInfo.startIndex = caretPosition;
        wordInfo.endIndex = caretPosition;

        // Move start back to beginning of word
        while (wordInfo.startIndex > 0 && !char.IsWhiteSpace(input[wordInfo.startIndex - 1]))
        {
            wordInfo.startIndex--;
        }

        // Move end forward to end of word
        while (wordInfo.endIndex < input.Length && !char.IsWhiteSpace(input[wordInfo.endIndex]))
        {
            wordInfo.endIndex++;
        }

        // Extract the word
        if (wordInfo.startIndex < wordInfo.endIndex)
        {
            wordInfo.word = input.Substring(wordInfo.startIndex, wordInfo.endIndex - wordInfo.startIndex);
        }

        // Check if this is the first word (command)
        string beforeWord = input.Substring(0, wordInfo.startIndex).Trim();
        wordInfo.isFirstWord = string.IsNullOrEmpty(beforeWord);

        return wordInfo;
    }

    /// <summary>
    /// Builds the full suggestion string with the completed word
    /// </summary>
    private string BuildFullSuggestion(string currentInput, WordInfo wordInfo, string completion)
    {
        return currentInput.Substring(0, wordInfo.startIndex) + 
               completion + 
               currentInput.Substring(wordInfo.endIndex);
    }

    /// <summary>
    /// Gets command completion suggestions
    /// </summary>
    private string GetCommandCompletion(string partial)
    {
        if (!commandCache.ContainsKey("commands"))
            return null;

        var matches = commandCache["commands"]
            .Where(cmd => cmd.StartsWith(partial, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(cmd => cmd)
            .ToList();

        return matches.FirstOrDefault();
    }

    /// <summary>
    /// Gets file/directory completion suggestions
    /// </summary>
    private string GetFileCompletion(string partial)
    {
        // Handle absolute vs relative paths
        string searchPath = "/";
        string searchPattern = partial;

        if (partial.Contains("/"))
        {
            int lastSlash = partial.LastIndexOf('/');
            searchPath = partial.Substring(0, lastSlash + 1);
            searchPattern = partial.Substring(lastSlash + 1);
            
            // If it's a relative path, resolve it relative to current directory
            if (!searchPath.StartsWith("/"))
            {
                searchPath = fileSystem.CurrentPath + "/" + searchPath;
            }
        }
        else
        {
            searchPath = fileSystem.CurrentPath;
        }

        // Get directory contents
        var result = fileSystem.ListDirectory(searchPath);
        if (!result.IsSuccess)
            return null;

        // Find matches
        var matches = result.Data
            .Where(node => node.Name.StartsWith(searchPattern, System.StringComparison.OrdinalIgnoreCase))
            .OrderBy(node => node.Name)
            .ToList();

        if (!matches.Any())
            return null;

        var bestMatch = matches.First();
        
        // Build the completion
        string completion;
        if (partial.Contains("/"))
        {
            // For paths, replace just the filename part
            int lastSlash = partial.LastIndexOf('/');
            completion = partial.Substring(0, lastSlash + 1) + bestMatch.Name;
        }
        else
        {
            completion = bestMatch.Name;
        }

        // Add trailing slash for directories
        if (bestMatch.IsDirectory && !completion.EndsWith("/"))
        {
            completion += "/";
        }

        return completion;
    }

    /// <summary>
    /// Gets completion from command history
    /// </summary>
    private string GetHistoryCompletion(string partial)
    {
        if (string.IsNullOrEmpty(partial))
            return null;

        var commands = history.GetAllCommands();
        var match = commands.FirstOrDefault(cmd => 
            cmd.StartsWith(partial, System.StringComparison.OrdinalIgnoreCase) && 
            cmd.Length > partial.Length);

        return match;
    }

    /// <summary>
    /// Refreshes the command cache
    /// </summary>
    private void RefreshCommandCache()
    {
        var commands = new List<string>
        {
            "ls", "cd", "mkdir", "touch", "cat", "grep", "wc", "gum",
            "ssh", "netstat", "nmap", "networks", "vpn-connect",
            "ps", "vuln-scan", "vulns", "exploit", "help", "clear",
            "alias", "owned", "quit"
        };

        commandCache["commands"] = commands;
    }

    /// <summary>
    /// Updates the file system reference when it changes
    /// </summary>
    public void UpdateFileSystem(VirtualFileSystem newFileSystem)
    {
        fileSystem = newFileSystem;
    }
}
