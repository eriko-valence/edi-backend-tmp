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

                CcdxService.LogEmsTransformStartedEventToAppInsights(payload.FileName, log);

                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"- Building input blob path: {inputBlobPath}");



                log.LogInformation($"- List blobs in azure blob storage location {inputBlobPath}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.ListBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                log.LogInformation($"- Filter for {logType} log blobs");
                List<CloudBlockBlob> usbdgLogBlobs = UsbdgDataProcessorService.FindUsbdgLogBlobs(logDirectoryBlobs, inputBlobPath);
                log.LogInformation($"- Filter for {logType} log report blobs");
                List<CloudBlockBlob> usbdgLogReportBlobs = UsbdgDataProcessorService.FindUsbdgLogReportBlobs(logDirectoryBlobs, inputBlobPath);

                log.LogInformation($"- Download {logType} log blobs");
                List<UsbdgJsonDataFileDto> usbdgLogFiles = await UsbdgDataProcessorService.DownloadUsbdgLogBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, log);
                log.LogInformation($"- Download {logType} log report blobs");
                UsbdgJsonReportFileDto emsLogMetadata = await UsbdgDataProcessorService.DownloadUsbdgLogReportBlobs(usbdgLogReportBlobs, inputContainer, inputBlobPath, log);

                log.LogInformation($"- Validate {logType} log blobs");
                List<UsbdgJsonDataFileDto> validatedUsbdgLogFiles = await UsbdgDataProcessorService.ValidateUsbdgLogBlobs(emsConfgContainer, usbdgLogFiles, log);

                log.LogInformation($"- Map {logType} log objects to csv records");
                List<UsbdgCsvDataRowDto> usbdbLogCsvRows = DataModelMappingService.MapUsbdgLogs(usbdgLogFiles);

                log.LogInformation($"- Transform {logType} csv records");
                log.LogInformation($"  - Convert relative time to total seconds (all records)");
                usbdbLogCsvRows = UsbdgDataProcessorService.ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(usbdbLogCsvRows);

                log.LogInformation($"  - Sort csv records using relative time total seconds");
                List<UsbdgCsvDataRowDto> sortedUsbdbLogCsvRows = usbdbLogCsvRows.OrderBy(i => (i.DurationSecs)).ToList();

                log.LogInformation($"  - Convert relative time (e.g., 'P9DT59M53S') to total seconds (report only)");
                int report_duration_total_seconds = UsbdgDataProcessorService.ConvertRelativeTimeStringToTotalSeconds(emsLogMetadata.emd_relt); // convert timespan to seconds

                log.LogInformation($"  - Calculate absolute time for each record using record relative time (781193) and report absolute time ('2021-06-20T23:00:02Z')");
                sortedUsbdbLogCsvRows = UsbdgDataProcessorService.CalculateAbsoluteTimeForUsbdgRecords(sortedUsbdbLogCsvRows, report_duration_total_seconds, emsLogMetadata.emd_abs);

                log.LogInformation($"- Write {logType} csv records to azure blob storage");
                string csvOutputBlobName = await UsbdgDataProcessorService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, sortedUsbdbLogCsvRows, log);

                log.LogInformation(" - Serialize http response body");
                string responseBody = HttpService.SerializeHttpResponseBody(csvOutputBlobName);

                log.LogInformation(" - Send http response message");

                CcdxService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, log);

                log.LogInformation(" - SUCCESS");

                CcdxService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, log);

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
