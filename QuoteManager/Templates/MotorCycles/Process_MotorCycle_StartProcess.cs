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
using System.Security.Cryptography;

namespace QuoteManager.Templates.MotorCycles
{
    internal class Process_MotorCycle_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string TrailerXmlTemplatePath, string[] headers, string row)
       {
            string controlFilePath = "control.log";

            // Clear previous control log
            File.WriteAllText(controlFilePath, "");
            
         
            // Load CSV data
            var recordsA = LoadCsv(headers, row);
            var recordsB = LoadCsvadditional(Path.Combine(ProcessingDirectory, "MOTORCYCLE_NSA.csv"));

            // Load mapping
            var mappings = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var nsamapping = LoadnsaMapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            
            
            // Load XML template
            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[TrailerXmlTemplatePath]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(xmlTemplate, recordsA, recordsB, mappings, controlFilePath, nsamapping);

           
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
        static List<Dictionary<string, string>> LoadCsvadditional(string filePath)
        {
            var reader = new StreamReader(filePath);
            var csv = new CsvReader(reader, new CsvConfiguration(CultureInfo.InvariantCulture));

            var records = new List<Dictionary<string, string>>();
            var dr = new CsvDataReader(csv);

            while (dr.Read())
            {
                var record = new Dictionary<string, string>();
                for (int i = 0; i < dr.FieldCount; i++)
                {
                    record[dr.GetName(i)] = dr[i]?.ToString() ?? "";
                }
                records.Add(record);
            }
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
                    string xmlField = parts[0].Trim(); // XML field (e.g., "building.data.sum.insured")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "SumInsured")

                    // Extract the prefix (if any) from the XML field format
                    string prefix = xmlField.Contains('.') ? xmlField.Split('.')[0] : "";

                    // Store in the dictionary as a tuple
                    mappings[csvField] = (prefix, xmlField);
                }
            }

            return mappings;
        }

        static Dictionary<string, (string Prefix, string XmlField)> LoadnsaMapping(string filePath)
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


        static string ProcessTemplate(XDocument template, 
            List<Dictionary<string, string>> recordsA, 
            List<Dictionary<string, string>> recordsB, 
            
            Dictionary<string, (string Prefix, string XmlField)> mappings, 
            string controlFilePath, 
            Dictionary<string, (string Prefix, string XmlField)> nsamappings)
        {
            XElement motorVehicle = template.Root;
            if (motorVehicle == null) return "";

            // Process File A records (MOTORVEHICLE.csv)
            foreach (var recordA in recordsA)
            {
                var uid = recordA.ContainsKey("UniqueExternalRef") ? recordA["UniqueExternalRef"] : "0";
                var item_id = recordA.ContainsKey("MotorCycleId") ? recordA["MotorCycleId"] : "0";
                foreach (var kvp in recordA)
                {
                    // Look for the mapping where the CSV field matches
                    if (mappings.TryGetValue(kvp.Key, out var mapping))
                    {
                        string xmlField = mapping.XmlField;

                        // Find the last occurrence of "building.data." and split after it
                        int elementEndIndex = xmlField.LastIndexOf("motor.cycle.") + "motor.cycle.".Length;

                        // The element name is everything before the attribute
                        string elementName = "motor.cycle";

                        // The attribute name is everything after "building.data."
                        string attributeName = xmlField.Substring(elementEndIndex);

                        // Remove any @ symbol if present and handle any array indices
                        attributeName = attributeName.Replace("@", "").Split('[')[0].Trim();

                        // Find the target element
                        XElement targetElement = motorVehicle.Name.LocalName == "motor.cycle"
                            ? motorVehicle
                            : motorVehicle.Descendants("motor.cycle").FirstOrDefault();

                        // Now set the attribute if the target element is found
                        if (targetElement != null)
                        {
                            if (string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                LogMissingValue(controlFilePath, "File A", uid, kvp.Key);
                            }
                            else
                            {
                                // Handle special cases for attribute names with hyphens
                                if (attributeName.Contains("-"))
                                {
                                    attributeName = attributeName.Replace("-", ".");
                                }

                                // Create the attribute name exactly as it appears in the XML
                                XName attributeXName = XName.Get(attributeName);
                                targetElement.SetAttributeValue(attributeXName, kvp.Value);
                            }
                        }
                    }
                }
                Process_NSA(recordsB, motorVehicle, nsamappings, controlFilePath, uid, item_id);
            }


            // Handle make and model mapping based on mm-code
            if (motorVehicle != null)
            {
                var mmCodeAttr = motorVehicle.Attribute("model");
                if (mmCodeAttr != null)
                {
                    string mmCode = mmCodeAttr.Value;
                    if (!string.IsNullOrEmpty(mmCode) && mmCode.Length >= 3)
                    {
                        // Set the "make" to the first 3 characters of mm-code
                        motorVehicle.SetAttributeValue("make", mmCode.Substring(0, 3));
                        // Set the "model" to the full mm-code
                        motorVehicle.SetAttributeValue("model", mmCode);
                    }
                    else
                    {
                        Console.WriteLine("Warning: mm-code attribute is missing or too short.");
                    }
                }
                else
                {
                    Console.WriteLine("Warning: mm-code attribute not found.");
                }
            }
            return template.ToString();
        }


        static void Process_NSA(List<Dictionary<string, string>> recordsB, XElement motorVehicle,
            Dictionary<string, (string Prefix, string XmlField)> nsamappings, 
            string controlFilePath, string uid, string item_id)
        {
            // Handle motor-vehicle seq separately
            XAttribute motorVehicleSeq = motorVehicle.Attribute("seq");
            if (motorVehicleSeq != null)
            {
                int motorSeqValue = int.TryParse(motorVehicleSeq.Value, out int seq) ? seq + 1 : 1;
                motorVehicle.SetAttributeValue("seq", motorSeqValue);
            }

            // Handle File B records (MOTORVEHICLE_NSA.csv)
            var matchingRecordsB = recordsB.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("BuildingId")
                                                          && b["UniqueExternalRef"] == uid
                                                          && b["MotorCycleId"] == item_id).ToList();
            XElement nsaTemplate = motorVehicle.Element("mcnsa");

            if (nsaTemplate != null)
            {
                nsaTemplate.Remove(); // Remove the template NSA element

                int nsaSeqCounter = 1;
                foreach (var recordB in matchingRecordsB)
                {
                    XElement newNsa = new XElement("mcnsa");

                    foreach (var kvp in recordB)
                    {
                        // Debug: Print what we're looking for
                        Console.WriteLine($"Looking for CSV key: {kvp.Key}");

                        // Debug: Print all available mappings for mcnsa
                        var mcnsaMappings = nsamappings.Where(m => m.Key.StartsWith("mcnsa.")).ToList();
                        Console.WriteLine($"All MCNSA mappings:");
                        foreach (var m in mcnsaMappings)
                        {
                            Console.WriteLine($"Key: {m.Key}, XmlField: {m.Value.XmlField}");
                        }

                        var relevantMappings = nsamappings
                            .Where(m =>
                            {
                                var parts = m.Value.XmlField.Split(new[] { "->" }, StringSplitOptions.None);
                                var match = parts.Length > 1 && parts[1].Trim().Equals(kvp.Key, StringComparison.OrdinalIgnoreCase);
                                Console.WriteLine($"Checking mapping: {m.Value.XmlField} against {kvp.Key}, match: {match}");
                                return match;
                            })
                            .Where(m => m.Key.StartsWith("mcnsa."))
                            .ToList();

                        Console.WriteLine($"Found {relevantMappings.Count} relevant mappings");



                        foreach (var mapping in relevantMappings)
                        {
                            // Clean the attribute name by removing @ and []
                            var attributeName = mapping.Key.Split('.')[1]
                                .Replace("@", "")
                                .Split('[')[0]
                                .Trim();

                            if (!string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                newNsa.SetAttributeValue(attributeName, kvp.Value);
                            }
                            else
                            {
                                LogMissingValue(controlFilePath, "File B", uid, kvp.Key);
                            }
                        }
                    }

                    newNsa.SetAttributeValue("seq", nsaSeqCounter++);
                    motorVehicle.Add(newNsa);
                }


            }
        }
        static void LogMissingValue(string filePath, string recordType, string uid, string missingField)
        {
            string logEntry = $"{DateTime.Now}: Missing {missingField} in {recordType} for UID {uid}";
            File.AppendAllText(filePath, logEntry + Environment.NewLine);
        }

    }
}
