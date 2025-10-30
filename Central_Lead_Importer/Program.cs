using Central_Lead_Importer.JobHelpers;
using DNS.Web.Question;
using leadimporter_M.BLL;
using Quartz;
using Quartz.Impl;
using System;
using System.Collections.Specialized;
using System.Configuration;
using System.Threading.Tasks;

namespace Central_Lead_Importer
{
    public class Program
    {
        public static async Task Main(string[] args)
        {

            CsvReader.Step_Three_Process_CSV_By_Line("Broker1");

            // 1. Create a scheduler factory and get a scheduler
            NameValueCollection props = new NameValueCollection
            {
                { "quartz.serializer.type", "binary" }
            };

            StdSchedulerFactory factory = new StdSchedulerFactory(props);
            IScheduler scheduler = await factory.GetScheduler();

            // Start the scheduler
            await scheduler.Start();

            // 2. Get broker configurations from App.config
            var appSettings = ConfigurationManager.AppSettings;

            foreach (string broker in appSettings.AllKeys)
            {
                if (broker.EndsWith(".Cron") && !broker.Equals("Shutdown.Cron", StringComparison.OrdinalIgnoreCase))

                {
                    string brokerName = broker.Replace(".Cron", "");
                    string cronExpression = appSettings[broker];

                    // Check if the broker is enabled
                    bool isEnabled = bool.Parse(appSettings[brokerName + ".Enabled"] ?? "false");

                    if (isEnabled)
                    {
                        // 3. Schedule job for enabled brokers only
                        await ScheduleBrokerJob(scheduler, brokerName, cronExpression);
                    }
                    else
                    {
                        Console.WriteLine($"Broker {brokerName} is disabled, skipping job scheduling.");
                    }
                }
            }
            // 4. Get shutdown cron expression from config
            string shutdownCronExpression = appSettings["Shutdown.Cron"];

            if (!string.IsNullOrWhiteSpace(shutdownCronExpression))
            {
                // 5. Schedule the shutdown job based on the cron expression from App.config
                await ScheduleShutdownJob(scheduler, shutdownCronExpression);
            }
            else
            {
                Console.WriteLine("Shutdown cron expression not found, skipping shutdown job scheduling.");
            }

            // Keep the console application running
            Console.WriteLine("Press any key to close the application manually, or it will shut down automatically based on the configured shutdown time...");
            Console.ReadKey();
        }

        
        
        // Function to schedule the job for a broker with a given cron expression
        public static async Task ScheduleBrokerJob(IScheduler scheduler, string broker, string cronExpression)
        {
            // Define the job and tie it to our BrokerJob class
            IJobDetail job = JobBuilder.Create<BrokerJob>()
                .WithIdentity(broker + "Job", "BrokersGroup")
                .UsingJobData("broker", broker) // Pass the broker name to the job
                .Build();

            // Create a trigger based on the cron expression
            ITrigger trigger = TriggerBuilder.Create()
                .WithIdentity(broker + "Trigger", "BrokersGroup")
                .WithCronSchedule(cronExpression)
                .ForJob(job)
                .Build();

            // Schedule the job using the trigger
            await scheduler.ScheduleJob(job, trigger);

            Console.WriteLine($"Scheduled job for {broker} with cron {cronExpression}");
        }

        
        
        // Function to schedule a shutdown job with the cron expression from the config
        public static async Task ScheduleShutdownJob(IScheduler scheduler, string cronExpression)
        {
            // Define the job and tie it to our ShutdownJob class
            IJobDetail shutdownJob = JobBuilder.Create<ShutdownJob>()
                .WithIdentity("ShutdownJob", "SystemGroup")
                .Build();

            // Create a trigger based on the cron expression from the config
            ITrigger shutdownTrigger = TriggerBuilder.Create()
                .WithIdentity("ShutdownTrigger", "SystemGroup")
                .WithCronSchedule(cronExpression) // Use the configured cron expression for shutdown
                .ForJob(shutdownJob)
                .Build();

            // Schedule the shutdown job
            await scheduler.ScheduleJob(shutdownJob, shutdownTrigger);

            Console.WriteLine($"Scheduled system shutdown job with cron {cronExpression}.");
        }
    }
}