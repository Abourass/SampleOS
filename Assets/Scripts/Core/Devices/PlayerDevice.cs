using System.Collections.Generic;
using SampleOS.Core.Apps;
using UnityEngine;

namespace SampleOS.Core.Devices
{
  /// <summary>
  /// Devices the player owns and can fully interact with (laptop, phone, home PC)
  /// </summary>
  public class PlayerDevice : Device
  {
    public DeviceForm FormFactor { get; private set; }

    // UI State for this device
    public DeviceUIState UIState { get; private set; }

    public enum DeviceForm
    {
      Laptop,
      Desktop,
      Phone,
      Tablet
    }

    public PlayerDevice(string id, string hostname, string ip, DeviceType type, DeviceForm form)
        : base(id, hostname, ip, type)
    {
      FormFactor = form;
      UIState = new DeviceUIState();
      IsPhysicallyAccessible = true;
    }

    public override bool CanInteractDirectly() => true;

    public override List<string> GetAvailableInteractionMethods()
    {
      return new List<string> { "physical", "ssh" };
    }

    // Player devices have full UI
    public void RegisterApp(IInteractiveApp app)
    {
      InteractiveApps.Add(app);
    }
  }

  /// <summary>
  /// Tracks UI state for windowed OS
  /// </summary>
  public class DeviceUIState
  {
    public List<WindowState> OpenWindows { get; private set; } = new List<WindowState>();
    public WindowState FocusedWindow { get; set; }

    public class WindowState
    {
      public string AppId { get; set; }
      public Rect WindowRect { get; set; }
      public bool IsMinimized { get; set; }
      public object AppData { get; set; } // App-specific state
    }
  }
}
