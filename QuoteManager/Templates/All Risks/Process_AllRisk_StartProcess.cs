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

namespace QuoteManager.Templates.All_Risks
{
    public class Process_AllRisk_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string AllRiskXmlTemplatePath, string[] headers, string row)
        {
            string controlFilePath = "control.log";

                // Clear previous control log
                File.WriteAllText(controlFilePath, "");

                // Load CSV data
                var recordsA = LoadCsv(headers, row);


                // Load mapping
                var mappings = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);

                // Load XML template
                XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[AllRiskXmlTemplatePath]);

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
            var mappings = new Dictionary<string, (string Prefix, string XmlField)>();

            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new string[] { "->" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string xmlField = parts[0].Trim(); // XML field (e.g., "motor-vehicle.car-colour")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "CarColour")

                    // Extract the prefix (if any) from the XML field format
                    string prefix = xmlField.Contains('.') ? xmlField.Split('.')[0] : "";

                    // Store in the dictionary as a tuple
                    mappings[csvField] = (prefix, xmlField); // Now storing CSV field as key, and tuple as value
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
                    // Look for the mapping where the CSV field matches
                    if (mappings.TryGetValue(kvp.Key, out var mapping))
                    {
                        string xmlField = mapping.XmlField;

                        // Find the last occurrence of "all.risks.item." and split after it
                        int elementEndIndex = xmlField.LastIndexOf("all.risks.item.") + "all.risks.item.".Length;

                        // The element name is everything before the attribute
                        string elementName = "all.risks.item";

                        // The attribute name is everything after "all.risks.item."
                        string attributeName = xmlField.Substring(elementEndIndex);

                        // Remove any @ symbol if present
                        attributeName = attributeName.Replace("@", "").Split('[')[0].Trim();

                        // Find the target element
                        XElement targetElement = motorVehicle.Name.LocalName == "all.risks.item"
                            ? motorVehicle
                            : motorVehicle.Descendants("all.risks.item").FirstOrDefault();

                        // Now set the attribute if the target element is found
                        if (targetElement != null)
                        {
                            if (string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                LogMissingValue(controlFilePath, "File A", uid, kvp.Key);
                            }
                            else
                            {
                                // Create the attribute name exactly as it appears in the XML
                                XName attributeXName = XName.Get(attributeName);
                                targetElement.SetAttributeValue(attributeXName, kvp.Value);
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
