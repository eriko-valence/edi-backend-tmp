//using lib_edi.Models.Azure.AppInsights;
using lib_edi_in_process.Models.Azure.AppInsights;
//using lib_edi.Models.Dto.Azure.AppInsights;
using lib_edi_in_process.Models.Dto.Azure.AppInsights;
//using lib_edi.Models.Edi.Data.Import;
using lib_edi_in_process.Models.Enums.Azure.AppInsights;
using lib_edi_in_process.Models.Enums.Emd;
//using lib_edi.Models.SendGrid;
//using lib_edi.Services.Errors;
using lib_edi_in_process.Services.Errors;
using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Extensibility;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Services.Azure
{
    public class AzureAppInsightsService
    {
        static TelemetryClient telemetryClient = null;

        /// <summary>
        /// Builds a dictionary of custom properties that track pipeline status information to be sent to App Insights
        /// </summary>
        /// <param name="pipelineEvent">An App Insights object with pipeline event information</param>
        /// <returns>
        /// A dictionary object of custom properties associated with a pipeline stage
        /// </returns>
        public static Dictionary<string, string> BuildCustomPropertiesObject(PipelineEvent pipelineEvent)
        {
            Dictionary<string, string> customProperties = new Dictionary<string, string>();

            if (pipelineEvent.ErrorCode != null)
            {
                customProperties.Add("errorCode", pipelineEvent.ErrorCode.ToString());
            }

            if (pipelineEvent.ErrorMessage != null)
            {
                customProperties.Add("errorMessage", pipelineEvent.ErrorMessage.ToString());
            }

            if (pipelineEvent.ExceptionMessage != null)
            {
                customProperties.Add("exceptionMessage", pipelineEvent.ExceptionMessage.ToString());
            }

            if (pipelineEvent.ExceptionInnerMessage != null)
            {
                customProperties.Add("exceptionInnerMessage", pipelineEvent.ExceptionInnerMessage.ToString());
            }

            if (pipelineEvent.StageName != PipelineStageEnum.Name.NONE)
            {
                customProperties.Add("pipelineStage", pipelineEvent.StageName.ToString());
            }

            if (pipelineEvent.EventName != Models.Enums.Azure.AppInsights.PipelineEventEnum.Name.NONE)
            {
                customProperties.Add("pipelineEvent", (string)pipelineEvent.EventName.ToString());
            }

            if (pipelineEvent.ReportFileName != null)
            {
                customProperties.Add("fileName", pipelineEvent.ReportFileName);
            }

            if (pipelineEvent.PipelineFailureType != PipelineFailureTypeEnum.Name.NONE)
            {
                customProperties.Add("pipelineFailureType", pipelineEvent.PipelineFailureType.ToString());
            }

            if (pipelineEvent.PipelineFailureReason != PipelineFailureReasonEnum.Name.NONE)
            {
                customProperties.Add("pipelineFailureReason", pipelineEvent.PipelineFailureReason.ToString());
            }

            if (pipelineEvent.LoggerType != DataLoggerTypeEnum.Name.NONE)
            {
                customProperties.Add("dataLoggerType", pipelineEvent.LoggerType.ToString());
            }
            // NHGH-3057 1723 Add EMD type to app insights logging
            if (pipelineEvent.EmdType != EmdEnum.Name.NONE)
            {
                customProperties.Add("emdType", pipelineEvent.EmdType.ToString());
            }


            return customProperties;
        }


        /// <summary>
        /// Sends a single telemetry pipeline stage log entry to App Insights
        /// </summary>
        /// <param name="pipelineStageName">A pipeline stage log entry name</param>
        /// <param name="customProps">A dictionary of properties to include with the telemetry</param>
        /// <param name="log">A Microsoft extensions logger object</param>
        public static async void LogEntry(PipelineStageEnum.Name pipelineStageName, Dictionary<string, string> customProps, ILogger log)
        {
            try
            {
                if (telemetryClient == null)
                {
                    TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                    configuration.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                    telemetryClient = new TelemetryClient(configuration);
                }
                telemetryClient.TrackEvent(pipelineStageName.ToString(), customProps);
            }
            catch (Exception e)
            {
                log.LogError("   - [azure_app_insights_service->log_entry]: an exception occured while sending app insights custom events");
                string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "97E7", EdiErrorsService.BuildErrorVariableArrayList());
                throw new Exception(customErrorMessage);
            }

        }

    }



    /// <summary>
    /// An application insights query settings object
    /// </summary>
    public class AppInsightsApiQueryAppErrorsSettings
    {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string AppID { get; set; }
        public string ApiKey { get; set; }
        public string ErrorQueryInterval { get; set; }
        public string ErrorQueryUrl { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }
}
