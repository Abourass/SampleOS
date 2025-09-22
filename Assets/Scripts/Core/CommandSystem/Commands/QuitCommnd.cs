using UnityEngine;

public class QuitCommand : ICommand
{
  public string Name => "quit";
  public string Description => "Exit the game with optional saving";
  public string Usage => "quit [options]\n  Options:\n  -s, --save     Save and exit without prompting\n  -n, --no-save  Exit without saving or prompting";

  private PlayerProgressManager progressManager;

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
    output.AppendText("Do you want to save before exiting? (y/n): ");
    // In a real implementation, we'd need to handle this prompt response
    // This would require additional terminal input handling that isn't shown in the provided code

    // For demonstration purposes, we'll just simulate the prompt
    output.AppendText("\nSimulating save prompt (would normally wait for user input)...\n");
    output.AppendText("Exiting game...\n");
    QuitGame();
  }

  private void SaveGame()
  {
    // Save game state using the progress manager
    progressManager.SaveProgress();
  }

  private void QuitGame()
  {
    // Exit the application
#if UNITY_EDITOR
    UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
  }
}
