using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.CLI
{
    public class GumCommand : CommandBase, IInteractiveCommand
    {
        private GumMode currentMode;
        private List<string> options = new List<string>();
        private int selectedIndex = 0;
        private string prompt = "";
        private bool waitingForInput = false;
        private string selectedValue = null;

        public override string Name => "gum";
        public override string Description => "Interactive CLI prompts (choose, input, confirm)";
        public override string Usage => "gum choose <option1> <option2> ... | gum input --placeholder \"text\" | gum confirm \"question\"";

        public bool IsWaitingForInput => waitingForInput;

        private enum GumMode
        {
            None,
            Choose,
            Input,
            Confirm
        }

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            if (args.Length == 0)
            {
                WriteError(context, Usage);
                return CommandResult.Error("No mode specified");
            }

            string mode = args[0].ToLower();

            switch (mode)
            {
                case "choose":
                    return ExecuteChoose(args.Skip(1).ToArray(), context);

                case "input":
                    return ExecuteInput(args.Skip(1).ToArray(), context);

                case "confirm":
                    return ExecuteConfirm(args.Skip(1).ToArray(), context);

                default:
                    WriteError(context, $"Unknown gum mode: {mode}");
                    WriteError(context, "Available modes: choose, input, confirm");
                    return CommandResult.Error($"Unknown mode: {mode}");
            }
        }

        private CommandResult ExecuteChoose(string[] args, CommandContext context)
        {
            if (args.Length == 0)
            {
                WriteError(context, "Usage: gum choose <option1> <option2> ...");
                return CommandResult.Error("No options provided");
            }

            currentMode = GumMode.Choose;
            options = new List<string>(args);
            selectedIndex = 0;
            waitingForInput = true;

            DisplayChooseOptions(context);
            return CommandResult.Ok();
        }

        private CommandResult ExecuteInput(string[] args, CommandContext context)
        {
            currentMode = GumMode.Input;
            prompt = "Input: ";

            // Parse placeholder
            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--placeholder" && i + 1 < args.Length)
                {
                    prompt = args[i + 1];
                }
            }

            waitingForInput = true;
            context.Stdout.SetColor(new Color(0.5f, 0.7f, 1f));
            WriteOutput(context, prompt);
            context.Stdout.SetColor(Color.white);

            return CommandResult.Ok();
        }

        private CommandResult ExecuteConfirm(string[] args, CommandContext context)
        {
            if (args.Length == 0)
            {
                WriteError(context, "Usage: gum confirm \"question\"");
                return CommandResult.Error("No question provided");
            }

            currentMode = GumMode.Confirm;
            prompt = string.Join(" ", args);
            options = new List<string> { "Yes", "No" };
            selectedIndex = 1; // Default to "No"
            waitingForInput = true;

            WriteOutput(context, prompt);
            DisplayChooseOptions(context);

            return CommandResult.Ok();
        }

        public void ProcessInput(string input, CommandContext context)
        {
            if (!waitingForInput)
                return;

            switch (currentMode)
            {
                case GumMode.Choose:
                case GumMode.Confirm:
                    ProcessChooseInput(input, context);
                    break;

                case GumMode.Input:
                    ProcessTextInput(input, context);
                    break;
            }
        }

        private void ProcessChooseInput(string input, CommandContext context)
        {
            switch (input.ToLower())
            {
                case "up":
                    selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
                    DisplayChooseOptions(context);
                    break;

                case "down":
                    selectedIndex = (selectedIndex + 1) % options.Count;
                    DisplayChooseOptions(context);
                    break;

                case "enter":
                    selectedValue = options[selectedIndex];
                    WriteOutput(context, "");

                    if (currentMode == GumMode.Confirm)
                    {
                        bool confirmed = selectedValue == "Yes";
                        context.Stdout.SetColor(confirmed ? new Color(0.3f, 1f, 0.3f) : new Color(1f, 0.3f, 0.3f));
                        WriteOutput(context, $"Selected: {selectedValue}");
                        context.Stdout.SetColor(Color.white);

                        // Set exit code based on confirmation
                        context.Environment.Set("GUM_CONFIRM", confirmed ? "1" : "0");
                    }
                    else
                    {
                        context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
                        WriteOutput(context, $"Selected: {selectedValue}");
                        context.Stdout.SetColor(Color.white);
                    }

                    // Store result in environment
                    context.Environment.Set("GUM_CHOICE", selectedValue);

                    waitingForInput = false;
                    currentMode = GumMode.None;
                    break;

                case "escape":
                    WriteOutput(context, "");
                    WriteError(context, "Selection cancelled");
                    waitingForInput = false;
                    currentMode = GumMode.None;
                    break;
            }
        }

        private void ProcessTextInput(string input, CommandContext context)
        {
            if (input.ToLower() == "escape")
            {
                WriteError(context, "Input cancelled");
                waitingForInput = false;
                currentMode = GumMode.None;
                return;
            }

            // Any other input is treated as the text input
            selectedValue = input;
            context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
            WriteOutput(context, $"Input: {selectedValue}");
            context.Stdout.SetColor(Color.white);

            // Store result in environment
            context.Environment.Set("GUM_INPUT", selectedValue);

            waitingForInput = false;
            currentMode = GumMode.None;
        }

        private void DisplayChooseOptions(CommandContext context)
        {
            WriteOutput(context, "");

            for (int i = 0; i < options.Count; i++)
            {
                if (i == selectedIndex)
                {
                    context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f)); // Green
                    WriteOutput(context, $"→ {options[i]}");
                    context.Stdout.SetColor(Color.white);
                }
                else
                {
                    context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f)); // Gray
                    WriteOutput(context, $"  {options[i]}");
                    context.Stdout.SetColor(Color.white);
                }
            }

            WriteOutput(context, "");
            context.Stdout.SetColor(new Color(0.5f, 0.5f, 0.5f));
            WriteOutput(context, "(Use arrow keys to navigate, Enter to select, Escape to cancel)");
            context.Stdout.SetColor(Color.white);
        }
    }
}
