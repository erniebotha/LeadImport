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

namespace QuoteManager.Templates.HouseHoldContents
{
    public class Process_HouseHoldContent_StartProcess
    {

        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string HHC_XmlTemplatePath, string[] headers, string row)
        {
            string controlFilePath = "control.log";

            // Clear previous control log
            File.WriteAllText(controlFilePath, "");

            // Load CSV data
            var HHC_records = LoadCsv(headers, row);
            var csv_propowned_records = LoadCsvadditional(Path.Combine(ProcessingDirectory, "HOUSEHOLDCONTENTPROPOWNEDCLAIMDETAILS.CSV"));
            var csv_current_riskrecords = LoadCsvadditional(Path.Combine(ProcessingDirectory, "HOUSEHOLDCONTENTCURRENTRISKCLAIMDETAILS.CSV"));

            // Load mapping
            var mappings = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var HHC_PropOwned_ClaimDetailmappings = Load_PropOwned_Mapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var HHC_current_riskrecords_ClaimDetailmappings = Load_CurrentRisk_Mapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            // Load XML template
            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[HHC_XmlTemplatePath]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(xmlTemplate, HHC_records, csv_propowned_records, csv_current_riskrecords, mappings, HHC_PropOwned_ClaimDetailmappings, HHC_current_riskrecords_ClaimDetailmappings, controlFilePath);

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
                    string xmlField = parts[0].Trim(); // XML field (e.g., "household.content.sum.insured")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "SumInsured")

                    // Extract the prefix (if any) from the XML field format
                    string prefix = xmlField.Contains('.') ? xmlField.Split('.')[0] : "";

                    // Store in the dictionary as a tuple
                    mappings[csvField] = (prefix, xmlField);
                }
            }

            return mappings;
        }
        static Dictionary<string, (string Prefix, string XmlField)> Load_PropOwned_Mapping(string filePath)
        {
            var mappings = new Dictionary<string, (string Prefix, string XmlField)>();
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new string[] { "->" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string xmlField = parts[0].Trim(); // XML field (e.g., "claims.buildings.owned.date.claim.buildings.owned")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "DateOfClaim")

                    // Only process if the XML field starts with "claims.buildings.owned"
                    if (xmlField.StartsWith("claims.buildings.owned"))
                    {
                        string prefix = "claims"; // First part of the path
                        mappings[csvField] = (prefix, xmlField);
                    }
                }
            }
            return mappings;
        }
        static Dictionary<string, (string Prefix, string XmlField)> Load_CurrentRisk_Mapping(string filePath)
        {
            var mappings = new Dictionary<string, (string Prefix, string XmlField)>();
            foreach (var line in File.ReadLines(filePath))
            {
                var parts = line.Split(new string[] { "->" }, StringSplitOptions.None);
                if (parts.Length == 2)
                {
                    string xmlField = parts[0].Trim(); // XML field (e.g., "claims.buildings.owned.date.claim.buildings.owned")
                    string csvField = parts[1].Trim(); // CSV field (e.g., "DateOfClaim")

                    // Only process if the XML field starts with "claims.buildings.owned"
                    if (xmlField.StartsWith("household.content.claims"))
                    {
                        string prefix = "claims"; // First part of the path
                        mappings[csvField] = (prefix, xmlField);
                    }
                }
            }
            return mappings;
        }

        static string ProcessTemplate(XDocument template, 
                                        List<Dictionary<string, string>> HHC_records,
                                        List<Dictionary<string, string>> csv_propowned_records,
                                        List<Dictionary<string, string>> csv_current_riskrecords,

                                        Dictionary<string, (string Prefix, string XmlField)> mappings,
                                        Dictionary<string, (string Prefix, string XmlField)> HHC_PropOwned_ClaimDetailmappings,
                                        Dictionary<string, (string Prefix, string XmlField)> HHC_current_riskrecords_ClaimDetailmappings,
                                        

                                        string controlFilePath)
        {
            XElement buildingData = template.Root;
            if (buildingData == null) return "";

       
            // Process File A records (BUILDINGS.csv)
            foreach (var HHC_record in HHC_records)
            {
                var uid = HHC_record.ContainsKey("UniqueExternalRef") ? HHC_record["UniqueExternalRef"] : "0";
                var item_id = HHC_record.ContainsKey("HouseholdContentId") ? HHC_record["HouseholdContentId"] : "0";


                foreach (var kvp in HHC_record)
                {
                    // Look for the mapping where the CSV field matches
                    if (mappings.TryGetValue(kvp.Key, out var mapping))
                    {
                        string xmlField = mapping.XmlField;

                        // Find the last occurrence of "household.content." and split after it
                        int elementEndIndex = xmlField.LastIndexOf("household.content.") + "household.content.".Length;

                        // The attribute name is everything after "household.content."
                        string attributeName = xmlField.Substring(elementEndIndex);

                        // Remove any @ symbol if present and handle any array indices
                        attributeName = attributeName.Replace("@", "").Split('[')[0].Trim();

                        // Find the target element
                        XElement targetElement = buildingData.Name.LocalName == "household.content"
                            ? buildingData
                            : buildingData.Descendants("household.content").FirstOrDefault();

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
                Process_propowned_Claim(csv_propowned_records, buildingData, HHC_PropOwned_ClaimDetailmappings, controlFilePath, uid, item_id);
                Process_current_risk(csv_current_riskrecords, buildingData, HHC_current_riskrecords_ClaimDetailmappings, controlFilePath, uid, item_id);
            }

            return template.ToString();
        }

        static void Process_propowned_Claim(List<Dictionary<string, string>> recordsC, XElement buildingData, Dictionary<string, (string Prefix, string XmlField)> buildings_PropOwned_ClaimDetailmappings, string controlFilePath, string uid, string item_id)
        {
            var matching_propowned_RecordsC = recordsC.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("HouseholdContentId")
                                                          && b["UniqueExternalRef"] == uid
                                                          && b["HouseholdContentId"] == item_id).ToList();

            // Set total count of matching records as "property-owned-claim" attribute
            int totalClaimsCount = matching_propowned_RecordsC.Count;
            buildingData.SetAttributeValue("property-owned-claim", totalClaimsCount);

            if (matching_propowned_RecordsC.Count == 0)
                return;


            XElement claimsElement = buildingData.Element("claims.buildings.owned");

            if (claimsElement != null)
            {
                claimsElement.Remove();
                foreach (var recordC in matching_propowned_RecordsC)
                {
                    XElement newClaim = new XElement("claims.buildings.owned");

                    foreach (var kvp in recordC)
                    {
                        // Look for column names from the image like "DateOfClaim", "InsurerName", etc.
                        if (buildings_PropOwned_ClaimDetailmappings.TryGetValue(kvp.Key, out var mapping))
                        {
                            // The mapping.XmlField contains the XML path like "claims.buildings.owned.date.claim.buildings.owned"
                            // We need to extract just the attribute part after the element name
                            string elementName = "claims.buildings.owned";
                            string fullPath = mapping.XmlField;

                            // Extract attribute name after the element prefix
                            if (fullPath.StartsWith(elementName))
                            {
                                // Calculate attribute name by removing the element prefix
                                string attributeName = fullPath.Substring(elementName.Length);

                                // Remove leading dot if present
                                if (attributeName.StartsWith("."))
                                    attributeName = attributeName.Substring(1);

                                // Set the attribute if we have a value
                                if (!string.IsNullOrWhiteSpace(kvp.Value))
                                {
                                    newClaim.SetAttributeValue(attributeName, kvp.Value);
                                }
                                else
                                {
                                    LogMissingValue(controlFilePath, "File C", uid, kvp.Key);
                                }
                            }
                        }
                    }

                    // Only add non-empty claims
                    if (newClaim.Attributes().Any())
                    {
                        buildingData.Add(newClaim);
                    }
                }
            }
        }
        static void Process_current_risk(List<Dictionary<string, string>> recordsC, XElement buildingData, Dictionary<string, (string Prefix, string XmlField)> buildings_current_riskrecords_ClaimDetailmappings, string controlFilePath, string uid, string item_id)
        {
            var matching_propowned_RecordsC = recordsC.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("HouseholdContentId")
                                                          && b["UniqueExternalRef"] == uid
                                                          && b["HouseholdContentId"] == item_id).ToList();

            // Set total count of matching records as "property-owned-claim" attribute
            int totalClaimsCount = matching_propowned_RecordsC.Count;
            buildingData.SetAttributeValue("recent.loss.count", totalClaimsCount);

            if (matching_propowned_RecordsC.Count == 0)
                return;


            XElement claimsElement = buildingData.Element("household.content.claims");

            if (claimsElement != null)
            {
                claimsElement.Remove();
                foreach (var recordC in matching_propowned_RecordsC)
                {
                    XElement newClaim = new XElement("household.content.claims");

                    foreach (var kvp in recordC)
                    {
                        // Look for column names from the image like "DateOfClaim", "InsurerName", etc.
                        if (buildings_current_riskrecords_ClaimDetailmappings.TryGetValue(kvp.Key, out var mapping))
                        {
                            // The mapping.XmlField contains the XML path like "claims.buildings.owned.date.claim.buildings.owned"
                            // We need to extract just the attribute part after the element name
                            string elementName = "household.content.claims";
                            string fullPath = mapping.XmlField;

                            // Extract attribute name after the element prefix
                            if (fullPath.StartsWith(elementName))
                            {
                                // Calculate attribute name by removing the element prefix
                                string attributeName = fullPath.Substring(elementName.Length);

                                // Remove leading dot if present
                                if (attributeName.StartsWith("."))
                                    attributeName = attributeName.Substring(1);

                                // Set the attribute if we have a value
                                if (!string.IsNullOrWhiteSpace(kvp.Value))
                                {
                                    newClaim.SetAttributeValue(attributeName, kvp.Value);
                                }
                                else
                                {
                                    LogMissingValue(controlFilePath, "File C", uid, kvp.Key);
                                }
                            }
                        }
                    }

                    // Only add non-empty claims
                    if (newClaim.Attributes().Any())
                    {
                        buildingData.Add(newClaim);
                    }
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
