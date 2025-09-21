using System;
using System.Collections.Generic;
using UnityEngine;

public class NmapCommand : ICommand
{
    private VirtualNetwork network;
    
    public string Name => "nmap";
    public string Description => "Scan for open ports on a remote system";
    public string Usage => "nmap <host>";
    
    public NmapCommand(VirtualNetwork network)
    {
        this.network = network;
    }
    
    public void Execute(string[] args, ITerminalOutput output)
    {
        if (args.Length < 1)
        {
            output.AppendText($"Usage: {Usage}\n");
            return;
        }
        
        string target = args[0];
        output.AppendText($"Scanning {target} for open ports...\n\n");
        
        RemoteSystem system = network.GetSystemByHostname(target);
        if (system == null)
        {
            output.AppendText($"Host {target} not found.\n");
            return;
        }
        
        // Get open ports
        List<int> ports = system.GetOpenPorts();
        
        if (ports.Count == 0)
        {
            output.AppendText("No open ports found.\n");
            return;
        }
        
        output.AppendText("PORT     SERVICE\n");
        output.AppendText("-------------------\n");
        
        foreach (int port in ports)
        {
            Software software = system.GetSoftwareOnPort(port);
            string serviceName = software != null ? software.Name : "unknown";
            
            output.AppendText($"{port}/tcp  {serviceName}\n");
        }
    }
}
