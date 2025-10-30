using DNS.Web.Policy;
using System;
using System.Configuration;
using System.Data.SqlClient;
using System.Data;

namespace QuoteManager.BLL
{
    public class DNS_Quotation
        {
            public static int CreateQuotation(string transformedXml)
            {
                try
                {
                    string connectionString = ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString;
                    string xmlPath = ConfigurationManager.AppSettings["XmlPath"];
                    int brokerCode = 1147;
                    int categoryId = 1;

                    var shared = new Shared.SharedCacheData();
                    DNS.SqlPersist.SqlProperties.ConnectionString = connectionString;

                    var dealHandler = new BtsProxy.DealHandlerProxy(shared);
                    var sysUserPxy = new BtsProxy.SysUserProxy("Administrator", shared);
                    var quotation = BtsProxy.QuotationProxy.CreateNewQuotation(sysUserPxy, categoryId, brokerCode);
                
                    quotation.CategoriesEx.PutItemXML(transformedXml);
                    Console.WriteLine($"Quote created: {quotation.Id}");
                    return quotation.Id;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error creating quotation: {ex.Message}");
                    return 0;
                }
            }

            public static void Addadditional(int quoteid,string xml)
            {
                try 
                {
                string connectionString = ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString;
                string xmlPath = ConfigurationManager.AppSettings["XmlPath"];



                var shared = new Shared.SharedCacheData();
                DNS.SqlPersist.SqlProperties.ConnectionString = connectionString;

                var dealHandler = new BtsProxy.DealHandlerProxy(shared);

                var sysUserPxy = new BtsProxy.SysUserProxy("Administrator", shared);

                var quotation = BtsProxy.QuotationProxy.GetQuotation(quoteid,1, sysUserPxy);
            
                quotation.CategoriesEx.PutItemXML(xml);
          

            Console.WriteLine($" added :{quotation.Id}");
        }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

}

        public static void CreatePolicy ( int quotationId, string externalPolicyNo, int Offering_id)
        {
            string query = @"
                           UPDATE dblive.dbo.PolicyAgreement 
                           SET Quotation_Id = @QuotationId,
                                Offering_id =@Offering_id,
                               IsQuote = 0,
                               ExternalPolicyNo = @ExternalPolicyNo
                           WHERE Id = @QuotationId";

            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString))
            using (SqlCommand cmd = new SqlCommand(query, conn))
            {
                cmd.Parameters.Add("@QuotationId", SqlDbType.Int).Value = quotationId;
                cmd.Parameters.Add("@ExternalPolicyNo", SqlDbType.VarChar).Value = externalPolicyNo;
                cmd.Parameters.Add("@Offering_id", SqlDbType.Int).Value = Offering_id;

                conn.Open();
                cmd.ExecuteNonQuery();
            }

        }
    }
}
