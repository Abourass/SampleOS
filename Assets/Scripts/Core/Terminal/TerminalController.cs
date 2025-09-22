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

    [Header("Font Settings")]
    [SerializeField] private TMP_FontAsset nerdFontAsset;

    private TerminalInputHandler inputHandler;
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
        inputHandler = new TerminalInputHandler(inputField, history, ProcessCommand);
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
        outputHandler.AppendText(input + "\n");
        commandProcessor.ProcessCommand(input, outputHandler);
        // Only display the standard prompt if we're not waiting for command input
        if (!commandProcessor.IsWaitingForCommandInput)
        {
            outputHandler.DisplayPrompt(commandProcessor.GetCurrentPath());
        }
        inputHandler.FocusInput();
    }
}
