using System;
using System.IO;
using System.Net;
using System.Net.Http;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.System.IO;
using lib_edi.Services.System.Net;
using lib_edi.Services.Errors;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using lib_edi.Services.Ems;
using lib_edi.Models.Enums.Azure.AppInsights;
using Microsoft.Azure.Functions.Worker;

namespace fa_ccdx_provider
{
    /// <summary>
    /// A telemetry provider that sends telemetry message into the data interchange (CCDX) system.
    /// </summary>
    /// <remarks>
    /// Telemetry Providers represent the source of cold chain telemetry data. The terms report, 
    /// message, and event are used interchangeably to mean a package of data. 
    /// EDI architecture: 
    /// - "A compressed data report file (and optionally a metadata file) 
    /// is sent from a New Horizons-managed Cold Chain Equipment (CCE) device (e.g. Metafridge, 
    /// Indigo, USBDG, etc) to a container (container_1) within an Azure Data Lake gen2 (ADLS) 
    /// instance."
    /// - "An Azure Blob Storage trigger defined on container_1 invokes an Azure Function (the CCDX Provider) 
    /// to send the file(s) received 'as-is' to a Kafka topic within the Cold Chain Data Interchange (CCDX) platform. 
    /// Report file(s) are removed from container_1 upon successful transmission to CCDX."
    /// Deletion behavior:
    /// - Blob ONLY gets deleted if there are no exceptions thrown
    /// - A blob is moved to a holding container ONLY if the http response from the CCDX upload was not successful
    /// - We will continue to evaluate the current deletion behavior - monitoring will help identify gaps (NHGH-1720)
    /// </remarks>
    public class Provide
    {

        private readonly ILogger<Provide> _logger;

