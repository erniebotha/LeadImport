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

namespace QuoteManager.Templates.Buildings
{
    public class Process_Building_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string XmlMappingFile, string BuildingXmlTemplatePath, string[] headers, string row)
        
        {
           string controlFilePath = "control.log";
            
            // Clear previous control log
            File.WriteAllText(controlFilePath, "");


            // Load CSV data
            #region 

            var csv_building_records    = LoadCsv(headers, row);
            var csv_geyser_records      = LoadCsvadditional(Path.Combine(ProcessingDirectory, "BUILDINGGEYSERCOVER.CSV"));
            var csv_propowned_records   = LoadCsvadditional(Path.Combine(ProcessingDirectory, "BUILDINGPROPOWNEDCLAIMDETAILS.CSV"));
            var csv_current_riskrecords = LoadCsvadditional(Path.Combine(ProcessingDirectory, "BUILDINGCURRENTRISKCLAIMDETAILS.CSV"));

            #endregion


            // Load mapping
            #region

            var mappings                                          = LoadMapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var geyser_Mapping                                    = Load_geyser_Mapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var buildings_PropOwned_ClaimDetailmappings           = Load_PropOwned_Mapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            var buildings_current_riskrecords_ClaimDetailmappings = Load_CurrentRisk_Mapping(ConfigurationManager.AppSettings[XmlMappingFile]);
            #endregion


            // Load XML template

            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[BuildingXmlTemplatePath]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(xmlTemplate, csv_building_records, csv_geyser_records, csv_propowned_records, csv_current_riskrecords, mappings, controlFilePath, geyser_Mapping, buildings_PropOwned_ClaimDetailmappings, buildings_current_riskrecords_ClaimDetailmappings);

            
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


        #region Load the mappings 
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

        static Dictionary<string, (string Prefix, string XmlField)> Load_geyser_Mapping(string filePath)
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
                    if (xmlField.StartsWith("claims.current.risk"))
                    {
                        string prefix = "claims"; // First part of the path
                        mappings[csvField] = (prefix, xmlField);
                    }
                }
            }
            return mappings;
        }


        #endregion



        static string ProcessTemplate(XDocument template, 
            List<Dictionary<string, string>> csv_building_records, 
            List<Dictionary<string, string>> csv_geyser_records,
            List<Dictionary<string, string>> csv_propowned_records, 
            List<Dictionary<string, string>> csv_current_riskrecords, 

            Dictionary<string, (string Prefix, string XmlField)> mappings, 
            string controlFilePath,
            Dictionary<string, (string Prefix, string XmlField)> geyser_Mapping, 
            Dictionary<string, (string Prefix, string XmlField)> buildings_PropOwned_ClaimDetailmappings,
            Dictionary<string, (string Prefix, string XmlField)> buildings_current_riskrecords_ClaimDetailmappings
            )
        {
            XElement buildingData = template.Root;
            if (buildingData == null)return "";


          
            // Process File A records (BUILDINGS.csv)
            foreach (var building_row in csv_building_records)
            {
                // Extract uid and item_id from the dictionary
                var uid = building_row.ContainsKey("UniqueExternalRef") ? building_row["UniqueExternalRef"] : "0";
                var item_id = building_row.ContainsKey("BuildingId") ? building_row["BuildingId"] : "0";

                
                foreach (var kvp in building_row)
                {
                    if (mappings.TryGetValue(kvp.Key, out var mapping))
                    {
                        string xmlField = mapping.XmlField;
                        int elementEndIndex = xmlField.LastIndexOf("building.data.") + "building.data.".Length;
                        string attributeName = xmlField.Substring(elementEndIndex).Replace("@", "").Split('[')[0].Trim();

                        XElement targetElement = buildingData.Name.LocalName == "building.data"
                            ? buildingData
                            : buildingData.Descendants("building.data").FirstOrDefault();

                        if (targetElement != null)
                        {
                            if (string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                LogMissingValue(controlFilePath, "File A", uid, kvp.Key);
                            }
                            else
                            {
                                if (attributeName.Contains("-"))
                                {
                                    attributeName = attributeName.Replace("-", ".");
                                }

                                XName attributeXName = XName.Get(attributeName);
                                targetElement.SetAttributeValue(attributeXName, kvp.Value);
                            }
                        }
                    }
                }
                Process_geyser(csv_geyser_records, buildingData, geyser_Mapping, controlFilePath, uid, item_id);
                Process_propowned_Claim(csv_propowned_records, buildingData, buildings_PropOwned_ClaimDetailmappings, controlFilePath, uid, item_id);
                Process_current_risk(csv_current_riskrecords, buildingData, buildings_current_riskrecords_ClaimDetailmappings, controlFilePath, uid, item_id);
            }


            return template.ToString();
        }

        static void Process_geyser(List<Dictionary<string, string>> geyser_records, XElement buildingData, Dictionary<string, (string Prefix, string XmlField)> geyser_Mapping, string controlFilePath, string uid,string item_id)
        {
            var matching_geyser_records = geyser_records.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("BuildingId")
                                                          && b["UniqueExternalRef"] == uid
                                                          && b["BuildingId"] == item_id).ToList();
            if (matching_geyser_records == null)
                return;
            // Set total count of matching records as "property-owned-claim" attribute
            int totalGeysersCount = matching_geyser_records.Count;
            buildingData.SetAttributeValue("geyser-count", totalGeysersCount);


            XAttribute geyserSeq = buildingData.Attribute("seq");
            if (geyserSeq != null)
            {
                int geyserSeqValue = int.TryParse(geyserSeq.Value, out int seq) ? seq + 1 : 1;
                buildingData.SetAttributeValue("seq", geyserSeq);
            }
            
            XElement geyserTemplate = buildingData.Element("bgc");

            if (geyserTemplate != null)
            {
                geyserTemplate.Remove();
                int nsaSeqCounter = 1;

                foreach (var geyser_row in matching_geyser_records)
                {
                    XElement newgeyser = new XElement("bgc");

                    foreach (var kvp in geyser_row)
                    {
                        var relevantMappings = geyser_Mapping
                            .Where(m => {
                                var parts = m.Value.XmlField.Split(new[] { "->" }, StringSplitOptions.None);
                                return parts.Length > 1 && parts[1].Trim().Equals(kvp.Key, StringComparison.OrdinalIgnoreCase);
                            })
                            .Where(m => m.Key.StartsWith("bgc."))
                            .ToList();

                        foreach (var mapping in relevantMappings)
                        {
                            var attributeName = mapping.Key.Split('.')[1].Replace("@", "").Split('[')[0].Trim();

                            if (!string.IsNullOrWhiteSpace(kvp.Value))
                            {
                                newgeyser.SetAttributeValue(attributeName, kvp.Value);
                            }
                            else
                            {
                                LogMissingValue(controlFilePath, "File B", uid, kvp.Key);
                            }
                        }
                    }

                    newgeyser.SetAttributeValue("seq", nsaSeqCounter++);
                    buildingData.Add(newgeyser);
                }
            }
        }

        static void Process_propowned_Claim(List<Dictionary<string, string>> recordsC, XElement buildingData, Dictionary<string, (string Prefix, string XmlField)> buildings_PropOwned_ClaimDetailmappings, string controlFilePath, string uid, string item_id)
        {
            var matching_propowned_RecordsC = recordsC.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("BuildingId")
                                                          && b["UniqueExternalRef"] == uid
                                                          && b["BuildingId"] == item_id).ToList();

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
                var matching_propowned_RecordsC = recordsC.Where(b => b.ContainsKey("UniqueExternalRef") && b.ContainsKey("BuildingId")
                                                              && b["UniqueExternalRef"] == uid
                                                              && b["BuildingId"] == item_id).ToList();

                // Set total count of matching records as "property-owned-claim" attribute
                int totalClaimsCount = matching_propowned_RecordsC.Count;
                buildingData.SetAttributeValue("recent.loss.count", totalClaimsCount);

                if (matching_propowned_RecordsC.Count == 0)
                    return;


                XElement claimsElement = buildingData.Element("claims.current.risk");

                if (claimsElement != null)
                {
                    claimsElement.Remove();
                    foreach (var recordC in matching_propowned_RecordsC)
                    {
                        XElement newClaim = new XElement("claims.current.risk");

                        foreach (var kvp in recordC)
                        {
                            // Look for column names from the image like "DateOfClaim", "InsurerName", etc.
                            if (buildings_current_riskrecords_ClaimDetailmappings.TryGetValue(kvp.Key, out var mapping))
                            {
                                // The mapping.XmlField contains the XML path like "claims.buildings.owned.date.claim.buildings.owned"
                                // We need to extract just the attribute part after the element name
                                string elementName = "claims.current.risk";
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


        //    static void ProcessFileC(List<Dictionary<string, string>> recordsC, XElement buildingData, Dictionary<string, (string Prefix, string XmlField)> CurrentRisk_ClaimDetailmappings, string controlFilePath, string uid)
        //    {
        //        var matchingRecordsC = recordsC.Where(b => b["UniqueExternalRef"] == uid).ToList();
        //        XElement nsaTemplate = buildingData.Element("claims.buildings.owned");
        //        if (nsaTemplate != null)
        //        {
        //            //nsaTemplate.Remove();

        //            foreach (var recordC in matchingRecordsC)
        //            {
        //                XElement newNsa = new XElement("claims.buildings.owned");
        //                foreach (var kvp in recordC)
        //                {
        //                    var relevantMappings = CurrentRisk_ClaimDetailmappings
        //.Where(m => {
        //    // Compare the dictionary key directly with kvp.Key
        //    // The key in your dictionary should be the field name (e.g., "DateOfClaim")
        //    return m.Key.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase);
        //})
        //.ToList();


        //                    //var relevantMappings = CurrentRisk_ClaimDetailmappings
        //                    //            .Where(m => {
        //                    //                var mappingValue = m.Value.XmlField; // Example: "(claims, claims.buildings.owned.date.claim.buildings.owned -> DateOfClaim)"
        //                    //                                                     // Extract the CSV field (last part after '->')
        //                    //                var arrowIndex = mappingValue.LastIndexOf("->");
        //                    //                if (arrowIndex == -1) return false; // Ensure valid format
        //                    //                var mappedCsvField = mappingValue.Substring(arrowIndex + 2).Trim(); // Extract "DateOfClaim"
        //                    //                return mappedCsvField.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase);
        //                    //            })
        //                    //            .ToList();
        //                    foreach (var mapping in relevantMappings)
        //                    {
        //                        // Extract the full attribute name by removing the element name prefix
        //                        string fullPath = mapping.Key;
        //                        string elementName = "claims.buildings.owned";

        //                        // If the mapping starts with the element name, extract the attribute part
        //                        if (fullPath.StartsWith(elementName + "."))
        //                        {
        //                            var attributeName = fullPath.Substring(elementName.Length + 1); // +1 to skip the dot

        //                            if (!string.IsNullOrWhiteSpace(kvp.Value))
        //                            {
        //                                newNsa.SetAttributeValue(attributeName, kvp.Value);
        //                            }
        //                            else
        //                            {
        //                                LogMissingValue(controlFilePath, "File C", uid, kvp.Key);
        //                            }
        //                        }
        //                        else
        //                        {
        //                            // For non-claims.buildings.owned mappings, use the old approach or adjust as needed
        //                            var attributeName = mapping.Key.Split('.')[1].Replace("@", "").Split('[')[0].Trim();
        //                            if (!string.IsNullOrWhiteSpace(kvp.Value))
        //                            {
        //                                newNsa.SetAttributeValue(attributeName, kvp.Value);
        //                            }
        //                            else
        //                            {
        //                                LogMissingValue(controlFilePath, "File C", uid, kvp.Key);
        //                            }
        //                        }
        //                    }
        //                }

        //                buildingData.Add(newNsa);
        //            }
        //        }
        //    }


        static void LogMissingValue(string filePath, string recordType, string uid, string missingField)
        {
            string logEntry = $"{DateTime.Now}: Missing {missingField} in {recordType} for UID {uid}";
            File.AppendAllText(filePath, logEntry + Environment.NewLine);
        }


    }

   
}
