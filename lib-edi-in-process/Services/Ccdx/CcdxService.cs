using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using lib_edi_in_process.Models.Azure.AppInsights;
using lib_edi_in_process.Models.Enums.Azure.AppInsights;
using lib_edi_in_process.Services.Azure;
using Microsoft.Extensions.Logging;
using lib_edi_in_process.Services.Errors;

namespace lib_edi_in_process.Services.Ccdx
{
    public class CcdxService
    {
        /// <summary>
        /// Pulls device type from CCDX header 'ce_type'
        /// </summary>
        /// <param name="ceHeader">CCDX 'ce_type' header. Example: 'org.nhgh.indigo_v2.report.dev' </param>
        public static string GetLoggerTypeFromCeHeader(string ceHeader)
        {
            string result = null;
            string[] headerParts = ceHeader.Split(".");
            if (headerParts.Length > 2)
            {
                result = headerParts[2];
            }
            return result;
        }

        /// <summary>
        /// Builds raw CCDX consumer blob path using raw CCDX provider blob path
        /// </summary>
        /// <param name="blobPath">Blob path in string format</param>
        /// <example>
        /// A telemetry report file is uploaded to the raw CCDX provider container at 11:59:59 PM on 2021-10-04:
        ///   Raw CCDX provider blob path: "usbdg/2021-10-04/usbdg001_2021-10-04_1629928806_logdata.json.zip"
        /// This telemetry report file is uploaded to the raw CCDX consumer container at 12:00:01 AM on 2021-10-05: 
        ///   Raw CCDX consumer blob path: "usbdg/2021-10-05/usbdg001_2021-10-04_1629928806_logdata.json.zip"
        /// </example>
        /// <remarks>
        /// EDI architecture: 
        /// - "The 'day' folder is derived dynamically from the current time, so it’s possible that the day value 
        /// in the consumer container is different than the day value in the provider container (e.g. if the event 
        /// happened right around midnight, or there was some extended delay in processing."
        /// - "The DEVICE_TYPE folder is derived using the ce-subject value added/returned by CCDX which contains 
        /// the original file path from the file sent by the provider."
        /// </remarks>
        public static string BuildRawCcdxConsumerBlobPath(string ceSubject, string ceType)
        {
            string blobPath = null;

            string deviceType = GetDeviceType(ceSubject, ceType);
            if (deviceType != null)
            {
                string reportFileName = Path.GetFileName(ceSubject);
                Path.GetFileName(ceSubject);
                string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd");
                blobPath = $"{deviceType}/{dateFolder}/{reportFileName}";
            }
            return blobPath;
        }

        /// <summary>
        /// Gets the device id from the ccdx ce-type or ce-subject headers
        /// </summary>
        /// <param name="ceSubject">ccdx ce-subject header value</param>
        /// <param name="ceType">ccdx ce-type header value</param>
        /// <example>
        /// ce-subject = "usbdg/2021-10-04/11/0161a794-173a-4843-879b-189ee4c625aa/"
        /// ce-type = "org.nhgh.cfd50.report.prod"
        /// </example>
        public static string GetDeviceType(string ceSubject, string ceType)
        {
            string deviceType = GetDeviceTypeFromCETypeHeader(ceType);
            if (deviceType == null)
            {
                deviceType = GetDataLoggerTypeFromBlobPath(ceSubject);
            }
            return deviceType;
        }

        /// <summary>
        /// Extracts data logger type (device type) from ce-type header
        /// </summary>
        /// <param name="ceTypeHeader">CCDX header 'ce-type'</param>
        /// <example>
        /// ce-type header value: "org.nhgh.cfd50.report.prod"
        /// </example>
        public static string GetDeviceTypeFromCETypeHeader(string ceTypeHeader)
        {
            string deviceType = null;

            if (ceTypeHeader != null)
            {
                string[] ceTypeHeaderParts = ceTypeHeader.Split('.');
                if (ceTypeHeaderParts.Length > 2)
                {
                    deviceType = ceTypeHeaderParts[2];
                }
            }

            return deviceType;
        }

        /// <summary>
        /// Sends CCDX Consumer started event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogCcdxConsumerStartedEventToAppInsights(string reportFileName, PipelineStageEnum.Name stageName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.ReportFileName = reportFileName ?? "";
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProps, log);
        }

        /// <summary>
        /// Extracts data logger type (device type) from the blob path
        /// </summary>
        /// <param name="blobPath">Blob path in string format</param>
        /// <example>
        /// blob path = "usbdg/2021-10-04/11/0161a794-173a-4843-879b-189ee4c625aa/"
        /// </example>
        /// <remarks>
        /// Blob path can come from the following sources:
        ///  - http request json payload body (transform function)
        ///  - blob name from the blob creation event trigger (provider function)
        ///  - ccdx ce_subject header (consumer function)
        /// </remarks>
        public static string GetDataLoggerTypeFromBlobPath(string blobPath)
        {
            string loggerType = null;
            if (blobPath != null && blobPath != "")
            {
                string[] parts = blobPath.Split('/');
                if (parts.Length > 1)
                {
                    loggerType = parts[0];
                }
            }
            return loggerType;
        }

        /// <summary>
        /// Sends CCDX Consumer succeeded event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogCcdxConsumerSuccessEventToAppInsights(string reportFileName, PipelineStageEnum.Name stageName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.ReportFileName = reportFileName ?? "";
            Dictionary<string, string> customProp = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProp, log);
        }

        /// <summary>
        /// Sends CCDX Consumer error event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        /// <param name="e">Exception object</param>
        /// <param name="errorCode">Error code</param>
        public static async void LogCcdxConsumerErrorEventToAppInsights(string reportFileName, PipelineStageEnum.Name stageName, ILogger log, Exception e, string errorCode)
        {
            string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
            pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION;
            pipelineEvent.ReportFileName = reportFileName ?? "";
            pipelineEvent.ErrorCode = errorCode;
            pipelineEvent.ErrorMessage = errorMessage;
            if (e != null)
            {
                pipelineEvent.ExceptionMessage = e.Message;
                pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
            }
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProps, log);
        }




    }
}
