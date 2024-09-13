using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
//using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.Validation;
using lib_edi.Models.Dto.Http;
using lib_edi.Helpers;
using lib_edi.Models.Edi;
using System.Dynamic;
using System.Collections;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Ems;
using lib_edi.Models.Azure.AppInsights;
using lib_edi.Models.Enums.Azure.AppInsights;
using CsvHelper;
using lib_edi.Models.Csv;
using lib_edi.Models.Emd.Csv;
using lib_edi.Models.Loggers.Csv;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Xml;
using System.Text.RegularExpressions;
using lib_edi.Services.Loggers;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Blobs;

namespace lib_edi.Services.CceDevice
{
    public class DataTransformService
    {
		/// <summary>
		/// Serializes JSON object
		/// </summary>
		/// <param name="emsLog">Name of JSON object </param>
		/// <returns>
		/// Text of serialized JSON object; Exception (48TV) otherwise
		/// </returns>
		public static async Task<string> SerializeJsonObject(dynamic emsLog)
		{
			try
			{
				var settings = new JsonSerializerSettings
				{
					NullValueHandling = NullValueHandling.Ignore,
					MissingMemberHandling = MissingMemberHandling.Ignore
				};

				return JsonConvert.SerializeObject(emsLog, settings);
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "48TV", null);
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Validates a list provided JSON data objects against a provided JSON schema
		/// </summary>
		/// <param name="listOfJsonDataoValidate">A list of validated JSON objects</param>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="blobNameJsonSchema">Blob name of JSON schema that will be used to validate the JSON data</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of validated JSON data objects; Exception thrown if at least one report fails validation (R85Y) or if the json definition file failed to be retrieved from blob storage (FY84)
		/// </returns>
		public static async Task<List<dynamic>> ValidateJsonObjects(BlobContainerClient BlobContainerClient, List<dynamic> listOfJsonDataoValidate, string blobNameJsonSchema, ILogger log)
		{
			List<dynamic> validatedJsonObjects = new();

			string configBlobJsonText;
			JsonSchema configJsonSchema;

			try
			{
				configBlobJsonText = await AzureStorageBlobService.DownloadBlobTextAsync(BlobContainerClient, blobNameJsonSchema);
				configJsonSchema = await JsonSchema.FromJsonAsync(configBlobJsonText);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "FY84", null);
				throw new Exception(customErrorMessage);
			}

			foreach (dynamic emsLog in listOfJsonDataoValidate)
			{
				string emsLogText = await SerializeJsonObject(emsLog);

				ICollection<ValidationError> errors = configJsonSchema.Validate(emsLogText);
				if (errors.Count == 0)
				{
					//log.LogInformation($"    - Validated: Yes");
					validatedJsonObjects.Add(emsLog);
				}
				else
				{
					string validationResultString = EdiErrorsService.BuildJsonValidationErrorString(errors);
					log.LogError($"    - Validated: No - {validationResultString}");
					string source = emsLog.EDI_SOURCE;
					ArrayList al = EdiErrorsService.BuildErrorVariableArrayList(source, validationResultString);
					string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "R85Y", al);
					throw new Exception(customErrorMessage);
				}
			}

			return validatedJsonObjects;
		}

		/// <summary>
		/// Validates a provided JSON data object against a provided schema 
		/// </summary>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="jsonDataToValidate">JSON data object to validate</param>
		/// <param name="blobNameJsonSchema">Blob name of JSON schema that will be used to validate the JSON data</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A vlaidated JSON data object; Exception thrown if validation fails (YR42) or if the json definition file failed to be retrieved from blob storage (4VN5)
		/// </returns>
		public static async Task<dynamic> ValidateJsonObject(BlobContainerClient BlobContainerClient, dynamic jsonDataToValidate, string blobNameJsonSchema, ILogger log)
		{
			// Deserialize JSON schema
			string jsonSchemaString;
			JsonSchema jsonSchemaObject;
			try
			{
				jsonSchemaString = await AzureStorageBlobService.DownloadBlobTextAsync(BlobContainerClient, blobNameJsonSchema);
				jsonSchemaObject = await JsonSchema.FromJsonAsync(jsonSchemaString);
			}
			catch (Exception e)
			{
				//log.LogError($"    - Validated: No");
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "4VN5", null);
				throw new Exception(customErrorMessage);
			}

