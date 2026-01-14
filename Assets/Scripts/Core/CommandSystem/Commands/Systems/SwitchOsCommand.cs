using System;
using System.Collections.Generic;
using System.Linq;
using SampleOS.Core.Devices;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
    /// <summary>
    /// Command to switch the operating system theme of the terminal.
    /// This is primarily for testing different OS-specific UI prefabs.
    /// Usage: switchos [linux|windows|mac]
    /// </summary>
    public class SwitchOsCommand : CommandBase
    {
        private static readonly Dictionary<string, string> OsThemes = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
        {
            { "linux", "Linux" },
            { "windows", "Windows" },
            { "win", "Windows" },
            { "mac", "Mac" },
            { "macos", "Mac" }
        };

        public override string Name => "switchos";
        public override string Description => "Switch the terminal OS theme (for testing different UI prefabs)";
        public override string Usage => "switchos [linux|windows|mac]";

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            // If no arguments, show current OS and available options
            if (args.Length == 0)
            {
                var currentOs = context.CurrentDevice?.OS?.Name ?? "Unknown";
                WriteOutput(context, $"Current OS: {currentOs}");
                WriteOutput(context, "");
                WriteOutput(context, "Available OS themes:");
                WriteOutput(context, "  linux   - Linux terminal theme");
                WriteOutput(context, "  windows - Windows terminal theme");
                WriteOutput(context, "  mac     - Mac terminal theme");
                WriteOutput(context, "");
                WriteOutput(context, "Usage: switchos [linux|windows|mac]");
                return CommandResult.Ok();
            }

            var requestedOs = args[0].ToLower();

            // Validate the OS theme
            if (!OsThemes.TryGetValue(requestedOs, out var osName))
            {
                WriteError(context, $"Unknown OS theme: {requestedOs}");
                WriteOutput(context, "Available themes: linux, windows, mac");
                return CommandResult.Error($"Unknown OS theme: {requestedOs}");
            }

            // Check if device is available
            if (context.CurrentDevice == null)
            {
                WriteError(context, "No device context available");
                return CommandResult.Error("No device context");
            }

            // Store old OS name
            var oldOsName = context.CurrentDevice.OS?.Name ?? "Unknown";

            // Update the device's OS
            if (context.CurrentDevice.OS == null)
            {
                context.CurrentDevice.OS = new OSInfo(osName, "1.0");
            }
            else
            {
                context.CurrentDevice.OS.Name = osName;
            }

            WriteOutput(context, $"Switched OS theme: {oldOsName} -> {osName}");
            WriteOutput(context, "");
            WriteOutput(context, "Note: Close and reopen the terminal to see the new theme.");
            WriteOutput(context, "You can use the 'quit' command to close the current terminal.");

            return CommandResult.Ok();
        }
    }
}
