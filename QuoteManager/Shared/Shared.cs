

using System;
using System.Data.SqlClient;
using System.IO;
using System.Xml;
using System.Configuration;

namespace Shared
{
    public class SharedCacheData : DNS.BTS.ISharedCacheData
    {
        private XmlDocument meadMcGrouter = new XmlDocument();
        private XmlDocument offeringMeta = new XmlDocument();
        private XmlDocument parsedOfferingMeta = new XmlDocument();
        private XmlDocument sqlXml = new XmlDocument();
        private XmlDocument roleRights = new XmlDocument();

        public string ConnectionString =>
            ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString;

        public string XmlPath =>
            ConfigurationManager.AppSettings["XmlPath"];

        public string FtsSystemPath => string.Empty;

        public string MQResultPath => string.Empty;

        public string MQSendPath => string.Empty;

        public XmlDocument MeadMcGrouther => meadMcGrouter;

        public XmlDocument OfferingMeta
        {
            get
            {
                if (!offeringMeta.HasChildNodes)
                {
                    offeringMeta.Load(Path.Combine(XmlPath, "OfferingMeta.xml"));
                }
                return offeringMeta;
            }
        }

        public XmlDocument ParcedOfferingMeta
        {
            get
            {
                if (!parsedOfferingMeta.HasChildNodes)
                {
                    parsedOfferingMeta.AppendChild(parsedOfferingMeta.CreateElement("offerings"));
                }
                return parsedOfferingMeta;
            }
        }

        public XmlDocument RoleRights
        {
            get
            {
                if (!roleRights.HasChildNodes)
                {
                    using (SqlConnection con = new SqlConnection(ConnectionString))
                    {
                        string xml = DNS.SqlPersist.SqlHelperClass.XmlRunProcedure("xmlGetRoleRights", null, con, null, 0);
                        roleRights.LoadXml("<roles>" + xml + "</roles>");
                    }
                }
                return roleRights;
            }
        }

        public XmlDocument SqlXml
        {
            get
            {
                if (!sqlXml.HasChildNodes)
                {
                    sqlXml.Load(Path.Combine(XmlPath, "sql-xml.xml"));
                }
                return sqlXml;
            }
        }

        public DNS.BTS.SystemType Type => DNS.BTS.SystemType.FSPSolutions;

        public string WebServiceUrl => string.Empty;
    }
}


