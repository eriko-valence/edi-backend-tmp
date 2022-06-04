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
using lib_edi.Models.Dto.Loggers;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Services.Errors;
using lib_edi.Exceptions;
using lib_edi.Services.System;
using Newtonsoft.Json.Linq;
using lib_edi.Helpers;
using lib_edi.Services.CceDevice;

namespace fa_adf_transform_usbdg
{
    /// <summary>
    /// Class methods that support the Transformation phase of the Equipment Monitoring System (EMS) Azure Data Factory (ADF) 
    /// Extract Transform Load (ETL) pipeline.
    /// </summary>
    public static class Transform
    {
        /// <summary>
        /// Azure function that transforms (including applying time correction) USBDG log files
        /// </summary>
        /// <param name="req">Http request object that triggers this function</param>
        /// <param name="inputContainer">Azure storage blob container with USBDG log files to transform</param>
        /// <param name="ouputContainer">Azure storage blob container where transformed USBDG log files are uploaded</param>
        /// <param name="emsConfgContainer">Azure storage blob container where EMS configuration files reside</param>
        /// <param name="log">Microsoft logging object</param>
        /// <remarks>
        /// - Indigo is not an EMD. It is considered a "logger" and would need something like a USBDG attached to it to send reports.
        /// - There may never be a "USBDG" report as it would probably always be sending data from some other device.
        /// - EMD's aren't usually the "source". They are just the relay for some other device's reports.
        /// - Unclear how USBDG+Indigo pairing will play out from a report standpoint
        /// </remarks>
        [FunctionName("transform")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer inputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer ouputContainer,
            [Blob("%AZURE_STORAGE_BLOB_CONTAINER_NAME_EMS_CONFIG%", FileAccess.ReadWrite, Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] CloudBlobContainer emsConfgContainer,
            ILogger log)
        {
            string logType = "usbdg";
            TransformHttpRequestMessageBodyDto payload = null;

            try
            {
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
                List<CloudBlockBlob> usbdgLogBlobs = UsbdgDataProcessorService.FindUsbdgLogBlobs(logDirectoryBlobs, inputBlobPath);
                log.LogInformation($"- Filter for {logType} log report blobs");
                CloudBlockBlob usbdgLogReportBlobs = UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);

                log.LogInformation($"- Download {logType} log blobs");
                List<dynamic> usbdgLogFiles = await UsbdgDataProcessorService.DownloadAndDeserializeJsonBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, log);
                log.LogInformation($"- Download {logType} log report blobs");
                //dynamic emsLogMetadata = await UsbdgDataProcessorService.DownloadUsbdgLogReportBlobs(usbdgLogReportBlobs, inputContainer, inputBlobPath, log);
                dynamic emsLogMetadata = null;

                log.LogInformation($"- Retrieving time values from EMD metadata");
                string emdRelativeTime = ObjectManager.GetJObjectPropertyValueAsString(emsLogMetadata,"RELT");
                string emdAbsoluteTime = ObjectManager.GetJObjectPropertyValueAsString(emsLogMetadata, "ABST");

                log.LogInformation($"- Validate {logType} log blobs");
                List<dynamic> validatedUsbdgLogFiles = await UsbdgDataProcessorService.ValidateUsbdgLogBlobs(emsConfgContainer, usbdgLogFiles, log);

                log.LogInformation($"- Map {logType} log objects to csv records");
                List<UsbdgSimCsvRecordDto> usbdbLogCsvRows = DataModelMappingService.MapUsbdgLogs(usbdgLogFiles, emsLogMetadata);

                log.LogInformation($"- Transform {logType} csv records");

                log.LogInformation($"  - Convert relative time to total seconds (all records)");
                usbdbLogCsvRows = UsbdgDataProcessorService.ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(usbdbLogCsvRows);

                log.LogInformation($"  - Sort csv records using relative time total seconds");
                List<UsbdgSimCsvRecordDto> sortedUsbdbLogCsvRows = usbdbLogCsvRows.OrderBy(i => (i._RELT_SECS)).ToList();

                log.LogInformation($"  - Convert relative time (e.g., 'P9DT59M53S') to total seconds (report only)");
                int DurationSecs = UsbdgDataProcessorService.ConvertRelativeTimeStringToTotalSeconds(emsLogMetadata); // convert timespan to seconds

                log.LogInformation($"  - Calculate absolute time for each record using record relative time (e.g., 781193) and report absolute time ('2021-06-20T23:00:02Z')");
                sortedUsbdbLogCsvRows = UsbdgDataProcessorService.CalculateAbsoluteTimeForUsbdgRecords(sortedUsbdbLogCsvRows, DurationSecs, emsLogMetadata);

                log.LogInformation($"  - Cloud upload times: ");
                log.LogInformation($"    - EMD (source: cellular) : {DateConverter.ConvertIso8601CompliantString(emdAbsoluteTime)} (UTC)");
                log.LogInformation($"    - Logger (source: real time clock) : {emdRelativeTime ?? ""} (Relative Time)");
                log.LogInformation($"    - Logger (source: real time clock) : {UsbdgDataProcessorService.ConvertRelativeTimeStringToTotalSeconds(emdRelativeTime)} (Duration in Seconds)");
                log.LogInformation($"  - Absolute time calculation results (first two records): ");
                if (usbdbLogCsvRows.Count > 1)
				{
                    log.LogInformation($"    - record[0].ElapsedSecs (Elapsed secs from activation time): {UsbdgDataProcessorService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[0].RELT)}");
                    log.LogInformation($"    - record[0].ABST (EMD cloud upload absolute time): {usbdbLogCsvRows[0].ABST}");
                    log.LogInformation($"    - record[0].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[0].RELT}");
                    log.LogInformation($"    - record[0]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[0]._RELT_SECS}");
                    log.LogInformation($"    - record[0]._ABST (calculated absolute time): {usbdbLogCsvRows[0].ABST_CALC}");
                    log.LogInformation($" ");
                    log.LogInformation($"    - record[1].ElapsedSecs (Elapsed secs from activation time): {UsbdgDataProcessorService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[1].RELT)}");
                    log.LogInformation($"    - record[1].ABST (EMD cloud upload absolute time): {usbdbLogCsvRows[1].ABST}");
                    log.LogInformation($"    - record[1].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[1].RELT}");
                    log.LogInformation($"    - record[1]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[1]._RELT_SECS}");
                    log.LogInformation($"    - record[1]._ABST (calculated absolute time): {usbdbLogCsvRows[1].ABST_CALC}");
                }

                log.LogInformation($"- Write {logType} csv records to azure blob storage");
                string csvOutputBlobName = await UsbdgDataProcessorService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, sortedUsbdbLogCsvRows, log);

                log.LogInformation(" - Serialize http response body");
                string responseBody = HttpService.SerializeHttpResponseBody(csvOutputBlobName);

                log.LogInformation(" - Send http response message");
                log.LogInformation("- Log successfully completed event to app insights");
                CcdxService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, log);
                log.LogInformation(" - SUCCESS");

                return new OkObjectResult(responseBody);
            }
            catch (Exception e)
            {
                string errorCode = "E2N8";
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList());
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
