using System.Xml.Linq;
using GateKeeper.Server.Interface;
using GateKeeper.Server.Models.Resources;

namespace GateKeeper.Server.Services
{
    public class ResourceService : IResourceService
    {
        private readonly string _resourceDirectory;

        public ResourceService()
        {
            _resourceDirectory = "C:/Users/Skidz/source/repos/GateKeeper/GateKeeper.Server/Resources";
        }

        public List<ResourceEntry> ListEntries(string resourceFileName)
        {
            var doc = LoadResourceFile(resourceFileName);
            if (doc == null)
            {
                return new List<ResourceEntry>();
            }

            var dataElements = doc.Descendants("data");
            var entries = new List<ResourceEntry>();

            foreach (var dataElem in dataElements)
            {
                var key = dataElem.Attribute("name")?.Value;
                var type = dataElem.Attribute("type")?.Value;
                var valueElem = dataElem.Element("value");
                var commentElem = dataElem.Element("comment");

                if (key != null)
                {
                    entries.Add(new ResourceEntry
                    {
                        Key = key,
                        Type = type ?? string.Empty,
                        Value = valueElem?.Value ?? string.Empty,
                        Comment = commentElem?.Value ?? string.Empty
                    });
                }
            }

            return entries;
        }

        public void AddEntry(string resourceFileName, AddResourceEntryRequest request)
        {
            var doc = LoadOrCreateResourceFile(resourceFileName);

            var existing = FindDataElement(doc, request.Key);
            if (existing != null)
            {
                throw new InvalidOperationException($"Key '{request.Key}' already exists in {resourceFileName}.resx");
            }

            var newElement = new XElement("data",
                new XAttribute("name", request.Key),
                string.IsNullOrWhiteSpace(request.Type) ? null : new XAttribute("type", request.Type),
                new XElement("value", request.Value ?? string.Empty),
                string.IsNullOrWhiteSpace(request.Comment) ? null : new XElement("comment", request.Comment)
            );

            doc.Root.Add(newElement);
            SaveResourceFile(resourceFileName, doc);
        }

        public void UpdateEntry(string resourceFileName, string key, UpdateResourceEntryRequest request)
        {
            var doc = LoadResourceFile(resourceFileName);
            if (doc == null)
            {
                throw new FileNotFoundException($"Resource file {resourceFileName}.resx not found.");
            }

            var dataElem = FindDataElement(doc, key);
            if (dataElem == null)
            {
                throw new KeyNotFoundException($"Key '{key}' not found in {resourceFileName}.resx");
            }

            // Update value
            var valueElem = dataElem.Element("value");
            if (valueElem == null)
            {
                valueElem = new XElement("value");
                dataElem.Add(valueElem);
            }
            valueElem.Value = request.Value ?? string.Empty;

            // Update type attribute
            if (string.IsNullOrWhiteSpace(request.Type))
            {
                dataElem.Attributes("type").Remove();
            }
            else
            {
                var typeAttr = dataElem.Attribute("type");
                if (typeAttr == null)
                {
                    dataElem.Add(new XAttribute("type", request.Type));
                }
                else
                {
                    typeAttr.Value = request.Type;
                }
            }

            // Update comment element
            var commentElem = dataElem.Element("comment");
            if (string.IsNullOrWhiteSpace(request.Comment))
            {
                commentElem?.Remove();
            }
            else
            {
                if (commentElem == null)
                {
                    commentElem = new XElement("comment", request.Comment);
                    dataElem.Add(commentElem);
                }
                else
                {
                    commentElem.Value = request.Comment;
                }
            }

            SaveResourceFile(resourceFileName, doc);
        }

        private XElement FindDataElement(XDocument doc, string key)
        {
            return doc.Descendants("data").FirstOrDefault(e => e.Attribute("name")?.Value == key);
        }

        private XDocument LoadOrCreateResourceFile(string resourceFileName)
        {
            var path = GetResourceFilePath(resourceFileName);
            if (!File.Exists(path))
            {
                var doc = new XDocument(
                    new XDeclaration("1.0", "utf-8", "yes"),
                    new XElement("root",
                        new XElement("resheader", new XAttribute("name", "resmimetype"),
                            new XElement("value", "text/microsoft-resx")),
                        new XElement("resheader", new XAttribute("name", "version"),
                            new XElement("value", "2.0")),
                        new XElement("resheader", new XAttribute("name", "reader"),
                            new XElement("value", "System.Resources.ResXResourceReader")),
                        new XElement("resheader", new XAttribute("name", "writer"),
                            new XElement("value", "System.Resources.ResXResourceWriter"))
                    )
                );
                doc.Save(path);
                return doc;
            }
            return LoadResourceFile(resourceFileName);
        }

        private XDocument LoadResourceFile(string resourceFileName)
        {
            var path = GetResourceFilePath(resourceFileName);
            if (!File.Exists(path))
            {
                return null;
            }
            return XDocument.Load(path);
        }

        private void SaveResourceFile(string resourceFileName, XDocument doc)
        {
            var path = GetResourceFilePath(resourceFileName);
            doc.Save(path);
        }

        private string GetResourceFilePath(string resourceFileName)
        {
            return Path.Combine(_resourceDirectory, $"{resourceFileName}.resx");
        }
    }
}
