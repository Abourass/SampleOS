using System;
using System.Collections.Generic;

public class PlayerVulnerabilityInventory
{
    private List<VulnerabilityReference> discoveredVulnerabilities = new List<VulnerabilityReference>();
    
    public void AddVulnerability(Vulnerability vuln, string host, int port, string softwareName)
    {
        // Check if already discovered
        foreach (var existingVuln in discoveredVulnerabilities)
        {
            if (existingVuln.Vulnerability.CVE == vuln.CVE && 
                existingVuln.HostIP == host && 
                existingVuln.Port == port)
            {
                return; // Already discovered
            }
        }
        
        // Add to discovered list
        discoveredVulnerabilities.Add(new VulnerabilityReference(vuln, host, port, softwareName));
    }
    
    public List<VulnerabilityReference> GetAllVulnerabilities()
    {
        return new List<VulnerabilityReference>(discoveredVulnerabilities);
    }
    
    public void SaveToLocalFile(VirtualFileSystem fs)
    {
        // Save vulnerability info to a file in home directory
        string content = "VULNERABILITY DATABASE\n=====================\n\n";
        content += "CVE          SEVERITY  TARGET           SOFTWARE        NAME\n";
        content += "------------------------------------------------------------------\n";
        
        foreach (var vuln in discoveredVulnerabilities)
        {
            string target = $"{vuln.HostIP}:{vuln.Port}";
            content += $"{vuln.Vulnerability.CVE,-13}{vuln.Vulnerability.Severity,-10}{target,-17}{vuln.SoftwareName,-15}{vuln.Vulnerability.Name}\n";
        }
        
        fs.CreateFile("/home/user/vulnerabilities.txt", content);
    }
}
