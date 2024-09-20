using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Net.Http;
using System.Net;
using System.Text;
using lib_edi.Services.System.Net;
using lib_edi.Services.Ccdx;
using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
using lib_edi.Models.Enums.Azure.AppInsights;
using Microsoft.Azure.Functions.Worker;

namespace fa_ccdx_provider_varo
{
    public class Provide
    {
        private readonly ILogger<Provide> _logger;

        public Provide(ILogger<Provide> logger)
        {
            _logger = logger;
        }

        [Function("publish-report-varo")]
        public async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req)
        {
            string logPrefix = "- [ccdx-provider-varo->run]:";
            string packageName = null;
            byte[] contentBytes = null;
			try
            {
                _logger.LogInformation($"{logPrefix} Extracted logger type: http trigger function processed a publishing request.");

                // NHGH-2799 2022-02-09 1418 Using these temporary package related variables until requirements are defined
                string emdType = Environment.GetEnvironmentVariable("EMD_TYPE");
                string timeString = DateTime.Now.ToString("yyyyMMddHHmmss");
                string dateString = DateTime.Now.ToString("yyyy-MM-dd");
                
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                _logger.LogInformation($"{logPrefix} Retrieve base64 encoded content for publishing to ccdx.");

                packageName = data?.name;
                _logger.LogInformation($"{logPrefix} Start processing report package {packageName}");
                _logger.LogInformation($"{logPrefix} Track ccdx provider started event (app insights)");
				CcdxService.LogCcdxProviderStartedEventToAppInsights(packageName, PipelineStageEnum.Name.CCDX_PROVIDER_VARO, _logger);
				string fullPackageName = $"{emdType}/{dateString}/{packageName}";
                string compressedContentBase64 = data?.content;
                contentBytes = Convert.FromBase64String(compressedContentBase64);
                MemoryStream stream = new MemoryStream(contentBytes);

                _logger.LogInformation($"{logPrefix}: Build multipart form data data content.");
                MultipartFormDataContent multipartFormDataByteArrayContent = await HttpService.BuildMultipartFormDataByteArrayContent(stream, "file", fullPackageName);

                _logger.LogInformation($"{logPrefix}: Build interchange request headers.");
                HttpRequestMessage requestMessage = CcdxService.BuildCcdxHttpMultipartFormDataRequestMessage(multipartFormDataByteArrayContent, fullPackageName, _logger);

                _logger.LogInformation($"{logPrefix}: Sending package into the interchange.");
				//HttpStatusCode httpStatusCode = HttpStatusCode.BadGateway;
				HttpStatusCode httpStatusCode = await HttpService.SendHttpRequestMessage(requestMessage);

				if (httpStatusCode == HttpStatusCode.OK )
                {
                    // NHGH-414 2021.09.21 
                    // Message got put on a highly durable topic. A 200 indicates successful entry into the data interchange. 
                    // However, the consumer downstream might not be able to handle a message of that size. 
                    // Consumer would own decision to reconfigure consumer to handle larger payload or jost not accept larger payloads.
                    _logger.LogInformation($"{logPrefix} Entry into the data interchange was successful");
                    _logger.LogInformation($"{logPrefix} Track ccdx provider success event (app insights)");
					CcdxService.LogCcdxProviderSuccessEventToAppInsights(packageName, PipelineStageEnum.Name.CCDX_PROVIDER_VARO, _logger);
                    _logger.LogInformation($"{logPrefix} DONE");
                    _logger.LogInformation($"{logPrefix} Successful entry into the interchange for report package {packageName}");
				} else
                {
					string errorCode = "NAJW";
                    _logger.LogError($"{logPrefix} Received http error {httpStatusCode} while uploading {packageName} to the interchange");
                    _logger.LogInformation($"{logPrefix} Track ccdx provider failed event (app insights)");
					CcdxService.LogCcdxProviderFailedEventToAppInsights(packageName, PipelineStageEnum.Name.CCDX_PROVIDER_VARO, _logger);
                    _logger.LogInformation($"{logPrefix} Log error message");
					string storageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
					string ccdxEndpoint = Environment.GetEnvironmentVariable("CCDX_HEADERS:MULTIPART_FORM_DATA_FILE_ENDPOINT");
					string errorString = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(httpStatusCode.ToString(), packageName, ccdxEndpoint));
                    _logger.LogError($"{logPrefix} Message: {errorString}");
					string blobContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING");

                    // Scope of holding container: To be used only for situations with CCDX transmission.
                    _logger.LogInformation($"{logPrefix} Move failed telemetry file {packageName} to holding container {blobContainerName} for further investigation");
					// EDI architecture: "Container where files are placed when there is a problem sending to CCDX via the provider. Examples 
					// includes files that are too large (>5MB), or when the backend has a problem such as Kafka brokers unavailable. This 
					// condition is typically indicated by a 5xx HTTP response from the CCDX POST by the provider. The purpose of this container 
					// is to allow for further analysis and troubleshooting. No retry logic is currently implemented for files in this container, 
					// but may be added in the future."
					await AzureStorageBlobService.UploadBlobToContainerUsingSdk(contentBytes, storageAccountConnectionString, blobContainerName, packageName);
                    _logger.LogInformation($"{logPrefix} Confirmed. Telemetry file {packageName} moved to container {blobContainerName}");
                    _logger.LogInformation($"{logPrefix} DONE");
				}

				var returnObject = new { publishedPackage = fullPackageName };
                HttpResponseMessage httpResponseMessage;
                httpResponseMessage = new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent(JsonConvert.SerializeObject(returnObject, Formatting.Indented), Encoding.UTF8, "application/json")
                };
                _logger.LogInformation($"{logPrefix}: Package successfully entered into the interchange.");

                return httpResponseMessage;

            }
            catch (Exception e)
            {
                _logger.LogError($"{logPrefix} Something went wrong while publishing the tarball to ccdx");
                _logger.LogError($"{logPrefix} Exception  : " + e.Message);
				string errorCode = "IWNE";
                _logger.LogError($"{logPrefix} error code : " + errorCode);
				string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(packageName));
                _logger.LogError($"{logPrefix} Track ccdx provider unexpected error event (app insights)");
                _logger.LogError($"An exception was thrown publishing {packageName} to ccdx: {e.Message} ({errorCode})");
				CcdxService.LogCcdxProviderErrorEventToAppInsights(packageName, PipelineStageEnum.Name.CCDX_PROVIDER_VARO, _logger, e, errorCode);
                _logger.LogError(e, errorMessage);

                HttpResponseMessage httpResponseMessage = new()
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
                return httpResponseMessage;
            }
        }
    }
}
