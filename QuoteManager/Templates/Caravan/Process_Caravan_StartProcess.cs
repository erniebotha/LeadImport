using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QuoteManager.Templates.Caravan
{
    public class Process_Caravan_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string CaravanXmlTemplatePath, string[] headers, string row)
        {
    
            string controlFilePath = "control.log";

            // Clear previous control log
            File.WriteAllText(controlFilePath, "");

            // Load CSV data
            var recordsA = LoadCsv(headers, row);


            // Load mapping
            var mappings = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);

            // Load XML template
            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[CaravanXmlTemplatePath]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(xmlTemplate, recordsA, mappings, controlFilePath);

            
            return finalXmlString;
        }

        static List<Dictionary<string, string>> LoadCsv(string[] headers, string rowData)
        {
            var records = new List<Dictionary<string, string>>();
            var record = new Dictionary<string, string>();

            string[] values = rowData.Split(',');

            for (int i = 0; i < headers.Length; i++)
            {
                record[headers[i]] = values.Length > i ? values[i] : ""; // Ensures missing values are handled safely
            }

            records.Add(record);
            return records;
        }

        static Dictionary<string, (string Prefix, string XmlField)> LoadMapping(string filePath)
        {
            var mappings = new Dictionary<string, (string, string)>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new string[] { "->" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string xmlField = parts[0].Trim(); // XML field (e.g., "motor-vehicle.car-colour")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "CarColour")

                    // Extract the prefix (if any) from the XML field format
                    string prefix = xmlField.Contains('.') ? xmlField.Split('.')[0] : "";

                    // Store in the dictionary
                    mappings[xmlField] = (prefix, line.Trim()); // Store the entire mapping line
                }
            }

            return mappings;
        }

        static string ProcessTemplate(XDocument template, List<Dictionary<string, string>> recordsA, Dictionary<string, (string Prefix, string XmlField)> mappings, string controlFilePath)
        {
            XElement motorVehicle = template.Root;
            if (motorVehicle == null) return "";

            var uid = recordsA.FirstOrDefault()?["UniqueExternalRef"] ?? "0";

            // Process File A records (MOTORVEHICLE.csv)
            foreach (var recordA in recordsA)
            {
                foreach (var kvp in recordA)
                {
                    // Find all mappings where the CSV field matches
                    var relevantMappings = mappings
                        .Where(m => m.Value.XmlField.Split(new[] { "->" }, StringSplitOptions.None)[1].Trim()
                        .Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                        .ToList();

                    foreach (var mapping in relevantMappings)
                    {
                        var xmlFieldParts = mapping.Key.Split('.');
                        if (xmlFieldParts.Length == 2) // Handle motor-vehicle.attribute format
                        {
                            var elementName = xmlFieldParts[0];
                            // Clean the attribute name by removing @ and []
                            var attributeName = xmlFieldParts[1]
                                .Replace("@", "")
                                .Split('[')[0]  // Remove any square bracket content
                                .Trim();

                            // Find the correct element (motor-vehicle or its children)
                            var targetElement = elementName == "caravan" ?
                                motorVehicle :
                                motorVehicle.Elements(elementName).FirstOrDefault();

                            if (targetElement != null)
                            {
                                if (string.IsNullOrWhiteSpace(kvp.Value))
                                {
                                    LogMissingValue(controlFilePath, "File A", uid, kvp.Key);
                                }
                                else
                                {
                                    targetElement.SetAttributeValue(attributeName, kvp.Value);
                                }
                            }
                        }
                    }
                }
            }


            return template.ToString();
        }
        static void LogMissingValue(string filePath, string recordType, string uid, string missingField)
        {
            string logEntry = $"{DateTime.Now}: Missing {missingField} in {recordType} for UID {uid}";
            File.AppendAllText(filePath, logEntry + Environment.NewLine);
        }
    }
}
