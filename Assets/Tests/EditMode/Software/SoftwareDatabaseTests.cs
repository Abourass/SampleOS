using NUnit.Framework;
using SampleOS.Core.SoftwarePackages;
using System;
using System.Collections.Generic;

namespace SampleOS.Tests.SoftwarePackages
{
  [TestFixture]
  public class SoftwareDatabaseTests
  {
    private SoftwareDatabase _database;

    [SetUp]
    public void Setup()
    {
      _database = new SoftwareDatabase();
    }

    [TearDown]
    public void TearDown()
    {
      _database = null;
    }

    #region WebServer Category Tests

    [Test]
    public void GenerateRandomSoftware_WebServerCategory_ReturnsWebServer()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual("webserver", software.Category);
      Assert.AreEqual("/usr/sbin/" + software.Name.ToLower(), software.InstallPath);
    }

    [Test]
    public void GenerateRandomSoftware_WebServer_ReturnsKnownSoftware()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var knownWebServers = new[] { "Apache", "Nginx", "Tomcat" };

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(software.Name, knownWebServers);
    }

    [Test]
    public void GenerateRandomSoftware_WebServer_HasCorrectPorts()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Greater(software.ListeningPorts.Count, 0, "Web server should have listening ports");

      if (software.Name == "Apache" || software.Name == "Nginx")
      {
        Assert.Contains(80, software.ListeningPorts, "Should have port 80");
        Assert.Contains(443, software.ListeningPorts, "Should have port 443");
      }
      else if (software.Name == "Tomcat")
      {
        Assert.Contains(8080, software.ListeningPorts, "Should have port 8080");
        Assert.Contains(8443, software.ListeningPorts, "Should have port 8443");
      }
    }

    [Test]
    public void GenerateRandomSoftware_WebServer_MultipleCallsReturnDifferentSoftware()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var generatedNames = new HashSet<string>();

      // Act - Generate 20 web servers, should get variety
      for (int i = 0; i < 20; i++)
      {
        var software = _database.GenerateRandomSoftware("webserver", systemDate);
        generatedNames.Add(software.Name);
      }

      // Assert - Should have more than one unique name (randomness)
      Assert.Greater(generatedNames.Count, 1,
          "Multiple calls should return different web servers due to randomness");
    }

    #endregion

    #region Database Category Tests

    [Test]
    public void GenerateRandomSoftware_DatabaseCategory_ReturnsDatabase()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual("database", software.Category);
      Assert.AreEqual("/opt/" + software.Name.ToLower(), software.InstallPath);
    }

    [Test]
    public void GenerateRandomSoftware_Database_ReturnsKnownDatabase()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var knownDatabases = new[] { "MySQL", "PostgreSQL", "MongoDB" };

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(software.Name, knownDatabases);
    }

    [Test]
    public void GenerateRandomSoftware_Database_HasCorrectPorts()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual(1, software.ListeningPorts.Count, "Database should have one listening port");

      if (software.Name == "MySQL")
      {
        Assert.Contains(3306, software.ListeningPorts);
      }
      else if (software.Name == "PostgreSQL")
      {
        Assert.Contains(5432, software.ListeningPorts);
      }
      else if (software.Name == "MongoDB")
      {
        Assert.Contains(27017, software.ListeningPorts);
      }
    }

    #endregion

    #region CMS Category Tests

    [Test]
    public void GenerateRandomSoftware_CMSCategory_ReturnsCMS()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("cms", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual("cms", software.Category);
    }

    [Test]
    public void GenerateRandomSoftware_CMS_ReturnsKnownCMS()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var knownCMS = new[] { "WordPress", "Drupal" };

      // Act
      var software = _database.GenerateRandomSoftware("cms", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(software.Name, knownCMS);
    }

    [Test]
    public void GenerateRandomSoftware_CMS_HasWebPorts()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("cms", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(80, software.ListeningPorts);
      Assert.Contains(443, software.ListeningPorts);
    }

    #endregion

    #region Firewall Category Tests

    [Test]
    public void GenerateRandomSoftware_FirewallCategory_ReturnsFirewall()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("firewall", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual("firewall", software.Category);
    }

    [Test]
    public void GenerateRandomSoftware_Firewall_ReturnsKnownFirewall()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var knownFirewalls = new[] { "IPTables", "UFW" };

      // Act
      var software = _database.GenerateRandomSoftware("firewall", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(software.Name, knownFirewalls);
    }

    [Test]
    public void GenerateRandomSoftware_Firewall_HasNoPorts()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("firewall", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual(0, software.ListeningPorts.Count,
          "Firewalls should not have listening ports");
    }

    #endregion

    #region FileServer Category Tests

    [Test]
    public void GenerateRandomSoftware_FileServerCategory_ReturnsFileServer()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("fileserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.AreEqual("fileserver", software.Category);
    }

    [Test]
    public void GenerateRandomSoftware_FileServer_ReturnsKnownFileServer()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var knownFileServers = new[] { "Samba", "NFS" };

      // Act
      var software = _database.GenerateRandomSoftware("fileserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Contains(software.Name, knownFileServers);
    }

    [Test]
    public void GenerateRandomSoftware_FileServer_HasCorrectPorts()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("fileserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.Greater(software.ListeningPorts.Count, 0,
          "File server should have listening ports");

      if (software.Name == "Samba")
      {
        Assert.Contains(139, software.ListeningPorts);
        Assert.Contains(445, software.ListeningPorts);
      }
      else if (software.Name == "NFS")
      {
        Assert.Contains(2049, software.ListeningPorts);
      }
    }

    #endregion

    #region Invalid Category Tests

    [Test]
    public void GenerateRandomSoftware_InvalidCategory_ReturnsNull()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("nonexistent", systemDate);

      // Assert
      Assert.IsNull(software);
    }

    [Test]
    public void GenerateRandomSoftware_EmptyCategory_ReturnsNull()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("", systemDate);

      // Assert
      Assert.IsNull(software, "Empty category should return null without throwing exception");
    }

    [Test]
    public void GenerateRandomSoftware_WhitespaceCategory_ReturnsNull()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("   ", systemDate);

      // Assert
      Assert.IsNull(software, "Whitespace category should return null");
    }

    [Test]
    public void GenerateRandomSoftware_NullCategory_ReturnsNull()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware(null, systemDate);

      // Assert
      Assert.IsNull(software, "Null category should return null without throwing exception");
    }

    #endregion

    #region Version Generation Tests

    [Test]
    public void GenerateRandomSoftware_NewerSystem_GeneratesNewerVersion()
    {
      // Arrange - System created recently
      var recentDate = DateTime.Now.AddDays(-30);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", recentDate);

      // Assert
      Assert.IsNotNull(software);
      // Version should be relatively recent (not ancient)
      Assert.IsNotNull(software.Version);
      Assert.Greater(software.Version.ToString().Length, 0);
    }

    [Test]
    public void GenerateRandomSoftware_OlderSystem_GeneratesOlderVersion()
    {
      // Arrange - System created 5 years ago
      var oldDate = DateTime.Now.AddYears(-5);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", oldDate);

      // Assert
      Assert.IsNotNull(software);
      // Software should have been released after system creation
      Assert.GreaterOrEqual(software.ReleaseDate, oldDate);
    }

    [Test]
    public void GenerateRandomSoftware_VersionFormat_IsValid()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      var versionString = software.Version.ToString();

      // Should contain at least major.minor
      Assert.IsTrue(versionString.Contains("."),
          "Version should contain dots (major.minor format)");

      // Should be parseable
      Assert.DoesNotThrow(() => new SoftwareVersion(versionString));
    }

    [Test]
    public void GenerateRandomSoftware_MultipleGenerations_CreatesDifferentVersions()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-2);
      var versions = new HashSet<string>();

      // Act - Generate 10 instances
      for (int i = 0; i < 10; i++)
      {
        var software = _database.GenerateRandomSoftware("webserver", systemDate);
        versions.Add(software.Version.ToString());
      }

      // Assert - Should have some variety in versions
      Assert.Greater(versions.Count, 1,
          "Multiple generations should produce different versions");
    }

    #endregion

    #region Release Date Tests

    [Test]
    public void GenerateRandomSoftware_ReleaseDateAfterSystemCreation()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-3);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.GreaterOrEqual(software.ReleaseDate, systemDate,
          "Software release date should be after system creation");
    }

    [Test]
    public void GenerateRandomSoftware_ReleaseDateNotInFuture()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.LessOrEqual(software.ReleaseDate, DateTime.Now.AddDays(1),
          "Software release date should not be in the future");
    }

    [Test]
    public void GenerateRandomSoftware_ReleaseDateWithinYearOfSystemCreation()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-2);

      // Act
      var software = _database.GenerateRandomSoftware("cms", systemDate);

      // Assert
      Assert.IsNotNull(software);
      var maxExpectedDate = systemDate.AddDays(365);
      Assert.LessOrEqual(software.ReleaseDate, maxExpectedDate,
          "Software should be released within a year of system creation");
    }

    #endregion

    #region Software Properties Tests

    [Test]
    public void GenerateRandomSoftware_SetsIsRunningToTrue()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.IsTrue(software.IsRunning, "Generated software should be running by default");
    }

    [Test]
    public void GenerateRandomSoftware_InitializesEmptyVulnerabilities()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("database", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.IsNotNull(software.Vulnerabilities);
      Assert.AreEqual(0, software.Vulnerabilities.Count,
          "Generated software should have no vulnerabilities initially");
    }

    [Test]
    public void GenerateRandomSoftware_HasValidName()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", systemDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.IsNotNull(software.Name);
      Assert.IsNotEmpty(software.Name);
    }

    #endregion

    #region All Categories Integration Test

    [Test]
    public void GenerateRandomSoftware_AllCategories_WorkCorrectly()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);
      var categories = new[] { "webserver", "database", "cms", "firewall", "fileserver" };

      // Act & Assert
      foreach (var category in categories)
      {
        var software = _database.GenerateRandomSoftware(category, systemDate);

        Assert.IsNotNull(software, $"Category '{category}' should generate software");
        Assert.AreEqual(category, software.Category,
            $"Software category should match requested category");
        Assert.IsNotNull(software.Name,
            $"Software name should not be null for category '{category}'");
        Assert.IsNotNull(software.Version,
            $"Software version should not be null for category '{category}'");
      }
    }

    #endregion

    #region Edge Cases

    [Test]
    public void GenerateRandomSoftware_FutureSystemDate_HandlesCorrectly()
    {
      // Arrange - System "created" in the future
      var futureDate = DateTime.Now.AddYears(1);

      // Act
      var software = _database.GenerateRandomSoftware("webserver", futureDate);

      // Assert
      Assert.IsNotNull(software);
      // Should still generate valid software even with future date
      Assert.IsNotNull(software.Version);
    }

    [Test]
    public void GenerateRandomSoftware_VeryOldSystemDate_HandlesCorrectly()
    {
      // Arrange - System created 20 years ago
      var veryOldDate = DateTime.Now.AddYears(-20);

      // Act
      var software = _database.GenerateRandomSoftware("database", veryOldDate);

      // Assert
      Assert.IsNotNull(software);
      Assert.GreaterOrEqual(software.ReleaseDate, veryOldDate);
    }

    [Test]
    public void GenerateRandomSoftware_CaseSensitiveCategory_ReturnsNull()
    {
      // Arrange - Try uppercase category
      var systemDate = DateTime.Now.AddYears(-1);

      // Act
      var software = _database.GenerateRandomSoftware("WEBSERVER", systemDate);

      // Assert
      Assert.IsNull(software, "Category lookup should be case-sensitive");
    }

    #endregion

    #region Consistency Tests

    [Test]
    public void GenerateRandomSoftware_SameCategoryMultipleTimes_AllValid()
    {
      // Arrange
      var systemDate = DateTime.Now.AddYears(-1);

      // Act - Generate 5 web servers
      for (int i = 0; i < 5; i++)
      {
        var software = _database.GenerateRandomSoftware("webserver", systemDate);

        // Assert
        Assert.IsNotNull(software, $"Generation {i} should succeed");
        Assert.AreEqual("webserver", software.Category);
        Assert.Greater(software.ListeningPorts.Count, 0);
        Assert.IsTrue(software.IsRunning);
      }
    }

    #endregion
  }
}
