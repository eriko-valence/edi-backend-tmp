using lib_edi.Models.Azure.AppInsights;
using lib_edi.Models.Dto.Ccdx;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Text;

namespace lib_edi.Services.Ccdx
{
	/// <summary>
	/// Provides methods for interacting with the cold chain data interchange (CCDX) system.
	/// </summary>
	public class CcdxService
	{

		/// <summary>
		/// Builds a CCDX multipart form data request message with a file payload
		/// </summary>
		/// <param name="httpMethod">HTTP method</param>
		/// <param name="httpRequestUriString">Requst uri</param>
		/// <param name="multipartFormDataContent">An HTTP multipart/form-data MIME container</param>
		/// <param name="requiredProviderHeaders">An HTTP multipart/form-data MIME container</param>
		/// <param name="blobReportName">Full path of report file blob name</param>
		/// <param name="log">An HTTP multipart/form-data MIME container</param>
		/// <returns>
		/// HTTP request message
		/// </returns>
		public static HttpRequestMessage BuildCcdxHttpMultipartFormDataRequestMessage(HttpMethod httpMethod, string httpRequestUriString, MultipartFormDataContent multipartFormDataContent, CcdxProviderSampleHeadersDto requiredProviderHeaders, string blobReportName, ILogger log)
		{
			try
			{
				// Headers provided by the Data Interchange Administrator during the onboarding process
				string ccdxHttpHeaderCESource = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_SOURCE");
				string ccdxHttpHeaderDXOwner = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_DX_OWNER");
				string ccdxHttpHeaderDXToken = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_DX_TOKEN");
				string ccdxHttpHeaderCESpecVersion = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_SPECVERSION");

				// Other headers
				//string ccdxHttpHeaderCEType = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_TYPE");

				string reportFileName = Path.GetFileName(blobReportName);
				string ceType = GetCETypeFromBlobPath(blobReportName);

				string ccdxEventTime = DateTime.UtcNow.ToString("o");

				Uri httpRequestUri = new Uri(httpRequestUriString);
				HttpRequestMessage requestMessage = new HttpRequestMessage(httpMethod, httpRequestUri);
				requestMessage.Headers.Add("ce-id", reportFileName); // use file name unique identifier for this data report (USBDG-360)
				requestMessage.Headers.Add("ce-specversion", ccdxHttpHeaderCESpecVersion); // the CloudEvent specification version
				requestMessage.Headers.Add("ce-type", ceType); // the type or classification of the transmitted data report
				requestMessage.Headers.Add("ce-source", ccdxHttpHeaderCESource); // identifies the Telemetry Provider that sent the data
				requestMessage.Headers.Add("dx-token", ccdxHttpHeaderDXToken); // access token that permits publication of data to the Data Interchange
				requestMessage.Headers.Add("dx-owner", ccdxHttpHeaderDXOwner); // data Owner that owns that data
				requestMessage.Headers.Add("ce-time", ccdxEventTime); // time the originating event was created

				requestMessage.Headers.Add("dx-location-latitude", requiredProviderHeaders.Location.Latitude);
				requestMessage.Headers.Add("dx-location-longitude", requiredProviderHeaders.Location.Longitude);
				requestMessage.Headers.Add("dx-location-accuracy", requiredProviderHeaders.Location.Accuracy);

				requestMessage.Headers.Add("dx-tdl-manufacturer", requiredProviderHeaders.Logger.Manufacturer);
				requestMessage.Headers.Add("dx-tdl-model", requiredProviderHeaders.Logger.Model);
				requestMessage.Headers.Add("dx-tdl-serial", requiredProviderHeaders.Logger.Serial);

				requestMessage.Headers.Add("dx-fridge-manufacturer", requiredProviderHeaders.Fridge.Manufacturer);
				requestMessage.Headers.Add("dx-fridge-model", requiredProviderHeaders.Fridge.Model);
				requestMessage.Headers.Add("dx-fridge-assigned-id", requiredProviderHeaders.Fridge.AssignedID);

				requestMessage.Headers.Add("dx-facility-name", requiredProviderHeaders.Facility.FacilityName);
				requestMessage.Headers.Add("dx-facility-contact-name", requiredProviderHeaders.Facility.ContactName);
				requestMessage.Headers.Add("dx-facility-contact-phone", requiredProviderHeaders.Facility.ContactPhone);

				requestMessage.Headers.ExpectContinue = false;
				requestMessage.Content = multipartFormDataContent;
				return requestMessage;
			}
			catch (Exception e)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "M92H", null);
				throw new Exception(customErrorMessage);
			}

		}

		/// <summary>
		/// Generate a value for the CCDX provider http request header 'ce-id' 
		/// </summary>
		/// <remarks>
		/// Combines cold chain equipment idnentifier (data logger serial) and event timestamp: 
		///   {data logger serial number}-{event timestamp}
		/// Example: log3456789asdf-2021-08-24T21:32:48.5586697Z
		/// </remarks>
		/// <returns>
		/// String value of the CCDX provider http request header 'ce-id'
		/// </returns>
		public static string GenerateCcdxUniqueReportID(string dataLoggerSN, string eventTime)
		{
			if (dataLoggerSN != null && eventTime != null)
			{
				return $"{dataLoggerSN}-{eventTime}";
			}
			else
			{
				return $"{GenerateRandomString()}-{DateTime.UtcNow.ToString("o")}";
			}
		}

		/// <summary>
		/// Returns the first part of a random GUID
		/// </summary>
		/// <returns>
		/// First part of a random GUID as a string
		/// </returns>
		public static string GenerateRandomString()
		{
			string guid = Guid.NewGuid().ToString();
			var randomId = guid.Substring(0, guid.IndexOf("-"));
			return randomId;
		}

		/// <summary>
		/// Builds the coldchain equipment type (ce-type) from a string formatted blob path
		/// </summary>
		/// <param name="path">Blob path in string format</param>
		/// <remarks>
		/// The ce-type would be "org.nhgh.usbdg.report.dev" with the following example report flie: 
		///   2021-09-03/usbdg/usbdg001_2021-09-03_1629928806_logdata_eriko_4.json.zip
		/// </remarks>
		public static string GetCETypeFromBlobPath(string path)
		{
			string ceTypeTemplate = Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_TYPE");
			string ceType = null;

			if (path != null)
			{
				string[] parts = path.Split('/');
				if (parts.Length > 0)
				{
					string folderName = parts[0];
					ceType = string.Format(ceTypeTemplate, folderName);
				}
			}
			return ceType;
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
		/// Builds EDI (EMS Data Integration) ADF (Azure Data Factory) curated blob path a using staged input blob path
		/// </summary>
		/// <param name="blobPath">Blob path in string format</param>
		/// <example>
		/// ADF uncompresses a telemetry report file at 11:59:59 PM on 2021-10-04 to the staged input blob container:
		///   Staged input blob path: "usbdg/2021-10-04/23/0161a794-173a-4843-879b-189ee4c625aa/"
		/// The ADF data transformation function processes these staged input files and uploads the results 
		/// to the curated blob container at 12:00:01 AM on 2021-10-05: 
		///   Curated output blob path: "usbdg/2021-10-05/00/0161a794-173a-4843-879b-189ee4c625aa/"
		/// </example>
		public static string BuildCuratedCcdxConsumerBlobPath(string blobPath, string blobName, string loggerType)
		{
			string curatedBlobPath = null; // indigo_v2/event.csv

			if (blobPath != null)
			{
				if (loggerType != null)
				{
					curatedBlobPath = $"{loggerType}/{blobPath.TrimEnd(new[] { '/' })}/{blobName}";
				}
			}

			return curatedBlobPath;
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
			string deviceType = null;
			string blobPath = null;

			deviceType = GetDeviceType(ceSubject, ceType);
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
		/// Checks if a cce telemetry file path extension is supported
		/// </summary>
		/// <param name="blobName">ccdx ce-subject header value</param>
		/// <returns>
		/// true if supported; false if not
		/// </returns>
		public static bool IsPathExtensionSupported(string blobName)
		{
			bool result = false;
			string fileExtension = Path.GetExtension(blobName);
			if (Path.GetExtension(blobName) == ".gz")
			{
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Extracts guid from a string formatted blob path
		/// </summary>
		/// <param name="path">Blob path in string format</param>
		/// <example>
		/// path = "usbdg/2021-10-04/11/0161a794-173a-4843-879b-189ee4c625aa/"
		/// </example>
		public static string GetGuidFromBlobPath(string path)
		{
			string guid = null;
			if (path != null)
			{
				string[] words = path.Split('/');
				if (words.Length > 2)
				{
					guid = words[words.Length -2];
				}
			}
			return guid;
		}

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
		/// Validate data logger type is supported by ETL pipeline
		/// </summary>
		/// <param name="loggerType">Blob path in string format</param>
		public static bool ValidateLoggerType(string loggerType)
		{
			bool result = false;

			if (loggerType != null)
			{
				if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.USBDG_DATASIM.ToString())
				{
					result = true;
				} else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.CFD50.ToString())
				{
					result = true;
				} else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_V2.ToString())
				{
					result = true;
				} else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.NO_LOGGER.ToString())
				{
					result = true;
				}
			}
			return result;
		}

		/// <summary>
		/// Validate ccdx type is supported by ETL pipeline
		/// </summary>
		/// <param name="path">Blob path in string format</param>
		public static bool ValidateCeTypeHeader(string ceType)
		{
			bool result = false;
			if (Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_TYPE_USBDG") == ceType)
			{
				result = true;
			} else if (Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_TYPE_CFD50") == ceType)
			{
				result = true;
			} else if (Environment.GetEnvironmentVariable("CCDX_PUBLISHER_HEADER_CE_TYPE_INDIGO_V2") == ceType)
			{
				result = true;
			} else if (CcdxService.ValidateLoggerType(ceType))
			{
				result = true;
			}
			return result;
		}

		/// <summary>
		/// Sends CCDX Provider started event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file that triggered the CCDX Providerr</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxProviderStartedEventToAppInsights(string reportFileName, ILogger log)
		{
			//Log trigger event to app insights
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Provider success event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file that triggered the CCDX Provider</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxProviderSuccessEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customPropsEnd = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customPropsEnd, log);
		}

		/// <summary>
		/// Sends CCDX Provider failed event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file that triggered the CCDX Provider</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxProviderFailedEventToAppInsights(string reportFileName, ILogger log)
		{

			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.HTTP_STATUS_CODE_ERROR;
			pipelineEvent.ReportFileName = reportFileName;

			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Provider failed event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file that triggered the CCDX Provider</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxProviderUnsupportedLoggerEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.VALIDATION;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNSUPPORTED_DATA_LOGGER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Provider error event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file that triggered the CCDX Provider</param>
		/// <param name="log">Microsoft extension logger</param>
		/// <param name="e">Exception object</param>
		/// <param name="errorCode">Error code</param>
		public static void LogCcdxProviderErrorEventToAppInsights(string reportFileName, ILogger log, Exception e, string errorCode)
		{
			string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION;
			pipelineEvent.ReportFileName = reportFileName;
			pipelineEvent.ErrorCode = errorCode;
			pipelineEvent.ErrorMessage = errorMessage;
			pipelineEvent.ExceptionMessage = e.Message;
			pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Consumer started event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxConsumerStartedEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_CONSUMER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_CONSUMER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Consumer succeeded event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxConsumerSuccessEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_CONSUMER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProp = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_CONSUMER, customProp, log);
		}

		/// <summary>
		/// Sends CCDX Consumer missing subject header event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxConsumerMissingSubjectHeaderEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_CONSUMER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.VALIDATION;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.MISSING_CE_SUBJECT_HEADER;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_CONSUMER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Consumer unsupported attachment extension event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxConsumerUnsupportedAttachmentExtensionEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_CONSUMER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.VALIDATION;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNSUPPORTED_EXTENSION;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_CONSUMER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Provider unsupported attachment extension event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogCcdxProviderUnsupportedAttachmentExtensionEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_PROVIDER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.VALIDATION;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNSUPPORTED_EXTENSION;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_PROVIDER, customProps, log);
		}

		/// <summary>
		/// Sends CCDX Consumer error event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		/// <param name="e">Exception object</param>
		/// <param name="errorCode">Error code</param>
		public static void LogCcdxConsumerErrorEventToAppInsights(string reportFileName, ILogger log, Exception e, string errorCode)
		{
			string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.CCDX_CONSUMER;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION;
			pipelineEvent.ReportFileName = reportFileName;
			pipelineEvent.ErrorCode = errorCode;
			pipelineEvent.ErrorMessage = errorMessage;
			if (e != null)
			{
				pipelineEvent.ExceptionMessage = e.Message;
				pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
			}
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.CCDX_CONSUMER, customProps, log);
		}

		/// <summary>
		/// EMS ADF data transformation stage has succeeded
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogEmsTransformSucceededEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
			pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
			pipelineEvent.LoggerType = DataLoggerTypeEnum.Name.USBDG_DATASIM;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
		}

		/// <summary>
		/// MetaFridge ADF data transformation stage has started
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogMetaFridgeTransformStartedEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
			pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
			pipelineEvent.LoggerType = DataLoggerTypeEnum.Name.CFD50;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
		}

		/// <summary>
		/// MetaFridge ADF data transformation stage has succeeded
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		public static void LogMetaFridgeTransformSucceededEventToAppInsights(string reportFileName, ILogger log)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
			pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
			pipelineEvent.LoggerType = DataLoggerTypeEnum.Name.CFD50;
			pipelineEvent.ReportFileName = reportFileName;
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
		}

		/// <summary>
		/// Sends MetaFridge ADF data transformatoin error event to App Insight
		/// </summary>
		/// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
		/// <param name="log">Microsoft extension logger</param>
		/// <param name="e">Exception object</param>
		/// <param name="errorCode">Error code</param>
		public static void LogMetaFridgeTransformErrorEventToAppInsights(string reportFileName, ILogger log, Exception e, string errorCode)
		{
			string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
			pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
			pipelineEvent.LoggerType = DataLoggerTypeEnum.Name.CFD50;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
			pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION;
			pipelineEvent.ReportFileName = reportFileName;
			pipelineEvent.ErrorCode = errorCode;
			pipelineEvent.ErrorMessage = errorMessage;
			if (e != null)
			{
				pipelineEvent.ExceptionMessage = e.Message;
				pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
			}
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
		}

		public static void ValidateCcdxConsumerCeTypeEnvVariables(ILogger log)
		{
			string envVarCeTypeUsbdgDataDim = "CCDX_PUBLISHER_HEADER_CE_TYPE_USBDG";
			string envVarCeTypeCfd50 = "CCDX_PUBLISHER_HEADER_CE_TYPE_CFD50";
			string envVarCeTypeIndigoV2 = "CCDX_PUBLISHER_HEADER_CE_TYPE_INDIGO_V2";
			string errorCode = "JC16";

			if (Environment.GetEnvironmentVariable(envVarCeTypeUsbdgDataDim) == null)
			{
				string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(envVarCeTypeUsbdgDataDim));
				log.LogError($"- [ccdx-consumer->run]: {errorMessage}");
				throw new Exception(errorMessage);
			}
			else if (Environment.GetEnvironmentVariable(envVarCeTypeCfd50) == null)
			{
				string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(envVarCeTypeCfd50));
				log.LogError($"- [ccdx-consumer->run]: {errorMessage}");
				throw new Exception(errorMessage);
			} else if (Environment.GetEnvironmentVariable(envVarCeTypeIndigoV2) == null)
			{
				string errorMessage = EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(envVarCeTypeIndigoV2));
				log.LogError($"- [ccdx-consumer->run]: {errorMessage}");
				throw new Exception(errorMessage);
			}
		}

		/// <summary>
		/// Builds a MetaFridge curated blob name using the path from the transformation Azure function request body payload
		/// </summary>
		/// <param name="requestBodyPath">Path from transformation Azure function request body payload</param>
		/// <example>
		/// requestBodyPath = "cfd50/2021-11-30/21/ae03a1b3-5698-417b-8b4a-905cee1015a9/";
		/// </example>
		public static string BuildCuratedCfd50BlobName(string requestBodyPath)
		{
			string curatedBlobName = null;

			DateTime dt = DateTime.UtcNow;

			string dateFolder = dt.ToString("yyyy-MM-dd");
			string hourFolder = dt.ToString("HH");

			if (requestBodyPath != null)
			{
				string[] words = requestBodyPath.Split('/');
				if (words.Length > 4)
				{
					curatedBlobName = $"{words[0]}/{dateFolder}/{hourFolder}/{words[3]}/out_mf.csv";
				}
			}
			return curatedBlobName;

		}
	}
}
