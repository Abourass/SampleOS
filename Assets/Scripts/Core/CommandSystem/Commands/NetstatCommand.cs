using System;
using System.Text;
using UnityEngine;

public class NetstatCommand : ICommand
{
    private VirtualNetwork network;

    public string Name => "netstat";
    public string Description => "Display network connections and device information";
    public string Usage => "netstat [-a|-d]";

    public NetstatCommand(VirtualNetwork network)
    {
        this.network = network;
    }

    public void Execute(string[] args, ITerminalOutput output)
    {
        // Parse options
        bool showAll = false;
        bool showDevices = false;

        if (args.Length > 0)
        {
            foreach (string arg in args)
            {
                if (arg == "-a") showAll = true;
                else if (arg == "-d") showDevices = true;
                else if (arg == "--help" || arg == "-h")
                {
                    DisplayHelp(output);
                    return;
                }
                else
                {
                    output.AppendText($"Unknown option: {arg}\n");
                    DisplayHelp(output);
                    return;
                }
            }
        }
        else
        {
            // Default behavior with no args - show devices
            showDevices = true;
        }

        if (showAll || showDevices)
        {
            DisplayNetworkDevices(output);
        }
    }

    private void DisplayNetworkDevices(ITerminalOutput output)
    {
        // Use original terminal color for headers
        Color defaultColor = Color.white;
        Color headerColor = new Color(0.5f, 0.8f, 1f); // Light blue
        Color hostColor = new Color(0.2f, 1f, 0.2f);   // Light green

        // Display a header
        output.SetColor(headerColor);
        output.AppendText("NETWORK DEVICES\n");
        output.AppendText("===============\n");
        output.SetColor(defaultColor);

        // Get devices from the network
        var devices = network.GetNetworkDevices();

        if (devices.Count == 0)
        {
            output.AppendText("No devices found on the network.\n");
            return;
        }

        // Format and display each device
        StringBuilder table = new StringBuilder();
        
        // Table header
        table.AppendLine("HOST            IP ADDRESS         STATUS    TYPE");
        table.AppendLine("--------------------------------------------------------");

        foreach (var device in devices)
        {
            string hostname = device.Hostname ?? "N/A";
            hostname = hostname.PadRight(15).Substring(0, 15);
            
            string ipAddress = device.IPAddress ?? "N/A";
            ipAddress = ipAddress.PadRight(18).Substring(0, 18);
            
            string status = "online";
            string type = device.Type ?? "unknown";

            table.AppendLine($"{hostname} {ipAddress} {status.PadRight(9)} {type}");
        }

        output.AppendText(table.ToString());
        output.AppendText("\n");
    }

    private void DisplayHelp(ITerminalOutput output)
    {
        output.AppendText($"Usage: {Usage}\n\n");
        output.AppendText("Options:\n");
        output.AppendText("  -a    Show all network information\n");
        output.AppendText("  -d    Show device information (default)\n");
        output.AppendText("  -h    Display this help message\n");
    }
}
