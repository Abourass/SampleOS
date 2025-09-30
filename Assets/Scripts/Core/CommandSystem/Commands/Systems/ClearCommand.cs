using SampleOS.Core.CommandSystem;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{

  public class ClearCommand : ICommand
  {
    public string Name => "clear";
    public string Description => "Clear the terminal screen";
    public string Usage => "clear";

    public ClearCommand()
    {
    }

    public void Execute(string[] args, ITerminalOutput output)
    {
      output.Clear();
    }
  }
}
