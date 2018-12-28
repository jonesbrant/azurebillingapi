using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.WindowsAzure.Storage;

namespace AzureBillingAPIFunction
{
    public static class GetUsageDetail
    {
        private static string apiuri = @"https://consumption.azure.com/v3/enrollments/{0}/usagedetailsbycustomdate?startTime={1}&endTime={1}";

        private static bool storeBlob(ref CloudStorageAccount account, string containerName, string blobName, string json)
        {
            try
            {
                var blobClient = account.CreateCloudBlobClient();
                var container = blobClient.GetContainerReference(containerName);
                var blockBlob = container.GetBlockBlobReference(blobName);
                //jsonString = "{\"a\":" + jsonString.TrimEnd('\r', '\n') + "}";
                blockBlob.UploadText(json);
                return true;
            }
            catch (Exception ex)
            {
                return false;
            }
        }

        [FunctionName("GetUsageDetail")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            log.Info("HTTP trigger function processed a request.");
            
            string jsonResponse = string.Empty;

            // parse query parameter
            string storageAccountKey = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "storageaccountkey", true) == 0)
                .Value;

            string storageAcccountName = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "storageaccountname", true) == 0)
                .Value;

            string storageContainerName = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "storageaccountcontainer", true) == 0)
                .Value;

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

            var acct = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageAcccountName, storageAccountKey), endpointSuffix: "core.usgovcloudapi.net", useHttps: true);

            storeBlob(ref acct, storageContainerName, "json" + reportdate + ".json", jsonResponse);

            return apikey == null
                ? req.CreateResponse(HttpStatusCode.BadRequest, "Please pass a name on the query string or in the request body")
                : req.CreateResponse(HttpStatusCode.OK, jsonResponse);
        }
    }
}
