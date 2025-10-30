using leadimport.net.Dashboard_api;
using QuoteManager.BLL;
using QuoteManager.Helper;
using QuoteManager.Templates.All_Risks;
using QuoteManager.Templates.Buildings;
using QuoteManager.Templates.Caravan;
using QuoteManager.Templates.HouseHoldContents;
using QuoteManager.Templates.MotorCycles;
using QuoteManager.Templates.Motorvehicle;
using QuoteManager.Templates.Person;
using QuoteManager.Templates.Trailer;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;

namespace QuoteManager
{
    public class ProcessCSVFiles
    {
        public static void StartProcess(string ProcessingDirectory)
        {
            // Initialize quote ID to track the created quotation
            int Quoteid = 0;

            try
            {
                // Check if the PERSON.csv file exists
                if (!File.Exists(Path.Combine(ProcessingDirectory, "PERSON.csv")))
                {
                    Console.WriteLine("PERSON.csv file not found.");
                    return;
                }

                // Open a StreamReader to read the contents of the PERSON.csv file
                using (var reader = new StreamReader(Path.Combine(ProcessingDirectory, "PERSON.csv")))
                {
                    string line;                    // Variable to store each line read from the file
                    int lineNumber = 0;             // Keep track of which line we're currently processing
                    string[] headers = null;        // Array to store column headers
                    int uniqueRefIndex = -1;        // Will store the index of the UniqueExternalRef column

                    // Read the file line by line until the end
                    while ((line = reader.ReadLine()) != null)
                    {
                        string uniqueRef = string.Empty;
                        lineNumber++;  // Increment line number for each new line

                        // Process the header row (first line)
                        if (lineNumber == 1)
                        {
                            // Split the first line into an array of column names
                            headers = line.Split(',');

                            // Find the index of the "UniqueExternalRef" column 
                            // Use OrdinalIgnoreCase to make the search case-insensitive
                            uniqueRefIndex = Array.FindIndex(headers, h => h.Equals("UniqueExternalRef", StringComparison.OrdinalIgnoreCase));

                            // Validate that the UniqueExternalRef column exists
                            if (uniqueRefIndex == -1)
                            {
                                Console.WriteLine("UniqueExternalRef column not found in PERSON.csv.");
                                return;
                            }
                            continue;  // Skip to next iteration to process data rows
                        }

                        // Split the current line into an array of values
                        string[] values = line.Split(',');

                        // Extract the UniqueExternalRef value using the index found earlier
                        uniqueRef = values[uniqueRefIndex];

                        // Print the UniqueExternalRef for tracking/debugging
                        Console.WriteLine($"Processing UniqueExternalRef: {uniqueRef}");

                        // Convert the current record to XML using a mapping configuration
                        string initialXmlOutput = Process_Person_StartProcess.StartProcess(ProcessingDirectory, "PERSON.csv", "mappingFilePath", "PersonXmlTemplatePath", headers, values);

                        // Transform the XML using FSP Value Mapping
                        string transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath("FSP_Value_Mapper_Path"), uniqueRef, initialXmlOutput);

                        // Skip if transformation fails
                        if (transformedXml == null)
                        {
                            Console.WriteLine("Skipping quotation creation due to unmatched entries in the PERSON.csv file transformation.");
                            continue;
                        }

                        // Create a quotation using the transformed XML
                        Quoteid =DNS_Quotation.CreateQuotation(transformedXml);
                        Console.WriteLine(Quoteid);
                        // If quotation is successfully created, process associated files
                        if (Quoteid != 0)
                        {
                            transformedXml = string.Empty;

                            // Find and process associated records from other files (Motor, Trailers, etc.)
                            var matchingRecordsWithDetails = ProcessAssociatedFiles(ProcessingDirectory, uniqueRef);

                            // Process each matching record from associated files
                            foreach (var (record, associatedHeaders, mapping, xmlTemplatePath, fspMapper, FileName) in matchingRecordsWithDetails)
                            {

                                if (mapping == "MotorMapping" & Quoteid != 0)
                                {
                                    string xml = Process_Motorvehicle_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);


                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);

                                }

                                else if (mapping == "TrailerMapping" & Quoteid != 0)
                                {
                                    string xml = Process_Trailer_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);

                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }

                                else if(mapping == "CaravanMapping" & Quoteid != 0)
                                {
                                    string xml = Process_Caravan_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);

                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }

                                else if (mapping == "AllRiskMapping" & Quoteid != 0)
                                {
                                    string xml = Process_AllRisk_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);

                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }

