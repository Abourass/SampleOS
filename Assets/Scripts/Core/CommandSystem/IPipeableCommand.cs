/// <summary>
/// Interface for commands that can accept piped input from another command
/// </summary>
public interface IPipeableCommand : ICommand
{
    /// <summary>
    /// Executes the command with piped input from a previous command
    /// </summary>
    /// <param name="args">Command arguments</param>
    /// <param name="output">Terminal output handler</param>
    /// <param name="inputText">Text received from previous command in pipe</param>
    void ExecuteWithInput(string[] args, ITerminalOutput output, string inputText);
}
