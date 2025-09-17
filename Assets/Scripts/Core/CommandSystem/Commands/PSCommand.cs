using System;
using UnityEngine;

public class PsCommand : ICommand
{
    private CommandProcessor commandProcessor;
    
    public string Name => "ps";
    public string Description => "List running processes on the system";
    public string Usage => "ps [aux]";
    
    public PsCommand(CommandProcessor commandProcessor)
    {
        this.commandProcessor = commandProcessor;
    }
    
    public void Execute(string[] args, ITerminalOutput output)
    {
        bool detailed = args.Length > 0 && args[0] == "aux";
        
        RemoteSystem currentSystem = commandProcessor.GetCurrentSystem();
        
        output.AppendText("PID   USER     %CPU %MEM  COMMAND\n");
        output.AppendText("----------------------------------------\n");
        
        int pid = 1;
        
        // System processes
        output.AppendText($"{pid,-6}root     0.0  0.1  /sbin/init\n");
        pid++;
        output.AppendText($"{pid,-6}root     0.0  0.2  /usr/sbin/sshd\n");
        pid++;
        
        // Software processes
        foreach (var software in currentSystem.InstalledSoftware)
        {
            if (software.IsRunning)
            {
                string user = software.Category == "service" ? "root" : currentSystem.Username;
                float cpu = UnityEngine.Random.Range(0.1f, 5.0f);
                float mem = UnityEngine.Random.Range(0.1f, 8.0f);
                
                output.AppendText($"{pid,-6}{user,-8}{cpu:F1}  {mem:F1}  {software.InstallPath}\n");
                pid++;
                
                // For services with child processes
                if (detailed && software.Category == "service" && software.ListeningPorts.Count > 0)
                {
                    foreach (int port in software.ListeningPorts)
                    {
                        cpu = UnityEngine.Random.Range(0.1f, 2.0f);
                        mem = UnityEngine.Random.Range(0.1f, 4.0f);
                        output.AppendText($"{pid,-6}{user,-8}{cpu:F1}  {mem:F1}  {software.InstallPath} --port={port}\n");
                        pid++;
                    }
                }
            }
        }
    }
}
