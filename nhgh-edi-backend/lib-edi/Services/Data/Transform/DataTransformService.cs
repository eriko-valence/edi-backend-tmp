using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
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
		public static string SerializeJsonObject(dynamic emsLog)
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
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "48TV", null);
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Validates a list provided JSON data objects against a provided JSON schema
		/// </summary>
		/// <param name="listOfJsonDataoValidate">A list of validated JSON objects</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="blobNameJsonSchema">Blob name of JSON schema that will be used to validate the JSON data</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of validated JSON data objects; Exception thrown if at least one report fails validation (R85Y) or if the json definition file failed to be retrieved from blob storage (FY84)
		/// </returns>
		public static async Task<List<dynamic>> ValidateJsonObjects(CloudBlobContainer cloudBlobContainer, List<dynamic> listOfJsonDataoValidate, string blobNameJsonSchema, ILogger log)
		{
			List<dynamic> validatedJsonObjects = new();

			string configBlobJsonText;
			JsonSchema configJsonSchema;

			try
			{
				configBlobJsonText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, blobNameJsonSchema);
				configJsonSchema = await JsonSchema.FromJsonAsync(configBlobJsonText);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "FY84", null);
				throw new Exception(customErrorMessage);
			}

			foreach (dynamic emsLog in listOfJsonDataoValidate)
			{
				string emsLogText = SerializeJsonObject(emsLog);

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
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "R85Y", al);
					throw new Exception(customErrorMessage);
				}
			}

			return validatedJsonObjects;
		}

		/// <summary>
		/// Validates a provided JSON data object against a provided schema 
		/// </summary>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="jsonDataToValidate">JSON data object to validate</param>
		/// <param name="blobNameJsonSchema">Blob name of JSON schema that will be used to validate the JSON data</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A vlaidated JSON data object; Exception thrown if validation fails (YR42) or if the json definition file failed to be retrieved from blob storage (4VN5)
		/// </returns>
		public static async Task<dynamic> ValidateJsonObject(CloudBlobContainer cloudBlobContainer, dynamic jsonDataToValidate, string blobNameJsonSchema, ILogger log)
		{
			// Deserialize JSON schema
			string jsonSchemaString;
			JsonSchema jsonSchemaObject;
			try
			{
				jsonSchemaString = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, blobNameJsonSchema);
				jsonSchemaObject = await JsonSchema.FromJsonAsync(jsonSchemaString);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4VN5", null);
				throw new Exception(customErrorMessage);
			}

			// Validate the JSON data against the schema
			string jsonStringToValidate = SerializeJsonObject(jsonDataToValidate);
			ICollection<ValidationError> errors = jsonSchemaObject.Validate(jsonStringToValidate);
			if (errors.Count == 0)
			{
				log.LogInformation($"    - Validated: Yes");
				return jsonDataToValidate;
			}
			else
			{
				string validationResultString = EdiErrorsService.BuildJsonValidationErrorString(errors);
				log.LogError($"    - Validated: No - {validationResultString}");
				string source = jsonDataToValidate.EDI_SOURCE;
				ArrayList al = EdiErrorsService.BuildErrorVariableArrayList(source, validationResultString);
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "YR42", al);
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
		public static string SerializeHttpResponseBody(string csvBlobName)
		{
			try
			{
				TransformHttpResponseMessageBodyDto emsLogResponseDto = new TransformHttpResponseMessageBodyDto();
				emsLogResponseDto.Path = csvBlobName;
				return JsonConvert.SerializeObject(emsLogResponseDto);
			}
			catch (Exception e)
			{
				string customError = EdiErrorsService.BuildExceptionMessageString(e, "X83E", null);
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
		/// Populates an EDI job object from logger data and USBDG metadata files
		/// </summary>
		/// <remarks>
		/// This EDI object holds properties useful further downstream in the processing
		/// </remarks>
		/// <param name="sourceUsbdgMetadata">A deserialized USBDG metadata</param>
		/// <param name="sourceLogs">A list of deserialized logger data files</param>
		/// <returns>
		/// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
		/// </returns>
		public static EdiJob PopulateEdiJobObject(dynamic sourceUsbdgMetadata, List<dynamic> sourceLogs)
		{
			string propName = null;
			string propValue = null;
			string sourceFile = null;
			EdiJob ediJob = new ();

			try
			{
				if (sourceLogs != null)
                {
					foreach (dynamic sourceLog in sourceLogs)
					{
						JObject sourceLogJObject = (JObject)sourceLog;

						// Grab the log header properties from the source log file
						var logHeaderObject = new ExpandoObject() as IDictionary<string, Object>;
						foreach (KeyValuePair<string, JToken> log1 in sourceLogJObject)
						{
							if (log1.Value.Type != JTokenType.Array)
							{
								logHeaderObject.Add(log1.Key, log1.Value);
								ObjectManager.SetObjectValue(ediJob.Logger, log1.Key, log1.Value);
							}
						}
					}
				}

				JObject sourceUsbdgMetadataJObject = (JObject)sourceUsbdgMetadata;
				var reportHeaderObject = new ExpandoObject() as IDictionary<string, Object>;

                foreach (KeyValuePair<string, JToken> log2 in sourceUsbdgMetadataJObject)
				{
					if (log2.Value.Type != JTokenType.Array)
					{
						reportHeaderObject.Add(log2.Key, log2.Value);
						ObjectManager.SetObjectValue(ediJob.UsbdgMetadata, log2.Key, log2.Value);
					}

					if (log2.Value.Type == JTokenType.Array && log2.Key == "records")
					{
						foreach (JObject z in log2.Value.Children<JObject>())
						{
							// Load each log record property
							foreach (JProperty prop in z.Properties())
							{
								propName = prop.Name;
								propValue = (string)prop.Value;
								ObjectManager.SetObjectValue(ediJob.UsbdgMetadata, prop.Name, prop.Value);
							}
						}
					}
				}
				return ediJob;
			}
			catch (Exception e)
			{
				throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "D39Y", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
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
        public static void LogEmsTransformSucceededEventToAppInsights(string reportFileName, DataLoggerTypeEnum.Name loggerType, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.LoggerType = loggerType;
            pipelineEvent.ReportFileName = reportFileName;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
        }

        /// <summary>
        /// EMS ADF data transformation stage has started
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogEmsTransformStartedEventToAppInsights(string reportFileName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.ReportFileName = reportFileName;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
        }

        /// <summary>
        /// Sends EMS ADF data transformatoin error event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        /// <param name="e">Exception object</param>
        /// <param name="errorCode">Error code</param>
        public static void LogEmsTransformErrorEventToAppInsights(string reportFileName, ILogger log, Exception e, string errorCode, string errorMessage, DataLoggerTypeEnum.Name loggerTypeEnum, PipelineFailureReasonEnum.Name failureReason)
        {
            //string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.LoggerType = loggerTypeEnum;
            pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
            pipelineEvent.PipelineFailureReason = failureReason;
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

        /// <summary>
        /// Writes denormalized USBDG log file csv records to Azure blob storage
        /// </summary>
        /// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
        /// <param name="requestBody">EMS log transformation http reqest object</param>
        /// <param name="usbdgRecords">A list of denormalized USBDG log records</param>
        /// <param name="log">Azure function logger object</param>
        /// <returns>
        /// Blob name of USBDG csv formatted log file; Exception (Q25U)
        /// </returns>
        public static async Task<string> WriteRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<EdiSinkRecord> usbdgRecords, string loggerTypeName, ILogger log)
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
                            log.LogInformation($"  - Determine object type using list of generic records");
                            var firstRecord = usbdgRecords.FirstOrDefault();
                            string recordType = firstRecord.GetType().Name;
                            log.LogInformation($"    - Record type: {recordType}");
                            if (recordType == "IndigoV2EventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_event.csv", loggerType);
                            }
                            else if (recordType == "Sl1EventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "sl1_event.csv", loggerType);
                            }
                            else if (recordType == "IndigoV2LocationRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgDeviceRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_device.csv", loggerType);
                            }
                            else if (recordType == "UsbdgEventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_event.csv", loggerType);
                            }
                            else
                            {
                                log.LogInformation($"  - Is record type supported? No");
                                return blobName;
                            }
                            log.LogInformation($"  - Blob: {blobName}");
                            log.LogInformation($"  - Get block blob reference");
                            CloudBlockBlob outBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                            log.LogInformation($"  - Open stream for writing to the blob");
                            using var writer = await outBlob.OpenWriteAsync();
                            log.LogInformation($"  - Initialize new instance of stream writer");
                            using var streamWriter = new StreamWriter(writer);
                            log.LogInformation($"  - Initialize new instance of csv writer");
                            using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                            log.LogInformation($"  - Pull child records from list");
                            var serializedParent = JsonConvert.SerializeObject(usbdgRecords);
                            if (recordType == "IndigoV2EventRecord")
                            {
                                List<IndigoV2EventRecord> records = JsonConvert.DeserializeObject<List<IndigoV2EventRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of indigo v2 event records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "Sl1EventRecord")
                            {
                                List<Sl1EventRecord> records = JsonConvert.DeserializeObject<List<Sl1EventRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of sl1 event records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "IndigoV2LocationRecord")
                            {
                                List<IndigoV2LocationRecord> records = JsonConvert.DeserializeObject<List<IndigoV2LocationRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of indigo v2 location records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                List<UsbdgLocationRecord> records = JsonConvert.DeserializeObject<List<UsbdgLocationRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of usbdg location records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgDeviceRecord")
                            {
                                List<UsbdgDeviceRecord> records = JsonConvert.DeserializeObject<List<UsbdgDeviceRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of usbdg device records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else if (recordType == "UsbdgEventRecord")
                            {
                                List<UsbdgEventRecord> records = JsonConvert.DeserializeObject<List<UsbdgEventRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of usbdg event records to the CSV file");
                                csvWriter.WriteRecords(records);
                            }
                            else
                            {
                                log.LogInformation($"  - Unsupported record type. Will not write list of records to CSV file.");
                            }

                        }
                        else
                        {
                            log.LogInformation($"  - Zero records found. CSV file will not be written to blog storage.");
                        }

                    }
                    catch (Exception e)
                    {
                        string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "Q25U", EdiErrorsService.BuildErrorVariableArrayList(blobName, cloudBlobContainer.Name));
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
        public static List<EmsEventRecord> ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(List<EmsEventRecord> records)
        {
            foreach (EmsEventRecord record in records)
            {
                try
                {
                    TimeSpan ts = XmlConvert.ToTimeSpan(record.RELT);
                    record._RELT_SECS = Convert.ToInt32(ts.TotalSeconds);
                }
                catch (Exception e)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "M34T", EdiErrorsService.BuildErrorVariableArrayList(record.RELT, record.EDI_SOURCE));
                    throw new Exception(customErrorMessage);
                }
            }
            return records;
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="metadata">USBDG object holding relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static int ConvertRelativeTimeStringToTotalSeconds(dynamic metadata)
        {
            string relativeTime = null;
            int result = 0;

            try
            {
                JObject sourceJObject = (JObject)metadata;
                relativeTime = GetKeyValutFromMetadataRecordsObject("RELT", metadata);
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                result = Convert.ToInt32(ts.TotalSeconds);
                return result;

            }
            catch (Exception e)
            {
                if (relativeTime != null)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
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
        public static string GetKeyValutFromMetadataRecordsObject(string key, dynamic metadata)
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
        /// Calculates the absolute timestamp for each Indigo V2 records using the USBDG metadata absolute timestamp and relative time of records
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <param name="reportDurationSeconds">USBDG metadata duration seconds (converted from relative seconds)</param>
        /// <param name="reportMetadata">USBDG metadata file json object</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a Indigo V2 record; Exception (4Q5D) otherwise
        /// </returns>
        public static List<EmsEventRecord> CalculateAbsoluteTimeForUsbdgRecords(List<EmsEventRecord> records, int reportDurationSeconds, dynamic reportMetadata)
        {
            string absoluteTime = GetKeyValutFromMetadataRecordsObject("ABST", reportMetadata);

            foreach (EmsEventRecord record in records)
            {
                DateTime? dt = CalculateAbsoluteTimeForUsbdgRecord(absoluteTime, reportDurationSeconds, record.RELT, record.EDI_SOURCE);
                record.EDI_RECORD_ABST_CALC = dt;
            }
            return records;
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
        private static DateTime? CalculateAbsoluteTimeForUsbdgRecord(string reportAbsoluteTime, int reportDurationSeconds, string recordRelativeTime, string sourceLogFile)
        {
            try
            {
                int recordDurationSeconds = ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = reportDurationSeconds - recordDurationSeconds; // How far away time wise is this record compared to the absolute time

                DateTime? reportAbsoluteDateTime = DateConverter.ConvertIso8601CompliantString(reportAbsoluteTime);

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
                string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
                throw new Exception(customErrorMessage);
            }
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="relativeTime">Relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static int ConvertRelativeTimeStringToTotalSeconds(string relativeTime)
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
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
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
        public static int CalculateElapsedSecondsFromLoggerActivationRelativeTime(string loggerActivationRelativeTime, string recordRelativeTime)
        {
            try
            {
                int loggerActivationRelativeTimeSecs = ConvertRelativeTimeStringToTotalSeconds(loggerActivationRelativeTime); // convert timespan to seconds
                int recordRelativeTimeSecs = ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = loggerActivationRelativeTimeSecs - recordRelativeTimeSecs; // How far away time wise is this record compared to the absolute time
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
        public static List<CloudBlockBlob> GetLogBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
        {
            List<CloudBlockBlob> listDataBlobs = new();
            List<CloudBlockBlob> listSyncBlobs = new();
            List<CloudBlockBlob> listCurrentDataBlobs = new();

            if (logDirectoryBlobs != null)
            {
                foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
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
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "L91T", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
                    throw new Exception(customErrorMessage);
                }
            }
            return listDataBlobs;
        }
    }
}
