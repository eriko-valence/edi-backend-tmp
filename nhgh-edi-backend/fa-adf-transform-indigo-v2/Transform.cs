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
using System.Net;
using lib_edi.Services.Ems;
using lib_edi.Models.Enums.Azure.AppInsights;

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
            string loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            DataLoggerTypeEnum.Name loggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
            TransformHttpRequestMessageBodyDto payload = null;

            try
            {
                
                string jsonSchemaBlobNameEmsCompliantLog = Environment.GetEnvironmentVariable("EMS_JSON_SCHEMA_FILENAME");
                string jsonSchemaBlobNameUsbdgMetadata = Environment.GetEnvironmentVariable("EMS_USBDG_METADATA_JSON_SCHEMA_FILENAME");

                log.LogInformation($"- Deserialize log transformation http request body");
                payload = await HttpService.DeserializeHttpRequestBody(req);

                log.LogInformation("- Validate http request body");
                HttpService.ValidateHttpRequestBody(payload);

                log.LogInformation("- Log started event to app insights");
                DataTransformService.LogEmsTransformStartedEventToAppInsights(payload.FileName, log);

                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                log.LogInformation($"- Incoming blob path: {inputBlobPath}");

                loggerType = payload.LoggerType ?? DataLoggerTypeEnum.Name.UNKNOWN.ToString();
                loggerTypeEnum = EmsService.GetDataLoggerType(loggerType);
                log.LogInformation($"- Detected logger type: '{loggerType}'");

                log.LogInformation($"- Pull all blobs from file package {inputBlobPath}");
                IEnumerable<IListBlobItem> logDirectoryBlobs = AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                //logType = DataTransformService.DetermineFilePackageType(logDirectoryBlobs);

                if (EmsService.IsFilePackageContentsEms(logDirectoryBlobs) && EmsService.ValidateLoggerType(loggerType))
                {
                    log.LogInformation($"- Pull '{loggerType}' log blobs from file package");  
                    List<CloudBlockBlob> usbdgLogBlobs = DataTransformService.GetLogBlobs(logDirectoryBlobs, inputBlobPath);

                    log.LogInformation($"- Pull usbdg report metadata blob from file package");
                    CloudBlockBlob usbdgReportMetadataBlob = UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);

                    log.LogInformation($"- Download '{loggerType}' log blobs");
                    List<dynamic> emsLogFiles = await AzureStorageBlobService.DownloadAndDeserializeJsonBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, log);
                    log.LogInformation($"- Download usbdg report metadata blob");
                    dynamic usbdgReportMetadata = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, log);

                    dynamic usbdgRecords = UsbdgDataProcessorService.GetUsbdgMetadataRecordsElement(usbdgReportMetadata);

                    log.LogInformation($"- Retrieving time values from EMD metadata");
                    string emdRelativeTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "RELT");
                    string emdAbsoluteTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "ABST");

                    log.LogInformation($"- Validate USBDG report metadata blob");
                    dynamic validatedUsbdgReportMetadataFile = await DataTransformService.ValidateJsonObject(emsConfgContainer, usbdgReportMetadata, jsonSchemaBlobNameUsbdgMetadata, log);

                    log.LogInformation($"- Validate '{loggerType}' log blobs");
                    List<dynamic> validatedUsbdgLogFiles = await DataTransformService.ValidateJsonObjects(emsConfgContainer, emsLogFiles, jsonSchemaBlobNameEmsCompliantLog, log);

                    log.LogInformation($"- Start tracking EDI job status");
                    EdiJob ediJob = UsbdgDataProcessorService.PopulateEdiJobObject(usbdgReportMetadata, emsLogFiles);

                    log.LogInformation($"- Assess EMS logger type using EMS log LMOD property");
                    EmsLoggerModelCheckResult loggerModelCheckResult = EmsService.GetEmsLoggerModelFromEmsLogLmodProperty(ediJob.Logger.LMOD);

                    if (loggerModelCheckResult.IsSupported)
                    {
                        loggerType = loggerModelCheckResult.LoggerModel.ToString().ToLower();
                        log.LogInformation($"- Map '{loggerType}' objects to csv records");
                        //List<IndigoV2EventRecord> usbdbLogCsvRows = DataModelMappingService.MapIndigoV2Events(emsLogFiles, ediJob);
                        List<EmsEventRecord> emsEventCsvRows = DataModelMappingService.MapEmsLoggerEvents(emsLogFiles, loggerType, ediJob);
                        //List<EdiSinkRecord> indigoLocationCsvRows = DataModelMappingService.MapIndigoV2Locations(usbdgReportMetadata, ediJob);
                        List<EdiSinkRecord> usbdgLocationCsvRows = DataModelMappingService.MapUsbdgLocations(usbdgReportMetadata, ediJob);
                        List<EdiSinkRecord> usbdgDeviceCsvRows = DataModelMappingService.MapUsbdgDevice(usbdgReportMetadata);
                        List<EdiSinkRecord> usbdgEventCsvRows = DataModelMappingService.MapUsbdgEvent(usbdgReportMetadata);

                        log.LogInformation($"- Transform '{loggerType}' csv records");
                        log.LogInformation($"  - Convert relative time to total seconds (all records)");
                        emsEventCsvRows = DataTransformService.ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(emsEventCsvRows);

                        log.LogInformation($"  - Sort csv records using relative time total seconds");
                        List<EmsEventRecord> sortedEmsEventCsvRows = emsEventCsvRows.OrderBy(i => (i._RELT_SECS)).ToList();

                        log.LogInformation($"  - Convert relative time (e.g., 'P9DT59M53S') to total seconds (report only)");
                        int DurationSecs = DataTransformService.ConvertRelativeTimeStringToTotalSeconds(usbdgReportMetadata); // convert timespan to seconds

                        log.LogInformation($"  - Calculate absolute time for each record using record relative time (e.g., 781193) and report absolute time ('2021-06-20T23:00:02Z')");
                        sortedEmsEventCsvRows = DataTransformService.CalculateAbsoluteTimeForUsbdgRecords(sortedEmsEventCsvRows, DurationSecs, usbdgReportMetadata);

                        log.LogInformation($"  - Cloud upload times: ");
                        log.LogInformation($"    - EMD (source: cellular) : {DateConverter.ConvertIso8601CompliantString(emdAbsoluteTime)} (UTC)");
                        log.LogInformation($"    - Logger (source: real time clock) : {emdRelativeTime ?? ""} (Relative Time)");
                        log.LogInformation($"    - Logger (source: real time clock) : {DataTransformService.ConvertRelativeTimeStringToTotalSeconds(emdRelativeTime)} (Duration in Seconds)");
                        log.LogInformation($"  - Absolute time calculation results (first two records): ");
                        if (sortedEmsEventCsvRows.Count > 1)
                        {
                            log.LogInformation($"    - record[0].ElapsedSecs (Elapsed secs from activation time): {DataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, sortedEmsEventCsvRows[0].RELT)}");
                            log.LogInformation($"    - record[0].RELT (Logger cloud upload relative time): {sortedEmsEventCsvRows[0].RELT}");
                            log.LogInformation($"    - record[0]._RELT_SECS (Logger cloud upload relative time seconds): {sortedEmsEventCsvRows[0]._RELT_SECS}");
                            log.LogInformation($"    - record[0]._ABST (calculated absolute time): {sortedEmsEventCsvRows[0].EDI_RECORD_ABST_CALC}");
                            log.LogInformation($" ");
                            log.LogInformation($"    - record[1].ElapsedSecs (Elapsed secs from activation time): {DataTransformService.CalculateElapsedSecondsFromLoggerActivationRelativeTime(emdRelativeTime, sortedEmsEventCsvRows[1].RELT)}");
                            log.LogInformation($"    - record[1].RELT (Logger cloud upload relative time): {sortedEmsEventCsvRows[1].RELT}");
                            log.LogInformation($"    - record[1]._RELT_SECS (Logger cloud upload relative time seconds): {sortedEmsEventCsvRows[1]._RELT_SECS}");
                            log.LogInformation($"    - record[1]._ABST (calculated absolute time): {sortedEmsEventCsvRows[1].EDI_RECORD_ABST_CALC}");
                        }

                        log.LogInformation($"- Write '{loggerType}' csv records to azure blob storage");
                        List<EdiSinkRecord> sortedEmsEventCsvRowsFinal = sortedEmsEventCsvRows.Cast<EdiSinkRecord>().ToList();

                        string r1 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, sortedEmsEventCsvRowsFinal, loggerType, log);
                        string r2 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, loggerType, log);
                        string r3 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, loggerType, log);
                        string r4 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, loggerType, log);

                        string blobPathFolderCurated = DataTransformService.BuildCuratedBlobFolderPath(payload.Path, loggerType);

                        log.LogInformation(" - Serialize http response body");
                        string responseBody = DataTransformService.SerializeHttpResponseBody(blobPathFolderCurated);

                        log.LogInformation(" - Send http response message");
                        log.LogInformation("- Send successfully completed event to app insights");
                        DataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, loggerTypeEnum, log);
                        log.LogInformation(" - SUCCESS");

                        return new OkObjectResult(responseBody);
                    } else {
                        string errorCode = "EHN9";
                        string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                        DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, null, errorCode, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNSUPPORTED_EMS_DEVICE);
                        //string errorMessage = $"Unknown file package";
                        log.LogError($"- {errorMessage}");
                        var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                        result.StatusCode = 500;
                        return result;
                    }
                // Account for file packages with no logger data files
                } else if (UsbdgDataProcessorService.IsFilePackageUsbdgOnly(logDirectoryBlobs) && EmsService.ValidateLoggerType(loggerType)) {

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

                    log.LogInformation($"- Map '{loggerType}' objects to csv records");
                    List<EdiSinkRecord> usbdgLocationCsvRows = DataModelMappingService.MapUsbdgLocations(usbdgReportMetadata, ediJob);
                    List<EdiSinkRecord> usbdgDeviceCsvRows = DataModelMappingService.MapUsbdgDevice(usbdgReportMetadata);
                    List<EdiSinkRecord> usbdgEventCsvRows = DataModelMappingService.MapUsbdgEvent(usbdgReportMetadata);

                    log.LogInformation($"- Write '{loggerType}' csv records to azure blob storage");
                    string r1 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, loggerType, log);
                    string r2 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, loggerType, log);
                    string r3 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, loggerType, log);

                    log.LogInformation(" - Serialize http response body");
                    string responseBody = DataTransformService.SerializeHttpResponseBody(r1);

                    log.LogInformation(" - Send http response message");
                    log.LogInformation("- Log successfully completed event to app insights");
                    DataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, loggerTypeEnum, log);
                    log.LogInformation(" - SUCCESS");

                    return new OkObjectResult(responseBody);
                } else {
                    string errorCode = "KHRD";
                    string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                    DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, null, errorCode, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_FILE_PACKAGE);
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
                DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, log, e, innerErrorCode, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION);
                if (e is BadRequestException)
                {
                    string errStr = $"Bad request thrown while validating '{loggerType}' transformation request";
                    log.LogError($"- {errStr}");
                    log.LogError($" - {errorMessage}");
                    return new BadRequestObjectResult(errorMessage);
                }
                else
                {
                    string errStr = $"Global level exception thrown while processing '{loggerType}' logs";
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