                                else if(mapping == "BuildingsMapping" & Quoteid != 0)
                                {
                                    string xml = Process_Building_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);


                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }

                               else if (mapping == "HouseHoldContentMapping" & Quoteid != 0)
                                {
                                    string xml = Process_HouseHoldContent_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);

                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }

                                else if(mapping == "MotorcycleMapping" & Quoteid != 0)
                                {
                                    string xml = Process_MotorCycle_StartProcess.StartProcess(ProcessingDirectory, mapping, xmlTemplatePath, associatedHeaders, record);
                                    transformedXml = MapXmlValuesToFSP.TransformXml(Configuration_Helper.GetConfigPath(fspMapper), uniqueRef.ToString(), xml);

                                    // Add additional information to the existing quotation
                                    DNS_Quotation.Addadditional(Quoteid, transformedXml);
                                }
                            }
                        }

                        DNS_Quotation.CreatePolicy(Quoteid ,uniqueRef, 1494);






                    }
                }
            }
            catch (Exception ex)
            {
                // Log any errors that occur during the processing
                Task_Api.LogError(DashboardConfig.TaskID, $"Error in StartProcess: {ex.Message}");
            }
        }

        #region ProcessAssociatedFiles returns record and header

        // Returns a list of matching records along with relevant metadata (headers, mapping, template, and mapper).
        private static List<(string Record, string[] Headers, string Mapping, string XmlTemplate, string FSPMapper, string FileName)> ProcessAssociatedFiles(string directory, string uniqueRef, IEnumerable<dynamic> specificFileMappings = null)
        {
            // Step 1: Define default mappings for files. Each mapping specifies:
            // - The file name to look for
            // - The name of the mapping logic to use
            // - The template path for XML generation
            // - The name of the mapper used for FSP (File-Specific Processing) values
            var defaultFileMappings = new[]
            {


          //new  { FileName = "PERSON.csv",          Mapping = "mappingFilePath",            XmlTemplate = "PersonXmlTemplatePath",          FSPMapper = "FSP_Value_Mapper_Path" },
          new  { FileName = "MOTORVEHICLE.csv",    Mapping = "MotorMapping",               XmlTemplate = "MotorXmlTemplatePath",           FSPMapper = "FSP_Values_Motor_Mapper" },
          new  { FileName = "TRAILERS.csv",        Mapping = "TrailerMapping",             XmlTemplate = "TrailerXmlTemplatePath",         FSPMapper = "FSP_Values_Trailer_Mapper" },
          new  { FileName = "CARAVANS.csv",        Mapping = "CaravanMapping",             XmlTemplate = "CaravanXmlTemplatePath",         FSPMapper = "FSP_Values_Caravan_Mapper" },
          new  { FileName = "ALLRISKS.csv",        Mapping = "AllRiskMapping",             XmlTemplate = "AllRiskXmlTemplatePath",         FSPMapper = "FSP_Values_AllRisks_Mapper" },
          new  { FileName = "BUILDINGS.csv",       Mapping = "BuildingsMapping",           XmlTemplate = "Buildings_XML_TemplatePath",     FSPMapper = "FSP_Values_Buildings_Mapper" },
          new  { FileName = "HOUSHOLDCONTENTS.csv",Mapping = "HouseHoldContentMapping",    XmlTemplate = "HouseHoldContentTemplatePath",   FSPMapper = "FSP_Values_HouseHoldContent_Mapper" },
          new  { FileName = "MOTORCYCLES.CSV",     Mapping = "MotorcycleMapping",          XmlTemplate = "MotorCycle_TemplatePath",        FSPMapper = "FSP_Values_MotorCycle_Mapper" }

            };

            // Step 2: Use provided file mappings if they are specified; otherwise, use the default mappings.
            var fileMappings = specificFileMappings ?? defaultFileMappings;

            // Step 3: Prepare a list to hold all matching records and their associated metadata.
            var matchingRecords = new List<(string Record, string[] Headers, string Mapping, string XmlTemplate, string FSPMapper, string FileName)>();

            // Step 4: Process each file mapping in the list.
            foreach (var mapping in fileMappings)
            {
                // Construct the full file path for the current file.
                string filePath = Path.Combine(directory, mapping.FileName);

                try
                {
                    // Step 5: Attempt to find records in the file that match the given unique reference.
                    // `FindMatchingRecordsWithHeaders` is assumed to return:
                    // - `headers`: An array of column headers in the file
                    // - `records`: A list of records that match the unique reference
                    var (headers, records) = FindMatchingRecordsWithHeaders(uniqueRef, filePath);

                    // Step 6: If any matching records are found, process them.
                    if (records.Any())
                    {
                        // Add each matching record to the list along with its metadata (headers, mapping, etc.).
                        foreach (var record in records)
                        {
                            matchingRecords.Add((record, headers, mapping.Mapping, mapping.XmlTemplate, mapping.FSPMapper, mapping.FileName));
                        }
                    }
                    else
                    {
                        // If no matching records are found, log this information.
                        Console.WriteLine($"No matching records found in {mapping.FileName}.");
                    }
                }
                catch (Exception ex)
                {
                    // Step 7: If an error occurs while processing the file, log the error message.
                    Console.WriteLine($"Error processing file {mapping.FileName}: {ex.Message}");
                }
            }

            // Step 8: Return the list of all matching records with their associated metadata.
            return matchingRecords;
        }

        private static (string[] Headers, IEnumerable<string> MatchingRecords) FindMatchingRecordsWithHeaders(string uniqueRef, string CSVDataFilePath)
        {
            string[] headers = null;
            var matchingRecords = new List<string>();

            try
            {
                // Retrieve the headers from the file.
                headers = CSVData.GetHeaders(CSVDataFilePath);

                // Retrieve all the remaining lines (excluding the headers).
                IEnumerable<string> CSVDataRows = CSVData.GetLines(CSVDataFilePath);

                foreach (var CSVDataRow in CSVDataRows)
                {
                    // Split the line into individual values based on the CSV delimiter.
                    string[] values = CSVDataRow.Split(',');

                    // Check if the first value (assumed to be the unique reference) matches the provided uniqueRef.
                    if (values.Length > 0 && values[0].Trim().Equals(uniqueRef, StringComparison.OrdinalIgnoreCase))
                    {
                        // Add the matching record to the list.
                        matchingRecords.Add(CSVDataRow);

                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }

            // Return both the headers and the matching records as a tuple.
            return (headers, matchingRecords);
        }

        public class CSVData
        {
            public static string[] GetHeaders(string filePath)
            {
                if (!System.IO.File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {Path.GetFileName(filePath)}");

                using (var reader = new StreamReader(filePath))
                {
                    string firstLine = reader.ReadLine();
                    if (firstLine == null)
                        throw new InvalidOperationException("The file is empty.");

                    return firstLine.Split(',');
                }
            }

            public static IEnumerable<string> GetLines(string filePath)
            {
                if (!System.IO.File.Exists(filePath))
                    throw new FileNotFoundException($"File not found: {Path.GetFileName(filePath)}");

                using (var reader = new StreamReader(filePath))
                {
                    string line;
                    // Skip the first line (headers)
                    reader.ReadLine();
                    while ((line = reader.ReadLine()) != null)
                    {
                        yield return line;
                    }
                }
            }
        }

        #endregion
    }
}
