using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Remoting.Messaging;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace CSV_Xml_Converter
{
    public class ConvertCSV_XML
    {
       
        static Dictionary<string, string> ReadMappingsFromFile(string filePath)
        {
            var mappings = new Dictionary<string, string>();
            foreach (var line in File.ReadAllLines(filePath))
            {
                var parts = line.Split('=');
                if (parts.Length == 2)
                {
                    mappings[parts[0].Trim()] = parts[1].Trim();
                }
            }
            return mappings;
        }

       public  static string MapCsvToXml(string xmlTemplatePath, string csvRow, string[] headers, Dictionary<string, string> mappings)
        {



            XDocument doc = XDocument.Load(xmlTemplatePath);
            string[] csvValues = csvRow.Split(',');

            var csvData = new Dictionary<string, string>();
            for (int i = 0; i < Math.Min(headers.Length, csvValues.Length); i++)
            {
                if (!string.IsNullOrEmpty(headers[i]))
                {
                    csvData[headers[i]] = csvValues[i];
                }
            }

            foreach (var mapping in mappings)
            {
                string csvHeader = mapping.Key;
                string xpath = mapping.Value;

                if (csvData.TryGetValue(csvHeader, out string csvValue))
                {
                    string[] pathParts = xpath.Split('@');
                    string elementPath = pathParts[0].TrimEnd('/');
                    string attributeName = pathParts.Length > 1 ? pathParts[1] : null;

                    if (elementPath == "//sh" || elementPath == "/sh")
                    {
                        if (attributeName != null)
                        {
                            doc.Root.SetAttributeValue(attributeName, csvValue);
                        }
                        continue;
                    }

                    try
                    {
                        XElement element = doc.XPathSelectElement(elementPath);
                        if (element != null)
                        {
                            if (attributeName != null)
                            {
                                element.SetAttributeValue(attributeName, csvValue);
                            }
                            else
                            {
                                element.Value = csvValue;
                            }
                        }
                        else
                        {
                            Console.WriteLine($"Warning: Element not found for XPath: {elementPath}");
                        }
                    }
                    catch (XPathException ex)
                    {
                        Console.WriteLine($"XPath error for '{elementPath}': {ex.Message}");
                    }
                }
            }

            return doc.ToString();
        }

    }
}
