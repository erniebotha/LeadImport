using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using System.Xml.XPath;

namespace QuoteManager.CategoryHelpers.Person
{
    public class PersonHelper
    {
        public static void HandleTelephoneAddresses(XDocument doc, Dictionary<string, string> csvData)
        {
            // Remove existing telephone addresses
            var existingTelAddresses = doc.XPathSelectElements("//address[@type-cd='TEL']").ToList();
            foreach (var addr in existingTelAddresses)
            {
                addr.Remove();
            }

            // Get the last address element to append after
            var lastAddress = doc.XPathSelectElements("//address").Last();
            var parent = lastAddress.Parent;

            // Counter for generating unique IDs
            int idCounter = 1;

            // Handle Mobile Phone
            if (csvData.TryGetValue("TelNoMobile", out string mobileNumber) &&
                !string.IsNullOrWhiteSpace(mobileNumber) &&
                IsValidPhoneNumber(mobileNumber))
            {
                var mobileElement = CreateTelephoneAddress(idCounter++, mobileNumber, isCellphone: true);
                parent.Add(mobileElement);
            }

            // Handle Home Phone (Residential)
            if (csvData.TryGetValue("TelNoHome", out string homeNumber) &&
                !string.IsNullOrWhiteSpace(homeNumber) &&
                IsValidPhoneNumber(homeNumber))
            {
                var homeElement = CreateTelephoneAddress(idCounter++, homeNumber, isResidential: true);
                parent.Add(homeElement);
            }

            // Handle Work Phone (Business)
            if (csvData.TryGetValue("TelNoWork", out string workNumber) &&
                !string.IsNullOrWhiteSpace(workNumber) &&
                IsValidPhoneNumber(workNumber))
            {
                var workElement = CreateTelephoneAddress(idCounter++, workNumber, isBusiness: true);
                parent.Add(workElement);
            }
        }

        private static XElement CreateTelephoneAddress(int id, string phoneNumber, bool isCellphone = false, bool isResidential = false, bool isBusiness = false)
        {
            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            string code = cleanNumber.Length >= 3 ? cleanNumber.Substring(0, 3) : "000";
            string number = cleanNumber.Length >= 3 ? cleanNumber.Substring(3) : cleanNumber;

            var element = new XElement("address",
                new XAttribute("ver", "2"),
                new XAttribute("type-cd", "TEL"),
                new XAttribute("code", code),
                new XAttribute("number", number),
                new XAttribute("is-cellphone", isCellphone ? "1" : "0"),
                new XAttribute("is-telephone", !isCellphone ? "1" : "0"),
                new XAttribute("is-fax", "0"));

            if (isResidential) element.Add(new XAttribute("is-residential", "1"));
            if (isBusiness) element.Add(new XAttribute("is-business", "1"));

            return element;
        }

        private static bool IsValidPhoneNumber(string phoneNumber)
        {
            string cleanNumber = new string(phoneNumber.Where(char.IsDigit).ToArray());
            return cleanNumber.Length >= 7;
        }

        public static void SetNonTelephoneAddressAttribute(XDocument doc, string csvHeader, string attributeName, string value)
        {
            XElement targetAddress = null;

            switch (csvHeader.ToLower())
            {
                // Physical address fields
                case "addressline1":
                case "code":
                case "suburb":
                case "residentialareatype":
                    targetAddress = doc.XPathSelectElement("//address[@type-cd='PHY']");
                    break;

                // Email address
                case "email":
                    targetAddress = doc.XPathSelectElement("//address[@type-cd='ELT']");
                    break;
            }

            // Set the attribute if we found a matching address element
            if (targetAddress != null && attributeName != null)
            {
                targetAddress.SetAttributeValue(attributeName, value);
            }
        }
    }
}
