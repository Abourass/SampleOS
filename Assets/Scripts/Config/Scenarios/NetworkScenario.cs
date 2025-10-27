using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Defines a complete network scenario with devices, software, and files
/// </summary>
[CreateAssetMenu(fileName = "NetworkScenario", menuName = "SampleOS/Network Scenario")]
public class NetworkScenario : ScriptableObject
{
  [Header("Scenario Info")]
  public string scenarioId;
  [TextArea(3, 10)]
  public string description;
  public DifficultyLevel difficulty;

  [Header("Network Configuration")]
  public NetworkDefinition network;

  [Header("Devices")]
  public List<ScenarioDeviceConfig> devices = new List<ScenarioDeviceConfig>();

  public enum DifficultyLevel
  {
    Tutorial,
    Easy,
    Medium,
    Hard,
    Expert
  }
}

[System.Serializable]
public class NetworkDefinition
{
  public string networkName;
  public string ipRange = "192.168.1.0/24";
  public string city;
  public NetworkSecurityLevel securityLevel;

  public enum NetworkSecurityLevel
  {
    Open,
    Low,
    Medium,
    High,
    Military
  }
}

[System.Serializable]
public class ScenarioDeviceConfig
{
  [Header("Device Identity")]
  public string deviceName;
  public string ipAddress;
  public DeviceType deviceType;

  [Header("Credentials")]
  public List<CredentialPair> credentials = new List<CredentialPair>();

  [Header("Software & Vulnerabilities")]
  public List<SoftwareInstallation> installedSoftware = new List<SoftwareInstallation>();

  [Header("File System")]
  public FileSystemMode fileSystemMode;
  public List<FileDefinition> customFiles = new List<FileDefinition>();
  public bool allowGeneratedFiles = true;

  [Header("Story Elements")]
  [TextArea(2, 5)]
  public string deviceDescription;
  public List<string> storyTags = new List<string>();
}

[System.Serializable]
public class CredentialPair
{
  public string username;
  public string password;
  public bool isDefaultCredential;
}

[System.Serializable]
public class SoftwareInstallation
{
  public string softwareId; // References SoftwareDatabase
  public string version;
  public List<string> vulnerabilityIds = new List<string>(); // References VulnerabilityDatabase
  public bool isExploitable = true;
}

[System.Serializable]
public class FileDefinition
{
  public string path; // Full path like /home/user/documents/secret.txt
  public FileType type;
  public string content;
  [TextArea(2, 5)]
  public string contentFromTextAsset; // For longer content, reference a .txt file
  public FilePermissions permissions = FilePermissions.ReadWrite;

  [Header("Story Integration")]
  public bool isQuestItem;
  public string questId;
  public List<string> tags = new List<string>();

  public enum FileType
  {
    Text,
    Binary,
    Executable,
    Config,
    Log,
    Email,
    Database
  }

  [System.Flags]
  public enum FilePermissions
  {
    Read = 1,
    Write = 2,
    Execute = 4,
    ReadWrite = Read | Write,
    ReadExecute = Read | Execute,
    All = Read | Write | Execute
  }
}

public enum FileSystemMode
{
  /// <summary>
  /// Generate base OS files, only add custom files from definition
  /// </summary>
  GeneratedWithCustom,

  /// <summary>
  /// Use ONLY the files defined in customFiles list
  /// </summary>
  CustomOnly,

  /// <summary>
  /// Fully generated filesystem with no custom files
  /// </summary>
  FullyGenerated
}
