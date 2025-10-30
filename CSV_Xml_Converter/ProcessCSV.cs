using CSV_Xml_Converter;
using leadimport.net.Dashboard_api;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProcessCSV
{
    //public class ProcessCSV
    //{
    //    public static string ReadCSV(string targetDir)
    //    {
    //        string csvFilePath = targetDir;
    //        string mappingsFilePath = @"path\to\your\mappings.json";
    //        string xmlTemplatePath = @"path\to\your\template.xml";

    //        // Read mappings from JSON file
    //        var mappings = ReadMappings(mappingsFilePath);

    //        // Read CSV data
    //        var records = ReadCsv(csvFilePath);

    //        // Generate XML based on the CSV data, mappings, and template
    //        var xmlOutput = GenerateXml(records, mappings, xmlTemplatePath);

    //        // Save the output to an XML file
    //        File.WriteAllText(@"path\to\output.xml", xmlOutput);
    //    }

    //    static Dictionary<string, Dictionary<string, string>> ReadMappings(string path)
    //    {
    //        string jsonContent = File.ReadAllText(path);
    //        return JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, string>>>(jsonContent);
    //    }

    //    static List<dynamic> ReadCsv(string path)
    //    {
    //        using (var reader = new StreamReader(path))
    //        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
    //        {
    //            return csv.GetRecords<dynamic>().ToList();
    //        }
    //    }

    //    static string GenerateXml(List<dynamic> records, Dictionary<string, Dictionary<string, string>> mappings, string templatePath)
    //    {
    //        // Load the XML template
    //        string templateContent = File.ReadAllText(templatePath);
    //        var template = Template.Parse(templateContent);

    //        // Create a list to hold the rendered XML for each record
    //        List<string> renderedXmlList = new List<string>();

    //        // Loop through each CSV record
    //        foreach (var record in records)
    //        {
    //            // Prepare a dictionary with the mapped values from the CSV
    //            var mappedValues = new Dictionary<string, string>();

    //            foreach (var mapping in mappings["csvToXml"])
    //            {
    //                mappedValues[mapping.Key] = record[mapping.Value];
    //            }

    //            // Render the XML using the template and the mapped values
    //            var renderedXml = template.Render(mappedValues);
    //            renderedXmlList.Add(renderedXml);
    //        }

    //        // Join all rendered XMLs (if multiple records) into one string
    //        return string.Join(Environment.NewLine, renderedXmlList);
    //    }

    //    static string MapValue(string value, Dictionary<string, string> mapping)
    //    {
    //        return mapping.ContainsKey(value.ToLower()) ? mapping[value.ToLower()] : value;
    //    }

    //}





    }

