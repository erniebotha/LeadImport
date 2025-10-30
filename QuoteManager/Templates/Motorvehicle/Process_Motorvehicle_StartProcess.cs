using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace QuoteManager.Templates.Motorvehicle
{
    public class Process_Motorvehicle_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string MotorvehicleXmlTemplatePath, string[] headers, string row)
        {
            string controlFilePath = "control.log";

            // Clear previous control log
            File.WriteAllText(controlFilePath, "");

            // Load CSV data

            var csv_motorvehicle_records = LoadCsv(headers, row);
            var csv_NSA_records = LoadNSARecords(Path.Combine(ProcessingDirectory, "MOTORVEHICLE_NSA.csv"));

            // Load mapping
            var mappings = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);

            // Load XML template
            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[MotorvehicleXmlTemplatePath]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(xmlTemplate, csv_motorvehicle_records, csv_NSA_records, mappings, controlFilePath);

           
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
        static List<Dictionary<string, string>> LoadNSARecords(string filePath)
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



        static string ProcessTemplate(XDocument template, List<Dictionary<string, string>> csv_motorvehicle_records, List<Dictionary<string, string>> csv_NSA_records, Dictionary<string, (string Prefix, string XmlField)> mappings, string controlFilePath)
        {
            XElement motorVehicle = template.Root;
            if (motorVehicle == null) return "";

            
            // Process File A records (MOTORVEHICLE.csv)
            foreach (var recordA in csv_motorvehicle_records)
            {
                var uid = csv_motorvehicle_records.FirstOrDefault()?["UniqueExternalRef"] ?? "0";
                var item_id = recordA.ContainsKey("VehicleID") ? recordA["VehicleID"] : "0";
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
                            var targetElement = elementName == "motor-vehicle" ?
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

                Process_NSA(csv_NSA_records, motorVehicle, mappings, controlFilePath, uid, item_id);

            }


            // Handle make and model mapping based on mm-code
            if (motorVehicle != null)
            {
                var mmCodeAttr = motorVehicle.Attribute("mm-code");
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

        static void Process_NSA(List<Dictionary<string, string>> csv_NSA_records, XElement motorVehicle, Dictionary<string, (string Prefix, string XmlField)> mappings, string controlFilePath, string uid, string item_id)
        {

            // Handle motor-vehicle seq separately
            XAttribute motorVehicleSeq = motorVehicle.Attribute("seq");
            if (motorVehicleSeq != null)
            {
                int motorSeqValue = int.TryParse(motorVehicleSeq.Value, out int seq) ? seq + 1 : 1;
                motorVehicle.SetAttributeValue("seq", motorSeqValue);
            }

            // Handle File B records (MOTORVEHICLE_NSA.csv)
            
            var matchingRecordsB = csv_NSA_records.Where(b => b["UniqueExternalRef"] == uid && b["VehicleId"] == item_id).ToList();
            XElement nsaTemplate = motorVehicle.Element("nsa");

            if (nsaTemplate != null)
            {
                nsaTemplate.Remove(); // Remove the template NSA element

                int nsaSeqCounter = 1;
                foreach (var recordB in matchingRecordsB)
                {
                    XElement newNsa = new XElement("nsa");

                    foreach (var kvp in recordB)
                    {
                        var relevantMappings = mappings
                            .Where(m => m.Value.XmlField.Split(new[] { "->" }, StringSplitOptions.None)[1].Trim()
                                .Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                            .Where(m => m.Key.StartsWith("nsa."))
                            .ToList();

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
