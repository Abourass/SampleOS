using System.Collections.Generic;

public enum DeviceCategory
{
    Workstation,
    Server,
    Router,
    IoTDevice,
    MobileDevice,
    EmbeddedSystem,
    IndustrialControl
}

public class DeviceType
{
    public DeviceCategory Category { get; private set; }
    public string Name { get; private set; }
    public Dictionary<string, float> SoftwareWeights { get; private set; }
    
    // Dictionary that maps software categories to likely software for this device
    // The float value represents the probability of this software being installed
    
    public DeviceType(DeviceCategory category, string name, Dictionary<string, float> softwareWeights)
    {
        Category = category;
        Name = name;
        SoftwareWeights = softwareWeights;
    }
}
