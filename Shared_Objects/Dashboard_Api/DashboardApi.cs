using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace leadimport.net.Dashboard_api
{
    public class Task_Api
    {
        public class InsertTaskLog_Propperties
        {
            public int Task_ID { get; set; }
            public string Message { get; set; } // Removed nullable annotation for C# 7.3
            public int Message_ID { get; set; }
        }

        public static void Start_Process(int taskid, string message, int messageid)
        {
            var TaskLogModel = new InsertTaskLog_Propperties()
            {
                Task_ID = taskid,
                Message = message,  // Ensure you handle null values here if necessary
                Message_ID = messageid
            };

            // Since C# 7.3 doesn't support 'async Main', we handle async synchronously here
            RunAsync(TaskLogModel).GetAwaiter().GetResult(); // Synchronously wait for the async task to complete
        }

        static async Task RunAsync(InsertTaskLog_Propperties TaskLogModel)
        {
            using (var client = new HttpClient())
            {
                client.BaseAddress = new Uri("http://192.168.200.57:2019/");
                client.DefaultRequestHeaders.Accept.Clear();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                // Manually serialize the object to JSON
                string jsonContent = JsonConvert.SerializeObject(TaskLogModel);
                var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

                // HTTP POST
                HttpResponseMessage response = await client.PostAsync("api/Task/InsertLog", content);

                // Handle success or error response (you can uncomment the logic if needed)
                // if (response.IsSuccessStatusCode)
                // {
                //     Console.WriteLine("Log inserted successfully.");
                // }
                // else
                // {
                //     Console.WriteLine("Could not call the API to insert the log details.");
                // }
            }
        }

        // Methods for logging different types of messages
        public static void LogNotification(int taskid, string message)
        {
            int LogType = 8;
            Start_Process(taskid, message, LogType);
        }

        public static void LogError(int taskid, string message)
        {
            int LogType = 2;
            Start_Process(taskid, message, LogType);
        }

        public static void LogSuccess(int taskid, string message)
        {
            int LogType = 3;
            Start_Process(taskid, message, LogType);
        }
    }
}
