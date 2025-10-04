using UnityEngine;
using System.Collections.Generic;
using SampleOS.Core.Player;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
  public class QuitCommand : CommandBase, IInteractiveCommand
  {
    private PlayerProgressManager progressManager;
    private bool waitingForConfirmation = false;
    private List<string> options = new List<string> { "Yes", "No" };
    private int selectedIndex = 0;

    public override string Name => "quit";
    public override string Description => "Exit the game";
    public override string Usage => "quit";

    public bool IsWaitingForInput => waitingForConfirmation;

    public QuitCommand(PlayerProgressManager progressManager)
    {
      this.progressManager = progressManager;
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      if (progressManager.HasUnsavedProgress())
      {
        waitingForConfirmation = true;
        selectedIndex = 0;
        DisplayConfirmation(context);
        return CommandResult.Ok();
      }

      QuitGame(context);
      return CommandResult.Ok();
    }

    public void ProcessInput(string input, CommandContext context)
    {
      if (!waitingForConfirmation)
        return;

      switch (input.ToLower())
      {
        case "up":
          selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
          DisplayConfirmation(context);
          break;

        case "down":
          selectedIndex = (selectedIndex + 1) % options.Count;
          DisplayConfirmation(context);
          break;

        case "enter":
          if (selectedIndex == 0) // Yes
          {
            progressManager.SaveProgress();
            QuitGame(context);
          }
          else // No
          {
            WriteOutput(context, "Quit cancelled.");
          }
          waitingForConfirmation = false;
          break;

        case "escape":
          WriteOutput(context, "Quit cancelled.");
          waitingForConfirmation = false;
          break;
      }
    }

    private void DisplayConfirmation(CommandContext context)
    {
      WriteOutput(context, "\nYou have unsaved progress. Save before quitting?");

      for (int i = 0; i < options.Count; i++)
      {
        if (i == selectedIndex)
        {
          WriteOutput(context, $"> {options[i]}");
        }
        else
        {
          WriteOutput(context, $"  {options[i]}");
        }
      }

      WriteOutput(context, "\nUse arrow keys to navigate, Enter to select, Escape to cancel.");
    }

    private void QuitGame(CommandContext context)
    {
      WriteOutput(context, "Saving progress and exiting...");
      progressManager.SaveProgress();

#if UNITY_EDITOR
      UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
  }
}
