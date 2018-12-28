using System;
using System.IO;
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
    public static class GetBillingCSVFile
    {
        private static byte[] getBlob(ref CloudStorageAccount account, string containerName, string blobName)
        {
            var blobClient = account.CreateCloudBlobClient();
            var container = blobClient.GetContainerReference(containerName);
            var blockBlob = container.GetBlockBlobReference(blobName);

            blockBlob.FetchAttributes();

            var fileByteLength = blockBlob.Properties.Length;

            var fileContent = new byte[fileByteLength];

            for (int i = 0; i < fileByteLength; i++)
            {
                fileContent[i] = 0x20;
            }

            blockBlob.DownloadToByteArray(fileContent, 0);
            
            return fileContent;
        }

        [FunctionName("GetBillingCSVFile")]
        public static async Task<HttpResponseMessage> Run([HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)]HttpRequestMessage req, TraceWriter log)
        {
            var result = new HttpResponseMessage(HttpStatusCode.OK);

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

            string blobFileName = req.GetQueryNameValuePairs()
                .FirstOrDefault(q => string.Compare(q.Key, "blobname", true) == 0)
                .Value;

            // Get request body
            dynamic data = await req.Content.ReadAsAsync<object>();

            var acct = new CloudStorageAccount(new Microsoft.WindowsAzure.Storage.Auth.StorageCredentials(storageAcccountName, storageAccountKey), endpointSuffix: "core.usgovcloudapi.net", useHttps: true);

            var file = getBlob(ref acct, storageContainerName, blobFileName);

            result.Content = new ByteArrayContent(file);
            result.Content.Headers.ContentDisposition = new System.Net.Http.Headers.ContentDispositionHeaderValue("attachment") { FileName = blobFileName };
            result.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/octet-stream");

            return result;
        }
    }
}
