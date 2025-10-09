using UnityEngine;
using System.Collections.Generic;
using SampleOS.Core.Services;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
  public class QuitCommand : CommandBase, IInteractiveCommand
  {
    private bool waitingForConfirmation = false;
    private List<string> options = new List<string> { "Yes", "No" };
    private int selectedIndex = 0;

    public override string Name => "quit";
    public override string Description => "Exit the game";
    public override string Usage => "quit";

    public bool IsWaitingForInput => waitingForConfirmation;

    public QuitCommand()
    {
      // No dependencies!
    }

    public override CommandResult Execute(string[] args, CommandContext context)
    {
      var gameStateManager = GameStateManager.Instance;

      if (gameStateManager == null)
      {
        WriteError(context, "Cannot quit: GameStateManager not found");
        return CommandResult.Error("Missing GameStateManager");
      }

      // Check if there's unsaved progress
      if (gameStateManager.HasUnsavedProgress())
      {
        waitingForConfirmation = true;
        selectedIndex = 0;
        DisplayConfirmation(context, gameStateManager);
        return CommandResult.Ok();
      }

      QuitGame(context, gameStateManager);
      return CommandResult.Ok();
    }

    public void ProcessInput(string input, CommandContext context)
    {
      if (!waitingForConfirmation)
        return;

      var gameStateManager = GameStateManager.Instance;

      switch (input.ToLower())
      {
        case "up":
          selectedIndex = (selectedIndex - 1 + options.Count) % options.Count;
          DisplayConfirmation(context, gameStateManager);
          break;

        case "down":
          selectedIndex = (selectedIndex + 1) % options.Count;
          DisplayConfirmation(context, gameStateManager);
          break;

        case "enter":
          if (selectedIndex == 0) // Yes
          {
            gameStateManager.SaveGameState();
            WriteOutput(context, "Progress saved.");
            QuitGame(context, gameStateManager);
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

    private void DisplayConfirmation(CommandContext context, GameStateManager gameStateManager)
    {
      // Show how long since last save
      var timeSinceLastSave = gameStateManager.GetTimeSinceLastSave();

      WriteOutput(context, "");
      context.Stdout.SetColor(new Color(1f, 0.7f, 0.2f));
      WriteOutput(context, "⚠ You have unsaved progress!");
      context.Stdout.SetColor(Color.white);

      if (timeSinceLastSave.HasValue)
      {
        WriteOutput(context, $"Last save: {FormatTimeSpan(timeSinceLastSave.Value)} ago");
      }
      else
      {
        WriteOutput(context, "Game has never been saved.");
      }

      WriteOutput(context, "");
      WriteOutput(context, "Save before quitting?");
      WriteOutput(context, "");

      for (int i = 0; i < options.Count; i++)
      {
        if (i == selectedIndex)
        {
          context.Stdout.SetColor(new Color(0.3f, 1f, 0.3f));
          WriteOutput(context, $"  > {options[i]}");
          context.Stdout.SetColor(Color.white);
        }
        else
        {
          context.Stdout.SetColor(new Color(0.5f, 0.5f, 0.5f));
          WriteOutput(context, $"    {options[i]}");
          context.Stdout.SetColor(Color.white);
        }
      }

      WriteOutput(context, "");
      context.Stdout.SetColor(new Color(0.7f, 0.7f, 0.7f));
      WriteOutput(context, "Use ↑↓ to navigate, Enter to select, Escape to cancel");
      context.Stdout.SetColor(Color.white);
    }

    private void QuitGame(CommandContext context, GameStateManager gameStateManager)
    {
      WriteOutput(context, "");
      WriteOutput(context, "Shutting down...");

      // Trigger cleanup events
      GameEvents.Instance.Trigger(GameEventType.GameExiting, null);

      // Small delay for visual feedback
      System.Threading.Tasks.Task.Delay(500).ContinueWith(_ =>
      {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
                Application.Quit();
#endif
      });
    }

    private string FormatTimeSpan(System.TimeSpan span)
    {
      if (span.TotalMinutes < 1)
        return "less than a minute";
      if (span.TotalMinutes < 60)
        return $"{(int)span.TotalMinutes} minute{(span.TotalMinutes >= 2 ? "s" : "")}";
      if (span.TotalHours < 24)
        return $"{(int)span.TotalHours} hour{(span.TotalHours >= 2 ? "s" : "")}";
      return $"{(int)span.TotalDays} day{(span.TotalDays >= 2 ? "s" : "")}";
    }
  }
}
