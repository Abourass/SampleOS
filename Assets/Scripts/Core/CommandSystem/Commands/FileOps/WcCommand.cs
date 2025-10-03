using System.Linq;

namespace SampleOS.Core.CommandSystem.Commands.FileOps
{
    public class WcCommand : CommandBase, IPipeableCommand
    {
        public override string Name => "wc";
        public override string Description => "Count lines, words, and characters";
        public override string Usage => "wc [-l] [-w] [-c] [file...] or ... | wc";

        public bool AcceptsPipedInput => true;

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            bool showLines = false;
            bool showWords = false;
            bool showChars = false;
            var files = new System.Collections.Generic.List<string>();

            // Parse arguments
            foreach (var arg in args)
            {
                if (arg == "-l") showLines = true;
                else if (arg == "-w") showWords = true;
                else if (arg == "-c") showChars = true;
                else if (!arg.StartsWith("-")) files.Add(arg);
            }

            // If no flags specified, show all
            if (!showLines && !showWords && !showChars)
            {
                showLines = showWords = showChars = true;
            }

            // Handle piped input
            if (HasPipedInput(context, out string pipedInput))
            {
                DisplayCounts(pipedInput, null, showLines, showWords, showChars, context);
                return CommandResult.Ok();
            }

            if (files.Count == 0)
            {
                WriteError(context, "Usage: wc [-l] [-w] [-c] [file...] or ... | wc");
                return CommandResult.Error("No input");
            }

            // Process files
            int totalLines = 0, totalWords = 0, totalChars = 0;
            bool hadErrors = false;

            foreach (var filePath in files)
            {
                var node = context.FileSystem.ResolvePath(filePath);

                if (node == null)
                {
                    WriteError(context, $"wc: {filePath}: No such file or directory");
                    hadErrors = true;
                    continue;
                }

                if (node.IsDirectory)
                {
                    WriteError(context, $"wc: {filePath}: Is a directory");
                    hadErrors = true;
                    continue;
                }

                var (lines, words, chars) = DisplayCounts(node.Content, filePath,
                    showLines, showWords, showChars, context);

                totalLines += lines;
                totalWords += words;
                totalChars += chars;
            }

            // Show totals if multiple files
            if (files.Count > 1)
            {
                DisplayCountLine(totalLines, totalWords, totalChars, "total",
                    showLines, showWords, showChars, context);
            }

            return hadErrors ? CommandResult.Error("Some files could not be read", 1) : CommandResult.Ok();
        }

        private (int lines, int words, int chars) DisplayCounts(
            string content, string fileName, bool showLines, bool showWords, bool showChars,
            CommandContext context)
        {
            int lines = content.Split('\n').Length;
            int words = content.Split(new[] { ' ', '\t', '\n' },
                System.StringSplitOptions.RemoveEmptyEntries).Length;
            int chars = content.Length;

            DisplayCountLine(lines, words, chars, fileName, showLines, showWords, showChars, context);

            return (lines, words, chars);
        }

        private void DisplayCountLine(
            int lines, int words, int chars, string label,
            bool showLines, bool showWords, bool showChars,
            CommandContext context)
        {
            var parts = new System.Collections.Generic.List<string>();

            if (showLines) parts.Add(lines.ToString().PadLeft(8));
            if (showWords) parts.Add(words.ToString().PadLeft(8));
            if (showChars) parts.Add(chars.ToString().PadLeft(8));

            string output = string.Join("", parts);
            if (!string.IsNullOrEmpty(label))
                output += " " + label;

            WriteOutput(context, output);
        }
    }
}
