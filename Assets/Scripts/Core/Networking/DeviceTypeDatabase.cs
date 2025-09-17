using System.Collections.Generic;

public class DeviceTypeDatabase
{
    private Dictionary<string, DeviceType> deviceTypes = new Dictionary<string, DeviceType>();
    
    public DeviceTypeDatabase()
    {
        // Initialize with common device types
        
        // Web server
        deviceTypes.Add("server", new DeviceType(
            DeviceCategory.Server,
            "Web Server",
            new Dictionary<string, float> {
                { "webserver", 0.95f },
                { "database", 0.6f },
                { "cms", 0.4f },
                { "firewall", 0.8f }
            }
        ));
        
        // Desktop workstation
        deviceTypes.Add("desktop", new DeviceType(
            DeviceCategory.Workstation,
            "Desktop Workstation",
            new Dictionary<string, float> {
                { "office", 0.8f },
                { "browser", 0.9f },
                { "fileserver", 0.3f },
                { "development", 0.5f }
            }
        ));
        
        // Network storage
        deviceTypes.Add("storage", new DeviceType(
            DeviceCategory.Server,
            "Network Storage",
            new Dictionary<string, float> {
                { "fileserver", 0.95f },
                { "backup", 0.8f },
                { "database", 0.4f },
                { "firewall", 0.7f }
            }
        ));
        
        // Embedded system
        deviceTypes.Add("embedded", new DeviceType(
            DeviceCategory.EmbeddedSystem,
            "Embedded Device",
            new Dictionary<string, float> {
                { "webserver", 0.7f },
                { "database", 0.2f },
                { "iot", 0.9f },
                { "firewall", 0.5f }
            }
        ));
        
        // Router
        deviceTypes.Add("router", new DeviceType(
            DeviceCategory.Router,
            "Network Router",
            new Dictionary<string, float> {
                { "webserver", 0.8f }, // Admin interface
                { "firewall", 0.95f },
                { "dns", 0.7f },
                { "dhcp", 0.8f }
            }
        ));
    }
    
    public DeviceType GetDeviceType(string typeName)
    {
        if (deviceTypes.TryGetValue(typeName.ToLower(), out DeviceType deviceType))
            return deviceType;
            
        // Return default type if not found
        return new DeviceType(
            DeviceCategory.Server,
            "Generic Server",
            new Dictionary<string, float> {
                { "webserver", 0.8f },
                { "database", 0.5f },
                { "firewall", 0.6f }
            }
        );
    }
}
