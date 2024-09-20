using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
//using Microsoft.WindowsAzure.Storage.Blob;
using System.Collections.Generic;
using lib_edi.Services.Errors;
using lib_edi.Services.CceDevice;
using lib_edi.Exceptions;
using lib_edi.Models.Csv;
using lib_edi.Models.Edi;
using lib_edi.Services.Loggers;
using lib_edi.Services.Azure;
using lib_edi.Services.Ems;
using lib_edi.Services.System.Net;
using lib_edi.Models.Enums.Emd;
using lib_edi.Models.Dto.Http;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Services.Data.Transform;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs;
using Microsoft.Azure.Functions.Worker;
using System.Linq;

namespace fa_adf_transform_varo
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
            //string loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            string verfiedLoggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
            string emdType = EmdEnum.Name.UNKNOWN.ToString();
            EmdEnum.Name emdTypeEnum = EmdEnum.Name.UNKNOWN;
            DataLoggerTypeEnum.Name loggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
            DataLoggerTypeEnum.Name verifiedLoggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
            TransformHttpRequestMessageBodyDto payload = null;
            string loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();

            try
            {

                string jsonSchemaBlobNameEmsCompliantLog = Environment.GetEnvironmentVariable("EMS_JSON_SCHEMA_FILENAME");

                payload = await HttpService.DeserializeHttpRequestBody(req);
                _logger.LogInformation($"- {payload.FileName} - Start processing incoming varo transformation request");
                await HttpService.ValidateHttpRequestBody(payload);
                DataTransformService.LogEmsTransformStartedEventToAppInsights(payload.FileName, PipelineStageEnum.Name.ADF_TRANSFORM_VARO, _logger);

                string inputBlobPath = $"{inputContainer.Name}/{payload.Path}";
                _logger.LogInformation($"- {payload.FileName}   - Container : {inputContainer.Name}");
                _logger.LogInformation($"- {payload.FileName}   - Path      : {payload.Path}");
                _logger.LogInformation($"- {payload.FileName}   - Type      : {payload.LoggerType}");

                emdType = payload.LoggerType ?? EmdEnum.Name.UNKNOWN.ToString();
                emdTypeEnum = EmsService.GetEmdType(emdType);
                loggerType = payload.LoggerType ?? DataLoggerTypeEnum.Name.UNKNOWN.ToString();
                loggerTypeEnum = EmsService.GetDataLoggerType(loggerType);

                IEnumerable<BlobItem> logDirectoryBlobs = await AzureStorageBlobService.GetListOfBlobsInDirectory(inputContainer, payload.Path, inputBlobPath);

                if (VaroDataProcessorService.IsThisVaroCollectedEmsReportPackage(logDirectoryBlobs, emdType))
                {
                    _logger.LogInformation($"- {payload.FileName} - Download and validate package contents");
                    List<BlobItem> varoCollectedEmsLogBlobs = await DataTransformService.GetLogBlobs(logDirectoryBlobs, inputBlobPath);
                    BlobItem varoReportMetadataBlob = await VaroDataProcessorService.GetReportMetadataBlob(logDirectoryBlobs, inputBlobPath);
                    List<dynamic> emsLogFiles = await AzureStorageBlobService.DownloadAndDeserializeJsonBlobs(varoCollectedEmsLogBlobs, inputContainer, inputBlobPath, _logger, payload?.FileName, emdTypeEnum, loggerTypeEnum);
                    dynamic varoReportMetadataObject = await AzureStorageBlobService.DownloadAndDeserializeJsonBlob(varoReportMetadataBlob, inputContainer, inputBlobPath, _logger);
                    await DataTransformService.ValidateJsonObjects(emsConfgContainer, emsLogFiles, jsonSchemaBlobNameEmsCompliantLog, _logger);
                    EdiJob ediJob = await VaroDataProcessorService.PopulateEdiJobObject(varoReportMetadataObject, emsLogFiles, varoCollectedEmsLogBlobs, varoReportMetadataBlob, payload.FileName, inputBlobPath, emdTypeEnum, loggerTypeEnum);
                    verfiedLoggerType = ediJob.Logger.Type.ToString().ToLower();

                    if (ediJob.Logger.Type != DataLoggerTypeEnum.Name.UNKNOWN)
                    {
                        _logger.LogInformation($"- {payload.FileName} - Transform package contents");
                        List<EmsEventRecord> emsEventCsvRows = await DataModelMappingService.MapEmsLoggerEvents(emsLogFiles, ediJob);
                        List<EdiSinkRecord> varoLocationsCsvRows = await DataModelMappingService.MapVaroLocations(ediJob);

						emsEventCsvRows = await DataTransformService.ConvertRelativeTimeToTotalSecondsForEmsLogRecords(emsEventCsvRows);
                        List<EmsEventRecord> sortedEmsEventCsvRows = emsEventCsvRows.OrderBy(i => (i.EDI_RELT_ELAPSED_SECS)).ToList();
                        sortedEmsEventCsvRows = await VaroDataProcessorService.CalculateAbsoluteTimeForVaroCollectedRecords(sortedEmsEventCsvRows, ediJob);                        
                        List<EdiSinkRecord> sortedEmsEventCsvRowsFinal = sortedEmsEventCsvRows.Cast<EdiSinkRecord>().ToList();

                        _logger.LogInformation($"- {payload.FileName} - Upload curated output to blob storage");
                        ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, sortedEmsEventCsvRowsFinal, verfiedLoggerType, _logger));
						ediJob.Emd.PackageFiles.CuratedFiles.Add(await DataTransformService.WriteRecordsToCsvBlob(ouputContainer, payload, varoLocationsCsvRows, verfiedLoggerType, _logger));

                        _logger.LogInformation($"- {payload.FileName} - Send transformation response");
                        string blobPathFolderCurated = DataTransformService.BuildCuratedBlobFolderPath(payload.Path, verfiedLoggerType);
                        string responseBody = await DataTransformService.SerializeHttpResponseBody(blobPathFolderCurated, ediJob.Emd.Type.ToString());
                        DataTransformService.LogEmsTransformSucceededEventToAppInsights(payload.FileName, ediJob.Emd.Type, ediJob.Logger.Type, PipelineStageEnum.Name.ADF_TRANSFORM_VARO, _logger);
                        _logger.LogInformation($"- {payload.FileName} - Done");

                        _logger.LogInformation($" PROCESSING SUMMARY");
                        VaroDataProcessorService.LogEmsPackageInformation(_logger, sortedEmsEventCsvRows, ediJob);

                        return new OkObjectResult(responseBody);
                    }
                    else
                    {
                        // NHGH-2710 (2022.11.18) - Set to unknown LMOD property value not on supported EMS logger list, so 
                        //string loggerType = DataLoggerTypeEnum.Name.UNKNOWN.ToString();
                        //loggerTypeEnum = EmsService.GetDataLoggerType(loggerType);
                        loggerTypeEnum = DataLoggerTypeEnum.Name.UNKNOWN;
                        string errorCode = "EHN9";
                        string errorMessage = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(ediJob.Logger.LMOD));
                        DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM_VARO, _logger, null, errorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNSUPPORTED_EMS_DEVICE);
                        //string errorMessage = $"Unknown file package";
                        _logger.LogError($"- {payload.FileName} - {errorMessage}");
                        var result = new ObjectResult(new { statusCode = 500, currentDate = DateTime.Now, message = errorMessage });
                        result.StatusCode = 500;
                        return result;
                    }
                    // Account for file packages with no logger data files
                } 
                else 
                {
                    string errorCode = "KHRD";
                    string errorMessage = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(payload.FileName));
                    DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM_VARO, _logger, null, errorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_FILE_PACKAGE);
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
                DataTransformService.LogEmsTransformErrorEventToAppInsights(payload?.FileName, emdTypeEnum, PipelineStageEnum.Name.ADF_TRANSFORM_VARO, _logger, e, innerErrorCode, errorMessage, loggerTypeEnum, PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION);
                if (e is BadRequestException)
                {
                    string errStr = $"Bad request thrown while validating '{emdType}' transformation request";
                    _logger.LogError($"- {payload.FileName} - {errStr}");
                    _logger.LogError($"- {payload.FileName}   - {errorMessage}");
                    return new BadRequestObjectResult(errorMessage);
                }
                else
                {
                    string errStr = $"Global level exception thrown while processing '{emdType}' logs";
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
