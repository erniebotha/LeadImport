using System;
using System.Configuration;

namespace leadimporter_M.BLL
{
    public class Temp_PieterVoorbeeld
    {
        static string polHolXml = @"<sh BTS-is-new='1' id='2565686' name='W GRAHAM' is-person='1' is-organization='0' trust='0' stakeholder-type='39' is-insured='1' co-insured='0' private-or-org='1' sh-judgement='0' sh-sequestrated-liquidated='0' under_admin='0' broker-fee-consent='1' include-umbrella-liability='1' require-parms='0' BTS-category-id='194'> <person id='2565686' name='W GRAHAM' full-names='WALDO' surname='GRAHAM' initials='W' identification-no='8202065264081' title-cd='1' gender-cd='1' birth-date='06/02/1982' marital-status-cd='1' id-type='1' passport-number='8202065264081'/> <address id='20114960' ver='1' type-cd='PHY' line-1='2533 HAWTHORNE,10 ADMIN' code='0299' suburb='BOSCHDAL' residential-area-type='1' years-at-address='0' months-at-address='9'/> <short-term ver='1' license-endorsed='0' driver-restriction='3' held-insurance-last-39-days='1' claim-count-last-3-years-2='1' insurer-cancelled='0' period-comp-car-insurance='4' proof-of-ncb='1' add-personal-liability='0' has-consent='1' itc-check-done='0' policy-excess='4' current-premium='0.00' held-nonmotor-insurance-last-39-days='0' sau-policy-issued='0'> <license-detail ver='1' seq='3' license-date='12/04/2012' license-category='2' license-type='2' vehicle-restriction='3'/> <license-detail ver='1' seq='1' license-date='12/04/2012' license-category='1' license-type='4' vehicle-restriction='2'/> <claim ver='1' seq='1' date='06/08/2024' insurer='15' type='16' claim-status='2' claim-category='1' amount='20000.00'/> </short-term> <income-details id='2565686' occupation-category='0' occupation='0' employment-status='0' source-of-funds-income='0' occupation-category-code='0'/> <insurance-details/> <parameter-request ver='1' seq='1' insurer-parms='40'/> </sh>";
        static string quoteExtXml = @"<quote-extension BTS-category-id='230'  BTS-is-new='1' ><topup-ext id='{0}' ver='1' BTS-category-id='230' BTS-is-new='1'/></quote-extension>";
        static string vehXml = @"<motor-vehicle BTS-description='' BTS-end-date='100-01-01 00:00:00:00' BTS-start-date='100-01-01 00:00:00:00' BTS-status='Created' BTS-category-id='8' comp-excess-include='0' day-address-differ='no' opendriver='no' one-rand-item='0' metalic-paint='no' car-colour='black' manual.premium='0.0000' is.registered.sa='1' flat-excess='3000' include.tools='0' include-extended-cover='yes' excess-waiver-theft='0' excess-waiver='1' cover-type-short-term='0' tracking-is-proactive='1' alarm-by-vesa='no' is-commercial='0' description='0' tracking-installed='0' days-work-from-home='0' tracing-device='NONE' alarm-type-id='FFA1996' insured-value='0' quotation-basis='Retail' sum-insured='' taxi-excess-reducer-value='4000' vehicle-type='1' mm-code='22071251' model='FOCUS 1.5 TDCI 5DR' make='FORD' year='2017' vehicle-detail='1' item-refno='1' ver='1' id='1123' code.rebuild.vehicle='0' BTS-is-new='1'>

<short-term flat-excess='3000' include.tools='0' include-extended-cover='yes' excess-waiver='1' ver='1' incr-flat-excess='1' include-car-hire='yes' include-african-traveller='0' Include-glass-cover='no' include-hail-damage='no' access-control='0' manned-security='0' overnight-parking-type-locked='yes' overnight-parking-cd='01. Garage' use-type-id='02. Private / Social' cover-type='Comprehensive' ishp='no'/>

<sh-link ver='1' id='6505042735227' link-type-id='SHRiskAddr' seq='3'/>

<sh-link ver='1' id='2565686' link-type-id='RegDrv' seq='2'/>

<sh-link ver='1' id='2565686' link-type-id='RegOwn' seq='1'/>

<nsa description='nice fog lights' seq='1' amount='20000'/>

<nsa description='nice fog lights2' seq='2' amount='20000'/>

</motor-vehicle>";
         public static void Voorbeeld(string[] args)
        {

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString;
                string xmlPath = ConfigurationManager.AppSettings["XmlPath"];


                int brokerCode = 1147;//fsp code
                int categoryId = 1;//personal lines;

                var shared = new Shared.SharedCacheData();
                DNS.SqlPersist.SqlProperties.ConnectionString = connectionString;

                var dealHandler = new BtsProxy.DealHandlerProxy(shared);

                var sysUserPxy = new BtsProxy.SysUserProxy("Administrator", shared);

                var quotation = BtsProxy.QuotationProxy.CreateNewQuotation(sysUserPxy, categoryId, brokerCode);
                
                 quotation.CategoriesEx.PutItemXML(polHolXml);
                quotation.CategoriesEx.PutItemXML(string.Format(quoteExtXml, quotation.Id));
                quotation.CategoriesEx.PutItemXML(vehXml);

                Console.WriteLine($"Quote created:{quotation.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }


        public static void Addadditional(int quoteid)
        {

            try
            {
                string connectionString = ConfigurationManager.ConnectionStrings["dblive-db"].ConnectionString;
                string xmlPath = ConfigurationManager.AppSettings["XmlPath"];


              

              
                
                DNS.Web.BTS.Deal quotation = new DNS.Web.BTS.Quotation(quoteid, 1);
             
              
                quotation.CategoriesEx.PutItemXML(vehXml);

                Console.WriteLine($"Quote created:{quotation.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }

        }
    }
}

