using System;
using System.IO;
using System.Net;
using System.Net.Http;
using lib_edi.Models.Dto.Ccdx;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.System.IO;
using lib_edi.Services.System.Net;
using lib_edi.Services.Errors;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace fa_ccdx_provider
{
    /// <summary>
    /// A telemetry provider that sends telemetry message into the data interchange (CCDX) system.
    /// </summary>
    /// <remarks>
    /// Telemetry Providers represent the source of cold chain telemetry data. The terms report, 
    /// message, and event are used interchangeably to mean a package of data. 
    /// </remarks>
    public static class Provide
    {
        /// <summary>
        /// Blob triggered azure function that uploads compressed cold chain telemetry files into the cold 
        /// chain data interchange (CCDX) using a multipart HTTP post request API endpoint. 
        /// </summary>
        /// <param name="ccBlobInput">Stream of bytes of a compressed cold chain telemetry blob</param>
        /// <param name="ccBlobInputName">Name of compressed cold chain telemetry blob</param>
        /// <remarks>
        /// - Telemetry files are uploaded to a CCDX multipart/form-data http endpoint as a byte array. 
        /// - Headers are used to route the report to the CCDX data consumer specified by the Data Owner. 
        /// - Only compressed USBDG telemetry data are currently supported.
        [FunctionName("ccdx-provider")]
        public static async Task Run(
            [BlobTrigger("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT%/{ccBlobInputName}", Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")]
            Stream ccBlobInput,
            string ccBlobInputName,
            ILogger log)
        {
            string reportFileName = null;
            try
            {
                log.LogInformation($"- [ccdx-provider->run]: Received telemetry file {ccBlobInputName}");
                reportFileName = Path.GetFileName(ccBlobInputName);
                log.LogInformation($"- [ccdx-provider->run]: Track ccdx provider started event (app insights)");
                CcdxService.LogCcdxProviderStartedEventToAppInsights(reportFileName, log);
                log.LogInformation($"- [ccdx-provider->run]: Validate incoming blob originated from supported data logger");
                if (CcdxService.ValidateCETypeHeaderUsingBlobPath(ccBlobInputName))
                {
                    log.LogInformation($"- [ccdx-provider->run]: Confirmed. Blob originated from supported data logger");
                    var sr = new StreamReader(ccBlobInput);
                    var body = await sr.ReadToEndAsync();
                    string storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                    string storageConnectionStringConfig = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                    string inputContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT");
                    string blobContainerNameConfig = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_CONFIG");
                    string fileCcdxPublisherSampleHeaderValues = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_SAMPLE_VALUES_FILENAME");

                    log.LogInformation($"- [ccdx-provider->run]: Retrieve sample ccdx provider metadata headers from blob storage");
                    string blobText = await AzureStorageBlobService.DownloadBlobTextAsync(storageConnectionStringConfig, blobContainerNameConfig, fileCcdxPublisherSampleHeaderValues);
                    CcdxProviderSampleHeadersDto sampleHeaders = JsonConvert.DeserializeObject<CcdxProviderSampleHeadersDto>(blobText);

                    log.LogInformation($"- [ccdx-provider->run]: Prepare ccdx provider http request with multipart content");
                    string ccdxHttpEndpoint = Environment.GetEnvironmentVariable("CCDX_HTTP_MULTIPART_FORM_DATA_FILE_ENDPOINT");
                    MultipartFormDataContent multipartFormDataByteArrayContent = HttpService.BuildMultipartFormDataByteArrayContent(ccBlobInput, "file", ccBlobInputName);
                    HttpRequestMessage requestMessage = CcdxService.BuildCcdxHttpMultipartFormDataRequestMessage(HttpMethod.Post, ccdxHttpEndpoint, multipartFormDataByteArrayContent, sampleHeaders, ccBlobInputName, log);

                    log.LogDebug($"- [ccdx-provider->run]: Request header metadata: ");
                    log.LogDebug($"- [ccdx-provider->run]:   ce-id: {HttpService.GetHeaderStringValue(requestMessage, "ce-id")}");
                    log.LogDebug($"- [ccdx-provider->run]:   ce-type: {HttpService.GetHeaderStringValue(requestMessage, "ce-type")}");
                    log.LogDebug($"- [ccdx-provider->run]:   ceTime: {HttpService.GetHeaderStringValue(requestMessage, "ce-time")}");

                    // Send the http request
                    log.LogInformation($"- [ccdx-provider->run]: Send the http request to {ccdxHttpEndpoint}");
                    HttpStatusCode httpStatusCode = await HttpService.SendHttpRequestMessage(requestMessage);

                    if (httpStatusCode == HttpStatusCode.OK)
                    {
                        // USBDG-357
                        // Message got put on a highly druable topic. A 200 indicates successful entry into the data interchange. 
                        // However, the consumer downstream might not be able to handle a message of that size. 
                        // Consumer would own decision to reconfigure consumer to handle larger payload or jost not accept larger payloads.
                        log.LogInformation($"- [ccdx-provider->run]: Entry into the data interchange was successful");
                        log.LogInformation($"- [ccdx-provider->run]: Track ccdx provider success event (app insights)");
                        CcdxService.LogCcdxProviderSuccessEventToAppInsights(reportFileName, log);
                        log.LogInformation($"- [ccdx-provider->run]: Done");
                    }
                    else
                    {
                        string errorCode = "2XYK";
                        log.LogError($"- [ccdx-provider->run]: Received http error {httpStatusCode} while uploading {reportFileName} to the interchange");
                        log.LogInformation($"- [ccdx-provider->run]: Track ccdx provider failed event (app insights)");
                        CcdxService.LogCcdxProviderFailedEventToAppInsights(reportFileName, log);
                        log.LogInformation($"- [ccdx-provider->run]: Log error message");
                        string errorString = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(httpStatusCode.ToString(), ccBlobInputName, ccdxHttpEndpoint));
                        log.LogError($" - [ccdx-provider->run]: Message: {errorString}");
                        string blobContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING");
                        string storageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                        //Scope of holding container: To be used only for situations with CCDX transmission.
                        log.LogInformation($"- [ccdx-provider->run]: Move failed telemetry file {reportFileName} to holding container {blobContainerName} for further investigation");
                        byte[] bytes = StreamService.ReadToEnd(ccBlobInput);
                        await AzureStorageBlobService.UploadBlobToContainerUsingSdk(bytes, storageAccountConnectionString, blobContainerName, reportFileName);
                        log.LogInformation($"- [ccdx-provider->run]: Confirmed. Telemetry file {reportFileName} moved to container {blobContainerName}");
                    }
                }
                else
                {
                    log.LogError($"- [ccdx-provider->run]: Incoming telemetry file {reportFileName} is not from a supported data logger");
                    log.LogInformation($"- [ccdx-provider->run]: Track ccdx provider unsupported logger event (app insights)");
                    CcdxService.LogCcdxProviderUnsupportedLoggerEventToAppInsights(reportFileName, log);
                }
            }
            catch (Exception e)
            {
                log.LogError($"- [ccdx-provider->run]: An unexpected exception occured: {e.Message}");
                string errorCode = "ND82";
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
                log.LogInformation($"- [ccdx-provider->run]: Track ccdx provider unexpected error event (app insights)");
                CcdxService.LogCcdxProviderErrorEventToAppInsights(reportFileName, log, e, errorCode);
                log.LogError(e, errorMessage);
            }
        }
    }
}
