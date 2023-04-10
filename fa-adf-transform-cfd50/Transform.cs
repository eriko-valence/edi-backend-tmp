using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Storage.Blob; // Microsoft.Azure.WebJobs.Extensions.Storage
using System.Collections.Generic;
using System.Xml;
using System.Linq;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.System.Net;
using lib_edi.Services.Ccdx;
using lib_edi.Services.Azure;
using lib_edi.Services.Loggers;
using lib_edi.Exceptions;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Models.Dto.CceDevice.Csv;

namespace fa_adf_transform_cfd50
{
    public static class Transform
    {
        /// <summary>
        /// Azure function that transforms (including applying time correction) CFD50 log files
        /// </summary>
        /// <param name="req">Http request object that triggers this function</param>
        /// <param name="inputContainer">Azure storage blob container with CFD50 log files to transform</param>
        /// <param name="ouputContainer">Azure storage blob container where transformed CFD50 log files are uploaded</param>
        /// <param name="emsConfgContainer">Azure storage blob container where EMS configuration files reside</param>
        /// <param name="log">Microsoft logging object</param>
        [FunctionName("transform")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer inputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer ouputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_EMS_CONFIG%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer emsConfgContainer,
            ILogger log)
        {
            string logType = "cfd50";
            TransformHttpRequestMessageBodyDto payload = null;
            try
            {
                log.LogInformation($"- [transform-cdf50->run]: Deserialize {logType} log transformation http request body");
                payload = await HttpService.DeserializeHttpRequestBody(req);

                log.LogInformation("- [transform-cdf50->run]: Validate http request body");
                HttpService.ValidateHttpRequestBody(payload);

                CcdxService.LogMetaFridgeTransformStartedEventToAppInsights(payload.FileName, log);

                string loggerType = CcdxService.GetDataLoggerTypeFromBlobPath(payload.FileName);
                log.LogInformation($"- [transform-cdf50->run]: Extracted logger type: {loggerType}");

                log.LogInformation($"- [transform-cdf50->run]: Building input blob path");
                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"- [transform-cdf50->run]: - Path: {inputBlobPath}");

                log.LogInformation($"- [transform-cdf50->run]: List blobs in storage container {inputContainer.Name}/{payload.Path}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                log.LogInformation($"- [transform-cdf50->run]: Filter for {logType} log blobs");
                List<CloudBlockBlob> metaFridgeLogBlobs = Cfd50DataProcessorService.FindMetaFridgeLogBlobs(logDirectoryBlobs, inputBlobPath);

                log.LogInformation("- [transform-cdf50->run]: Download metafridge log blobs");
                List<dynamic> metaFridgeLogFiles = await Cfd50DataProcessorService.DownloadsAndDeserializesMetaFridgeLogBlobs(metaFridgeLogBlobs, inputContainer, inputBlobPath, log);

                log.LogInformation($"- Validate {logType} log blobs");
                List<dynamic> validatedUsbdgLogFiles = await Cfd50DataProcessorService.ValidateCfd50LogBlobs(emsConfgContainer, metaFridgeLogFiles, log);

                log.LogInformation($"- [transform-cdf50->run]: Map {logType} log objects to csv records");
                List<Cfd50CsvRecordDto> metaFridgeCsvRows = DataModelMappingService.MapMetaFridgeLogs(metaFridgeLogFiles);

                log.LogInformation($"- [transform-cdf50->run]: Write {logType} csv records to azure blob storage");
                string csvOutputBlobName = await Cfd50DataProcessorService.WriteMetaFridgeLogRecordsToCsvBlob(ouputContainer, payload, metaFridgeCsvRows, log);

                log.LogInformation("- [transform-cdf50->run]: Serialize http response body");
                string responseBody = HttpService.SerializeHttpResponseBody(csvOutputBlobName);
                log.LogInformation("- [transform-cdf50->run]: Send http response message");
                log.LogInformation("- [transform-cdf50->run]: SUCCESS");
                CcdxService.LogMetaFridgeTransformSucceededEventToAppInsights(payload.FileName, log);
                return new OkObjectResult(responseBody);

            }
            catch (Exception e)
            {
                string errorCode = "K79T";

                CcdxService.LogMetaFridgeTransformErrorEventToAppInsights(payload?.FileName, log, e, errorCode);

                if (e is BadRequestException)
                {
                    string errStr = $"Bad request thrown while validating {logType} transformation request";
                    log.LogError($"- {errStr}");
                    log.LogError($" - {e.Message}");
                    return new BadRequestObjectResult(e.Message);
                }
                else
                {
                    string errStr = $"Global level exception thrown while processing {logType} logs";
                    log.LogError($"- {errStr}");
                    log.LogError($" - {e.Message}");
                    var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = e.Message });
                    result.StatusCode = 500;
                    return result;
                }
            }
        }
    }
}
