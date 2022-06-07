using CsvHelper;
using lib_edi.Helpers;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Http;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.Errors;
using lib_edi.Services.System;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NJsonSchema;
using NJsonSchema.Validation;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using lib_edi.Services.CceDevice;
using lib_edi.Models.Edi;
using System.Dynamic;
using lib_edi.Models.Csv;
using lib_edi.Models.Emd.Csv;

namespace lib_edi.Services.Loggers
{
	/// <summary>
	/// A class that provides methods processing USBDG log files
	/// </summary>
	public class UsbdgDataProcessorService : DataTransformService
	{
		public static object UsbdgLogProcessorService { get; private set; }

		/// <summary>
		/// Deserializes USBDG log text
		/// </summary>
		/// <param name="blobName">Blob name of USBDG log </param>
		/// <param name="blobText">Downloaded text of USBDG log blob</param>
		/// <returns>
		/// Deserialized USBDG log object; Exception (582N) otherwise
		/// </returns>
		private static JObject DeserializeUsbdgLogText(string blobName, string blobText)
		{
			try
			{
				//return JsonConvert.DeserializeObject<UsbdgJsonDataFileDto>(blobText);
				//return JsonConvert.DeserializeObject(blobText);
				dynamic results = JsonConvert.DeserializeObject<dynamic>(blobText);
				//JObject results = JObject.Parse(blobText);
				return results;
			}
			catch (Exception e)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "582N", EdiErrorsService.BuildErrorVariableArrayList(blobName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Deserialized USBDG log report text
		/// </summary>
		/// <param name="blobName">Blob name of USBDG log report </param>
		/// <param name="blobText">Downloaded text of USBDG log report blob</param>
		/// <returns>
		/// Deserializes USBDG log report object; Exception (89EX) otherwise
		/// </returns>
		private static dynamic DeserializeUsbdgLogReportText(string blobName, string blobText)
		{
			try
			{
				return JsonConvert.DeserializeObject<dynamic>(blobText);
			}
			catch (Exception e)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "89EX", EdiErrorsService.BuildErrorVariableArrayList(blobName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Serializes USBDG log
		/// </summary>
		/// <param name="emsLog">EMS log object </param>
		/// <returns>
		/// Serialized USBDG log text; Exception (48TV) otherwise
		/// </returns>
		private static string SerializeUsbdgLogText(dynamic emsLog)
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
		/// Returns a list of only USBDB log blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <param name="blobPath">blob storage path</param>
		/// <returns>
		/// List containing only USBDG log blobs; Exception (L91T)
		/// </returns>
		public static List<CloudBlockBlob> FindUsbdgLogBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
		{
			List<CloudBlockBlob> usbdgLogBlobs = new List<CloudBlockBlob>();

			if (logDirectoryBlobs != null)
			{
				foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
				{
					if (logBlob.Name.Contains("DATA") || logBlob.Name.Contains("CURRENT"))
					{
						usbdgLogBlobs.Add(logBlob);
					}
				}
				if (usbdgLogBlobs.Count == 0)
				{
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "L91T", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
					throw new Exception(customErrorMessage);
				}
			}
			return usbdgLogBlobs;
		}

		/// <summary>
		/// Returns a list of only USBDG metadata report blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <returns>
		/// List containing only USBDG metadata report blobs; Exception (RV62) otherwise
		/// </returns>
		public static CloudBlockBlob GetReportMetadataBlob(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
		{
			List<CloudBlockBlob> usbdgLogReportBlobs = new();

			if (logDirectoryBlobs != null)
			{
				foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
				{
					if (logBlob.Name.Contains("report"))
					{
						usbdgLogReportBlobs.Add(logBlob);
					}
				}
			}

			if (usbdgLogReportBlobs.Count == 0)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "RV62", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}

			return usbdgLogReportBlobs.First();
		}

		/// <summary>
		/// Calculates record total duration in seconds using relative time (e.g., P8DT30S) of that record
		/// </summary>
		/// <param name="records">List of denormalized USBDG records </param>
		/// <returns>
		/// List of denormalized USBDG records (with the calculated duration seconds); Exception (M34T) otherwise
		/// </returns>
		public static List<UsbdgSimCsvRecordDto> ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(List<UsbdgSimCsvRecordDto> records)
		{
			foreach (UsbdgSimCsvRecordDto record in records)
			{
				try
				{
					TimeSpan ts = XmlConvert.ToTimeSpan(record.RELT);
					record._RELT_SECS = Convert.ToInt32(ts.TotalSeconds);
				}
				catch (Exception e)
				{
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "M34T", EdiErrorsService.BuildErrorVariableArrayList(record.RELT, record.Source));
					throw new Exception(customErrorMessage);
				}
			}
			return records;
		}

		/// <summary>
		/// Gets the absolute timestamp of USBDG records using the report absolute timestamp and relative time of records
		/// </summary>
		/// <param name="records">List of denormalized USBDG records </param>
		/// <param name="reportDurationSeconds">USBDG log report duration seconds</param>
		/// <param name="reportAbsoluteTimestamp">USBDG log report record relative time (e.g., P8DT30S)</param>
		/// <returns>
		/// Absolute timestamp (DateTime) of a USBDG record; Exception (4Q5D) otherwise
		/// </returns>
		public static List<UsbdgSimCsvRecordDto> CalculateAbsoluteTimeForUsbdgRecords(List<UsbdgSimCsvRecordDto> records, int reportDurationSeconds, dynamic reportAbsoluteTimestamp)
		{
			string absoluteTime = ObjectManager.GetJObjectPropertyValueAsString(reportAbsoluteTimestamp, "ABST");

			foreach (UsbdgSimCsvRecordDto record in records)
			{
				DateTime? dt = CalculateAbsoluteTimeForUsbdgRecord(absoluteTime, reportDurationSeconds, record.RELT, record.Source);
				record.ABST_CALC = dt;
			}
			return records;
		}

		/// <summary>
		/// Converts relative time to total seconds
		/// </summary>
		/// <param name="relativeTime">Relative time (e.g., P8DT30S)</param>
		/// <returns>
		/// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
		/// </returns>
		public static int ConvertRelativeTimeStringToTotalSeconds(dynamic metadata)
		{
			string relativeTime = null;

			try
			{
				relativeTime = ObjectManager.GetJObjectPropertyValueAsString(metadata, "RELT");
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
		/// Gets the absolute timestamp of a record using the report absolute timestamp and record relative time
		/// </summary>
		/// <param name="reportAbsoluteTime">Log report absolute timestamp</param>
		/// <param name="reportDurationSeconds">Log report duration seconds</param>
		/// <param name="recordRelativeTime">Log report record relative time (e.g., P8DT30S)</param>
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
				} else
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
			catch (Exception e)
			{
				//string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
				//throw new Exception(customErrorMessage);
				throw e;
			}
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
		public static async Task<string> WriteUsbdgLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<UsbdgSimCsvRecordDto> usbdgRecords, ILogger log)
		{
			string blobName = "";
			if (requestBody != null)
			{
				if (requestBody.Path != null)
				{
					try
					{
						//string loggerType = CcdxService.GetDataLoggerTypeFromBlobPath(requestBody.Path);
						//string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd/HH");
						//string guidFolder = CcdxService.GetGuidFromBlobPath(requestBody.Path);

						blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path);

						//blobName = $"{dateFolder}/{guidFolder}/out.csv";
						log.LogInformation($"  - Blob: {blobName}");
						log.LogInformation($"  - Get block blob reference");
						CloudBlockBlob outBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
						log.LogInformation($"  - Open stream for writing to the blob");
						using var writer = await outBlob.OpenWriteAsync();
						log.LogInformation($"  - Initialize new instance of stream writer");
						using var streamWriter = new StreamWriter(writer);
						log.LogInformation($"  - Initialize new instance of csv writer");
						using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
						log.LogInformation($"  - Write list of records to the CSV file");
						csvWriter.WriteRecords(usbdgRecords);
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
		/// Downloads and deserializes a list of USBDG logs stored in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of USBDG log blobs located in this virtual directory</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="blobPath">Path to USBDG log log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized USBDG log objects that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
		/// </returns>
		public static async Task<List<dynamic>> DownloadUsbdgLogBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{

			List<dynamic> usbdgLogFiles = new List<dynamic>();
			foreach (CloudBlockBlob logBlob in blobs)
			{
				string emsBlobPath = $"{ cloudBlobContainer.Name}/{ logBlob.Name}";
				log.LogInformation($"  - Blob: {emsBlobPath}");
				string emsLogJsonText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, logBlob.Name);
				dynamic emsLog = DeserializeUsbdgLogText(logBlob.Name, emsLogJsonText);
				emsLog._SOURCE = $"{emsBlobPath}";
				usbdgLogFiles.Add(emsLog);
			}

			if (usbdgLogFiles.Count == 0)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "C26Z", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}

			return usbdgLogFiles;
		}

		/// <summary>
		/// Validates 
		/// </summary>
		/// <param name="emsLogs">A list of downloaded USBDG log objects</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of validated USBDG log objects; Exception thrown if at least one report fails validation (R85Y) or if the json definition file failed to be retrieved 
		/// </returns>
		public static async Task<List<dynamic>> ValidateUsbdgLogBlobs(CloudBlobContainer cloudBlobContainer, List<dynamic> emsLogs, ILogger log)
		{
			List<dynamic> validatedEmsLogs = new List<dynamic>();

			string usbdgConfigBlobName;
			string usbdgConfigBlobJson;
			JsonSchema emsLogJsonSchema;

			try
			{
				usbdgConfigBlobName = Environment.GetEnvironmentVariable("EMS_LOG_JSON_SCHEMA_DEFINITION_FILE_NAME");
				usbdgConfigBlobJson = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, usbdgConfigBlobName);
				emsLogJsonSchema = await JsonSchema.FromJsonAsync(usbdgConfigBlobJson);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "FY84", null);
				throw new Exception(customErrorMessage);
			}

			foreach (dynamic emsLog in emsLogs)
			{
				string emsLogText = SerializeUsbdgLogText(emsLog);

				ICollection<ValidationError> errors = emsLogJsonSchema.Validate(emsLogText);
				if (errors.Count == 0)
				{
					log.LogInformation($"    - Validated: Yes");
					validatedEmsLogs.Add(emsLog);
				}
				else
				{
					string validationResultString = EdiErrorsService.BuildJsonValidationErrorString(errors);
					log.LogError($"    - Validated: No - {validationResultString}");
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "R85Y", EdiErrorsService.BuildErrorVariableArrayList(emsLog._SOURCE, validationResultString));
					throw new Exception(customErrorMessage);
				}
			}

			return validatedEmsLogs;
		}

		/// <summary>
		/// Downloads and deserializes a USBDG log report stored in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of USBDG log report blobs located in this virtual directory</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized USBDG log report objects that have been downloaded from Azure blob storage; Exception (P76H) otherwise
		/// </returns>
		public static async Task<dynamic> DownloadUsbdgLogReportBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{
			dynamic emsLogMetadata = null;

			foreach (CloudBlockBlob reportBlob in blobs)
			{
				log.LogInformation($"  - Blob: {cloudBlobContainer.Name}/{reportBlob.Name}");
				string blobText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, reportBlob.Name);
				emsLogMetadata = DeserializeUsbdgLogReportText(reportBlob.Name, blobText);
			}

			if (emsLogMetadata == null)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "P76H", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}

