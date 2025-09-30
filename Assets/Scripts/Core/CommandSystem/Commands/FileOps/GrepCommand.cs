using System.Linq;
using System.Text.RegularExpressions;

namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
    public class GrepCommand : CommandBase, IPipeableCommand
    {
        public override string Name => "grep";
        public override string Description => "Search for patterns in files";
        public override string Usage => "grep [pattern] [file...] or ... | grep [pattern]";
        
        public bool AcceptsPipedInput => true;

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            if (args.Length == 0)
            {
                WriteError(context, "Usage: grep [pattern] [file...] or ... | grep [pattern]");
                return CommandResult.Error("Missing pattern");
            }

            string pattern = args[0];

            // Handle piped input
            if (HasPipedInput(context, out string pipedInput))
            {
                return SearchInText(pattern, pipedInput, context);
            }

            // Search in files
            if (args.Length == 1)
            {
                WriteError(context, "No files specified and no piped input");
                return CommandResult.Error("No input");
            }

            return SearchInFiles(pattern, args.Skip(1).ToArray(), context);
        }

        private CommandResult SearchInText(string pattern, string text, CommandContext context)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                int matchCount = 0;

                foreach (var line in text.Split('\n'))
                {
                    if (context.CancellationToken.IsCancellationRequested)
                        return CommandResult.Error("Cancelled");

                    if (regex.IsMatch(line))
                    {
                        WriteOutput(context, line);
                        matchCount++;
                    }
                }

                return matchCount > 0 
                    ? CommandResult.Ok() 
                    : CommandResult.Error("No matches", 1);
            }
            catch (System.Exception ex)
            {
                WriteError(context, $"Regex error: {ex.Message}");
                return CommandResult.FromException(ex);
            }
        }

        private CommandResult SearchInFiles(string pattern, string[] files, CommandContext context)
        {
            try
            {
                var regex = new Regex(pattern, RegexOptions.IgnoreCase);
                int totalMatches = 0;

                foreach (var file in files)
                {
                    var node = context.FileSystem.ResolvePath(file);
                    if (node == null)
                    {
                        WriteError(context, $"File not found: {file}");
                        continue;
                    }

                    if (node.IsDirectory)
                    {
                        WriteError(context, $"Is a directory: {file}");
                        continue;
                    }

                    foreach (var line in node.Content.Split('\n'))
                    {
                        if (regex.IsMatch(line))
                        {
                            WriteOutput(context, files.Length > 1 ? $"{file}: {line}" : line);
                            totalMatches++;
                        }
                    }
                }

                return totalMatches > 0 
                    ? CommandResult.Ok() 
                    : CommandResult.Error("No matches", 1);
            }
            catch (System.Exception ex)
            {
                WriteError(context, $"Error: {ex.Message}");
                return CommandResult.FromException(ex);
            }
        }
    }
}
