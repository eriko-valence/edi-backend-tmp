using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using lib_edi.Services.Errors;
using lib_edi.Services.Ccdx;
using lib_edi.Exceptions;
using lib_edi.Services.System.Net;
using lib_edi.Services.Loggers;
using lib_edi.Helpers;
using lib_edi.Models.Dto.CceDevice.Csv;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Storage.Blob;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Azure;
using lib_edi.Services.CceDevice;
using Newtonsoft.Json.Linq;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Csv;
//using Microsoft.Azure.Storage.Blob; // Microsoft.Azure.WebJobs.Extensions.Storage

namespace fa_adf_transform_indigo_v2
{
    public static class Transform
    {
        [FunctionName("transform")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer inputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer ouputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_EMS_CONFIG%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer emsConfgContainer,
            ILogger log)
        {
            string logType = "indigo-v2"; // TODO - add as an application setting
            TransformHttpRequestMessageBodyDto payload = null;

            try
            {
                string logJsonSchemaFileName = Environment.GetEnvironmentVariable("EMS_LOG_JSON_SCHEMA_DEFINITION_FILE_NAME");

                log.LogInformation($"- Deserialize {logType} log transformation http request body");
                payload = await HttpService.DeserializeHttpRequestBody(req);

                log.LogInformation("- Validate http request body");
                HttpService.ValidateHttpRequestBody(payload);

                log.LogInformation("- Log started event to app insights");
                CcdxService.LogEmsTransformStartedEventToAppInsights(payload.FileName, log);

                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"- Building input blob path: {inputBlobPath}");

                log.LogInformation($"- List blobs in azure blob storage location {inputBlobPath}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                log.LogInformation($"- Filter for {logType} log blobs");
                List<CloudBlockBlob> usbdgLogBlobs = IndigoDataTransformService.GetLogBlobs(logDirectoryBlobs, inputBlobPath);
                log.LogInformation($"- Filter for {logType} log report blobs");
                CloudBlockBlob usbdgReportMetadataBlob = UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);

                log.LogInformation($"- Download {logType} log blobs");
                List<dynamic> indigoLogFiles = await DataTransformService.DownloadAndDeserializeJsonBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, log);
                log.LogInformation($"- Download {logType} log report blobs");
                dynamic usbdgReportMetadata = await DataTransformService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, log);

                dynamic usbdgRecords = UsbdgDataProcessorService.GetUsbdgMetadataRecordsElement(usbdgReportMetadata);

                log.LogInformation($"- Retrieving time values from EMD metadata");
                string emdRelativeTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "RELT");
                string emdAbsoluteTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "ABST");

                log.LogInformation($"- Validate {logType} log blobs");
                List<dynamic> validatedUsbdgLogFiles = await DataTransformService.ValidateLogJsonObjects(emsConfgContainer, indigoLogFiles, logJsonSchemaFileName, log);

                log.LogInformation($"- Map {logType} log objects to csv records");
                List<EdiSinkRecord> usbdbLogCsvRows = IndigoDataTransformService.MapSourceToSinkEvents(indigoLogFiles);

                log.LogInformation($"- Transform {logType} csv records");
                string responseBody = null;
                /*
                log.LogInformation($"  - Convert relative time to total seconds (all records)");
                usbdbLogCsvRows = IndigoDataTransformService.ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(usbdbLogCsvRows);

                log.LogInformation($"  - Sort csv records using relative time total seconds");
                List<IndigoV2EventRecord> sortedUsbdbLogCsvRows = usbdbLogCsvRows.OrderBy(i => (i._RELT_SECS)).ToList();

                log.LogInformation($"  - Convert relative time (e.g., 'P9DT59M53S') to total seconds (report only)");
                int DurationSecs = IndigoDataTransformService.ConvertRelativeTimeStringToTotalSeconds(usbdgReportMetadata); // convert timespan to seconds

                log.LogInformation($"  - Calculate absolute time for each record using record relative time (e.g., 781193) and report absolute time ('2021-06-20T23:00:02Z')");
                sortedUsbdbLogCsvRows = IndigoDataTransformService.CalculateAbsoluteTimeForUsbdgRecords(sortedUsbdbLogCsvRows, DurationSecs, usbdgReportMetadataBlob);

                log.LogInformation($"  - Cloud upload times: ");
                log.LogInformation($"    - EMD (source: cellular) : {DateConverter.ConvertIso8601CompliantString(emdAbsoluteTime)} (UTC)");
                log.LogInformation($"    - Logger (source: real time clock) : {emdRelativeTime ?? ""} (Relative Time)");
                log.LogInformation($"    - Logger (source: real time clock) : {IndigoDataTransformService.ConvertRelativeTimeStringToTotalSeconds(emdRelativeTime)} (Duration in Seconds)");
                log.LogInformation($"  - Absolute time calculation results (first two records): ");
                if (usbdbLogCsvRows.Count > 1)
                {
                    log.LogInformation($"    - record[0].ElapsedSecs (Elapsed secs from activation time): {IndigoDataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[0].RELT)}");
                    //log.LogInformation($"    - record[0].ABST (EMD cloud upload absolute time): {usbdbLogCsvRows[0].ABST}");
                    log.LogInformation($"    - record[0].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[0].RELT}");
                    log.LogInformation($"    - record[0]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[0]._RELT_SECS}");
                    log.LogInformation($"    - record[0]._ABST (calculated absolute time): {usbdbLogCsvRows[0].ABST_CALC}");
                    log.LogInformation($" ");
                    log.LogInformation($"    - record[1].ElapsedSecs (Elapsed secs from activation time): {IndigoDataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[1].RELT)}");
                    //log.LogInformation($"    - record[1].ABST (EMD cloud upload absolute time): {usbdbLogCsvRows[1].ABST}");
                    log.LogInformation($"    - record[1].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[1].RELT}");
                    log.LogInformation($"    - record[1]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[1]._RELT_SECS}");
                    log.LogInformation($"    - record[1]._ABST (calculated absolute time): {usbdbLogCsvRows[1].ABST_CALC}");
                }

                log.LogInformation($"- Write {logType} csv records to azure blob storage");
                string csvOutputBlobName = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, sortedUsbdbLogCsvRows, log);

                log.LogInformation(" - Serialize http response body");
                string responseBody = DataTransformService.SerializeHttpResponseBody(csvOutputBlobName);

                log.LogInformation(" - Send http response message");
                log.LogInformation("- Log successfully completed event to app insights");
                AzureAppInsightsService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, log);
                log.LogInformation(" - SUCCESS");
                */

                return new OkObjectResult(responseBody);
            }
            catch (Exception e)
            {
                string errorCode = "E2N8";
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                string exceptionInnerMessage = EdiErrorsService.GetInnerException(e);

                CcdxService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, e, errorCode);

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