			// Validate the JSON data against the schema
			string jsonStringToValidate = await SerializeJsonObject(jsonDataToValidate);
			ICollection<ValidationError> errors = jsonSchemaObject.Validate(jsonStringToValidate);
			if (errors.Count == 0)
			{
				//log.LogInformation($"    - Validated: Yes");
				return jsonDataToValidate;
			}
			else
			{
				string validationResultString = EdiErrorsService.BuildJsonValidationErrorString(errors);
				log.LogError($"    - Validated: No - {validationResultString}");
				string source = jsonDataToValidate.EDI_SOURCE;
				ArrayList al = EdiErrorsService.BuildErrorVariableArrayList(source, validationResultString);
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "YR42", al);
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Gets the Newtonsoft.Json.Linq.JObject with the specified property name
		/// </summary>
		/// <param name="jTokenObject">Newtonsoft.Json.Linq.JObject</param>
		/// <param name="propertyName">Property name of Newtonsoft.Json.Linq.JObject that will be retrieved</param>
		/// <returns>
		/// Newtonsoft.Json.Linq.JObject if successful; null otherwise
		/// </returns>
		public static string GetJObjectPropertyValueAsString(JObject jTokenObject, string propertyName)
		{
			try
			{
				if (jTokenObject != null)
				{
					if (propertyName != null)
					{
						return jTokenObject.GetValue(propertyName).Value<string>();
					}
					else
					{
						return null;
					}
				}
				else
				{
					return null;
				}
			}
			catch
			{
				return null;
			}
		}

