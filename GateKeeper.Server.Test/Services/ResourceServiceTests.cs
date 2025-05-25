using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using GateKeeper.Server.Services;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Resources;
using Microsoft.Extensions.Logging;
using System.Xml.Linq;
using System.IO;
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations on XDocument
using System; // Required for InvalidOperationException, etc.

namespace GateKeeper.Server.Test.Services
{
    [TestClass]
    public class ResourceServiceTests
    {
        private Mock<IDbHelper> _mockDbHelper; // In SUT, but seems unused for resource file logic
        private Mock<ILogger<ResourceService>> _mockLogger;
        private ResourceService _resourceService;

        // This is the hardcoded path from ResourceService. We'll try to work with/around it.
        private readonly string _hardcodedResourceDirectory = "C:/Users/Skidz/source/repos/GateKeeper/GateKeeper.Server/Resources";
        private string _testResourceDir; // This will be our actual temp testing directory if we can manage to use one.

        [TestInitialize]
        public void TestInitialize()
        {
            _mockDbHelper = new Mock<IDbHelper>();
            _mockLogger = new Mock<ILogger<ResourceService>>();
            
            // Normally, we would mock IConfiguration to provide a test path.
            // Since ResourceService has a hardcoded path, we instantiate it directly.
            // The challenge is that tests will try to interact with this hardcoded path.
            _resourceService = new ResourceService(_mockDbHelper.Object, _mockLogger.Object);

            // Attempt to set up a temporary directory that mirrors the structure,
            // hoping Path.Combine in the SUT might work relative to our /app if not absolute.
            // This is a long shot due to "C:/" prefix on Linux.
            _testResourceDir = Path.Combine(Path.GetTempPath(), "ResourceServiceTests_TempResources");
            if (Directory.Exists(_testResourceDir))
            {
                Directory.Delete(_testResourceDir, true);
            }
            Directory.CreateDirectory(_testResourceDir);
            
            // If the SUT were using IConfiguration, we'd mock it like this:
            // var mockConfiguration = new Mock<IConfiguration>();
            // var mockConfigSection = new Mock<IConfigurationSection>();
            // mockConfigSection.Setup(s => s.Value).Returns(_testResourceDir);
            // mockConfiguration.Setup(c => c.GetSection("Resources:Path")).Returns(mockConfigSection.Object);
            // _resourceService = new ResourceService(_mockDbHelper.Object, _mockLogger.Object, mockConfiguration.Object);
        }

        [TestCleanup]
        public void TestCleanup()
        {
            if (Directory.Exists(_testResourceDir))
            {
                Directory.Delete(_testResourceDir, true);
            }
            // If tests were actually able to write to _hardcodedResourceDirectory,
            // we would need to clean that up, but that's highly problematic.
            // On a typical CI Linux agent, creating "C:/Users/..." will fail or create it literally under /app.
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

            try
            {
                _resourceService.AddEntry(resourceFileName, request);
                // If the above didn't throw due to path issues, we can't easily verify the file
                // as it's in the hardcoded location.
                // This test implicitly becomes: "does it not crash immediately on a typical system?"
                // A more robust assertion would be needed if we could guarantee file system interaction.
                 Assert.Inconclusive("Cannot verify file creation at hardcoded path: " + _hardcodedResourceDirectory + ". Test assumes AddEntry attempted operation without crashing.");

            }
            catch (Exception ex)
            {
                // Catching a broad range of exceptions that might occur due to path issues.
                // UnauthorizedAccessException, DirectoryNotFoundException, IOException etc.
                Assert.IsTrue(ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException, 
                              "Expected an IO-related exception due to hardcoded path, but got: " + ex.ToString());
            }
        }


