using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using lib_edi.Services.System.Net;
using lib_edi.Services.Ccdx;

namespace fa_ccdx_provider_varo
{
    public static class Provide
    {
        [FunctionName("publish-report-varo")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            try
            {
                log.LogInformation($"- [ccdx-provider->run]: Extracted logger type: http trigger function processed a publishing request.");

                // NHGH-2799 2022-02-09 1418 Using these temporary package related variables until requirements are defined
                string emdType = Environment.GetEnvironmentVariable("EMD_TYPE");
                string timeString = DateTime.Now.ToString("yyyyMMddHHmmss");
                string dateString = DateTime.Now.ToString("yyyy-MM-dd");
                
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                log.LogInformation($"- [ccdx-provider->run]: Retrieve base64 encoded content for publishing to ccdx.");

                string packageName = data?.name;
                string fullPackageName = $"{emdType}/{dateString}/{packageName}";
                string compressedContentBase64 = data?.content;
                byte[] contentBytes = Convert.FromBase64String(compressedContentBase64);
                MemoryStream stream = new MemoryStream(contentBytes);

                log.LogInformation($"- [ccdx-provider->run]: Build multipart form data data content.");

                MultipartFormDataContent multipartFormDataByteArrayContent = HttpService.BuildMultipartFormDataByteArrayContent(stream, "file", fullPackageName);
                log.LogInformation($"- [ccdx-provider->run]: Build ccdx request headers.");

                HttpRequestMessage requestMessage = CcdxService.BuildCcdxHttpMultipartFormDataRequestMessage(multipartFormDataByteArrayContent, fullPackageName, log);
                log.LogInformation($"- [ccdx-provider->run]: Send package to ccdx.");

                HttpStatusCode httpStatusCode = await HttpService.SendHttpRequestMessage(requestMessage);

                var returnObject = new { publishedPackage = fullPackageName };
                HttpResponseMessage httpResponseMessage;
                httpResponseMessage = new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(returnObject, Formatting.Indented), Encoding.UTF8, "application/json")
                };
                log.LogInformation($"- [ccdx-provider->run]: Sent successful http response.");

                return httpResponseMessage;

            }
            catch (Exception e)
            {
                log.LogError("Something went wrong while publishing the tarball to ccdx");
                log.LogError("Exception: " + e.Message);
                HttpResponseMessage httpResponseMessage = new()
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
                return httpResponseMessage;
            }
        }
    }
}
