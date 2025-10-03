using NUnit.Framework;
using System;
using System.Linq;
using SampleOS.Core.SoftwarePackages;

namespace SampleOS.Tests.SoftwarePackages
{
  [TestFixture]
  public class SoftwareTests
  {
    #region Constructor Tests

    [Test]
    public void Constructor_ValidParameters_CreatesSoftware()
    {
      // Arrange
      var name = "Apache";
      var version = "2.4.41";
      var category = "webserver";
      var releaseDate = new DateTime(2020, 1, 15);

      // Act
      var software = new Software(name, version, category, releaseDate);

      // Assert
      Assert.AreEqual(name, software.Name);
      Assert.AreEqual(version, software.Version.ToString());
      Assert.AreEqual(category, software.Category);
      Assert.AreEqual(releaseDate, software.ReleaseDate);
    }

    [Test]
    public void Constructor_SetsDefaultIsRunningToTrue()
    {
      // Act
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Assert
      Assert.IsTrue(software.IsRunning);
    }

    [Test]
    public void Constructor_InitializesEmptyListeningPorts()
    {
      // Act
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Assert
      Assert.IsNotNull(software.ListeningPorts);
      Assert.AreEqual(0, software.ListeningPorts.Count);
    }

    [Test]
    public void Constructor_InitializesEmptyVulnerabilities()
    {
      // Act
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Assert
      Assert.IsNotNull(software.Vulnerabilities);
      Assert.AreEqual(0, software.Vulnerabilities.Count);
    }

    [Test]
    public void Constructor_ParsesVersionString()
    {
      // Arrange
      var versionString = "2.4.41";

      // Act
      var software = new Software("Apache", versionString, "webserver", DateTime.Now);

      // Assert
      Assert.AreEqual(versionString, software.Version.ToString());
    }

    #endregion

    #region InstallPath Generation Tests

    [Test]
    public void GenerateInstallPath_WebserverCategory_ReturnsUsrSbinPath()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Assert
      Assert.AreEqual("/usr/sbin/apache", software.InstallPath);
    }

    [Test]
    public void GenerateInstallPath_DatabaseCategory_ReturnsOptPath()
    {
      // Arrange
      var software = new Software("MySQL", "8.0.21", "database", DateTime.Now);

      // Assert
      Assert.AreEqual("/opt/mysql", software.InstallPath);
    }

    [Test]
    public void GenerateInstallPath_DefaultCategory_ReturnsUsrBinPath()
    {
      // Arrange
      var software = new Software("Git", "2.30.0", "tool", DateTime.Now);

      // Assert
      Assert.AreEqual("/usr/bin/git", software.InstallPath);
    }

    [Test]
    public void GenerateInstallPath_UnknownCategory_ReturnsUsrBinPath()
    {
      // Arrange
      var software = new Software("CustomApp", "1.0.0", "unknown", DateTime.Now);

      // Assert
      Assert.AreEqual("/usr/bin/customapp", software.InstallPath);
    }

    [Test]
    public void GenerateInstallPath_ConvertsNameToLowercase()
    {
      // Arrange
      var software = new Software("APACHE", "2.4.41", "webserver", DateTime.Now);

      // Assert
      Assert.AreEqual("/usr/sbin/apache", software.InstallPath);
    }

    [Test]
    public void GenerateInstallPath_MixedCaseName_ConvertsToLowercase()
    {
      // Arrange
      var software = new Software("MySQLServer", "8.0.21", "database", DateTime.Now);

      // Assert
      Assert.AreEqual("/opt/mysqlserver", software.InstallPath);
    }

    #endregion

    #region Port Management Tests

    [Test]
    public void AddPort_NewPort_AddsPortToList()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act
      software.AddPort(80);

