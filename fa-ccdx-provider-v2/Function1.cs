using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.System.IO;
using lib_edi.Services.System.Net;
using lib_edi.Services.Errors;
using lib_edi.Services.Ems;
using lib_edi.Models.Enums.Azure.AppInsights;

namespace fa_ccdx_provider_v2
{
    public class Function1
    {
        private readonly ILogger<Function1> _logger;

        public Function1(ILogger<Function1> logger)
        {
            _logger = logger;
        }

        [Function(nameof(Function1))]
        public async Task Run([BlobTrigger("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT%/{ccBlobInputName}", Source = BlobTriggerSource.EventGrid, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] Stream ccBlobInput, string ccBlobInputName, ILogger log)
        {
            string reportFileName = null;
            string logPrefix = "- [ccdx-provider-usbdg->run]:";
            try
            {
                string storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                string inputContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT");
                log.LogInformation($"{logPrefix} Received telemetry file {ccBlobInputName}");
                log.LogInformation($"{logPrefix} Track ccdx provider started event (app insights)");
                reportFileName = Path.GetFileName(ccBlobInputName);
                CcdxService.LogCcdxProviderStartedEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, log);

                log.LogInformation($"{logPrefix} Validate incoming blob file extension");
                string fileExtension = Path.GetExtension(ccBlobInputName);
                log.LogInformation($"{logPrefix} File extension: {fileExtension}");
                // NHGH-1711 (2022.09.12) Virtual directory creation events can cause a blob trigger. However,
                // These virtual creation events are not 'PutBlob' events, which is required to track a file
                // package through the EDI pipeline. This file extension check filters out the virtual folder
                // creation noise. 
                if (CcdxService.IsPathExtensionSupported(ccBlobInputName))
                {
                    string loggerType = CcdxService.GetDataLoggerTypeFromBlobPath(ccBlobInputName);
                    log.LogInformation($"{logPrefix} Extracted logger type: {loggerType}");
                    log.LogInformation($"{logPrefix} Validate incoming blob originated from supported data logger");
                    if (EmsService.ValidateCceDeviceType(loggerType))
                    {
                        log.LogInformation($"{logPrefix} Confirmed. Blob originated from supported data logger '{loggerType}'");
                        var sr = new StreamReader(ccBlobInput);
                        var body = await sr.ReadToEndAsync();
                        string storageConnectionStringConfig = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                        string blobContainerNameConfig = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_CONFIG");
                        // 2024.02.29 1602 NHGH-3305 Remove sample ccdx headers
                        /*
                        string fileCcdxPublisherSampleHeaderValues = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_SAMPLE_VALUES_FILENAME");
                        log.LogInformation($"{logPrefix} Retrieve sample ccdx provider metadata headers from blob storage");
                        string blobText = await AzureStorageBlobService.DownloadBlobTextAsync(storageConnectionStringConfig, blobContainerNameConfig, fileCcdxPublisherSampleHeaderValues);
                        CcdxProviderSampleHeadersDto sampleHeaders = JsonConvert.DeserializeObject<CcdxProviderSampleHeadersDto>(blobText);
                        */
                        log.LogInformation($"{logPrefix} Prepare ccdx provider http request with multipart content");
                        string ccdxHttpEndpoint = Environment.GetEnvironmentVariable("CCDX_HTTP_MULTIPART_FORM_DATA_FILE_ENDPOINT");
                        MultipartFormDataContent multipartFormDataByteArrayContent = await HttpService.BuildMultipartFormDataByteArrayContent(ccBlobInput, "file", ccBlobInputName);
                        HttpRequestMessage requestMessage = await CcdxService.BuildCcdxHttpMultipartFormDataRequestMessage(HttpMethod.Post, ccdxHttpEndpoint, multipartFormDataByteArrayContent, ccBlobInputName, log);
                        log.LogInformation($"{logPrefix} Request header metadata: ");
                        log.LogInformation($"{logPrefix}   ce-id: {HttpService.GetHeaderStringValue(requestMessage, "ce-id")}");
                        log.LogInformation($"{logPrefix}   ce-type: {HttpService.GetHeaderStringValue(requestMessage, "ce-type")}");
                        log.LogInformation($"{logPrefix}   ce-time: {HttpService.GetHeaderStringValue(requestMessage, "ce-time")}");
                        // Send the http request
                        log.LogInformation($"{logPrefix} Send the http request to {ccdxHttpEndpoint}");
                        HttpStatusCode httpStatusCode = await HttpService.SendHttpRequestMessage(requestMessage);
                        if (httpStatusCode == HttpStatusCode.OK)
                        {
                            // NHGH-414 2021.09.21
                            // Message got put on a highly druable topic. A 200 indicates successful entry into the data interchange. 
                            // However, the consumer downstream might not be able to handle a message of that size. 
                            // Consumer would own decision to reconfigure consumer to handle larger payload or jost not accept larger payloads.
                            log.LogInformation($"{logPrefix} Entry into the data interchange was successful");
                            log.LogInformation($"{logPrefix} Track ccdx provider success event (app insights)");
                            CcdxService.LogCcdxProviderSuccessEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, log);
                            log.LogInformation($"{logPrefix} Cleaning up .... deleting telemetry file {ccBlobInputName}");
                            await AzureStorageBlobService.DeleteBlob(storageConnectionString, inputContainerName, ccBlobInputName);
                            log.LogInformation($"{logPrefix} DONE");
                        }
                        else
                        {
                            string errorCode = "2XYK";
                            log.LogError($"{logPrefix} Received http error {httpStatusCode} while uploading {reportFileName} to the interchange");
                            log.LogInformation($"{logPrefix} Track ccdx provider failed event (app insights)");
                            CcdxService.LogCcdxProviderFailedEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, log);
                            log.LogInformation($"{logPrefix} Log error message");
                            string errorString = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(httpStatusCode.ToString(), ccBlobInputName, ccdxHttpEndpoint));
                            log.LogError($"{logPrefix} Message: {errorString}");
                            string blobContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING");
                            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                            //Scope of holding container: To be used only for situations with CCDX transmission.
                            log.LogInformation($"{logPrefix} Move failed telemetry file {reportFileName} to holding container {blobContainerName} for further investigation");
                            byte[] bytes = await StreamService.ReadToEnd(ccBlobInput);
                            // EDI architecture: "Container where files are placed when there is a problem sending to CCDX via the provider. Examples 
                            // includes files that are too large (>5MB), or when the backend has a problem such as Kafka brokers unavailable. This 
                            // condition is typically indicated by a 5xx HTTP response from the CCDX POST by the provider. The purpose of this container 
                            // is to allow for further analysis and troubleshooting. No retry logic is currently implemented for files in this container, 
                            // but may be added in the future."
                            await AzureStorageBlobService.UploadBlobToContainerUsingSdk(bytes, storageAccountConnectionString, blobContainerName, reportFileName);
                            log.LogInformation($"{logPrefix} Confirmed. Telemetry file {reportFileName} moved to container {blobContainerName}");
                            log.LogInformation($"{logPrefix} Cleaning up .... deleting telemetry file {ccBlobInputName}");
                            await AzureStorageBlobService.DeleteBlob(storageConnectionString, inputContainerName, ccBlobInputName);
                            log.LogInformation($"{logPrefix} DONE");
                        }
                    }
                    else
                    {
                        log.LogError($"{logPrefix} Incoming telemetry file {reportFileName} is not from a supported data logger");
                        log.LogInformation($"{logPrefix} Track ccdx provider unsupported logger event (app insights)");
                        CcdxService.LogCcdxProviderUnsupportedLoggerEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, loggerType, log);
                    }
                }
                else
                {
                    log.LogInformation($"{logPrefix} Blob {ccBlobInputName} has an unsupported attachment extension and will not be sent to the interchange");
                }
            }
            catch (Exception e)
            {
                string errorCode = "ND82";
                log.LogError($"{logPrefix} An unexpected exception occured: {e.Message}");
                log.LogError($"{logPrefix} error code : " + errorCode);
                log.LogError($"An exception was thrown publishing {reportFileName} to ccdx: {e.Message} ({errorCode})");

                string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
                log.LogInformation($"{logPrefix} Track ccdx provider unexpected error event (app insights)");
                CcdxService.LogCcdxProviderErrorEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, log, e, errorCode);
                log.LogError(e, errorMessage);
            }
        }
    }
}
