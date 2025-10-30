using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace QuoteManager.Helper
{
    public class Configuration_Helper
    {
        // Helper method to get a configuration path by key
        public static string GetConfigPath(string configKey)
        {
            return ConfigurationManager.AppSettings[configKey];
        }
    }
}
