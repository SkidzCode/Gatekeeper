using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Resources;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration; // Added for IConfiguration
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations on XDocument
using System;
using GateKeeper.Server.Models.Configuration;
using Microsoft.Extensions.Options; // Required for InvalidOperationException, etc.

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class ResourceServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper;
        private Mock<ILogger<ResourceService>> _mockLogger;
        private Mock<IOptions<ResourceSettingsConfig>> _mockResourceSettingsOptions; // Added
        private ResourceService _resourceService;
        private string _testResourceDir;

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockLogger = new Mock<ILogger<ResourceService>>();
            _mockResourceSettingsOptions = new Mock<IOptions<ResourceSettingsConfig>>(); // Added

            _testResourceDir = Path.Combine(Path.GetTempPath(), $"ResourceServiceTests_TempResources_{Guid.NewGuid()}");
            if (Directory.Exists(_testResourceDir))
            {
                Directory.Delete(_testResourceDir, true);
            }
            Directory.CreateDirectory(_testResourceDir);

            // Setup ResourceSettingsConfig
            var resourceSettings = new ResourceSettingsConfig { Path = _testResourceDir };
            _mockResourceSettingsOptions.Setup(o => o.Value).Returns(resourceSettings);
            
            _resourceService = new ResourceService(_mockDbHelper.Object, _mockLogger.Object, _mockResourceSettingsOptions.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testResourceDir))
            {
                Directory.Delete(_testResourceDir, true);
            }
        }
        
        // Helper to create a dummy .resx file content
        private string GetResxContent(List<ResourceEntry> entries)
        {
            var root = new XElement("root",
                new XElement("resheader", new XAttribute("name", "resmimetype"), new XElement("value", "text/microsoft-resx")),
                new XElement("resheader", new XAttribute("name", "version"), new XElement("value", "2.0")),
                new XElement("resheader", new XAttribute("name", "reader"), new XElement("value", "System.Resources.ResXResourceReader")),
                new XElement("resheader", new XAttribute("name", "writer"), new XElement("value", "System.Resources.ResXResourceWriter"))
            );

            foreach (var entry in entries)
            {
                root.Add(new XElement("data",
                    new XAttribute("name", entry.Key),
                    string.IsNullOrWhiteSpace(entry.Type) ? null : new XAttribute("type", entry.Type),
                    new XElement("value", entry.Value ?? string.Empty),
                    string.IsNullOrWhiteSpace(entry.Comment) ? null : new XElement("comment", entry.Comment)
                ));
            }
            return new XDocument(new XDeclaration("1.0", "utf-8", "yes"), root).ToString();
        }

        // Due to the hardcoded path in ResourceService, these tests will likely fail
        // to interact with the file system as intended on a typical Linux build agent.
        // They are written to reflect what *should* happen if the path was configurable
        // and pointed to _testResourceDir.

        [TestMethod]
        public void ListEntries_FileDoesNotExist_ReturnsEmptyList()
        {
            // This test assumes the hardcoded path does not exist or is inaccessible.
            // On Linux, "C:/Users/..." won't exist by default.
            var entries = _resourceService.ListEntries("nonexistentfile");
            Assert.IsNotNull(entries);
            Assert.AreEqual(0, entries.Count);
        }

        [TestMethod]
        public void AddEntry_ToNonExistentFile_CreatesFileAndAddsEntry()
        {
            // This test is problematic due to the hardcoded path.
            // If we could control the path, we'd check _testResourceDir.
            // Here, we call it and expect it to *attempt* to write to the hardcoded path.
            // This will likely fail or do nothing testable in a standard CI environment.
            var resourceFileName = "testAddCreate";
            var request = new AddResourceEntryRequest { Key = "newKey", Value = "newValue", Type = "System.String", Comment = "A new key" };

            _resourceService.AddEntry(resourceFileName, request);
            
            var filePath = Path.Combine(_testResourceDir, $"{resourceFileName}.resx");
            Assert.IsTrue(File.Exists(filePath));
            var entries = _resourceService.ListEntries(resourceFileName);
            Assert.AreEqual(1, entries.Count);
            Assert.AreEqual(request.Key, entries[0].Key);
            Assert.AreEqual(request.Value, entries[0].Value);
        }


        [TestMethod]
        public void AddEntry_KeyAlreadyExists_ThrowsInvalidOperationException()
        {
            var resourceFileName = "testAddDuplicate";
            var request1 = new AddResourceEntryRequest { Key = "existingKey", Value = "value1" };
            var request2 = new AddResourceEntryRequest { Key = "existingKey", Value = "value2" };

            // First add will create file and add key in _testResourceDir.
            _resourceService.AddEntry(resourceFileName, request1); 

            // Second add should throw InvalidOperationException.
            Assert.ThrowsException<InvalidOperationException>(() =>
                _resourceService.AddEntry(resourceFileName, request2)
            );
        }

        [TestMethod]
        public void UpdateEntry_FileDoesNotExist_ThrowsFileNotFoundException()
        {
            var request = new UpdateResourceEntryRequest { Value = "updatedValue" };
            Assert.ThrowsException<FileNotFoundException>(() =>
                _resourceService.UpdateEntry("nonexistentUpdateFile", "somekey", request)
            );
        }
        
        [TestMethod]
        public void UpdateEntry_KeyNotFound_ThrowsKeyNotFoundException()
        {
            // Similar to AddEntry_KeyAlreadyExists, this relies on file system interaction.
            // Assume a file could be created (e.g., via AddEntry) but without the target key.
            var resourceFileName = "testUpdateKeyNotFound";
            var initialEntry = new AddResourceEntryRequest { Key = "presentKey", Value = "initialValue" };
            var updateRequest = new UpdateResourceEntryRequest { Value = "updatedValue" };

            // Attempt to create a file with one key in _testResourceDir
            _resourceService.AddEntry(resourceFileName, initialEntry);

            // Then attempt to update a different, non-existent key.
            Assert.ThrowsException<KeyNotFoundException>(() =>
                _resourceService.UpdateEntry(resourceFileName, "nonExistentKey", updateRequest)
            );
        }

        [TestMethod]
        public void ListEntries_FileExists_ReturnsCorrectEntries()
        {
            // ARRANGE
            var resourceFileName = "sampleList";
            var expectedEntries = new List<ResourceEntry>
            {
                new ResourceEntry { Key = "Greeting", Value = "Hello", Type="System.String", Comment="A greeting" },
                new ResourceEntry { Key = "Farewell", Value = "Goodbye", Type="System.String", Comment="A farewell" }
            };
            var filePath = Path.Combine(_testResourceDir, $"{resourceFileName}.resx");
            File.WriteAllText(filePath, GetResxContent(expectedEntries));

            // ACT
            var actualEntries = _resourceService.ListEntries(resourceFileName);

            // ASSERT
            Assert.AreEqual(expectedEntries.Count, actualEntries.Count);
            for(int i=0; i < expectedEntries.Count; i++)
            {
                Assert.AreEqual(expectedEntries[i].Key, actualEntries[i].Key);
                Assert.AreEqual(expectedEntries[i].Value, actualEntries[i].Value);
                Assert.AreEqual(expectedEntries[i].Type, actualEntries[i].Type);
                Assert.AreEqual(expectedEntries[i].Comment, actualEntries[i].Comment);
            }
        }
    }
}
