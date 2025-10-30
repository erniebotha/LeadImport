using Central_Lead_Importer.JobHelpers;
using Quartz;
using QuoteManager;
using System;
using System.Configuration;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;

namespace Central_Lead_Importer
{
    /*  [DisallowConcurrentExecution]
     *  prevent a single job from running multiple times concurrently,
     *  even if it's triggered multiple times. However,
     *  this will not prevent two different jobs (in this case, one for each broker) from running
    */

    [DisallowConcurrentExecution] 
    public class BrokerJob : IJob
    {

        //here the steps are defined  each step logic can be found in the Jobhelpers folder in the filehandler class.  
        // besids the steps (process flow) there should not be any business logic code here.  
        public Task Execute(IJobExecutionContext context)
        {
            // Get the broker name
            string broker = context.JobDetail.JobDataMap.GetString("broker");

            // STEP 1: Download files from the SFTP server
              SFTPFileHandler.Step_One_DownloadFilesFromSFTP(broker);

            
           // STEP 2: Move files from Queued to processing folder
               FileMover.Step_two_MoveToProcessing(broker);


            // STEP 3 open the Person.CSV File read it line by line and Map to Myriad lookup Values  converts the new line to xml 
            

            CsvReader.Step_Three_Process_CSV_By_Line(broker);
               


            // STEP 4: 


            return Task.CompletedTask;
        }


       
    }
    
}