		/// <summary>
		/// Serializes an EMS log transformation http response body
		/// </summary>
		/// <param name="csvBlobName">Name of csv azure storage blob</param>
		/// <returns>
		/// A serialized string of the EMS log transformation http reseponse body if successful; Exception (X83E) otherwise
		/// </returns>
		public static async Task<string> SerializeHttpResponseBody(string csvBlobName, string emdType)
		{
			try
			{
				TransformHttpResponseMessageBodyDto emsLogResponseDto = new TransformHttpResponseMessageBodyDto();
				emsLogResponseDto.Path = csvBlobName;
				emsLogResponseDto.EmdType = emdType;
				return JsonConvert.SerializeObject(emsLogResponseDto);
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "X83E", null);
				throw new Exception(customError);
			}
		}

		/// <summary>
		/// Gets EMD source property from JSON object
		/// </summary>
		/// <param name="jo">JSON object</param>
		/// <returns>
		/// A string value of the EMD source property; "unknown" otherwise
		/// </returns>
		public static string GetSourceFile(JObject jo)
		{
			string sourceFile = null;
			if (jo != null)
			{
				sourceFile = ObjectManager.GetJObjectPropertyValueAsString(jo, "EDI_SOURCE");
			}

			if (sourceFile != null)
			{
				return sourceFile;
			}
			else
			{
				return "unknown";
			}
		}

        /// <summary>
        /// Builds a curated blob path using staged input blob path
        /// </summary>
        /// <param name="blobPath">Blob path in string format</param>
        /// <example>
        /// ADF uncompresses a telemetry report file at 11:59:59 PM on 2021-10-04 to the staged input blob container:
        ///   Staged input blob path: "usbdg/2021-10-04/23/0161a794-173a-4843-879b-189ee4c625aa/"
        /// The ADF data transformation function processes these staged input files and uploads the results 
        /// to the curated blob container at 12:00:01 AM on 2021-10-05: 
        ///   Curated output blob path: "usbdg/2021-10-05/00/0161a794-173a-4843-879b-189ee4c625aa/"
        /// </example>
        public static string BuildCuratedBlobPath(string blobPath, string blobName, string loggerType)
        {
            string curatedBlobPath = null;

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
        /// Builds a curated blob folder path using staged input blob path
        /// </summary>
        /// <param name="blobPath">Staged blob path in string format</param>
        public static string BuildCuratedBlobFolderPath(string blobPath, string loggerType)
        {
            string curatedBlobPath = null;
            if (blobPath != null)
            {
                if (loggerType != null)
                {
                    curatedBlobPath = $"{loggerType}/{blobPath.TrimEnd(new[] { '/' })}";
                }
            }
            return curatedBlobPath;
        }


        /// <summary>
        /// EMS ADF data transformation stage has succeeded
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogEmsTransformSucceededEventToAppInsights(string reportFileName, EmdEnum.Name emdType, DataLoggerTypeEnum.Name loggerType, PipelineStageEnum.Name stageName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.LoggerType = loggerType;
            pipelineEvent.ReportFileName = reportFileName;
			// NHGH-3057 1652 Add EMD type to app insights logging
			pipelineEvent.EmdType = emdType;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProps, log);
        }

        /// <summary>
        /// EMS ADF data transformation stage has started
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogEmsTransformStartedEventToAppInsights(string reportFileName, PipelineStageEnum.Name stageName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.ReportFileName = reportFileName;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProps, log);
        }

        /// <summary>
        /// Sends EMS ADF data transformatoin error event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        /// <param name="e">Exception object</param>
        /// <param name="errorCode">Error code</param>
        public static void LogEmsTransformErrorEventToAppInsights(string reportFileName, EmdEnum.Name emdType, PipelineStageEnum.Name stageName, ILogger log, Exception e, string errorCode, string errorMessage, DataLoggerTypeEnum.Name loggerTypeEnum, PipelineFailureReasonEnum.Name failureReason)
        {
            //string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
            pipelineEvent.StageName = stageName;
            pipelineEvent.LoggerType = loggerTypeEnum;
            pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
            pipelineEvent.PipelineFailureReason = failureReason;
            pipelineEvent.ReportFileName = reportFileName;
            pipelineEvent.ErrorCode = errorCode;
            pipelineEvent.ErrorMessage = errorMessage;
            pipelineEvent.EmdType = emdType;
            if (e != null)
            {
                pipelineEvent.ExceptionMessage = e.Message;
                pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
            }
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(stageName, customProps, log);
        }

		/// <summary>
		/// Writes denormalized USBDG log file csv records to Azure blob storage
		/// </summary>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="requestBody">EMS log transformation http reqest object</param>
		/// <param name="usbdgRecords">A list of denormalized USBDG log records</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// Blob name of USBDG csv formatted log file; Exception (Q25U)
		/// </returns>
		public static async Task<string> WriteRecordsToCsvBlob(BlobContainerClient BlobContainerClient, TransformHttpRequestMessageBodyDto requestBody, List<EdiSinkRecord> usbdgRecords, string loggerTypeName, ILogger log)
        {
            string blobName = "";
            string loggerType = loggerTypeName.ToString().ToLower();

            if (requestBody != null)
            {
                if (requestBody.Path != null)
                {
                    try
                    {
                        if (usbdgRecords.Count > 0)
                        {
                            var firstRecord = usbdgRecords.FirstOrDefault();
                            string recordType = firstRecord.GetType().Name;
                            /*
                                NHGH-3096 2023.09.19 1401 A USBDG collected Indigo V2 report package uploaded with "no_logger" 
                                    will have a base record of EmsEventRecord. This is an edge case scenario that is not supported.
                                    So the expected behavior is that curated files for these Indigo V2 logger data files will NOT 
                                    be be created and uploaded to the "curated_output" container.  
                            */
                            if (recordType == "IndigoV2EventRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_event.csv", loggerType);
                            }
                            else if (recordType == "IndigoChargerV2EventRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_charger_v2_event.csv", loggerType);
                            }
                            else if (recordType == "Sl1EventRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "sl1_event.csv", loggerType);
                            }
                            else if (recordType == "IndigoV2LocationRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgDeviceRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_device.csv", loggerType);
                            }
                            else if (recordType == "UsbdgEventRecord")
                            {
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_event.csv", loggerType);
                            }
							else if (recordType == "VaroLocationRecord")
							{
								blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "varo_location.csv", loggerType);
							}
							else
                            {
                                return blobName;
                            }
                            BlobClient outBlob = BlobContainerClient.GetBlobClient(blobName);
                            // using var writer = await outBlob.OpenWriteAsync();
                            using var writer = await outBlob.OpenWriteAsync(true);
                            using var streamWriter = new StreamWriter(writer);
                            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                            var serializedParent = JsonConvert.SerializeObject(usbdgRecords);
                            if (recordType == "IndigoV2EventRecord")
                            {
                                List<IndigoV2EventRecord> records = JsonConvert.DeserializeObject<List<IndigoV2EventRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "IndigoChargerV2EventRecord")
                            {
                                List<IndigoChargerV2EventRecord> records = JsonConvert.DeserializeObject<List<IndigoChargerV2EventRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "Sl1EventRecord")
                            {
                                List<Sl1EventRecord> records = JsonConvert.DeserializeObject<List<Sl1EventRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "IndigoV2LocationRecord")
                            {
                                List<IndigoV2LocationRecord> records = JsonConvert.DeserializeObject<List<IndigoV2LocationRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                List<UsbdgLocationRecord> records = JsonConvert.DeserializeObject<List<UsbdgLocationRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgDeviceRecord")
                            {
                                List<UsbdgDeviceRecord> records = JsonConvert.DeserializeObject<List<UsbdgDeviceRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgEventRecord")
                            {
                                List<UsbdgEventRecord> records = JsonConvert.DeserializeObject<List<UsbdgEventRecord>>(serializedParent);
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "VaroLocationRecord")
                            {
								List<VaroLocationRecord> records = JsonConvert.DeserializeObject<List<VaroLocationRecord>>(serializedParent);
								csvWriter.WriteRecords(records);
							}
                            else
                            {
                                //log.LogInformation($"  - Unsupported record type. Will not write list of records to CSV file.");
                            }

                        }
                        else
                        {
                            //log.LogInformation($"  - Zero records found. CSV file will not be written to blog storage.");
                        }

                    }
                    catch (Exception e)
                    {
                        string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "Q25U", EdiErrorsService.BuildErrorVariableArrayList(blobName, BlobContainerClient.Name));
                        throw new Exception(customErrorMessage);
                    }
                }
            }
            return blobName;
        }

        /// <summary>
        /// Calculates record total duration in seconds using relative time (e.g., P8DT30S) of that record
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <returns>
        /// List of denormalized USBDG records (with the calculated duration seconds); Exception (M34T) otherwise
        /// </returns>
        public static async Task<List<EmsEventRecord>> ConvertRelativeTimeToTotalSecondsForEmsLogRecords(List<EmsEventRecord> records)
        {
            foreach (EmsEventRecord record in records)
            {
                try
                {
                    TimeSpan ts = XmlConvert.ToTimeSpan(record.RELT);
                    record.EDI_RELT_ELAPSED_SECS = Convert.ToInt32(ts.TotalSeconds);
                }
                catch (Exception e)
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "M34T", EdiErrorsService.BuildErrorVariableArrayList(record.RELT, record.EDI_SOURCE));
                    throw new Exception(customErrorMessage);
                }
            }
            return records;
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="metadata">USBDG object holding relative time (e.g., P8DT30S)</param>
        /// <param name="ediJob">EDI job object</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static async Task<int> ConvertRelativeTimeStringToTotalSeconds(dynamic metadata, EdiJob ediJob)
        {
            string relativeTime = null;
            int result = 0;

            try
            {
                //JObject sourceJObject = (JObject)metadata;
                //relativeTime = GetKeyValueFromMetadataRecordsObject("RELT", metadata);
                relativeTime = UsbdgDataProcessorService.GetUsbdgMountTimeRelt(ediJob);
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                result = Convert.ToInt32(ts.TotalSeconds);
                return result;

            }
            catch (Exception e)
            {
                if (relativeTime != null)
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
                    throw new Exception(customErrorMessage);
                }
            }
        }

        /// <summary>
        /// Returns a key value from an array "records" in a JSON object 
        /// </summary>
        /// <param name="key">Name of property to find in array "records" of a JSON object</param>
        /// <param name="metadata">JSON object to search</param>
        /// <returns>
        /// Key value found
        /// </returns>
        public static string GetKeyValueFromMetadataRecordsObject(string key, dynamic metadata)
        {
            JObject sourceJObject = (JObject)metadata;
            string result = null;

            // Grab the log header properties from the source metadata file
            foreach (KeyValuePair<string, JToken> source in sourceJObject)
            {
                if (source.Value.Type == JTokenType.Array && source.Key == "records")
                {
                    // Iterate each log record
                    foreach (JObject z in source.Value.Children<JObject>())
                    {
                        // Load each log record property
                        foreach (JProperty prop in z.Properties())
                        {
                            string propName = prop.Name;

                            if (propName == key)
                            {
                                result = (string)prop.Value;
                            }
                        }
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="relativeTime">Relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static async Task<int> ConvertRelativeTimeStringToTotalSeconds(string relativeTime)
        {
            try
            {
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                return Convert.ToInt32(ts.TotalSeconds);
            }
            catch (Exception e)
            {
                if (relativeTime != null)
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
                    throw new Exception(customErrorMessage);
                }
            }
        }

        /// <summary>
        /// Calculate a record's elapsed time (in seconds) since the logger activation relative time
        /// </summary>
        /// <param name="loggerActivationRelativeTime">Logger activation relative time</param>
        /// <param name="recordRelativeTime">Record's relative time</param>
        /// <returns>
        /// Seconds that elapsed snce logger activation relative time
        /// </returns>
        public static async Task<int> CalculateElapsedSecondsFromLoggerMountRelativeTime(string loggerActivationRelativeTime, string recordRelativeTime)
        {
            try
            {
                int loggerActivationRelativeTimeSecs = await ConvertRelativeTimeStringToTotalSeconds(loggerActivationRelativeTime); // convert timespan to seconds
                int recordRelativeTimeSecs = await ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = loggerActivationRelativeTimeSecs - recordRelativeTimeSecs; // How far away time wise is this record compared to the absolute time
                int elapsedDays = (elapsedSeconds / 86000);
                return elapsedSeconds;
            }
            catch (Exception)
            {
                //string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
                //throw new Exception(customErrorMessage);
                throw;
            }
        }

        /// <summary>
        /// Returns a list of Indigo V2 log blobs from a collection of blobs
        /// </summary>
        /// <param name="logDirectoryBlobs">Full list of blobs </param>
        /// <param name="blobPath">blob storage path</param>
        /// <returns>
        /// List containing only Indigo V2 log blobs; Exception (L91T)
        /// </returns>
        public static async Task<List<BlobItem>> GetLogBlobs(IEnumerable<BlobItem> logDirectoryBlobs, string blobPath)
        {
            List<BlobItem> listDataBlobs = new();
            List<BlobItem> listSyncBlobs = new();
            List<BlobItem> listCurrentDataBlobs = new();

            if (logDirectoryBlobs != null)
            {
                foreach (BlobItem logBlob in logDirectoryBlobs)
                {
                    // NHGH-2812 (2023.02.16) - we only want EMS logger (data, current data, and sync) files
                    if (EmsService.IsThisEmsDataFile(logBlob.Name))
                    {
                        listDataBlobs.Add(logBlob);
                    }
                    else if (EmsService.IsThisEmsCurrentDataFile(logBlob.Name))
                    {
                        listCurrentDataBlobs.Add(logBlob);
                    }
                    else if (EmsService.IsThisEmsSyncDataFile(logBlob.Name))
                    {
                        listSyncBlobs.Add(logBlob);
                    }

                }

                // NHGH-2812 (2023.02.16) - Sync files have most recent data and should be processed last
                listDataBlobs.AddRange(listCurrentDataBlobs);
                listDataBlobs.AddRange(listSyncBlobs);

                if (listDataBlobs.Count == 0)
                {
                    string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "L91T", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
                    throw new Exception(customErrorMessage);
                }
            }
            return listDataBlobs;
        }

        /// <summary>
        /// Returns absolute time from EMS report metadata. Falls back to the logger file name if missing from the metadata. 
        /// </summary>
        /// <param name="ediJob">EDI job object</param>
        /// <returns>
        /// Absolute time
        /// </returns>
        public static string GetUsbdgMountTimeRelt(EdiJob ediJob)
        {
            string result = null;
            if (ediJob != null)
            {
                if (ediJob.Emd != null)
                {
                    if (ediJob.Emd.Metadata.Usbdg.MountTime != null)
                    {
                        if (ediJob.Emd.Metadata.Usbdg.MountTime.RELT != null)
                        {
                            result = ediJob.Emd.Metadata.Usbdg.MountTime.RELT;
                        }
                    }
                }
            }
            return result;
        }

        public static DataLoggerTypeEnum.Name GetLoggerTypeFromEmsPackage(EdiJob ediJob, DataLoggerTypeEnum.Name dataLoggerType)
        {
            DataLoggerTypeEnum.Name result;
            if (dataLoggerType == DataLoggerTypeEnum.Name.NO_LOGGER)
            {
                result = DataLoggerTypeEnum.Name.NO_LOGGER;
            } 
            else 
            {
                string loggerModelToCheck = ediJob.Logger.LMOD ?? "";
                EmsLoggerModelCheckResult loggerModelCheckResult = EmsService.GetEmsLoggerModelFromEmsLogLmodProperty(loggerModelToCheck);
                string verfiedLoggerType = loggerModelCheckResult.LoggerModelEnum.ToString().ToLower();
                result = EmsService.GetDataLoggerType(verfiedLoggerType);
            }

            return result;
        }

        /// <summary>
        /// Gets the absolute timestamp of a record using the report absolute timestamp and record relative time
        /// </summary>
        /// <param name="reportAbsoluteTime">Logger record absolute timestamp</param>
        /// <param name="reportDurationSeconds">USBDG metadata duration seconds (converted from relative seconds)</param>
        /// <param name="recordRelativeTime">Logger record relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a USBDG record; Exception (4Q5D) otherwise
        /// </returns>
        public static async Task<DateTime?> CalculateAbsoluteTimeForEmsRecord(string reportAbsoluteTime, int reportDurationSeconds, string recordRelativeTime, string sourceLogFile)
        {
            try
            {
                int recordDurationSeconds = await ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = reportDurationSeconds - recordDurationSeconds; // How far away time wise is this record compared to the absolute time

                DateTime? reportAbsoluteDateTime = await DateConverter.ConvertIso8601CompliantString(reportAbsoluteTime);

                TimeSpan ts = TimeSpan.FromSeconds(elapsedSeconds);
                if (reportAbsoluteDateTime != null)
                {
                    DateTime reportAbsDateTime = (DateTime)reportAbsoluteDateTime;
                    DateTime UtcTime = reportAbsDateTime.Subtract(ts);
                    return UtcTime;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
                throw new Exception(customErrorMessage);
            }
        }

		public static void LogEmsTransformWarningEventToAppInsights(string reportFileName, EmdEnum.Name emdType, PipelineStageEnum.Name stageName, ILogger log, Exception e, string errorCode, string errorMessage, DataLoggerTypeEnum.Name loggerTypeEnum, PipelineFailureReasonEnum.Name pipelineFailureReasonEnum)
		{
			PipelineEvent pipelineEvent = new PipelineEvent();
			pipelineEvent.EventName = PipelineEventEnum.Name.WARN;
			pipelineEvent.StageName = stageName;
			pipelineEvent.LoggerType = loggerTypeEnum;
			pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.WARN;
			pipelineEvent.PipelineFailureReason = pipelineFailureReasonEnum;
			pipelineEvent.ReportFileName = reportFileName;
			pipelineEvent.ErrorCode = errorCode;
			pipelineEvent.ErrorMessage = errorMessage;
			pipelineEvent.EmdType = emdType;
			if (e != null)
			{
				pipelineEvent.ExceptionMessage = e.Message;
				pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
			}
			Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
			AzureAppInsightsService.LogEntry(stageName, customProps, log);
		}

		public static void LogEmsPackageInformation(ILogger log, List<EmsEventRecord> records, EdiJob ediJob)
        {
            log.LogInformation($" ###########################################################################");
            log.LogInformation($" #  - EDI package information ");
            log.LogInformation($" #    - EMD type ..............................: {ediJob.Emd.Type}");
            log.LogInformation($" #    - Logger type ...........................: {ediJob.Logger.Type}");
            log.LogInformation($" #    - Staged path ...........................: {ediJob.Emd.PackageFiles.StagedBlobPath}");
            log.LogInformation($" #    - Report package file name ..............: {ediJob.Emd.PackageFiles.ReportPackageFileName ?? "NOT_FOUND"}");
            log.LogInformation($" #    - Report metadata file name .............: {ediJob.Emd.PackageFiles.ReportMetadataFileName ?? "NOT_FOUND"}");
            if (ediJob.Logger.Type == DataLoggerTypeEnum.Name.NO_LOGGER)
            {
                log.LogInformation($" #    - Sync file name ........................: N/A");
            } else
            {
                log.LogInformation($" #    - Sync file name ........................: {ediJob.Emd.PackageFiles.SyncFileName ?? "NOT_FOUND"}");
            }
            if (ediJob.Emd.Type == EmdEnum.Name.USBDG)
            {
                log.LogInformation($" #    - Report creation time source ...........: {ediJob.Emd.Metadata.Usbdg.CreationTime.SOURCE}");
                if (ediJob.Logger.Type == DataLoggerTypeEnum.Name.NO_LOGGER)
                {
                    log.LogInformation($" #    - EMD logger mount time source ..........: N/A");
                } else
                {
                    log.LogInformation($" #    - EMD logger mount time source ..........: {ediJob.Emd.Metadata.Usbdg.MountTime.SOURCE}");
                }
                    
                log.LogInformation($" #  - EMD report creation times ");
                log.LogInformation($" #    - Absolute UTC (ISO 8601 string format) .: {ediJob.Emd.Metadata.Usbdg.CreationTime.ABST}");
                log.LogInformation($" #    - Absolute UTC (date/time object) .......: {ediJob.Emd.Metadata.Usbdg.CreationTime.ABST_UTC}");
                log.LogInformation($" #    - Relative (ISO 8601 duration format) ...: {ediJob.Emd.Metadata.Usbdg.CreationTime.RELT ?? ""}");
                if (ediJob.Logger.Type == DataLoggerTypeEnum.Name.NO_LOGGER)
                {
                    log.LogInformation($" #  - EMD logger mount times ");
                    log.LogInformation($" #    - Absolute UTC (ISO 8601 string format) .: N/A");
                    log.LogInformation($" #    - Absolute UTC (date/time object) .......: N/A");
                    log.LogInformation($" #    - Relative (ISO 8601 duration format) ...: N/A");
                    log.LogInformation($" #    - Relative (seconds since activation) ...: N/A");
                }
                else
                {
                    log.LogInformation($" #  - EMD logger mount times ");
                    log.LogInformation($" #    - Absolute UTC (ISO 8601 string format) .: {ediJob.Emd.Metadata.Usbdg.MountTime.ABST}");
                    log.LogInformation($" #    - Absolute UTC (date/time object) .......: {ediJob.Emd.Metadata.Usbdg.MountTime.Calcs.ABST_UTC}");
                    log.LogInformation($" #    - Relative (ISO 8601 duration format) ...: {ediJob.Emd.Metadata.Usbdg.MountTime.RELT ?? ""}");
                    log.LogInformation($" #    - Relative (seconds since activation) ...: {ediJob.Emd.Metadata.Usbdg.MountTime.Calcs.RELT_ELAPSED_SECS}");
                }
            }
            else if (ediJob.Emd.Type == EmdEnum.Name.VARO)
            {
                log.LogInformation($" #    - Report creation time source ...........: {ediJob.Emd.Metadata.Varo.CreationTime.SOURCE}");
                log.LogInformation($" #    - EMD logger mount time source ..........: {ediJob.Emd.Metadata.Varo.MountTime.SOURCE}");
                log.LogInformation($" #  - EMD report creation times ");
                log.LogInformation($" #    - Absolute UTC (ISO 8601 string format) .: {ediJob.Emd.Metadata.Varo.CreationTime.ABST}");
                log.LogInformation($" #    - Absolute UTC (date/time object) .......: {ediJob.Emd.Metadata.Varo.CreationTime.ABST_UTC}");
                log.LogInformation($" #   - Relative (ISO 8601 duration format) ....: N/A");
                log.LogInformation($" #  - EMD logger mount times ");
                log.LogInformation($" #    - Absolute UTC (ISO 8601 string format) .: {ediJob.Emd.Metadata.Varo.MountTime.ABST}");
                log.LogInformation($" #    - Absolute UTC (date/time object) .......: {ediJob.Emd.Metadata.Varo.MountTime.Calcs.ABST_UTC}");
                log.LogInformation($" #    - Relative (ISO 8601 duration format) ...: {ediJob.Emd.Metadata.Varo.MountTime.RELT ?? ""}");
                log.LogInformation($" #    - Relative (seconds since activation) ...: {ediJob.Emd.Metadata.Varo.MountTime.Calcs.RELT_ELAPSED_SECS}");
            }

            // NHGH-2819 2023.03.15 1929 Only log if EMD report package has logger data
            if (records != null)
            {
                if (records.Count > 1)
                {
                    int first = (records.Count - 1);
                    int last = (0);
                    log.LogInformation($" #  - EDI logger event records (sample only) ");
                    log.LogInformation($" #    - records[{last}] times");
                    log.LogInformation($" #      - Relative (ISO 8601 duration format) .: {records[last].RELT}");
                    log.LogInformation($" #      - Relative (seconds since activation) .: {records[last].EDI_RELT_ELAPSED_SECS}");
                    if (ediJob.Emd.Type == EmdEnum.Name.VARO)
                    {
                        log.LogInformation($" #      - Relative (seconds since mount time) .: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Varo.MountTime.RELT, records[last].RELT)}");
                    } else if (ediJob.Emd.Type == EmdEnum.Name.USBDG)
                    {
                        log.LogInformation($" #      - Relative (seconds since mount time) .: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Usbdg.MountTime.RELT, records[last].RELT)}");
                    }
                        
                    log.LogInformation($" #      - Absolute UTC (date/time object) .....: {records[last].EDI_ABST}");
                    log.LogInformation($" #    - records[{first}] times");
                    log.LogInformation($" #      - Relative (ISO 8601 duration format) .: {records[first].RELT}");
                    log.LogInformation($" #      - Relative (seconds since activation) .: {records[first].EDI_RELT_ELAPSED_SECS}");

                    if (ediJob.Emd.Type == EmdEnum.Name.VARO)
                    {
                        log.LogInformation($" #      - Relative (seconds since mount time) .: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Varo.MountTime.RELT, records[first].RELT)}");
                    }
                    else if (ediJob.Emd.Type == EmdEnum.Name.USBDG)
                    {
                        log.LogInformation($" #      - Relative (seconds since mount time) .: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Usbdg.MountTime.RELT, records[first].RELT)}");
                    }

                    log.LogInformation($" #      - Absolute UTC (date/time object) .....: {records[first].EDI_ABST}");
                }
            }

            if (ediJob.Emd.PackageFiles.StagedFiles != null)
            {
                if (ediJob.Emd.PackageFiles.StagedFiles.Count > 0)
                {
                    log.LogInformation($" #  - EDI staged files ");
                    foreach (string stagedFile in ediJob.Emd.PackageFiles.StagedFiles)
                    {
                        log.LogInformation($" #    - {stagedFile}");
                    }

                }
            }

            if (ediJob.Emd.PackageFiles.CuratedFiles != null)
            {
                if (ediJob.Emd.PackageFiles.CuratedFiles.Count > 0)
                {
                    log.LogInformation($" #  - EDI curated files ");
                    foreach (string curatedFiles in ediJob.Emd.PackageFiles.CuratedFiles)
                    {
                        log.LogInformation($" #    - {curatedFiles}");
                    }

                }
            }

            log.LogInformation($" ###########################################################################");
        }

        public static string GetSyncFileNameFromBlobPath(string blobPath)
        {
            string syncFileName = null;
            if (EmsService.IsThisEmsSyncDataFile(blobPath))
            {
                string[] parts = blobPath.Split("/"); ;
                string logFileName = parts[parts.Length - 1];
                Match m = EmsService.IsThisEmsSyncFile(logFileName);
                if (m.Success)
                {
                    syncFileName = logFileName;
                }
            }
            return syncFileName;
        }

        public static string GetFileNameFromPath(string path)
        {
            string[] parts = path.Split("/"); ;
            string logFileName = parts[parts.Length - 1];
            return logFileName;

        }


    }
}
