using CsvHelper.Configuration;
using CsvHelper;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using System.Xml.XPath;
using QuoteManager.CategoryHelpers.Person;

namespace QuoteManager.Templates.Person
{
    public class Process_Person_StartProcess
    {
        public static string StartProcess(string ProcessingDirectory, string FileName, string MappingFile, string XmlTemplate, string[] headers, string[] row)
        {
            string controlFilePath = "control.log";

            // Clear previous control log
            File.WriteAllText(controlFilePath, "");

            // Load CSV data
            var recordsA                 = LoadCsv(headers ,row);
            var Person_Claims_Records    = LoadCsvadditional((Path.Combine(ProcessingDirectory, "PERSONCLAIMDETAILS.CSV")));

            // Load mapping
            var mappings = LoadMapping(ConfigurationManager.AppSettings[MappingFile]);

            // Load XML template
            XDocument xmlTemplate = XDocument.Load(ConfigurationManager.AppSettings[XmlTemplate]);

            // Process template and get XML string
            string finalXmlString = ProcessTemplate(FileName, xmlTemplate, recordsA, mappings, controlFilePath);

            return finalXmlString;
        }


        static List<Dictionary<string, string>> LoadCsv(string[] headers, string[] rowData)
        {
            var records = new List<Dictionary<string, string>>();
            var record = new Dictionary<string, string>();

            for (int i = 0; i < headers.Length; i++)
            {
                record[headers[i]] = rowData.Length > i ? (rowData[i]?.ToString() ?? "") : "";
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



        static string ProcessTemplate(string FileName, XDocument template, List<Dictionary<string, string>> recordsA, Dictionary<string, (string Prefix, string XmlField)> mappings, string controlFilePath)
        {
            XElement Person = template.Root;
            if (Person == null) return "";

            var uid = recordsA.FirstOrDefault()?["UniqueExternalRef"] ?? "0";

            foreach (var recordA in recordsA)
            {
                // Ensure modifications are saved
                ProcessMappings(Person, recordA, mappings, controlFilePath, FileName, uid);

                // Process telephone addresses
                HandleTelephoneAddresses(template, recordA);
            }

            // Force a full re-evaluation of the XML
            XDocument updatedTemplate = new XDocument(template);

            return updatedTemplate.ToString();
        }

        private static void ProcessMappings(XElement Person, Dictionary<string, string> recordA, Dictionary<string, (string Prefix, string XmlField)> mappings, string controlFilePath, string FileName, string uid)
        {
            foreach (var kvp in recordA)
            {
                var relevantMappings = mappings
                    .Where(m => m.Value.XmlField.Split(new[] { "->" }, StringSplitOptions.None)[1].Trim()
                        .Equals(kvp.Key, StringComparison.OrdinalIgnoreCase))
                    .ToList();

                foreach (var mapping in relevantMappings)
                {
                    var xmlFieldParts = mapping.Key.Split('.');
                    if (xmlFieldParts.Length == 2 || xmlFieldParts.Length > 2) // Ensure nested fields are processed
                    {
                        ProcessXmlField(Person, xmlFieldParts, kvp, controlFilePath, FileName, uid);
                       
                    }
                }
            }
        }
        private static void ProcessXmlField(XElement Person, string[] xmlFieldParts, KeyValuePair<string, string> kvp, string controlFilePath, string FileName, string uid)
        {
            if (xmlFieldParts.Length < 2) return;

            var parentElementName = xmlFieldParts[0];
            var targetElement = Person.Descendants(parentElementName).FirstOrDefault();

            if (targetElement == null) return;

            // Specific handling for license-detail
            if (parentElementName == "short-term" && xmlFieldParts.Length > 2 && xmlFieldParts[1] == "license-detail")
            {
                var licenseDetailElement = targetElement.Descendants("license-detail").FirstOrDefault();
                if (licenseDetailElement != null)
                {
                    string finalName = xmlFieldParts[xmlFieldParts.Length - 1]
                        .Replace("@", "")
                        .Split('[')[0]
                        .Trim();

                  

                    licenseDetailElement.SetAttributeValue(finalName, kvp.Value);
                }
                return;
            }

            // Existing logic for other elements
            XElement currentElement = targetElement;
            for (int i = 1; i < xmlFieldParts.Length - 1; i++)
            {
                var elementName = xmlFieldParts[i];
                var childElement = currentElement.Element(elementName);
                if (childElement == null)
                {
                    childElement = new XElement(elementName);
                    currentElement.Add(childElement);
                }
                currentElement = childElement;
            }

            string finalName2 = xmlFieldParts[xmlFieldParts.Length - 1]
                .Replace("@", "")
                .Split('[')[0]
                .Trim();

            if (currentElement.Attribute(finalName2) != null)
            {
                currentElement.SetAttributeValue(finalName2, kvp.Value);
            }
            else
            {
                var subElement = currentElement.Element(finalName2);
                if (subElement == null)
                {
                    subElement = new XElement(finalName2);
                    currentElement.Add(subElement);
                }
                subElement.Value = kvp.Value;
            }
        }
        public static void HandleTelephoneAddresses(XDocument doc, Dictionary<string, string> csvData)
        {
            // Remove existing telephone addresses
            var existingTelAddresses = doc.XPathSelectElements("//address[@type-cd='TEL']").ToList();
            foreach (var addr in existingTelAddresses)
            {
                addr.Remove();
            }

            // Get the last address element to append after
            var addressElements = doc.XPathSelectElements("//address").ToList();
            if (!addressElements.Any()) return;  // Safety check

            var lastAddress = addressElements.Last();
            var parent = lastAddress.Parent;
            if (parent == null) return;  // Safety check

            // Counter for generating unique IDs
            int idCounter = 1;

            // Handle Mobile Phone
            if (csvData.TryGetValue("TelNoMobile", out string mobileNumber) &&
                !string.IsNullOrWhiteSpace(mobileNumber) &&
                IsValidPhoneNumber(mobileNumber))
            {
                var mobileElement = CreateTelephoneAddress(idCounter++, mobileNumber, isCellphone: true);
                parent.Add(mobileElement);
            }

            // Handle Home Phone (Residential)
            if (csvData.TryGetValue("TelNoHome", out string homeNumber) &&
                !string.IsNullOrWhiteSpace(homeNumber) &&
                IsValidPhoneNumber(homeNumber))
            {
                var homeElement = CreateTelephoneAddress(idCounter++, homeNumber, isResidential: true);
                parent.Add(homeElement);
            }

            // Handle Work Phone (Business)
            if (csvData.TryGetValue("TelNoWork", out string workNumber) &&
                !string.IsNullOrWhiteSpace(workNumber) &&
                IsValidPhoneNumber(workNumber))
            {
                var workElement = CreateTelephoneAddress(idCounter++, workNumber, isBusiness: true);
                parent.Add(workElement);
            }
        }

        private static XElement CreateTelephoneAddress(int id, string phoneNumber,
            bool isCellphone = false, bool isResidential = false, bool isBusiness = false)
        {
            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            string code = cleanNumber.Length >= 3 ? cleanNumber.Substring(0, 3) : "000";
            string number = cleanNumber.Length >= 3 ? cleanNumber.Substring(3) : cleanNumber;

            var element = new XElement("address",
                new XAttribute("ver", "2"),
                new XAttribute("type-cd", "TEL"),
                new XAttribute("code", code),
                new XAttribute("number", number),
                new XAttribute("is-cellphone", isCellphone ? "1" : "0"),
                new XAttribute("is-telephone", !isCellphone ? "1" : "0"),
                new XAttribute("is-fax", "0"));

            if (isResidential) element.Add(new XAttribute("is-residential", "1"));
            if (isBusiness) element.Add(new XAttribute("is-business", "1"));

            return element;
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return cleanNumber.Length >= 7;
        }
        static void LogMissingValue(string filePath, string recordType, string uid, string missingField)
        {
            string logEntry = $"{DateTime.Now}: Missing {missingField} in {recordType} for UID {uid}";
            File.AppendAllText(filePath, logEntry + Environment.NewLine);
        }

    }
}