        public Provide(ILogger<Provide> logger)
        {
            _logger = logger;
        }

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
        [Function("ccdx-provider")]
        public async Task Run(
            [BlobTrigger("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT%/{ccBlobInputName}", Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")]
            Stream ccBlobInput,
            string ccBlobInputName)
        {
            string reportFileName = null;
            string logPrefix = "- [ccdx-provider-usbdg->run]:";
			try
            {
                string storageConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                string inputContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT");
                _logger.LogInformation($"{logPrefix} Received telemetry file {ccBlobInputName}");
                _logger.LogInformation($"{logPrefix} Track ccdx provider started event (app insights)");
				reportFileName = Path.GetFileName(ccBlobInputName);
                CcdxService.LogCcdxProviderStartedEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, _logger);

                _logger.LogInformation($"{logPrefix} Validate incoming blob file extension");
                string fileExtension = Path.GetExtension(ccBlobInputName);
                _logger.LogInformation($"{logPrefix} File extension: {fileExtension}");
                // NHGH-1711 (2022.09.12) Virtual directory creation events can cause a blob trigger. However,
                // These virtual creation events are not 'PutBlob' events, which is required to track a file
                // package through the EDI pipeline. This file extension check filters out the virtual folder
                // creation noise. 
                if (CcdxService.IsPathExtensionSupported(ccBlobInputName))
                {
                    string loggerType = CcdxService.GetDataLoggerTypeFromBlobPath(ccBlobInputName);
                    _logger.LogInformation($"{logPrefix} Extracted logger type: {loggerType}");
                    _logger.LogInformation($"{logPrefix} Validate incoming blob originated from supported data logger");
                    if (EmsService.ValidateCceDeviceType(loggerType))
                    {
                        _logger.LogInformation($"{logPrefix} Confirmed. Blob originated from supported data logger '{loggerType}'");
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
                        _logger.LogInformation($"{logPrefix} Prepare ccdx provider http request with multipart content");
                        string ccdxHttpEndpoint = Environment.GetEnvironmentVariable("CCDX_HTTP_MULTIPART_FORM_DATA_FILE_ENDPOINT");
                        MultipartFormDataContent multipartFormDataByteArrayContent = await HttpService.BuildMultipartFormDataByteArrayContent(ccBlobInput, "file", ccBlobInputName);
                        HttpRequestMessage requestMessage = await CcdxService.BuildCcdxHttpMultipartFormDataRequestMessage(HttpMethod.Post, ccdxHttpEndpoint, multipartFormDataByteArrayContent, ccBlobInputName, _logger);
                        _logger.LogInformation($"{logPrefix} Request header metadata: ");
                        _logger.LogInformation($"{logPrefix}   ce-id: {HttpService.GetHeaderStringValue(requestMessage, "ce-id")}");
                        _logger.LogInformation($"{logPrefix}   ce-type: {HttpService.GetHeaderStringValue(requestMessage, "ce-type")}");
                        _logger.LogInformation($"{logPrefix}   ce-time: {HttpService.GetHeaderStringValue(requestMessage, "ce-time")}");
                        // Send the http request
                        _logger.LogInformation($"{logPrefix} Send the http request to {ccdxHttpEndpoint}");
                        HttpStatusCode httpStatusCode = await HttpService.SendHttpRequestMessage(requestMessage);
                        if (httpStatusCode == HttpStatusCode.OK)
                        {
                            // NHGH-414 2021.09.21
                            // Message got put on a highly druable topic. A 200 indicates successful entry into the data interchange. 
                            // However, the consumer downstream might not be able to handle a message of that size. 
                            // Consumer would own decision to reconfigure consumer to handle larger payload or jost not accept larger payloads.
                            _logger.LogInformation($"{logPrefix} Entry into the data interchange was successful");
                            _logger.LogInformation($"{logPrefix} Track ccdx provider success event (app insights)");
                            CcdxService.LogCcdxProviderSuccessEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, _logger);
                            _logger.LogInformation($"{logPrefix} Cleaning up .... deleting telemetry file {ccBlobInputName}");
                            await AzureStorageBlobService.DeleteBlob(storageConnectionString, inputContainerName, ccBlobInputName);
                            _logger.LogInformation($"{logPrefix} DONE");
                        }
                        else
                        {
                            string errorCode = "2XYK";
                            _logger.LogError($"{logPrefix} Received http error {httpStatusCode} while uploading {reportFileName} to the interchange");
                            _logger.LogInformation($"{logPrefix} Track ccdx provider failed event (app insights)");
                            CcdxService.LogCcdxProviderFailedEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, _logger);
                            _logger.LogInformation($"{logPrefix} Log error message");
                            string errorString = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(httpStatusCode.ToString(), ccBlobInputName, ccdxHttpEndpoint));
                            _logger.LogError($"{logPrefix} Message: {errorString}");
                            string blobContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_HOLDING");
                            string storageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_INPUT_CONNECTION_STRING");
                            //Scope of holding container: To be used only for situations with CCDX transmission.
                            _logger.LogInformation($"{logPrefix} Move failed telemetry file {reportFileName} to holding container {blobContainerName} for further investigation");
                            byte[] bytes = await StreamService.ReadToEnd(ccBlobInput);
                            // EDI architecture: "Container where files are placed when there is a problem sending to CCDX via the provider. Examples 
                            // includes files that are too large (>5MB), or when the backend has a problem such as Kafka brokers unavailable. This 
                            // condition is typically indicated by a 5xx HTTP response from the CCDX POST by the provider. The purpose of this container 
                            // is to allow for further analysis and troubleshooting. No retry logic is currently implemented for files in this container, 
                            // but may be added in the future."
                            await AzureStorageBlobService.UploadBlobToContainerUsingSdk(bytes, storageAccountConnectionString, blobContainerName, reportFileName);
                            _logger.LogInformation($"{logPrefix} Confirmed. Telemetry file {reportFileName} moved to container {blobContainerName}");
                            _logger.LogInformation($"{logPrefix} Cleaning up .... deleting telemetry file {ccBlobInputName}");
                            await AzureStorageBlobService.DeleteBlob(storageConnectionString, inputContainerName, ccBlobInputName);
                            _logger.LogInformation($"{logPrefix} DONE");
                        }
                    }
                    else
                    {
                        _logger.LogError($"{logPrefix} Incoming telemetry file {reportFileName} is not from a supported data logger");
                        _logger.LogInformation($"{logPrefix} Track ccdx provider unsupported logger event (app insights)");
                        CcdxService.LogCcdxProviderUnsupportedLoggerEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, loggerType, _logger);
                    }
                }
                else
                {
                    _logger.LogInformation($"{logPrefix} Blob {ccBlobInputName} has an unsupported attachment extension and will not be sent to the interchange");
                }
            }
            catch (Exception e)
            {
				string errorCode = "ND82";
                _logger.LogError($"{logPrefix} An unexpected exception occured: {e.Message}");
                _logger.LogError($"{logPrefix} error code : " + errorCode);
                _logger.LogError($"An exception was thrown publishing {reportFileName} to ccdx: {e.Message} ({errorCode})");

				string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
                _logger.LogInformation($"{logPrefix} Track ccdx provider unexpected error event (app insights)");
                CcdxService.LogCcdxProviderErrorEventToAppInsights(reportFileName, PipelineStageEnum.Name.CCDX_PROVIDER, _logger, e, errorCode);
                _logger.LogError(e, errorMessage);
            }
        }
    }
}
