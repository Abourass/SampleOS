// using SampleOS.Core.Devices;
// using SampleOS.Core.Terminal;
// using UnityEngine;

// namespace SampleOS.Core.Apps
// {
//   /// <summary>
//   /// The terminal is now an app that runs on devices
//   /// </summary>
//   public class TerminalApp : IInteractiveApp
//   {
//     public string AppId => "terminal";
//     public string DisplayName => "Terminal";
//     public AppCategory Category => AppCategory.Terminal;

//     private TerminalController controller;
//     private Device hostDevice;

//     public TerminalApp(Device device)
//     {
//       hostDevice = device;
//     }

//     public bool CanRunOnDevice(Device device)
//     {
//       // Terminal runs on everything except maybe IoT devices
//       return device.DeviceType.Category != DeviceCategory.IoTDevice;
//     }

//     public void OnAppOpened()
//     {
//       // Initialize terminal for this device
//       if (controller == null)
//         controller = new TerminalController(hostDevice.FileSystem);
//     }

//     public void OnAppClosed()
//     {
//       // Save history, etc.
//     }

//     public void RenderUI()
//     {
//       // Unity UI rendering
//     }

//     public object SerializeState()
//     {
//       return controller?.SerializeState();
//     }

//     public void DeserializeState(object state)
//     {
//       controller?.DeserializeState(state);
//     }
//   }
// }
