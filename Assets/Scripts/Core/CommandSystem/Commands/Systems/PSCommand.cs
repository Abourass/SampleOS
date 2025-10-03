using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace SampleOS.Core.CommandSystem.Commands.Systems
{
    public class PsCommand : CommandBase
    {
        private CommandProcessor processor;
        private List<ProcessInfo> systemProcesses;
        private int nextPid = 100;

        public override string Name => "ps";
        public override string Description => "Display running processes";
        public override string Usage => "ps [options]\nOptions:\n  -a    Show all processes\n  -u    Show user-oriented format\n  -x    Show processes without controlling terminals\n  aux   Show all processes in user-oriented format";

        public PsCommand(CommandProcessor processor)
        {
            this.processor = processor;
            InitializeSystemProcesses();
        }

        private void InitializeSystemProcesses()
        {
            systemProcesses = new List<ProcessInfo>
            {
                new ProcessInfo { PID = 1, User = "root", CPU = 0.0f, Memory = 0.1f, VSZ = 169848, RSS = 13648, TTY = "?", Stat = "Ss", Start = "00:00", Time = "0:01", Command = "/sbin/init" },
                new ProcessInfo { PID = 2, User = "root", CPU = 0.0f, Memory = 0.0f, VSZ = 0, RSS = 0, TTY = "?", Stat = "S", Start = "00:00", Time = "0:00", Command = "[kthreadd]" },
                new ProcessInfo { PID = 3, User = "root", CPU = 0.0f, Memory = 0.0f, VSZ = 0, RSS = 0, TTY = "?", Stat = "I<", Start = "00:00", Time = "0:00", Command = "[rcu_gp]" },
                new ProcessInfo { PID = 4, User = "root", CPU = 0.0f, Memory = 0.0f, VSZ = 0, RSS = 0, TTY = "?", Stat = "I<", Start = "00:00", Time = "0:00", Command = "[rcu_par_gp]" },
                new ProcessInfo { PID = 5, User = "root", CPU = 0.0f, Memory = 0.0f, VSZ = 0, RSS = 0, TTY = "?", Stat = "I", Start = "00:00", Time = "0:00", Command = "[kworker/0:0]" },
                new ProcessInfo { PID = 50, User = "root", CPU = 0.1f, Memory = 0.2f, VSZ = 128000, RSS = 8192, TTY = "?", Stat = "Ss", Start = "00:00", Time = "0:02", Command = "/usr/sbin/sshd -D" },
                new ProcessInfo { PID = 75, User = "root", CPU = 0.0f, Memory = 0.3f, VSZ = 256000, RSS = 12288, TTY = "?", Stat = "Ssl", Start = "00:00", Time = "0:01", Command = "/usr/sbin/networkd" },
                new ProcessInfo { PID = 100, User = "user", CPU = 0.5f, Memory = 1.2f, VSZ = 512000, RSS = 49152, TTY = "pts/0", Stat = "Ss", Start = "00:05", Time = "0:03", Command = "/usr/bin/terminal" }
            };
        }

        public override CommandResult Execute(string[] args, CommandContext context)
        {
            // Parse options
            bool showAll = false;
            bool userFormat = false;
            bool showNoTTY = false;

            foreach (var arg in args)
            {
                if (arg == "-a")
                    showAll = true;
                else if (arg == "-u")
                    userFormat = true;
                else if (arg == "-x")
                    showNoTTY = true;
                else if (arg == "aux" || arg == "-aux")
                {
                    showAll = true;
                    userFormat = true;
                    showNoTTY = true;
                }
                else
                {
                    WriteError(context, $"ps: unknown option: {arg}");
                    WriteError(context, Usage);
                    return CommandResult.Error($"Unknown option: {arg}");
                }
            }

            // Get processes to display
            var processes = GetProcessList(showAll, showNoTTY);

            // Display based on format
            if (userFormat)
            {
                DisplayUserFormat(processes, context);
            }
            else
            {
                DisplayStandardFormat(processes, context);
            }

            return CommandResult.Ok();
        }

        private List<ProcessInfo> GetProcessList(bool showAll, bool showNoTTY)
        {
            var processes = new List<ProcessInfo>(systemProcesses);

            // Add current command processor state
            if (processor.IsExecuting)
            {
                processes.Add(new ProcessInfo
                {
                    PID = nextPid++,
                    User = "user",
                    CPU = 5.2f,
                    Memory = 0.8f,
                    VSZ = 128000,
                    RSS = 32768,
                    TTY = "pts/0",
                    Stat = "R+",
                    Start = DateTime.Now.ToString("HH:mm"),
                    Time = "0:00",
                    Command = "<running command>"
                });
            }

            // Add ps itself
            processes.Add(new ProcessInfo
            {
                PID = nextPid++,
                User = "user",
                CPU = 0.0f,
                Memory = 0.1f,
                VSZ = 64000,
                RSS = 4096,
                TTY = "pts/0",
                Stat = "R+",
                Start = DateTime.Now.ToString("HH:mm"),
                Time = "0:00",
                Command = "ps"
            });

            // Filter based on options
            if (!showAll)
            {
                // Only show user's processes
                processes = processes.Where(p => p.User == "user").ToList();
            }

            if (!showNoTTY)
            {
                // Only show processes with a TTY
                processes = processes.Where(p => p.TTY != "?").ToList();
            }

            return processes;
        }

        private void DisplayStandardFormat(List<ProcessInfo> processes, CommandContext context)
        {
            // Header
            WriteOutput(context, "  PID TTY          TIME CMD");

            // Process lines
            foreach (var proc in processes)
            {
                string line = string.Format("{0,5} {1,-12} {2,8} {3}",
                    proc.PID,
                    proc.TTY,
                    proc.Time,
                    proc.Command);
                WriteOutput(context, line);
            }
        }

        private void DisplayUserFormat(List<ProcessInfo> processes, CommandContext context)
        {
            // Header
            WriteOutput(context, "USER       PID %CPU %MEM    VSZ   RSS TTY      STAT START   TIME COMMAND");

            // Process lines
            foreach (var proc in processes)
            {
                string line = string.Format("{0,-10} {1,5} {2,4:F1} {3,4:F1} {4,6} {5,5} {6,-8} {7,-4} {8,5} {9,6} {10}",
                    proc.User,
                    proc.PID,
                    proc.CPU,
                    proc.Memory,
                    proc.VSZ,
                    proc.RSS,
                    proc.TTY,
                    proc.Stat,
                    proc.Start,
                    proc.Time,
                    proc.Command);
                WriteOutput(context, line);
            }
        }

        private class ProcessInfo
        {
            public int PID { get; set; }
            public string User { get; set; }
            public float CPU { get; set; }
            public float Memory { get; set; }
            public int VSZ { get; set; }  // Virtual memory size
            public int RSS { get; set; }  // Resident set size
            public string TTY { get; set; }
            public string Stat { get; set; }  // Process state
            public string Start { get; set; }
            public string Time { get; set; }
            public string Command { get; set; }
        }
    }
}
