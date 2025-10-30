using System;
using System.Configuration;

namespace leadimport.net.Dashboard_api
{
    public static class DashboardConfig
    {
        public static int TaskID
        {
            get
            {
                // Retrieve the TaskID from the app.config using ConfigurationManager
                var taskID = ConfigurationManager.AppSettings["TaskID"];

                if (string.IsNullOrEmpty(taskID))
                    throw new InvalidOperationException("TaskID is missing from your app.config.");

                return int.Parse(taskID);
            }
        }
    }
}

