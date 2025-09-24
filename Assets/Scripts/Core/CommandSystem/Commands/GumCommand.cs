using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

/// <summary>
/// Implements a simplified version of the Gum CLI tool for creating interactive TUI elements
/// </summary>
public class GumCommand : ICommand, IInteractiveCommand, IFileSystemCommand
{
    private VirtualFileSystem fileSystem;
    private bool isWaitingForInput;
    private GumMode currentMode;
    private string[] currentOptions;
    private string currentPrompt;
    private string currentTitle;
    private ITerminalOutput currentOutput;
    private int selectedIndex;
    private List<VirtualNode> fileList;

    public string Name => "gum";
    public string Description => "A tool for glamorous shell scripts with TUI components";
    public string Usage => "gum <style> [options]\n" +
                          "  Styles:\n" +
                          "    choose    - Choose an option from a list\n" +
                          "    confirm   - Ask for confirmation (y/n)\n" +
                          "    file      - Select a file from the filesystem\n" +
                          "  Common Options:\n" +
                          "    --title    - Set the title for the prompt\n" +
                          "    --prompt   - Set the prompt text";

    public bool IsWaitingForInput => isWaitingForInput;

    private enum GumMode
    {
        Choose,
        Confirm,
        File
    }

    public GumCommand(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }

    public void SetFileSystem(VirtualFileSystem fs)
    {
        fileSystem = fs;
    }

    public void Execute(string[] args, ITerminalOutput output)
    {
        if (args.Length == 0)
        {
            output.AppendText($"Error: Style required\n{Usage}\n");
            return;
        }

        string style = args[0].ToLower();
        
        // Parse common options
        currentTitle = ExtractOption(args, "--title");
        currentPrompt = ExtractOption(args, "--prompt");
        currentOutput = output;

        switch (style)
        {
            case "choose":
                ExecuteChoose(args, output);
                break;
            case "confirm":
                ExecuteConfirm(args, output);
                break;
            case "file":
                ExecuteFile(args, output);
                break;
            default:
                output.AppendText($"Error: Unknown style '{style}'\n{Usage}\n");
                break;
        }
    }

    private void ExecuteChoose(string[] args, ITerminalOutput output)
    {
        // Extract options after the "choose" command
        List<string> options = new List<string>();
        for (int i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--"))
            {
                options.Add(args[i]);
            }
            else
            {
                // Skip option and its value
                i++;
            }
        }

        if (options.Count == 0)
        {
            output.AppendText("Error: No options provided for choose\n");
            return;
        }

        currentMode = GumMode.Choose;
        currentOptions = options.ToArray();
        selectedIndex = 0;
        
        DisplayChooseOptions(output);
    }

    private void ExecuteConfirm(string[] args, ITerminalOutput output)
    {
        currentMode = GumMode.Confirm;
        
        string prompt = currentPrompt ?? "Confirm?";
        string displayTitle = currentTitle != null ? $"{currentTitle}\n" : "";
        
        output.AppendText($"{displayTitle}{prompt} (y/n): ");
        isWaitingForInput = true;
    }

    private void ExecuteFile(string[] args, ITerminalOutput output)
    {
        string path = ".";
        
        // Check for path argument
        for (int i = 1; i < args.Length; i++)
        {
            if (!args[i].StartsWith("--") && args[i-1] != "--title" && args[i-1] != "--prompt")
            {
                path = args[i];
                break;
            }
        }
        
        var result = fileSystem.ListDirectory(path);
        if (!result.IsSuccess)
        {
            output.AppendText($"Error: {result.ErrorMessage}\n");
            return;
        }
        
        fileList = result.Data;
        
        // Filter out only regular files if needed
        // fileList = fileList.Where(f => !f.IsDirectory).ToList();
        
        if (fileList.Count == 0)
        {
            output.AppendText("No files found in the directory\n");
            return;
        }
        
        currentMode = GumMode.File;
        selectedIndex = 0;
        
        DisplayFileList(output);
    }

