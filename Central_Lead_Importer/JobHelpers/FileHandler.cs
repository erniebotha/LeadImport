using leadimport.net.Dashboard_api;
using QuoteManager;
using System;
using System.Configuration;

namespace Central_Lead_Importer.JobHelpers
{
    public class SFTPFileHandler
    {
        public static void Step_One_DownloadFilesFromSFTP(string broker)
        {
            string host = ConfigurationManager.AppSettings[broker + ".SftpHost"];
            int port = int.Parse(ConfigurationManager.AppSettings[broker + ".SftpPort"]);
            string username = ConfigurationManager.AppSettings[broker + ".SftpUsername"];
            string password = ConfigurationManager.AppSettings[broker + ".SftpPassword"];
            string remoteDirectory = ConfigurationManager.AppSettings[broker + ".RemoteDirectory"];
            string localDirectory = ConfigurationManager.AppSettings[broker + ".LocalDirectory"];

            Console.WriteLine($"Step_One:  triggered for broker: {broker} at {DateTime.Now}");
            
            SftpClientHelper.SftpClientHelper.DownloadFilesFromSftp(host, port, username, password, remoteDirectory, localDirectory);
        }
    }

    public class FileMover
    {
        public static void Step_two_MoveToProcessing(string broker)
        {
            string sourceDir = ConfigurationManager.AppSettings[$"{broker}.LocalDirectory"];
            string targetDir = ConfigurationManager.AppSettings[$"{broker}.LocalProcessingDirectory"];

            // Check if the source and target directories are configured
            if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
            {
                Task_Api.LogError(DashboardConfig.TaskID, "Source or target directory not configured.");
                Console.WriteLine("Source or target directory not configured.");
                return; // Exit if directories are not configured
            }


            Console.WriteLine($"Step_two triggered for broker: {broker} at {DateTime.Now}");
            FileHandler.MoveToProcessing.MoveToProcessingFolder(sourceDir, targetDir);

        }
    
       public static void Step_two_MoveToArchive(string broker)
        {
            string sourceDir = ConfigurationManager.AppSettings[$"{broker}.LocalProcessingDirectory"];
            string targetDir = ConfigurationManager.AppSettings[$"{broker}.LocalArchiveDirectory"];

            // Check if the source and target directories are configured
            if (string.IsNullOrEmpty(sourceDir) || string.IsNullOrEmpty(targetDir))
            {
                Task_Api.LogError(DashboardConfig.TaskID, "Source or target directory not configured.");
                Console.WriteLine("Source or target directory not configured.");
                return; // Exit if directories are not configured
            }


            Console.WriteLine($"Step_four triggered for broker: {broker} at {DateTime.Now}");
            FileHandler.MoveToProcessing.MoveToProcessingFolder(sourceDir, targetDir);

        }
    }

    public class CsvReader
    {
        public static void Step_Three_Process_CSV_By_Line(string broker)
        {
            string LocalProcessingDirectory = ConfigurationManager.AppSettings[$"{broker}.LocalProcessingDirectory"];

           
            if (string.IsNullOrEmpty(LocalProcessingDirectory))
            {
                Task_Api.LogError(DashboardConfig.TaskID, "Source or target directory not configured.");

                return; // Exit if directories are not configured
            }

            Console.WriteLine($"Step_three triggered for broker: {broker} at {DateTime.Now}");
            QuoteManager.ProcessCSVFiles.StartProcess(LocalProcessingDirectory);



        }
    }

}
