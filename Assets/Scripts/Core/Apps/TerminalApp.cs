using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using SampleOS.Core.CommandSystem;
using SampleOS.Core.Devices;
using UnityEngine;

namespace SampleOS.Core.Apps
{
  public class TerminalApp : IInteractiveApp
  {
    // IInteractiveApp implementation
    public string InstanceId { get; private set; }
    public string AppId => "terminal";
    public string DisplayName => "Terminal";
    public AppCategory Category => AppCategory.Terminal;
    public Device HostDevice { get; private set; }
    
    // Terminal-specific state
    private CommandProcessor commandProcessor;
    private CommandContext context;
    private TerminalConfig config;
    private List<string> commandHistory;
    
    public TerminalApp()
    {
      InstanceId = Guid.NewGuid().ToString();
      commandHistory = new List<string>();
    }
    
    public void Initialize(Device hostDevice)
    {
      HostDevice = hostDevice;
      
      // Create device-specific command context
      context = new CommandContext
      {
        CurrentDevice = hostDevice,
        WorkingDirectory = hostDevice.FileSystem.GetNode("/home/user"),
        CurrentUser = hostDevice.OS?.CurrentUser ?? "user",
        SourceApp = this
      };
      
      // Create isolated command processor for this terminal
      commandProcessor = new CommandProcessor();
      commandProcessor.Initialize();
      
      // Load device-specific terminal config
      config = LoadTerminalConfig(hostDevice);
      
      Debug.Log($"[TerminalApp] Initialized on {hostDevice.Hostname} (Instance: {InstanceId})");
    }
    
    public void Update(float deltaTime)
    {
      // Terminal doesn't need per-frame updates currently
      // But this hook exists for future needs (animations, etc.)
    }
    
    public void Shutdown()
    {
      Debug.Log($"[TerminalApp] Shutting down instance {InstanceId} on {HostDevice.Hostname}");
      commandProcessor = null;
      context = null;
    }
    
    // Terminal-specific API
    public async Task<CommandResult> ExecuteCommandAsync(string input)
    {
      if (string.IsNullOrWhiteSpace(input))
        return CommandResult.Empty();
      
      commandHistory.Add(input);
      
      // Update context before executing
      context.CommandHistory = commandHistory;
      
      return await commandProcessor.ExecuteAsync(input, context);
    }
    
    public TerminalConfig GetConfig() => config;
    public CommandContext GetContext() => context;
    public List<string> GetCommandHistory() => commandHistory;
    
    private TerminalConfig LoadTerminalConfig(Device device)
    {
      // Try to load device-specific config
      // Fall back to OS-specific defaults
      // Fall back to global defaults
      
      // TODO: Implement config loading from device
      // For now, create OS-appropriate default
      return TerminalConfig.CreateDefault(device.OS?.Name ?? "Linux");
    }
    
    // IInteractiveApp UI hooks
    public void OnAppOpened()
    {
      Debug.Log($"[TerminalApp] Opened on {HostDevice.Hostname}");
    }
    
    public void OnAppClosed()
    {
      Debug.Log($"[TerminalApp] Closed on {HostDevice.Hostname}");
    }
    
    public void OnFocusGained()
    {
      // Could play focus sound, update UI, etc.
    }
    
    public void OnFocusLost()
    {
      // Could dim UI, etc.
    }
    
    public bool CanRunOnDevice(Device device)
    {
      // Terminals can run on any device with a filesystem
      return device?.FileSystem != null;
    }
    
    // State serialization
    public object SerializeState()
    {
      return new TerminalState
      {
        instanceId = InstanceId,
        deviceHostname = HostDevice.Hostname,
        workingDirectory = context.WorkingDirectory.FullPath,
        commandHistory = new List<string>(commandHistory),
        currentUser = context.CurrentUser
      };
    }
    
    public void DeserializeState(object state)
    {
      if (state is TerminalState termState)
      {
        commandHistory = termState.commandHistory;
        context.WorkingDirectory = HostDevice.FileSystem.GetNode(termState.workingDirectory);
        context.CurrentUser = termState.currentUser;
        
        Debug.Log($"[TerminalApp] Restored state for {InstanceId}");
      }
    }
    
    [Serializable]
    private class TerminalState
    {
      public string instanceId;
      public string deviceHostname;
      public string workingDirectory;
      public List<string> commandHistory;
      public string currentUser;
    }
  }
}
