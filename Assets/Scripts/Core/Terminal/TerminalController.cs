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

    private TerminalInputHandler inputHandler;
    private TerminalOutputHandler outputHandler;
    private TerminalHistory history;
    private CommandProcessor commandProcessor;

    private void Awake()
    {
        commandProcessor = new CommandProcessor();
        outputHandler = new TerminalOutputHandler(outputText, scrollRect, promptConfig, commandProcessor);
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
        outputHandler.AppendText("> " + input + "\n");
        commandProcessor.ProcessCommand(input, outputHandler);
        outputHandler.DisplayPrompt(commandProcessor.GetCurrentPath());
        inputHandler.FocusInput();
    }
}
