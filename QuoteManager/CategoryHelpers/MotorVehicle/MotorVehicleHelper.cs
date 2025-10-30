using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace QuoteManager.CategoryHelpers.MotorVehicle
{
    public class MotorVehicleHelper
    {
        public static void MapMakeandModel(XDocument doc, Dictionary<string, string> csvData)
        {
         //   var motorVehicleElement = doc.Root.Element("motor-vehicle");
            var motorVehicleElement = doc.Descendants("motor-vehicle").FirstOrDefault();
            // Check if the element and mm-code attribute are present
            if (motorVehicleElement != null && motorVehicleElement.Attribute("mm-code") != null)
            {
                string mmCode = motorVehicleElement.Attribute("mm-code").Value;

                if (!string.IsNullOrEmpty(mmCode) && mmCode.Length >= 3)
                {
                    // Set the "make" to the first 3 characters of mm-code
                    motorVehicleElement.SetAttributeValue("make", mmCode.Substring(0, 3));

                    // Set the "model" to the full mm-code
                    motorVehicleElement.SetAttributeValue("model", mmCode);
                }
                else
                {
                    Console.WriteLine("Warning: mm-code attribute is missing or too short.");
                }
            }
            else
            {
                Console.WriteLine("Warning: motor-vehicle element or mm-code attribute not found.");
            }
        }
    }
}
