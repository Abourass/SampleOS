using SampleOS.Core.Devices;

namespace SampleOS.Core.Session
{
  public class RemoteConnection
  {
    public Device SourceDevice { get; private set; }
    public Device TargetDevice { get; private set; }
    public string Username { get; private set; }
    public string ConnectionType { get; private set; } // "ssh", "rdp", etc.
    public System.DateTime ConnectedAt { get; private set; }

    public RemoteConnection(Device source, Device target, string user)
    {
      SourceDevice = source;
      TargetDevice = target;
      Username = user;
      ConnectedAt = System.DateTime.Now;
    }
  }
}
