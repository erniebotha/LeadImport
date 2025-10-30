using leadimport.net.Dashboard_api;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileHandler
{
    public class MoveToProcessing
    {
        public static void MoveToProcessingFolder(string sourceDir, string targetDir)
        {
          

            try
            {
                // Get the list of files in the source directory
                var files = Directory.GetFiles(sourceDir);

                // Check if any files are found
                if (files.Length > 0)
                {
                    MoveFiles(files, targetDir);
                }
                else
                {
                    Console.WriteLine("No files found in the source directory.");
                    return; // Exit if directories are not configured
                }

            }
            catch (Exception ex)
            {
                // Catch any exceptions and log the error message

                Task_Api.LogError(DashboardConfig.TaskID, $"Error in Task1: {ex.Message}");
                Console.WriteLine("Error in Task1: {ex.Message}");
                return; // Exit if directories are not configured
            }
        }

        public static void MoveFiles(string[] files, string targetDir)
        {
            foreach (var file in files)
            {
                var fileName = Path.GetFileName(file);
                var targetPath = Path.Combine(targetDir, fileName);

                try
                {
                    File.Move(file, targetPath);
                    Task_Api.LogNotification(DashboardConfig.TaskID, $"Moved file {fileName} to {targetPath}");
                }
                catch (Exception ex)
                {
                    Task_Api.LogNotification(DashboardConfig.TaskID, $"Error moving file {fileName}: {ex.Message}");
                    Console.WriteLine($"Error moving file {fileName}: {ex.Message}");
                    return; // Stop processing files if an error occurs
                }
            }
        }

    }
}
