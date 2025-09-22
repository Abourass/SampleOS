using UnityEngine;

public class QuitCommand : IInteractiveCommand
{
  public string Name => "quit";
  public string Description => "Exit the game with optional saving";
  public string Usage => "quit [options]\n  Options:\n  -s, --save     Save and exit without prompting\n  -n, --no-save  Exit without saving or prompting";

  private PlayerProgressManager progressManager;
  private bool isWaitingForInput;

  public bool IsWaitingForInput => isWaitingForInput;

  public QuitCommand(PlayerProgressManager progressManager)
  {
    this.progressManager = progressManager;
  }

  public void Execute(string[] args, ITerminalOutput output)
  {
    if (args.Length > 0)
    {
      string flag = args[0].ToLower();

      if (flag == "-s" || flag == "--save")
      {
        // Save and exit without prompting
        output.AppendText("Saving game...\n");
        SaveGame();
        output.AppendText("Game saved. Exiting game...\n");
        QuitGame();
        return;
      }

      if (flag == "-n" || flag == "--no-save")
      {
        // Exit without saving or prompting
        output.AppendText("Exiting game without saving...\n");
        QuitGame();
        return;
      }
    }

    // Default behavior - prompt the user
    RequestInput("Do you want to save before exiting? (y/n): ", output);
  }

  public void RequestInput(string prompt, ITerminalOutput output)
  {
    output.AppendText(prompt);
    isWaitingForInput = true;
  }

  public void ProcessInput(string input, ITerminalOutput output)
  {
    isWaitingForInput = false;

    string response = input.Trim().ToLower();
    if (response == "y" || response == "yes")
    {
      output.AppendText("Saving game...\n");
      SaveGame();
      output.AppendText("Game saved. Exiting game...\n");
    }
    else if (response == "n" || response == "no")
    {
      output.AppendText("Exiting game without saving...\n");
    }
    else
    {
      output.AppendText("Invalid response. Exiting game without saving...\n");
    }

    QuitGame();
  }

  private void SaveGame()
  {
    progressManager.SaveProgress();
  }

  private void QuitGame()
  {
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
  }
}
