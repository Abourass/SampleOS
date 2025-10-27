using System.Collections.Generic;

namespace SampleOS.Core.Devices
{
  /// <summary>
  /// Remote network devices (servers, workstations, IoT devices)
  /// May or may not be physically accessible
  /// </summary>
  public class RemoteDevice : Device
  {
    public RemoteAccessType AccessType { get; private set; }

    public enum RemoteAccessType
    {
      SSHOnly,        // Terminal only
      RDPCapable,     // Can show limited GUI
      WebInterface,   // Has web admin panel
      APIOnly,        // REST/API interface only
      Physical        // Must be at location to use
    }

    public RemoteDevice(string id, string hostname, string ip, DeviceType type, RemoteAccessType access)
        : base(id, hostname, ip, type)
    {
      AccessType = access;
    }

    public override bool CanInteractDirectly() => IsPhysicallyAccessible;

    public override List<string> GetAvailableInteractionMethods()
    {
      var methods = new List<string>();

      if (IsPhysicallyAccessible)
        methods.Add("physical");

      switch (AccessType)
      {
        case RemoteAccessType.SSHOnly:
          methods.Add("ssh");
          break;
        case RemoteAccessType.RDPCapable:
          methods.Add("ssh");
          methods.Add("rdp");
          break;
        case RemoteAccessType.WebInterface:
          methods.Add("http");
          methods.Add("https");
          break;
        case RemoteAccessType.APIOnly:
          methods.Add("api");
          break;
      }

      return methods;
    }
  }
}
