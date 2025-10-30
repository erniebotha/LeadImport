using System;
using System.Collections.Generic;
using System.IO;

namespace QuoteManager.BLL
{
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
  
}
