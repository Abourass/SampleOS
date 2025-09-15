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
        outputHandler = new TerminalOutputHandler(outputText, scrollRect, promptConfig, commandProcessor);
        history = new TerminalHistory();
        inputHandler = new TerminalInputHandler(inputField, history, ProcessCommand);
    }

    private void VerifyFontCharacters()
    {
        if (outputText != null)
        {
            string test = "\nTesting font symbols:\n" +
                          "Powerline separators: \uE0B0 \uE0B1 \uE0B2 \uE0B3\n" +
                          "Icons: \uF007 \uF00C \uF00D \uF017 \uF07B\n" +
                          "Basic Latin: ABCDEFG` abcdefg 1234567890" +
                          "\nTesting rich text:\n" +
                    "<color=#FF0000>Red Text</color> " +
                    "<font=\"FiraCodeRetina SDF\"><mark=#00FF00 padding=\"4,4,2,2\"><color=#FF0000>Green Background</color></mark></font> " +
                    "<mark=#FFFF00><color=#0000FF>Blue Text on Yellow</color></mark> " +
                    $"Icon test: {PowerlineBadge.ICON_FOLDER} {PowerlineBadge.ICON_USER}";

            outputText.text += test;

            string iconTest = "\nIcon rendering tests:\n" +
                  $"1. Plain icon: {PowerlineBadge.ICON_FOLDER}\n" +
                  $"2. Icon in font tag: <font=\"{PowerlineBadge.FONT_NAME}\">{PowerlineBadge.ICON_FOLDER}</font>\n" +
                  $"3. Icon in color tag: <color=#FF0000>{PowerlineBadge.ICON_FOLDER}</color>\n" +
                  $"4. Icon in font+color: <font=\"{PowerlineBadge.FONT_NAME}\"><color=#FF0000>{PowerlineBadge.ICON_FOLDER}</color></font>\n" +
                  $"5. Icon in mark: <mark=#00FF00>{PowerlineBadge.ICON_FOLDER}</mark>\n"
                + $"6. Icon in font+mark: <font=\"{PowerlineBadge.FONT_NAME}\"><mark=#00FF00>{PowerlineBadge.ICON_FOLDER}</mark></font>\n"
                + $"7. Icon in font+color+mark: <font=\"{PowerlineBadge.FONT_NAME}\"><mark=#00FF00><color=#FF0000>{PowerlineBadge.ICON_FOLDER}</color></mark></font>\n";

            outputText.text += iconTest;
        }
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
        // VerifyFontCharacters();
    }

    private void ProcessCommand(string input)
    {
        outputHandler.AppendText("> " + input + "\n");
        commandProcessor.ProcessCommand(input, outputHandler);
        outputHandler.DisplayPrompt(commandProcessor.GetCurrentPath());
        inputHandler.FocusInput();
    }
}