    private void DisplayChooseOptions(ITerminalOutput output)
    {
        output.Clear();
        
        if (currentTitle != null)
        {
            output.AppendText($"{currentTitle}\n");
        }
        
        if (currentPrompt != null)
        {
            output.AppendText($"{currentPrompt}\n");
        }
        
        for (int i = 0; i < currentOptions.Length; i++)
        {
            if (i == selectedIndex)
            {
                output.SetColor(Color.cyan);
                output.AppendText($"▶ {currentOptions[i]}\n");
                output.SetColor(Color.white);
            }
            else
            {
                output.AppendText($"  {currentOptions[i]}\n");
            }
        }
        
        output.AppendText("\nUse arrow keys ↑/↓ to navigate, Enter to select, Esc to cancel\n");
        isWaitingForInput = true;
    }

    private void DisplayFileList(ITerminalOutput output)
    {
        output.Clear();
        
        if (currentTitle != null)
        {
            output.AppendText($"{currentTitle}\n");
        }
        
        string prompt = currentPrompt ?? "Select a file:";
        output.AppendText($"{prompt}\n\n");
        
        for (int i = 0; i < fileList.Count; i++)
        {
            string prefix = fileList[i].IsDirectory ? "📁 " : "📄 ";
            
            if (i == selectedIndex)
            {
                output.SetColor(Color.cyan);
                output.AppendText($"▶ {prefix}{fileList[i].Name}\n");
                output.SetColor(Color.white);
            }
            else
            {
                output.AppendText($"  {prefix}{fileList[i].Name}\n");
            }
        }
        
        output.AppendText("\nUse arrow keys ↑/↓ to navigate, Enter to select, Esc to cancel\n");
        isWaitingForInput = true;
    }

    public void RequestInput(string prompt, ITerminalOutput output)
    {
        output.AppendText(prompt);
        isWaitingForInput = true;
    }

    public void ProcessInput(string input, ITerminalOutput output)
    {
        // Debug output to troubleshoot input issues
        // output.AppendText($"Debug - Received input: '{input}'\n");
        
        if (currentMode == GumMode.Confirm)
        {
            ProcessConfirmInput(input, output);
        }
        else if (input == "up" || input == "down" || input == "enter" || input == "escape")
        {
            // Handle navigation commands
            ProcessNavigationInput(input, output);
        }
        else
        {
            // For regular text input in other modes
            if (input.ToLower() == "escape")
            {
                isWaitingForInput = false;
                output.Clear();
                output.AppendText("Cancelled\n");
                return;
            }
            
            // Interpret y/n in confirm mode regardless of case
            if (currentMode == GumMode.Confirm)
            {
                ProcessConfirmInput(input, output);
            }
        }
    }

    private void ProcessConfirmInput(string input, ITerminalOutput output)
    {
        isWaitingForInput = false;
        string response = input.Trim().ToLower();
        
        if (response == "y" || response == "yes")
        {
            output.AppendText("yes\n");
        }
        else
        {
            output.AppendText("no\n");
        }
    }

    private void ProcessNavigationInput(string input, ITerminalOutput output)
    {
        switch (input.ToLower())
        {
            case "up":
                selectedIndex = Math.Max(0, selectedIndex - 1);
                break;
                
            case "down":
                // Use the right collection depending on mode
                if (currentMode == GumMode.Choose && currentOptions != null)
                {
                    selectedIndex = Math.Min(currentOptions.Length - 1, selectedIndex + 1);
                }
                else if (currentMode == GumMode.File && fileList != null)
                {
                    selectedIndex = Math.Min(fileList.Count - 1, selectedIndex + 1);
                }
                break;
                
            case "enter":
                isWaitingForInput = false;
                
                if (currentMode == GumMode.Choose && currentOptions != null && currentOptions.Length > 0)
                {
                    output.Clear();
                    output.AppendText($"{currentOptions[selectedIndex]}\n");
                }
                else if (currentMode == GumMode.File && fileList != null && fileList.Count > 0)
                {
                    output.Clear();
                    output.AppendText($"{fileList[selectedIndex].Name}\n");
                }
                return;
                
            case "escape":
                isWaitingForInput = false;
                output.Clear();
                output.AppendText("Cancelled\n");
                return;
        }
        
        // Refresh the display
        if (currentMode == GumMode.Choose)
        {
            DisplayChooseOptions(output);
        }
        else if (currentMode == GumMode.File)
        {
            DisplayFileList(output);
        }
    }

    private string ExtractOption(string[] args, string optionName)
    {
        for (int i = 1; i < args.Length - 1; i++)
        {
            if (args[i] == optionName)
            {
                return args[i + 1];
            }
        }
        return null;
    }
}
