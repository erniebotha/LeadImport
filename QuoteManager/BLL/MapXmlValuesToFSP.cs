using QuoteManager.Helper;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QuoteManager.BLL
{
    public class MapXmlValuesToFSP
    {
        // Transforms the initial XML with an external mapping configuration


        public static string TransformXml(string FSP_Values_MappingFilePath, string uniqueRef, string xmlOutput)
        {
            string mappingXml = System.IO.File.ReadAllText(FSP_Values_MappingFilePath);
            var mapper = new XmlValueMapper(mappingXml);
            return mapper.TransformXml(uniqueRef, xmlOutput);
        }
    }
    public class XmlValueMapper
    {
        private readonly XDocument _mappingsDoc;
        private Dictionary<string, Dictionary<string, string>> _mappingCache;

        public XmlValueMapper(string mappingXml)
        {
            _mappingsDoc = XDocument.Parse(mappingXml);
            InitializeMappingCache();
        }

        private void InitializeMappingCache()
        {
            _mappingCache = new Dictionary<string, Dictionary<string, string>>(StringComparer.OrdinalIgnoreCase);

            var mappings = _mappingsDoc.Descendants("mapping")
                .Where(m => m.Element("property") != null);

            foreach (var mapping in mappings)
            {
                var property = mapping.Element("property").Value;
                var lookups = mapping.Elements("lookup")
                    .ToDictionary(
                        l => l.Element("from").Value,
                        l => l.Element("to").Value,
                        StringComparer.OrdinalIgnoreCase
                    );

                _mappingCache[property] = lookups;
            }
        }
        public string TransformXml(string uniqueRef, string inputXml)
        {
            string unmatchedLogFilePath = $"E://{uniqueRef}.txt";
            var inputDoc = XDocument.Parse(inputXml);
            var unmatchedEntries = new List<string>();
            var genericLookups = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "yes", "1" },
        { "no", "0" }
    };

            foreach (var mapping in _mappingCache)
            {
                var propertyName = mapping.Key;
                var lookups = mapping.Value;

                foreach (var element in inputDoc.Descendants())
                {
                    // Handle element value mapping
                    if (string.Equals(element.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
                    {
                        ProcessElementValue(element, propertyName, lookups, genericLookups, unmatchedEntries, uniqueRef);
                    }

                    // Handle attribute value mapping
                    foreach (var attribute in element.Attributes())
                    {
                        if (string.Equals(attribute.Name.LocalName, propertyName, StringComparison.OrdinalIgnoreCase))
                        {
                            ProcessElementValue(attribute, propertyName, lookups, genericLookups, unmatchedEntries, uniqueRef);
                        }
                    }
                }
            }

            // First, handle the generic "yes" and "no" mappings globally in the XML
            foreach (var element in inputDoc.Descendants())
            {
                ProcessElementValue(element, null, null, genericLookups, unmatchedEntries, uniqueRef);

                foreach (var attribute in element.Attributes())
                {
                    ProcessElementValue(attribute, null, null, genericLookups, unmatchedEntries, uniqueRef);
                }
            }

            // Log unmatched entries to a file, but only if the property exists in _mappingCache
            // Log unmatched entries to a file, but only if the property exists in _mappingCache
            if (unmatchedEntries.Any(entry =>
                _mappingCache.Keys.Any(key =>
                    entry.IndexOf(key, StringComparison.OrdinalIgnoreCase) >= 0)))
            {
                File.WriteAllLines(unmatchedLogFilePath, unmatchedEntries);
                return null;
            }

            return inputDoc.ToString();
        }

        private void ProcessElementValue(XObject elementOrAttribute, string propertyName,
                                         Dictionary<string, string> lookups,
                                         Dictionary<string, string> genericLookups,
                                         List<string> unmatchedEntries, string uniqueRef)
        {
            string currentValue = null;
            if (elementOrAttribute is XElement element)
            {
                currentValue = element.Value.Trim();
            }
            else if (elementOrAttribute is XAttribute attribute)
            {
                currentValue = attribute.Value.Trim();
            }

            // Check if the current value matches genericLookups first
            if (genericLookups.TryGetValue(currentValue, out string mappedValue))
            {
                if (elementOrAttribute is XElement elem) elem.Value = mappedValue;
                else if (elementOrAttribute is XAttribute attr) attr.Value = mappedValue;
            }
            // Then, try to map using the specific lookups if propertyName is available
            else if (propertyName != null && lookups != null && lookups.TryGetValue(currentValue, out mappedValue))
            {
                if (elementOrAttribute is XElement elem) elem.Value = mappedValue;
                else if (elementOrAttribute is XAttribute attr) attr.Value = mappedValue;
            }
            // Log unmatched values
            else if (propertyName != null && lookups != null)
            {
                unmatchedEntries.Add($"UniqueRef: '{uniqueRef}', Unmatched {elementOrAttribute.GetType().Name}: '{propertyName}', Value: '{currentValue}'");
            }
        }
    }
}