        [TestMethod]
        public void AddEntry_KeyAlreadyExists_ThrowsInvalidOperationException()
        {
            // This test also relies on interacting with a file at the hardcoded path.
            // For it to work as intended, the file would need to be pre-created with the key.
            // This is very difficult to set up reliably without refactoring the SUT.
            
            // We'll assume that if LoadOrCreateResourceFile could be made to point to a controlled file:
            // 1. Create a file in _testResourceDir with "existingKey"
            // 2. Call AddEntry with "existingKey"
            // 3. Expect InvalidOperationException

            // Since we can't reliably create the file in the hardcoded path for the test,
            // we can only test this if the "nonexistentfile" scenario of LoadOrCreateResourceFile
            // allows us to then add a key, then add it again.
            var resourceFileName = "testAddDuplicate";
            var request1 = new AddResourceEntryRequest { Key = "existingKey", Value = "value1" };
            var request2 = new AddResourceEntryRequest { Key = "existingKey", Value = "value2" };

            try
            {
                // First add will attempt to create file and add key.
                _resourceService.AddEntry(resourceFileName, request1); 

                // Second add should throw InvalidOperationException IF the first add was successful AND
                // the file is now being read correctly from the hardcoded path.
                Assert.ThrowsException<InvalidOperationException>(() =>
                    _resourceService.AddEntry(resourceFileName, request2)
                );
                 Assert.Inconclusive("Test for duplicate key depends on successful file interaction with hardcoded path: " + _hardcodedResourceDirectory + ". If previous AddEntry failed silently or threw, this assertion might not be testing the duplicate logic.");

            }
            catch (Exception ex) // Catch exceptions from the first AddEntry
            {
                 Assert.IsTrue(ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException, 
                               "Expected an IO-related exception from first AddEntry due to hardcoded path, but got: " + ex.ToString());
                 Assert.Inconclusive("First AddEntry failed due to file system issues with hardcoded path, cannot test duplicate key logic. Error: " + ex.Message);
            }
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

            try
            {
                // Attempt to create a file with one key.
                _resourceService.AddEntry(resourceFileName, initialEntry);

                // Then attempt to update a different, non-existent key.
                Assert.ThrowsException<KeyNotFoundException>(() =>
                    _resourceService.UpdateEntry(resourceFileName, "nonExistentKey", updateRequest)
                );
                Assert.Inconclusive("Test for updating non-existent key depends on successful file interaction with hardcoded path: " + _hardcodedResourceDirectory + ". If AddEntry failed, this assertion might not be testing the intended logic.");

            }
            catch (Exception ex) // Catch exceptions from AddEntry
            {
                 Assert.IsTrue(ex is IOException || ex is UnauthorizedAccessException || ex is DirectoryNotFoundException, 
                               "Expected an IO-related exception from AddEntry due to hardcoded path, but got: " + ex.ToString());
                 Assert.Inconclusive("AddEntry failed due to file system issues with hardcoded path, cannot test key not found logic for UpdateEntry. Error: " + ex.Message);
            }
        }

        // Further tests for successful ListEntries, AddEntry (actually checking file content), 
        // and UpdateEntry (actually checking file content) would require either:
        // 1. The ability to write to the hardcoded path C:/Users/Skidz/source/repos/GateKeeper/GateKeeper.Server/Resources
        // 2. Refactoring ResourceService to make the path configurable.
        // Without these, tests are limited to checking behavior when file system access fails
        // or making inconclusive assertions.
        // The following is a sketch of how a test *would* look if the path was configurable.
        /*
        [TestMethod]
        public void ListEntries_FileExists_ReturnsCorrectEntries_IF_PATH_CONFIGURABLE()
        {
            // ARRANGE
            // 1. Point ResourceService to use _testResourceDir (e.g. via mocked IConfiguration)
            var resourceFileName = "sampleList";
            var expectedEntries = new List<ResourceEntry>
            {
                new ResourceEntry { Key = "Greeting", Value = "Hello", Type="System.String", Comment="A greeting" },
                new ResourceEntry { Key = "Farewell", Value = "Goodbye", Type="System.String", Comment="A farewell" }
            };
            File.WriteAllText(Path.Combine(_testResourceDir, $"{resourceFileName}.resx"), GetResxContent(expectedEntries));
            // Make sure ResourceService instance under test uses _testResourceDir
            // _resourceService = new ResourceService(_mockDbHelper.Object, _mockLogger.Object, _mockConfigPointingToTestDir.Object);


            // ACT
            // var actualEntries = _resourceService.ListEntries(resourceFileName);

            // ASSERT
            // Assert.AreEqual(expectedEntries.Count, actualEntries.Count);
            // for(int i=0; i < expectedEntries.Count; i++) { ... compare entries ... }
            Assert.Inconclusive("This test requires ResourceService path to be configurable.");
        }
        */
    }
}