      // Assert
      Assert.AreEqual(1, software.ListeningPorts.Count);
      Assert.IsTrue(software.ListeningPorts.Contains(80));
    }

    [Test]
    public void AddPort_MultiplePorts_AddsAllPorts()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act
      software.AddPort(80);
      software.AddPort(443);
      software.AddPort(8080);

      // Assert
      Assert.AreEqual(3, software.ListeningPorts.Count);
      Assert.IsTrue(software.ListeningPorts.Contains(80));
      Assert.IsTrue(software.ListeningPorts.Contains(443));
      Assert.IsTrue(software.ListeningPorts.Contains(8080));
    }

    [Test]
    public void AddPort_DuplicatePort_DoesNotAddDuplicate()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act
      software.AddPort(80);
      software.AddPort(80);
      software.AddPort(80);

      // Assert
      Assert.AreEqual(1, software.ListeningPorts.Count);
      Assert.AreEqual(80, software.ListeningPorts[0]);
    }

    [Test]
    public void AddPort_PortZero_AddsPort()
    {
      // Arrange
      var software = new Software("TestApp", "1.0.0", "tool", DateTime.Now);

      // Act
      software.AddPort(0);

      // Assert
      Assert.AreEqual(1, software.ListeningPorts.Count);
      Assert.IsTrue(software.ListeningPorts.Contains(0));
    }

    [Test]
    public void AddPort_HighPortNumber_AddsPort()
    {
      // Arrange
      var software = new Software("TestApp", "1.0.0", "tool", DateTime.Now);

      // Act
      software.AddPort(65535);

      // Assert
      Assert.IsTrue(software.ListeningPorts.Contains(65535));
    }

    [TestCase(22)]
    [TestCase(80)]
    [TestCase(443)]
    [TestCase(3306)]
    [TestCase(8080)]
    public void AddPort_CommonPorts_AddsSuccessfully(int port)
    {
      // Arrange
      var software = new Software("TestApp", "1.0.0", "tool", DateTime.Now);

      // Act
      software.AddPort(port);

      // Assert
      Assert.IsTrue(software.ListeningPorts.Contains(port));
    }

    #endregion

    #region Vulnerability Tests

    [Test]
    public void HasVulnerability_NoVulnerabilities_ReturnsFalse()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act
      var result = software.HasVulnerability();

      // Assert
      Assert.IsFalse(result);
    }

    [Test]
    public void HasVulnerability_WithVulnerabilities_ReturnsTrue()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);
      var vuln = new Vulnerability(
        "CVE-2020-1234",
        "Test Vulnerability",
        VulnerabilityType.RemoteCodeExecution,
        8,
        "Test Description",
        "2.0.0",
        "2.4.50",
        "exploit-test {host} {port}"
      );
      software.Vulnerabilities.Add(vuln);

      // Act
      var result = software.HasVulnerability();

      // Assert
      Assert.IsTrue(result);
    }

    [Test]
    public void HasVulnerability_MultipleVulnerabilities_ReturnsTrue()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);
      software.Vulnerabilities.Add(new Vulnerability(
        "CVE-2020-1234",
        "Vuln 1",
        VulnerabilityType.RemoteCodeExecution,
        7,
        "Description 1",
        "2.0.0",
        "2.5.0",
        "exploit-1 {host} {port}"
      ));
      software.Vulnerabilities.Add(new Vulnerability(
        "CVE-2020-5678",
        "Vuln 2",
        VulnerabilityType.SQLInjection,
        6,
        "Description 2",
        "2.0.0",
        "2.5.0",
        "exploit-2 {host} {port}"
      ));

      // Act
      var result = software.HasVulnerability();

      // Assert
      Assert.IsTrue(result);
    }

    #endregion

    #region Vulnerability Probability Tests

    [Test]
    public void GetVulnerabilityProbability_BrandNewSoftware_ReturnsLowProbability()
    {
      // Arrange - Software released today
      var software = new Software("NewApp", "1.0.0", "tool", DateTime.Now);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.Less(probability, 0.01f, "Brand new software should have very low vulnerability probability");
    }

    [Test]
    public void GetVulnerabilityProbability_OneYearOldSoftware_ReturnsModerrateProbability()
    {
      // Arrange - Software released 1 year ago
      var oneYearAgo = DateTime.Now.AddDays(-365);
      var software = new Software("OldApp", "1.0.0", "tool", oneYearAgo);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.Greater(probability, 0.09f); // ~10%
      Assert.Less(probability, 0.11f);
    }

    [Test]
    public void GetVulnerabilityProbability_ThreeYearOldSoftware_ReturnsHigherProbability()
    {
      // Arrange - Software released 3 years ago (~1095 days)
      var threeYearsAgo = DateTime.Now.AddDays(-1095);
      var software = new Software("OldApp", "1.0.0", "tool", threeYearsAgo);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.GreaterOrEqual(probability, 0.29f); // ~30%
      Assert.LessOrEqual(probability, 0.31f);
    }

    [Test]
    public void GetVulnerabilityProbability_VeryOldSoftware_CapsAt90Percent()
    {
      // Arrange - Software released 10 years ago
      var tenYearsAgo = DateTime.Now.AddDays(-3650);
      var software = new Software("AncientApp", "1.0.0", "tool", tenYearsAgo);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.AreEqual(0.9f, probability, 0.01f); // Capped at 90%
    }

    [Test]
    public void GetVulnerabilityProbability_ExtremelyOldSoftware_StillCapsAt90Percent()
    {
      // Arrange - Software released 20 years ago
      var twentyYearsAgo = DateTime.Now.AddDays(-7300);
      var software = new Software("FossilApp", "1.0.0", "tool", twentyYearsAgo);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.AreEqual(0.9f, probability, 0.01f, "Probability should be capped at 90% regardless of age");
    }

    [Test]
    public void GetVulnerabilityProbability_IncreasesWithAge()
    {
      // Arrange
      var newSoftware = new Software("New", "1.0.0", "tool", DateTime.Now);
      var oldSoftware = new Software("Old", "1.0.0", "tool", DateTime.Now.AddDays(-1000));

      // Act
      var newProb = newSoftware.GetVulnerabilityProbability();
      var oldProb = oldSoftware.GetVulnerabilityProbability();

      // Assert
      Assert.Greater(oldProb, newProb, "Older software should have higher vulnerability probability");
    }

    [TestCase(0, 0.0f, 0.01f)]      // Brand new
    [TestCase(365, 0.09f, 0.11f)]   // 1 year = ~10%
    [TestCase(730, 0.19f, 0.21f)]   // 2 years = ~20%
    [TestCase(1095, 0.29f, 0.31f)]  // 3 years = ~30%
    [TestCase(3650, 0.9f, 0.9f)]    // 10 years (capped at 90%)
    public void GetVulnerabilityProbability_VariousAges_ReturnsExpectedRange(int daysOld, float minExpected, float maxExpected)
    {
      // Arrange
      var releaseDate = DateTime.Now.AddDays(-daysOld);
      var software = new Software("TestApp", "1.0.0", "tool", releaseDate);

      // Act
      var probability = software.GetVulnerabilityProbability();

      // Assert
      Assert.GreaterOrEqual(probability, minExpected,
          $"Probability for {daysOld} days old should be >= {minExpected}");
      Assert.LessOrEqual(probability, maxExpected,
          $"Probability for {daysOld} days old should be <= {maxExpected}");
    }

    #endregion

    #region IsRunning Property Tests

    [Test]
    public void IsRunning_CanBeSetToFalse()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act
      software.IsRunning = false;

      // Assert
      Assert.IsFalse(software.IsRunning);
    }

    [Test]
    public void IsRunning_CanBeToggledMultipleTimes()
    {
      // Arrange
      var software = new Software("Apache", "2.4.41", "webserver", DateTime.Now);

      // Act & Assert
      Assert.IsTrue(software.IsRunning);

      software.IsRunning = false;
      Assert.IsFalse(software.IsRunning);

      software.IsRunning = true;
      Assert.IsTrue(software.IsRunning);

      software.IsRunning = false;
      Assert.IsFalse(software.IsRunning);
    }

    #endregion

    #region Integration Tests

    [Test]
    public void CompleteScenario_WebServer_ConfiguresCorrectly()
    {
      // Arrange & Act
      var apache = new Software("Apache", "2.4.41", "webserver", new DateTime(2019, 8, 15));
      apache.AddPort(80);
      apache.AddPort(443);
      apache.Vulnerabilities.Add(new Vulnerability(
        "CVE-2019-1234",
        "Path Traversal",
        VulnerabilityType.RemoteCodeExecution,
        8,
        "Allows path traversal attack",
        "2.0.0",
        "2.4.50",
        "exploit-path-traversal {host} {port}"
      ));

      // Assert
      Assert.AreEqual("Apache", apache.Name);
      Assert.AreEqual("/usr/sbin/apache", apache.InstallPath);
      Assert.AreEqual(2, apache.ListeningPorts.Count);
      Assert.IsTrue(apache.HasVulnerability());
      Assert.IsTrue(apache.IsRunning);
      Assert.Greater(apache.GetVulnerabilityProbability(), 0.1f); // Over 1 year old
    }

    [Test]
    public void CompleteScenario_DatabaseServer_ConfiguresCorrectly()
    {
      // Arrange & Act
      var mysql = new Software("MySQL", "8.0.21", "database", new DateTime(2020, 7, 1));
      mysql.AddPort(3306);
      mysql.IsRunning = true;

      // Assert
      Assert.AreEqual("MySQL", mysql.Name);
      Assert.AreEqual("/opt/mysql", mysql.InstallPath);
      Assert.AreEqual(1, mysql.ListeningPorts.Count);
      Assert.IsTrue(mysql.ListeningPorts.Contains(3306));
      Assert.IsFalse(mysql.HasVulnerability());
      Assert.IsTrue(mysql.IsRunning);
    }

    [Test]
    public void CompleteScenario_MultiplePortsAndVulnerabilities()
    {
      // Arrange
      var software = new Software("ComplexApp", "3.2.1", "webserver", DateTime.Now.AddYears(-2));

      // Act
      software.AddPort(8080);
      software.AddPort(8443);
      software.AddPort(9000);

      software.Vulnerabilities.Add(new Vulnerability(
        "CVE-2021-0001",
        "SQL Injection",
        VulnerabilityType.SQLInjection,
        9,
        "SQL injection vulnerability",
        "3.0.0",
        "3.5.0",
        "exploit-sqli {host} {port}"
      ));
      software.Vulnerabilities.Add(new Vulnerability(
        "CVE-2021-0002",
        "XSS",
        VulnerabilityType.RemoteCodeExecution,
        6,
        "Cross-site scripting vulnerability",
        "3.0.0",
        "3.5.0",
        "exploit-xss {host} {port}"
      ));
      software.Vulnerabilities.Add(new Vulnerability(
        "CVE-2021-0003",
        "CSRF",
        VulnerabilityType.Authentication,
        5,
        "Cross-site request forgery",
        "3.0.0",
        "3.5.0",
        "exploit-csrf {host} {port}"
      ));

      // Assert
      Assert.AreEqual(3, software.ListeningPorts.Count);
      Assert.AreEqual(3, software.Vulnerabilities.Count);
      Assert.IsTrue(software.HasVulnerability());
      Assert.Greater(software.GetVulnerabilityProbability(), 0.19f); // 2 years = ~20%
      Assert.Less(software.GetVulnerabilityProbability(), 0.21f);
    }

    #endregion

    #region Edge Cases

    [Test]
    public void Constructor_EmptyName_StillCreatesInstance()
    {
      // Act
      var software = new Software("", "1.0.0", "tool", DateTime.Now);

      // Assert
      Assert.AreEqual("", software.Name);
      Assert.AreEqual("/usr/bin/", software.InstallPath); // Empty name converts to empty string
    }

    [Test]
    public void Constructor_FutureReleaseDate_HandlesCorrectly()
    {
      // Arrange
      var futureDate = DateTime.Now.AddDays(365);

      // Act
      var software = new Software("FutureApp", "2.0.0", "tool", futureDate);

      // Assert
      Assert.AreEqual(futureDate, software.ReleaseDate);
      // Vulnerability probability might be negative or zero for future software
      var probability = software.GetVulnerabilityProbability();
      Assert.LessOrEqual(probability, 0.0f);
    }

    [Test]
    public void AddPort_NegativePort_StillAdds()
    {
      // Arrange
      var software = new Software("TestApp", "1.0.0", "tool", DateTime.Now);

      // Act
      software.AddPort(-1);

      // Assert
      Assert.IsTrue(software.ListeningPorts.Contains(-1));
    }

    #endregion
  }
}