			return emsLogMetadata;
		}

		/// <summary>
		/// Maps raw EMD and logger files to CSV compatible format
		/// </summary>
		/// <remarks>
		/// This mapping denormalizes the logger data file into records ready for CSV serialization.
		/// </remarks>
		/// <param name="emdLogFile">A deserialized logger data file</param>
		/// <param name="metadataFile">A deserialized EMD metadata file</param>
		/// <returns>
		/// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
		/// </returns>
		public static EdiJob PopulateEdiJobObject(dynamic sourceUsbdgMetadata, List<dynamic> sourceLogs)
		{
			string propName = null;
			string propValue = null;
			string sourceFile = null;
			EdiJob ediJob = new EdiJob();

			try
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
							ObjectManager.SetObjectValue(ref ediJob, log1.Key, log1.Value);
						} if (log1.Value.Type == JTokenType.Array && log1.Key == "records")
						{
							foreach (JObject z in log1.Value.Children<JObject>())
							{
								// Load each log record property
								foreach (JProperty prop in z.Properties())
								{
									propName = prop.Name;
									propValue = (string)prop.Value;
									ObjectManager.SetObjectValue(ref ediJob, prop.Name, prop.Value);
								}

								//sinkCsvEventRecords.Add((IndigoV2EventRecord)sinkCsvEventRecord);
							}
						}
					}

				}

				// 
				JObject sourceUsbdgMetadataJObject = (JObject)sourceUsbdgMetadata;
				var reportHeaderObject = new ExpandoObject() as IDictionary<string, Object>;
				foreach (KeyValuePair<string, JToken> log2 in sourceUsbdgMetadataJObject)
				{
					if (log2.Value.Type != JTokenType.Array)
					{
						reportHeaderObject.Add(log2.Key, log2.Value);
						ObjectManager.SetObjectValue(ref ediJob, log2.Key, log2.Value);
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
		/// Maps raw EMD and logger files to CSV compatible format
		/// </summary>
		/// <remarks>
		/// This mapping denormalizes the logger data file into records ready for CSV serialization.
		/// </remarks>
		/// <param name="emdLogFile">A deserialized logger data file</param>
		/// <param name="metadataFile">A deserialized EMD metadata file</param>
		/// <returns>
		/// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
		/// </returns>
		public static List<EdiSinkRecord> MapUsbdgDevice(dynamic sourceUsbdgMetadata)
		{
			string propName = null;
			string propValue = null;
			string sourceFile = null;

			try
			{
				List<EdiSinkRecord> sinkCsvLocationsRecords = new();
				JObject sourceJObject = (JObject)sourceUsbdgMetadata;
				sourceFile = DataTransformService.GetSourceFile(sourceJObject);

				EdiSinkRecord sinkUsbdgDeviceRecord = new UsbdgDeviceRecord();

				// Grab the log header properties from the source metadata file
				var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
				foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
				{
					if (log1.Value.Type != JTokenType.Array)
					{
						sourceHeaders.Add(log1.Key, log1.Value);
						ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
					}
				}
				sinkCsvLocationsRecords.Add(sinkUsbdgDeviceRecord);
				return sinkCsvLocationsRecords;
			}
			catch (Exception e)
			{
				throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "CPA8", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
			}
		}

		/// <summary>
		/// Maps raw EMD and logger files to CSV compatible format
		/// </summary>
		/// <remarks>
		/// This mapping denormalizes the logger data file into records ready for CSV serialization.
		/// </remarks>
		/// <param name="emdLogFile">A deserialized logger data file</param>
		/// <param name="metadataFile">A deserialized EMD metadata file</param>
		/// <returns>
		/// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
		/// </returns>
		public static List<EdiSinkRecord> MapUsbdgEvent(dynamic sourceUsbdgMetadata)
		{
			string propName = null;
			string propValue = null;
			string sourceFile = null;

			try
			{
				List<EdiSinkRecord> sinkCsvLocationsRecords = new();
				JObject sourceJObject = (JObject)sourceUsbdgMetadata;
				sourceFile = DataTransformService.GetSourceFile(sourceJObject);

				EdiSinkRecord sinkUsbdgDeviceRecord = new UsbdgEventRecord();

				// Grab the log header properties from the source metadata file
				var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
				foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
				{
					if (log1.Value.Type != JTokenType.Array)
					{
						sourceHeaders.Add(log1.Key, log1.Value);
						ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
					}

					// Load log record properties into csv record object
					if (log1.Value.Type == JTokenType.Array && log1.Key == "records")
					{
						// Iterate each log record
						foreach (JObject z in log1.Value.Children<JObject>())
						{
							// Load each log record property
							foreach (JProperty prop in z.Properties())
							{
								propName = prop.Name;
								propValue = (string)prop.Value;
								ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, prop.Name, prop.Value);
							}
						}
					}
				}




				sinkCsvLocationsRecords.Add(sinkUsbdgDeviceRecord);
				return sinkCsvLocationsRecords;
			}
			catch (Exception e)
			{
				throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "DYUF", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
			}
		}

		public static dynamic GetUsbdgMetadataRecordsElement(dynamic metadata)
		{
			dynamic recordsElement = null;

			if (metadata != null)
			{
				if (metadata.records != null)
				{
					if (metadata.records.Type == JTokenType.Array)
                    {
						if (metadata.records.Count > 0)
						{
							recordsElement = metadata.records[0];
						}
					}
				}
			}

			return recordsElement;
		}




	}
}
