using Renci.SshNet;
using System;
using System.IO;

namespace SftpClientHelper
{
    public class SftpClientHelper
    {
        public static void DownloadFilesFromSftp(string host, int port, string username, string password, string remoteDirectory, string localDirectory)
        {
            // Create the local directory if it doesn't exist
            if (!Directory.Exists(localDirectory))
            {
                Directory.CreateDirectory(localDirectory);
            }

            using (var sftp = new SftpClient(host, port, username, password))
            {
                try
                {
                    sftp.Connect();
                    Console.WriteLine($"Connected to SFTP: {host}");

                    // List all files in the remote directory
                    var files = sftp.ListDirectory(remoteDirectory);
                    foreach (var file in files)
                    {
                        if (!file.IsDirectory && !file.Name.StartsWith("."))
                        {
                            // Download each file to the local directory
                            string localFilePath = Path.Combine(localDirectory, file.Name);
                            using (Stream fileStream = File.OpenWrite(localFilePath))
                            {
                                sftp.DownloadFile(file.FullName, fileStream);
                            }
                            Console.WriteLine($"Downloaded file: {file.Name} to {localFilePath}");
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error: {ex.Message}");
                }
                finally
                {
                    sftp.Disconnect();
                }
            }
        }
    }
}

