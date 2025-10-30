using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Central_Lead_Importer
{
    public class ShutdownJob : IJob
    {
        public Task Execute(IJobExecutionContext context)
        {
            Console.WriteLine("Shutting down the application based on the configured cron time...");

            // Shut down the application gracefully
            Environment.Exit(0);  // Exits the application

            return Task.CompletedTask;
        }
    }
}
