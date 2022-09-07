using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using lib_edi.Services.Errors;
using lib_edi.Services.Ccdx;
using lib_edi.Exceptions;
using lib_edi.Services.System.Net;
using lib_edi.Services.Loggers;
using lib_edi.Helpers;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Azure.Storage.Blob;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Azure;
using lib_edi.Services.CceDevice;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Csv;
using lib_edi.Models.Edi;
using lib_edi.Models.Enums.Emd;

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
            string logType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            TransformHttpRequestMessageBodyDto payload = null;

            try
            {
                string jsonSchemaBlobNameIndigoV2Log = Environment.GetEnvironmentVariable("EMS_INDIGOV2_JSON_SCHEMA_FILENAME");
                string jsonSchemaBlobNameUsbdgMetadata = Environment.GetEnvironmentVariable("EMS_USBDG_METADATA_JSON_SCHEMA_FILENAME");

                log.LogInformation($"- Deserialize log transformation http request body");
                payload = await HttpService.DeserializeHttpRequestBody(req);

                log.LogInformation("- Validate http request body");
                HttpService.ValidateHttpRequestBody(payload);

                log.LogInformation("- Log started event to app insights");
                IndigoDataTransformService.LogEmsTransformStartedEventToAppInsights(payload.FileName, log);

                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"- Building input blob path: {inputBlobPath}");

                log.LogInformation($"- Pull all blobs from file package {inputBlobPath}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                logType = DataTransformService.DetermineFilePackageType(logDirectoryBlobs);

                if (IndigoDataTransformService.IsFilePackageIndigoV2(logDirectoryBlobs))
                {
                    log.LogInformation($"- Pull {logType} log blobs from file package");  
                    List<CloudBlockBlob> usbdgLogBlobs = IndigoDataTransformService.GetLogBlobs(logDirectoryBlobs, inputBlobPath);

                    log.LogInformation($"- Pull usbdg report metadata blob from file package");
                    CloudBlockBlob usbdgReportMetadataBlob = UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);

                    log.LogInformation($"- Download {logType} log blobs");
                    List<dynamic> indigoLogFiles = await AzureStorageBlobService.DownloadAndDeserializeJsonBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, log);
                    log.LogInformation($"- Download usbdg report metadata blob");
                    dynamic usbdgReportMetadata = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, log);

                    dynamic usbdgRecords = UsbdgDataProcessorService.GetUsbdgMetadataRecordsElement(usbdgReportMetadata);

                    log.LogInformation($"- Retrieving time values from EMD metadata");
                    string emdRelativeTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "RELT");
                    string emdAbsoluteTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "ABST");

                    log.LogInformation($"- Validate USBDG report metadata blob");
                    dynamic validatedUsbdgReportMetadataFile = await DataTransformService.ValidateJsonObject(emsConfgContainer, usbdgReportMetadata, jsonSchemaBlobNameUsbdgMetadata, log);

                    log.LogInformation($"- Validate {logType} log blobs");
                    List<dynamic> validatedUsbdgLogFiles = await DataTransformService.ValidateJsonObjects(emsConfgContainer, indigoLogFiles, jsonSchemaBlobNameIndigoV2Log, log);

                    log.LogInformation($"- Start tracking EDI job status");
                    EdiJob ediJob = UsbdgDataProcessorService.PopulateEdiJobObject(usbdgReportMetadata, indigoLogFiles);

                    log.LogInformation($"- Map {logType} objects to csv records");
                    List<IndigoV2EventRecord> usbdbLogCsvRows = DataModelMappingService.MapIndigoV2Events(indigoLogFiles, ediJob);
                    //List<EdiSinkRecord> indigoLocationCsvRows = DataModelMappingService.MapIndigoV2Locations(usbdgReportMetadata, ediJob);
                    List<EdiSinkRecord> usbdgLocationCsvRows = DataModelMappingService.MapUsbdgLocations(usbdgReportMetadata, ediJob);
                    List<EdiSinkRecord> usbdgDeviceCsvRows = DataModelMappingService.MapUsbdgDevice(usbdgReportMetadata);
                    List<EdiSinkRecord> usbdgEventCsvRows = DataModelMappingService.MapUsbdgEvent(usbdgReportMetadata);


                    log.LogInformation($"- Transform {logType} csv records");
                    log.LogInformation($"  - Convert relative time to total seconds (all records)");
                    usbdbLogCsvRows = IndigoDataTransformService.ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(usbdbLogCsvRows);

                    log.LogInformation($"  - Sort csv records using relative time total seconds");
                    List<IndigoV2EventRecord> sortedUsbdbLogCsvRows = usbdbLogCsvRows.OrderBy(i => (i._RELT_SECS)).ToList();

                    log.LogInformation($"  - Convert relative time (e.g., 'P9DT59M53S') to total seconds (report only)");
                    int DurationSecs = IndigoDataTransformService.ConvertRelativeTimeStringToTotalSeconds(usbdgReportMetadata); // convert timespan to seconds

                    log.LogInformation($"  - Calculate absolute time for each record using record relative time (e.g., 781193) and report absolute time ('2021-06-20T23:00:02Z')");
                    sortedUsbdbLogCsvRows = IndigoDataTransformService.CalculateAbsoluteTimeForUsbdgRecords(sortedUsbdbLogCsvRows, DurationSecs, usbdgReportMetadata);

                    log.LogInformation($"  - Cloud upload times: ");
                    log.LogInformation($"    - EMD (source: cellular) : {DateConverter.ConvertIso8601CompliantString(emdAbsoluteTime)} (UTC)");
                    log.LogInformation($"    - Logger (source: real time clock) : {emdRelativeTime ?? ""} (Relative Time)");
                    log.LogInformation($"    - Logger (source: real time clock) : {IndigoDataTransformService.ConvertRelativeTimeStringToTotalSeconds(emdRelativeTime)} (Duration in Seconds)");
                    log.LogInformation($"  - Absolute time calculation results (first two records): ");
                    if (usbdbLogCsvRows.Count > 1)
                    {
                        log.LogInformation($"    - record[0].ElapsedSecs (Elapsed secs from activation time): {IndigoDataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[0].RELT)}");
                        log.LogInformation($"    - record[0].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[0].RELT}");
                        log.LogInformation($"    - record[0]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[0]._RELT_SECS}");
                        log.LogInformation($"    - record[0]._ABST (calculated absolute time): {usbdbLogCsvRows[0].EDI_RECORD_ABST_CALC}");
                        log.LogInformation($" ");
                        log.LogInformation($"    - record[1].ElapsedSecs (Elapsed secs from activation time): {IndigoDataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, usbdbLogCsvRows[1].RELT)}");
                        log.LogInformation($"    - record[1].RELT (Logger cloud upload relative time): {usbdbLogCsvRows[1].RELT}");
                        log.LogInformation($"    - record[1]._RELT_SECS (Logger cloud upload relative time seconds): {usbdbLogCsvRows[1]._RELT_SECS}");
                        log.LogInformation($"    - record[1]._ABST (calculated absolute time): {usbdbLogCsvRows[1].EDI_RECORD_ABST_CALC}");
                    }

                    log.LogInformation($"- Write {logType} csv records to azure blob storage");
                    List<EdiSinkRecord> sortedUsbdbLogCsvRowsBase = sortedUsbdbLogCsvRows.Cast<EdiSinkRecord>().ToList();
                    string csvOutputBlobName = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, sortedUsbdbLogCsvRowsBase, DataLoggerTypeEnum.Name.INDIGO_V2, log);
                    //string csvOutputBlobName2 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, indigoLocationCsvRows, DataLoggerTypeEnum.Name.INDIGO_V2, log);
                    string csvOutputBlobName3 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, DataLoggerTypeEnum.Name.INDIGO_V2, log);
                    string csvOutputBlobName4 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, DataLoggerTypeEnum.Name.INDIGO_V2, log);
                    string csvOutputBlobName5 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, DataLoggerTypeEnum.Name.INDIGO_V2, log);

                    log.LogInformation(" - Serialize http response body");
                    string responseBody = DataTransformService.SerializeHttpResponseBody(csvOutputBlobName);

                    log.LogInformation(" - Send http response message");
                    log.LogInformation("- Log successfully completed event to app insights");
                    IndigoDataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, DataLoggerTypeEnum.Name.INDIGO_V2, log);
                    log.LogInformation(" - SUCCESS");

                    return new OkObjectResult(responseBody);
                // Account for file packages with no logger data files
                } else if (UsbdgDataProcessorService.IsFilePackageUsbdgOnly(logDirectoryBlobs)) {

                    log.LogInformation($"- Pull usbdg report metadata blob from file package");
                    CloudBlockBlob usbdgReportMetadataBlob = UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);

                    log.LogInformation($"- Download USBDG report metadata blob");
                    dynamic usbdgReportMetadata = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, log);

                    log.LogInformation($"- Validate USBDG report metadata blob");
                    dynamic validatedUsbdgReportMetadataFile = await DataTransformService.ValidateJsonObject(emsConfgContainer, usbdgReportMetadata, jsonSchemaBlobNameUsbdgMetadata, log);

                    dynamic usbdgRecords = UsbdgDataProcessorService.GetUsbdgMetadataRecordsElement(usbdgReportMetadata);

                    log.LogInformation($"- Retrieving time values from EMD metadata");
                    string emdRelativeTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "RELT");
                    string emdAbsoluteTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "ABST");

                    log.LogInformation($"- Start tracking EDI job status");
                    EdiJob ediJob = UsbdgDataProcessorService.PopulateEdiJobObject(usbdgReportMetadata, null);

                    log.LogInformation($"- Map {logType} objects to csv records");
                    List<EdiSinkRecord> usbdgLocationCsvRows = DataModelMappingService.MapUsbdgLocations(usbdgReportMetadata, ediJob);
                    List<EdiSinkRecord> usbdgDeviceCsvRows = DataModelMappingService.MapUsbdgDevice(usbdgReportMetadata);
                    List<EdiSinkRecord> usbdgEventCsvRows = DataModelMappingService.MapUsbdgEvent(usbdgReportMetadata);

                    log.LogInformation($"- Write {logType} csv records to azure blob storage");
                    string csvOutputBlobName3 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, DataLoggerTypeEnum.Name.NO_LOGGER, log);
                    string csvOutputBlobName4 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, DataLoggerTypeEnum.Name.NO_LOGGER, log);
                    string csvOutputBlobName5 = await IndigoDataTransformService.WriteUsbdgLogRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, DataLoggerTypeEnum.Name.NO_LOGGER, log);

                    log.LogInformation(" - Serialize http response body");
                    string responseBody = DataTransformService.SerializeHttpResponseBody(csvOutputBlobName3);

                    log.LogInformation(" - Send http response message");
                    log.LogInformation("- Log successfully completed event to app insights");
                    IndigoDataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, DataLoggerTypeEnum.Name.NO_LOGGER, log);
                    log.LogInformation(" - SUCCESS");

                    return new OkObjectResult(responseBody);
                } else {
                    string errorCode = "KHRD";
                    string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                    IndigoDataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, null, errorCode);
                    //string errorMessage = $"Unknown file package";
                    log.LogError($"- {errorMessage}");
                    var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                    result.StatusCode = 500;
                    return result;
                }
            }
            catch (Exception e)
            {
                string errorCode = "E2N8";
                
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                string innerErrorCode = EdiErrorsService.GetInnerErrorCodeFromMessage(errorMessage, errorCode);
                IndigoDataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, e, innerErrorCode);
                if (e is BadRequestException)
                {
                    string errStr = $"Bad request thrown while validating {logType} transformation request";
                    log.LogError($"- {errStr}");
                    log.LogError($" - {errorMessage}");
                    return new BadRequestObjectResult(errorMessage);
                }
                else
                {
                    string errStr = $"Global level exception thrown while processing {logType} logs";
                    log.LogError($"- {errStr}");
                    log.LogError($" - {errorMessage}");
                    var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                    result.StatusCode = 500;
                    return result;
                }
            }
        }
    }
}
