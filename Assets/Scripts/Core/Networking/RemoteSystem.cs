public class RemoteSystem
{
  public string Name { get; private set; }
  public string IPAddress { get; private set; }
  public string Hostname { get; private set; }
  public string Type { get; private set; }
  public VirtualFileSystem FileSystem { get; private set; }

  private string username;

  public RemoteSystem(string name, string hostname, string ipAddress, string type, string defaultUser)
  {
    Name = name;
    Hostname = hostname;
    IPAddress = ipAddress;
    Type = type;
    username = defaultUser;
    FileSystem = new VirtualFileSystem();

    // Customize file system for this remote machine
    CustomizeFileSystem();
  }

  private void CustomizeFileSystem()
  {
    // Add custom files/directories for this system
  }

  public bool Authenticate(string user, string password)
  {
    // Simple authentication for demo purposes
    return user == username && (password == "password" || string.IsNullOrEmpty(password));
  }
}
