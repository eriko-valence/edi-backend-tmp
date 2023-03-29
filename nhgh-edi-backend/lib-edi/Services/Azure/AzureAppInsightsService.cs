using lib_edi.Models.Azure.AppInsights;
using lib_edi.Models.Dto.Azure.AppInsights;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using lib_edi.Models.SendGrid;
using lib_edi.Services.Errors;
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

namespace lib_edi.Services.Azure
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


			return customProperties;
		}


		/// <summary>
		/// Sends a single telemetry pipeline stage log entry to App Insights
		/// </summary>
		/// <param name="pipelineStageName">A pipeline stage log entry name</param>
		/// <param name="customProps">A dictionary of properties to include with the telemetry</param>
		/// <param name="log">A Microsoft extensions logger object</param>
		public static void LogEntry(PipelineStageEnum.Name pipelineStageName, Dictionary<string, string> customProps, ILogger log)
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
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "97E7", EdiErrorsService.BuildErrorVariableArrayList());
				throw new Exception(customErrorMessage);
			}

		}

		/// <summary>
		/// Retrieves custom event errors from App Insights using the App Insights REST API
		/// </summary>
		/// <param name="log">A Microsoft extensions logger object</param>
		/// <returns>
		/// A list of Pogo LT app errors
		/// </returns>
		public static Dictionary<string, PipelineJobStatus> GetDailyAppInsightsCustomEvents(ILogger log)
		{

			try
			{
				AppInsightsApiQueryAppErrorsSettings appInsightsSettings = GetAppInsightsApiQueryAppErrorsSettings();
				AppInsightsCustomErrorsResponseDto appInsightsCustomErrorsResponseDto;
				//List<PipelineJobStatusEntry> listPipelineJobStatusEntry;
				Dictionary<string, PipelineJobStatus> listPipelineJobStatus;

				if (appInsightsSettings != null)
				{
					string appid = appInsightsSettings.AppID;
					string apikey = appInsightsSettings.ApiKey;
					string queryTimespan = appInsightsSettings.ErrorQueryInterval;
					string queryParam = "customEvents | extend pipelineStage = customDimensions.['PipelineStage'] | extend FileName = customDimensions.['FileName'] | project timestamp, name, pipelineStage, FileName";
					string URL = appInsightsSettings.ErrorQueryUrl;

					HttpClient client = new HttpClient();
					client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
					client.DefaultRequestHeaders.Add("x-api-key", apikey);
					var req = string.Format(URL, appid, queryTimespan, queryParam);
					HttpResponseMessage response = client.GetAsync(req).Result;
					if (response.IsSuccessStatusCode)
					{
						string result = response.Content.ReadAsStringAsync().Result;
						appInsightsCustomErrorsResponseDto = JsonConvert.DeserializeObject<AppInsightsCustomErrorsResponseDto>(result);
						listPipelineJobStatus = BuildListPipelineJobStatusEntryList(appInsightsCustomErrorsResponseDto);
						return listPipelineJobStatus;
					}
					else
					{
						log.LogError($"   - [azure_app_insights_service->get_daily_errors]: received an unsuccessful status code {response.StatusCode}: {response.ReasonPhrase}");
						string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "C36G", EdiErrorsService.BuildErrorVariableArrayList(response.ReasonPhrase));
						throw new Exception(customErrorMessage);
					}
				}
				else
				{
					log.LogError("   - [azure_app_insights_service->get_daily_errors]: missing api query settings");
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "EN5G", EdiErrorsService.BuildErrorVariableArrayList());
					throw new Exception(customErrorMessage);
				}
			}
			catch (Exception e)
			{
				log.LogError("   - [azure_app_insights_service->get_daily_errors]: an exception occured while retrieving app insights custom events");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "QDJ3", EdiErrorsService.BuildErrorVariableArrayList());
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Builds an App Insights API query app errors settings object
		/// </summary>
		/// <returns>
		/// An App Insights API query app errors settings object
		/// </returns>
		public static AppInsightsApiQueryAppErrorsSettings GetAppInsightsApiQueryAppErrorsSettings()
		{
			AppInsightsApiQueryAppErrorsSettings settings = new AppInsightsApiQueryAppErrorsSettings();
			settings.AppID = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_APP_ID");
			settings.ApiKey = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_KEY");
			settings.ErrorQueryInterval = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_ERROR_QUERY_INTERVAL");
			settings.ErrorQueryUrl = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_ERROR_QUERY_URL");

			if (settings.AppID == null) { return null; }
			if (settings.ApiKey == null) { return null; }
			if (settings.ErrorQueryInterval == null) { return null; }
			if (settings.ErrorQueryUrl == null) { return null; }

			return settings;
		}

		/// <summary>
		/// Builds a list of Pogo LT app errors from the App Insights custom errors response DTO object
		/// </summary>
		/// <param name="errors">A App Insights custom errors response DTO object</param>
		/// <returns>
		/// A list of Pogo LT app errors
		/// </returns>
		public static Dictionary<string, PipelineJobStatus> BuildListPipelineJobStatusEntryList(AppInsightsCustomErrorsResponseDto errors)
		{
			List<PipelineJobStatusEntry> listPipelineJobStatusEntry = new List<PipelineJobStatusEntry>();

			foreach (AppInsightsCustomErrorsResponseDtoTable table in errors.tables)
			{
				foreach (List<object> row in table.rows)
				{
					PipelineJobStatusEntry appError = new PipelineJobStatusEntry();
					appError.TimeStamp = row[0]?.ToString() ?? "";
					appError.PipelineStageName = row[2]?.ToString() ?? "";
					appError.FileName = row[3]?.ToString() ?? "";
					listPipelineJobStatusEntry.Add(appError);
				}
			}

			Dictionary<string, PipelineJobStatus> list = new Dictionary<string, PipelineJobStatus>();

			foreach (var entry in listPipelineJobStatusEntry)
			{
				if (list.ContainsKey(entry.FileName))
				{
					if (list[entry.FileName].JobStageResults.ContainsKey(entry.PipelineStageName))
					{
						list[entry.FileName].JobStageResults[entry.PipelineStageName].Add(entry.TimeStamp);
					}
					else
					{
						list[entry.FileName].JobStageResults.Add(entry.PipelineStageName, new List<string>());
						list[entry.FileName].JobStageResults[entry.PipelineStageName].Add(entry.TimeStamp);
					}
				}
				else
				{
					list.Add(entry.FileName, new PipelineJobStatus());
					list[entry.FileName].JobStageResults.Add(entry.PipelineStageName, new List<string>());
					list[entry.FileName].JobStageResults[entry.PipelineStageName].Add(entry.TimeStamp);
				}
			}

			return list;
		}

        /// <summary>
        /// Retrieves custom event errors from App Insights using the App Insights REST API
        /// </summary>
        /// <param name="settings">An App Insights Api query app errors settings object</param>
        /// <param name="log">A Microsoft extensions logger object</param>
        /// <returns>
        /// A list of Pogo LT app errors
        /// </returns>
        public static List<PogoLTAppError> GetDailyAppInsightsAppErrors(AppInsightsApiQueryAppErrorsSettings settings, ILogger log)
        {
            AppInsightsCustomErrorsResponseDto appInsightsErrorsDto;
            List<PogoLTAppError> appErrors;

            if (settings != null)
            {
                string appid = settings.AppID;
                string apikey = settings.ApiKey;
                string queryTimespan = settings.ErrorQueryInterval;
                string queryParam = "customEvents | extend errorName = customDimensions.['ErrorName'] | extend orchestrationID = customDimensions.['OrchestrationID'] | extend errorType = customDimensions.['ErrorType'] | extend authenticatedUserID = customDimensions.['AuthenticatedUserID'] | where isnotempty(tostring(customDimensions.['ErrorName'])) | project timestamp, name, errorName, errorType, authenticatedUserID, orchestrationID";
                string URL = settings.ErrorQueryUrl;

                HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
                client.DefaultRequestHeaders.Add("x-api-key", apikey);
                var req = string.Format(URL, appid, queryTimespan, queryParam);
                HttpResponseMessage response = client.GetAsync(req).Result;
                if (response.IsSuccessStatusCode)
                {
                    string result = response.Content.ReadAsStringAsync().Result;
                    appInsightsErrorsDto = JsonConvert.DeserializeObject<AppInsightsCustomErrorsResponseDto>(result);
                    appErrors = BuildPogoLTAppErrorsList(appInsightsErrorsDto);
                    return appErrors;
                }
                else
                {
                    log.LogError($"   - [azure_app_insights_service->get_daily_errors]: received an unsuccessful status code {response.StatusCode}: {response.ReasonPhrase}");
                    return new List<PogoLTAppError>(); // return zero results
                }

            }
            else
            {
                log.LogError("   - [azure_app_insights_service->get_daily_errors]: missing api query settings");
                return new List<PogoLTAppError>(); // return zero results
            }


        }

        /// <summary>
        /// Builds a list of Pogo LT app errors from the App Insights custom errors response DTO object
        /// </summary>
        /// <param name="errors">A App Insights custom errors response DTO object</param>
        /// <returns>
        /// A list of Pogo LT app errors
        /// </returns>
        public static List<PogoLTAppError> BuildPogoLTAppErrorsList(AppInsightsCustomErrorsResponseDto errors)
        {
            List<PogoLTAppError> listPogoLTAppError = new List<PogoLTAppError>();

            foreach (AppInsightsCustomErrorsResponseDtoTable table in errors.tables)
            {
                foreach (List<object> row in table.rows)
                {
                    PogoLTAppError appError = new PogoLTAppError();
                    appError.TimeStamp = row[0]?.ToString() ?? "";
                    appError.ErrorName = row[2]?.ToString() ?? "";
                    appError.ErrorType = row[3]?.ToString() ?? "";
                    appError.QueryEmail = row[4]?.ToString() ?? "";
                    appError.OrchestrationID = row[5]?.ToString() ?? "";
                    listPogoLTAppError.Add(appError);
                }
            }
            return listPogoLTAppError;
        }

        public static void LogEvent(string eventName, Dictionary<string, string> customProps)
        {
            if (telemetryClient == null)
            {
                TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                telemetryClient = new TelemetryClient(configuration);
            }
            telemetryClient.TrackEvent(eventName, customProps);
        }

        public static void LogEvent(EdiImportJobStats jobStats)
        {
            Dictionary<string, string> customProps = new()
            {
                { "job_name", jobStats.EdiJobName.ToString() },
                { "job_status", jobStats.EdiJobStatus.ToString() },
                { "job_exception_message", jobStats.ExceptionMessage },
                { "queried", jobStats.Queried.ToString() },
                { "loaded", jobStats.Loaded.ToString() },
                { "excluded", jobStats.Skipped.ToString() },
                { "failed", jobStats.Failed.ToString() }
            };

            if (telemetryClient == null)
            {
                TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                telemetryClient = new TelemetryClient(configuration);
            }
            telemetryClient.TrackEvent(jobStats.EdiFunctionApp.ToString(), customProps);

        }

        public static void LogException(Exception e)
        {
            if (telemetryClient == null)
            {
                TelemetryConfiguration configuration = TelemetryConfiguration.CreateDefault();
                configuration.InstrumentationKey = Environment.GetEnvironmentVariable("APPINSIGHTS_INSTRUMENTATIONKEY");
                telemetryClient = new TelemetryClient(configuration);
            }
            telemetryClient.TrackException(e);
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
