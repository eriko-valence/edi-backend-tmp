using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using lib_edi.Services.Errors;
using lib_edi.Exceptions;
using lib_edi.Services.System.Net;
using lib_edi.Services.Loggers;
using System.Collections.Generic;
using System.Linq;
using Azure.Storage.Blobs.Models;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Azure;
using lib_edi.Services.CceDevice;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Csv;
using lib_edi.Models.Edi;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Ems;
using lib_edi.Models.Enums.Azure.AppInsights;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;

namespace fa_adf_transform_indigo_v2
{
    public class Transform
    {

        private readonly ILogger<Transform> _logger;

        public Transform(ILogger<Transform> logger)
        {
            _logger = logger;
        }

        [Function("transform")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            [BlobInput("%AZURE_STORAGE_BLOB_CONTAINER_NAME_INPUT_UNCOMPRESSED%", Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] BlobContainerClient inputContainer,
            [BlobInput("%AZURE_STORAGE_BLOB_CONTAINER_NAME_OUTPUT_PROCESSED%", Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] BlobContainerClient ouputContainer,
            [BlobInput("%AZURE_STORAGE_BLOB_CONTAINER_NAME_EMS_CONFIG%", Connection = "AZURE_STORAGE_INPUT_CONNECTION_STRING")] BlobContainerClient emsConfgContainer)
        {
            string loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            string verfiedLoggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            string emdType = EmdEnum.Name.UNKNOWN.ToString();
            EmdEnum.Name emdTypeEnum = EmdEnum.Name.UNKNOWN;
            DataLoggerTypeEnum.Name loggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
            DataLoggerTypeEnum.Name verifiedLoggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
            TransformHttpRequestMessageBodyDto payload = null;

            try
            {
                
                string jsonSchemaBlobNameEmsCompliantLog = Environment.GetEnvironmentVariable("EMS_JSON_SCHEMA_FILENAME");
                string jsonSchemaBlobNameUsbdgMetadata = Environment.GetEnvironmentVariable("EMS_USBDG_METADATA_JSON_SCHEMA_FILENAME");

                payload = await HttpService.DeserializeHttpRequestBody(req);
                _logger.LogInformation($"- {payload.FileName} - Start processing incoming usbdg transformation request");
                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                _logger.LogInformation($"- {payload.FileName}   - Container : {inputContainer.Name}");
                _logger.LogInformation($"- {payload.FileName}   - Path      : {payload.Path}");
                _logger.LogInformation($"- {payload.FileName}   - Type      : {payload.LoggerType}");
                HttpService.ValidateHttpRequestBody(payload);
                DataTransformService.LogEmsTransformStartedEventToAppInsights(payload.FileName, PipelineStageEnum.Name.ADF_TRANSFORM, _logger);

                emdType = payload.LoggerType ?? EmdEnum.Name.UNKNOWN.ToString();
                emdTypeEnum = EmsService.GetEmdType(emdType);
                loggerType = payload.LoggerType ?? DataLoggerTypeEnum.Name.UNKNOWN.ToString();
                loggerTypeEnum = EmsService.GetDataLoggerType(loggerType);

                IEnumerable<BlobItem> logDirectoryBlobs = await AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                if (EmsService.IsFilePackageContentsEms(logDirectoryBlobs) && EmsService.ValidateCceDeviceType(loggerType))
                {
                    _logger.LogInformation($"- {payload.FileName} - Detected report package type: USBDG collected (with logger data files)");
                    _logger.LogInformation($"- {payload.FileName} - Download and validate package contents");
                    List<BlobItem> usbdgLogBlobs = await DataTransformService.GetLogBlobs(logDirectoryBlobs, inputBlobPath);
                    BlobItem usbdgReportMetadataBlob = await UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);
                    List<dynamic> emsLogFiles = await AzureStorageBlobService.DownloadAndDeserializeJsonBlobs(usbdgLogBlobs, inputContainer, inputBlobPath, _logger, payload?.FileName, emdTypeEnum, loggerTypeEnum);
                    dynamic usbdgMetadataJsonObject = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, _logger);
                    await DataTransformService.ValidateJsonObject(emsConfgContainer, usbdgMetadataJsonObject, jsonSchemaBlobNameUsbdgMetadata, _logger);
                    await DataTransformService.ValidateJsonObjects(emsConfgContainer, emsLogFiles, jsonSchemaBlobNameEmsCompliantLog, _logger);
                    EdiJob ediJob = await UsbdgDataProcessorService.PopulateEdiJobObject(usbdgMetadataJsonObject, emsLogFiles, usbdgLogBlobs, usbdgReportMetadataBlob, payload.FileName, inputBlobPath, emdTypeEnum, loggerTypeEnum);
                    string emdAbsoluteTime = UsbdgDataProcessorService.GetUsbdgMountTimeAbst(ediJob);
                    string emdRelativeTime = DataTransformService.GetUsbdgMountTimeRelt(ediJob);
                    verfiedLoggerType = ediJob.Logger.Type.ToString().ToLower();

                    if (ediJob.Logger.Type != DataLoggerTypeEnum.Name.UNKNOWN)
                    {
                        _logger.LogInformation($"- {payload.FileName} - Transform package contents");
                        List<EmsEventRecord> emsEventCsvRows = await DataModelMappingService.MapEmsLoggerEvents(emsLogFiles, ediJob);
                        List<EdiSinkRecord> usbdgLocationCsvRows = await DataModelMappingService.MapUsbdgLocations(usbdgMetadataJsonObject, ediJob);
                        List<EdiSinkRecord> usbdgDeviceCsvRows = await DataModelMappingService.MapUsbdgDevice(usbdgMetadataJsonObject);
                        List<EdiSinkRecord> usbdgEventCsvRows = await DataModelMappingService.MapUsbdgEvent(usbdgMetadataJsonObject);
                        emsEventCsvRows = await DataTransformService.ConvertRelativeTimeToTotalSecondsForEmsLogRecords(emsEventCsvRows);
                        List<EmsEventRecord> sortedEmsEventCsvRows = emsEventCsvRows.OrderBy(i => (i.EDI_RELT_ELAPSED_SECS)).ToList();
                        int DurationSecs = await DataTransformService.ConvertRelativeTimeStringToTotalSeconds(usbdgMetadataJsonObject, ediJob); // convert timespan to seconds
                        sortedEmsEventCsvRows = await UsbdgDataProcessorService.CalculateAbsoluteTimeForUsbdgRecords(sortedEmsEventCsvRows, ediJob);
                        List<EdiSinkRecord> sortedEmsEventCsvRowsFinal = sortedEmsEventCsvRows.Cast<EdiSinkRecord>().ToList();

                        //string r1 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, sortedEmsEventCsvRowsFinal, verfiedLoggerType, log);
                        //string r2 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, verfiedLoggerType, log);
                        //string r3 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, verfiedLoggerType, log);
                        //string r4 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, verfiedLoggerType, log);

                        _logger.LogInformation($"- {payload.FileName} - Upload curated output to blob storage");
                        ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, sortedEmsEventCsvRowsFinal, verfiedLoggerType, _logger));
                        ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, verfiedLoggerType, _logger));
                        ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, verfiedLoggerType, _logger));
                        ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, verfiedLoggerType, _logger));

                        _logger.LogInformation($"- {payload.FileName} - Send transformation response");
                        string blobPathFolderCurated = DataTransformService.BuildCuratedBlobFolderPath(payload.Path, verfiedLoggerType);
                        string responseBody = await DataTransformService.SerializeHttpResponseBody(blobPathFolderCurated, ediJob.Emd.Type.ToString());
                        DataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, ediJob.Emd.Type, ediJob.Logger.Type, PipelineStageEnum.Name.ADF_TRANSFORM, _logger);
                        _logger.LogInformation($"- {payload.FileName} - Done");

                        _logger.LogInformation($" PROCESSING SUMMARY");
                        UsbdgDataProcessorService.LogEmsPackageInformation(_logger, sortedEmsEventCsvRows, ediJob);

                        return new OkObjectResult(responseBody);
                    } else {
                        // NHGH-2710 (2022.11.18) - Set to unknown LMOD property value not on supported EMS logger list, so 
                        loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
                        loggerTypeEnum = EmsService.GetDataLoggerType(loggerType);
                        string errorCode = "EHN9";
                        string errorMessage = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(ediJob.Logger.LMOD));
                        DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM, _logger, null, errorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNSUPPORTED_EMS_DEVICE);
                        //string errorMessage = $"Unknown file package";
                        _logger.LogError($"- {payload.FileName} - {errorMessage}");
                        var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                        result.StatusCode = 500;
                        return result;
                    }
                // Account for file packages with no logger data files
                } else if (UsbdgDataProcessorService.IsFilePackageUsbdgOnly(logDirectoryBlobs) && EmsService.ValidateCceDeviceType(loggerType)) {

                    _logger.LogInformation($"- {payload.FileName} - Detected report package type: USBDG collected (no logger data files)");
                    _logger.LogInformation($"- {payload.FileName} - Download and validate package contents");
                    // NHGH-2819 2023.03.16 0950 USBDG collected package with no logger data
                    emdTypeEnum = EmdEnum.Name.USBDG;
                    loggerTypeEnum = DataLoggerTypeEnum.Name.NO_LOGGER;
                    BlobItem usbdgReportMetadataBlob = await UsbdgDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);
                    dynamic usbdgReportMetadata = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(usbdgReportMetadataBlob, inputContainer, inputBlobPath, _logger);
                    dynamic validatedUsbdgReportMetadataFile = await DataTransformService.ValidateJsonObject(emsConfgContainer, usbdgReportMetadata, jsonSchemaBlobNameUsbdgMetadata, _logger);
                    dynamic usbdgRecords = UsbdgDataProcessorService.GetUsbdgMetadataRecordsElement(usbdgReportMetadata);
                    string emdRelativeTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "RELT");
                    string emdAbsoluteTime = DataTransformService.GetJObjectPropertyValueAsString(usbdgRecords, "ABST");
                    EdiJob ediJob = await UsbdgDataProcessorService.PopulateEdiJobObject(usbdgReportMetadata, null, null, usbdgReportMetadataBlob, payload.FileName, inputBlobPath, emdTypeEnum, loggerTypeEnum);

                    _logger.LogInformation($"- {payload.FileName} - Transform package contents");
                    List<EdiSinkRecord> usbdgLocationCsvRows = await DataModelMappingService.MapUsbdgLocations(usbdgReportMetadata, ediJob);
                    List<EdiSinkRecord> usbdgDeviceCsvRows = await DataModelMappingService.MapUsbdgDevice(usbdgReportMetadata);
                    List<EdiSinkRecord> usbdgEventCsvRows = await DataModelMappingService.MapUsbdgEvent(usbdgReportMetadata);

                    //string r1 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, loggerType, log);
                    //string r2 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, loggerType, log);
                    //string r3 = await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, loggerType, log);
                    _logger.LogInformation($"- {payload.FileName} - Upload curated output to blob storage");
                    ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgDeviceCsvRows, loggerType, _logger));
                    ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgEventCsvRows, loggerType, _logger));
                    ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, usbdgLocationCsvRows, loggerType, _logger));

                    _logger.LogInformation($"- {payload.FileName} - Send transformation response");
                    string responseBody = await DataTransformService.SerializeHttpResponseBody(ediJob.Emd.PackageFiles.CuratedFiles[0], ediJob.Emd.Type.ToString());
                    DataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, ediJob.Emd.Type, loggerTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM, _logger);
                    _logger.LogInformation($"- {payload.FileName} - Done");

                    _logger.LogInformation($" PROCESSING SUMMARY");
                    UsbdgDataProcessorService.LogEmsPackageInformation(_logger, null, ediJob);

                    return new OkObjectResult(responseBody);
                } else {
                    string errorCode = "KHRD";
                    string errorMessage = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                    DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM, _logger, null, errorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_FILE_PACKAGE);
                    //string errorMessage = $"Unknown file package";
                    _logger.LogError($"- {payload.FileName} - {errorMessage}");
                    var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                    result.StatusCode = 500;
                    return result;
                }
            }
            catch (Exception e)
            {
                string errorCode = "E2N8";
                _logger.LogError($"- {payload.FileName} - error code : " + errorCode);

				string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                string innerErrorCode = EdiErrorsService.GetInnerErrorCodeFromMessage(errorMessage, errorCode);
                DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM, _logger, e, innerErrorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION);
                if (e is BadRequestException)
                {
                    string errStr = $"Bad request thrown while validating '{loggerType}' transformation request";
                    _logger.LogError($"- {payload.FileName} - {errStr}");
                    _logger.LogError($"- {payload.FileName}   - {errorMessage}");
                    return new BadRequestObjectResult(errorMessage);
                }
                else
                {
                    string errStr = $"Global level exception thrown while processing '{loggerType}' logs";
                    _logger.LogError($"- {payload.FileName} - {errStr}");
                    _logger.LogError($"- {payload.FileName}   - {errorMessage}");
                    var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                    result.StatusCode = 500;
                    return result;
                }
            }
        }
    }
}
