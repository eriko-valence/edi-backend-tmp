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
        /// Azure function that transforms MetaFridge log files
        /// </summary>
        /// <param name="req">Http request object that triggers this function</param>
        /// <param name="inputContainer">Azure storage blob container with MetaFridge log files to transform</param>
        /// <param name="ouputContainer">Azure storage blob container were transformed MetaFridge log files are uploaded</param>
        /// <param name="log">Microsoft logging object</param>
        [FunctionName("transformMF")]
        public static async Task<IActionResult> RunMf(
        [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
        [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer inputContainer,
        [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer ouputContainer,
        ILogger log)
        {
            string logType = "metafridge";
            TransformHttpRequestMessageBodyDto payload = null;
            try
            {

                log.LogInformation($"- Deserialize {logType} log transformation http request body");
                payload = await HttpService.DeserializeHttpRequestBody(req);

                CcdxService.LogMetaFridgeTransformStartedEventToAppInsights(payload.FileName, log);

                log.LogInformation("- Validate http request body");
                HttpService.ValidateHttpRequestBody(payload);

                log.LogInformation($"- Building input blob path");
                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"  - Path: {inputBlobPath}");

                log.LogInformation($"- List blobs in storage container {inputContainer.Name}/{payload.Path}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.ListBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                log.LogInformation($"- Filter for {logType} log blobs");
                List<CloudBlockBlob> metaFridgeLogBlobs = Cfd50DataProcessorService.FindMetaFridgeLogBlobs(logDirectoryBlobs, inputBlobPath);

                log.LogInformation("- Download metafridge log blobs");
                List<Cfd50JsonDataFileDto> metaFridgeLogFiles = await Cfd50DataProcessorService.DownloadsAndDeserializesMetaFridgeLogBlobs(metaFridgeLogBlobs, inputContainer, inputBlobPath, log);

                log.LogInformation($"- Map {logType} log objects to csv records");
                List<Cfd50CsvDataRowDto> metaFridgeCsvRows = DataModelMappingService.MapMetaFridgeLogs(metaFridgeLogFiles);

                log.LogInformation($"- Write {logType} csv records to azure blob storage");
                string csvOutputBlobName = await Cfd50DataProcessorService.WriteMetaFridgeLogRecordsToCsvBlob(ouputContainer, payload, metaFridgeCsvRows, log);

                log.LogInformation(" - Serialize http response body");
                string responseBody = HttpService.SerializeHttpResponseBody(csvOutputBlobName);
                log.LogInformation(" - Send http response message");
                log.LogInformation(" - SUCCESS");
                CcdxService.LogMetaFridgeTransformSucceededEventToAppInsights(payload.FileName, log);
                return new OkObjectResult(responseBody);

            }
            catch (Exception e)
            {
                string errorCode = "K79T";

                CcdxService.LogMetaFridgeTransformErrorEventToAppInsights(payload.FileName, log, e, errorCode);

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
