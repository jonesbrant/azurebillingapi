using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;

namespace AzureBillingAPIFunction
{
    public static class GetUsageDetail
    {
        private static string apiuri = @"https://consumption.azure.com/v3/enrollments/{0}/usagedetailsbycustomdate?startTime={1}&endTime={1}";

        [FunctionName("GetUsageDetail")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("HTTP trigger function processed a request.");
            string jsonResponse = string.Empty;

            // parse query parameter
            string apikey = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "apikey", true) == 0)
                .Value;

            string enrollment = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "enrollment", true) == 0)
                .Value;

            string reportdate = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "reportdate", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            // Set variables to query string or body data
            apikey = apikey ?? data?.apikey;
            enrollment = enrollment ?? data?.enrollment;
            reportdate = reportdate ?? data?.reportdate;

            var apitoken = "bearer " + apikey;
            var apiurl = apiuri.Replace("{0}", enrollment).Replace("{1}", reportdate);

            // Call Azure Billing API
            var webRequest = WebRequest.Create(apiurl);
            webRequest.Method = "GET";
            webRequest.ContentType = "application/json";
            webRequest.Headers.Add("Authorization", apitoken);

            using (var s = webRequest.GetResponse().GetResponseStream())
            {
                using (var sr = new System.IO.StreamReader(s))
                {
                    jsonResponse = sr.ReadToEnd();
                }
            }

            return apikey == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, jsonResponse);
        }
    }
}
