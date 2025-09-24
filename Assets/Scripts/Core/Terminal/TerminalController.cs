using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class TerminalController : MonoBehaviour
{
    [SerializeField] private TMP_Text outputText;
    [SerializeField] private TMP_InputField inputField;
    [SerializeField] private ScrollRect scrollRect;
    [SerializeField] private TerminalConfig config;
    [SerializeField] private PromptConfig promptConfig;
    [SerializeField] private TerminalInputHandler inputHandler;  // Now serialized

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset nerdFontAsset;

    private TerminalOutputHandler outputHandler;
    private TerminalHistory history;
    private CommandProcessor commandProcessor;

    private void Awake()
    {
        // Apply the Nerd Font to text components
        if (nerdFontAsset != null)
        {
            outputText.font = nerdFontAsset;

            // Also apply to the input field text
            if (inputField.textComponent != null)
            {
                inputField.textComponent.font = nerdFontAsset;
            }
        }

        commandProcessor = new CommandProcessor();
        outputHandler = new TerminalOutputHandler(outputText, scrollRect, promptConfig, commandProcessor, this);
        history = new TerminalHistory();
        
        // Initialize the input handler with the required references
        if (inputHandler == null)
        {
            Debug.LogError("TerminalInputHandler reference is missing!");
            return;
        }
        
        inputHandler.Initialize(history, ProcessCommand, commandProcessor);
    }

    private void Start()
    {
        InitializeTerminal();
    }

    private void InitializeTerminal()
    {
        outputHandler.DisplayWelcomeMessage(config.welcomeMessage);
        outputHandler.DisplayPrompt(commandProcessor.GetCurrentPath());
        inputHandler.FocusInput();
    }

    private void ProcessCommand(string input)
    {
        // Don't echo navigation commands from interactive mode
        if (!commandProcessor.IsWaitingForCommandInput || 
            (input != "up" && input != "down" && input != "enter" && input != "escape"))
        {
            outputHandler.AppendText(input + "\n");
        }
        
        commandProcessor.ProcessCommand(input, outputHandler);
        
        // Only display the standard prompt if we're not waiting for command input
        if (!commandProcessor.IsWaitingForCommandInput)
        {
            outputHandler.DisplayPrompt(commandProcessor.GetCurrentPath());
        }
        
        inputHandler.FocusInput();
    }
}
