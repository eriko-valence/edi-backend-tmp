using CsvHelper;
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

namespace lib_edi.Services.Loggers
{
	/// <summary>
	/// A class that provides methods processing USBDG log files
	/// </summary>
	public class UsbdgDataProcessorService
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
		private static UsbdgJsonDataFileDto DeserializeUsbdgLogText(string blobName, string blobText)
		{
			try
			{
				return JsonConvert.DeserializeObject<UsbdgJsonDataFileDto>(blobText);
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
		private static UsbdgJsonReportFileDto DeserializeUsbdgLogReportText(string blobName, string blobText)
		{
			try
			{
				return JsonConvert.DeserializeObject<UsbdgJsonReportFileDto>(blobText);
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
		private static string SerializeUsbdgLogText(UsbdgJsonDataFileDto emsLog)
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
		/// Returns a list of only USBDB log report blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <returns>
		/// List containing only USBDG log report blobs; Exception (RV62) otherwise
		/// </returns>
		public static List<CloudBlockBlob> FindUsbdgLogReportBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
		{
			List<CloudBlockBlob> usbdgLogReportBlobs = new List<CloudBlockBlob>();

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

			return usbdgLogReportBlobs;
		}

		/// <summary>
		/// Calculates record total duration in seconds using relative time (e.g., P8DT30S) of that record
		/// </summary>
		/// <param name="records">List of denormalized USBDG records </param>
		/// <returns>
		/// List of denormalized USBDG records (with the calculated duration seconds); Exception (M34T) otherwise
		/// </returns>
		public static List<UsbdgCsvDataRowDto> ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(List<UsbdgCsvDataRowDto> records)
		{
			foreach (UsbdgCsvDataRowDto record in records)
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
		public static List<UsbdgCsvDataRowDto> CalculateAbsoluteTimeForUsbdgRecords(List<UsbdgCsvDataRowDto> records, int reportDurationSeconds, string reportAbsoluteTimestamp)
		{
			foreach (UsbdgCsvDataRowDto record in records)
			{
				DateTime dt = CalculateAbsoluteTimeForUsbdgRecord(reportAbsoluteTimestamp, reportDurationSeconds, record.RELT, record.Source);
				record._ABST = dt;
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
		/// Gets the absolute timestamp of a USBDG record using the report absolute timestamp and record relative time
		/// </summary>
		/// <param name="reportAbsoluteTime">USBDG log report absolute timestamp</param>
		/// <param name="reportDurationSeconds">USBDG log report duration seconds</param>
		/// <param name="recordRelativeTime">USBDG log report record relative time (e.g., P8DT30S)</param>
		/// <returns>
		/// Absolute timestamp (DateTime) of a USBDG record; Exception (4Q5D) otherwise
		/// </returns>
		private static DateTime CalculateAbsoluteTimeForUsbdgRecord(string reportAbsoluteTime, int reportDurationSeconds, string recordRelativeTime, string sourceLogFile)
		{
			try
			{
				int recordDurationSeconds = ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
				int elapsedSeconds = reportDurationSeconds - recordDurationSeconds; // How far away time wise is this record compared to the absolute time
				
				DateTime reportAbsoluteDateTime = DateTimeService.ConvertIso8601CompliantString(reportAbsoluteTime);

				TimeSpan ts = TimeSpan.FromSeconds(elapsedSeconds);
				DateTime UtcTime = reportAbsoluteDateTime.Subtract(ts);
				return UtcTime;
			}
			catch (Exception e)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
				throw new Exception(customErrorMessage);
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
		public static async Task<string> WriteUsbdgLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<UsbdgCsvDataRowDto> usbdgRecords, ILogger log)
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
		public static async Task<List<UsbdgJsonDataFileDto>> DownloadUsbdgLogBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{

			List<UsbdgJsonDataFileDto> usbdgLogFiles = new List<UsbdgJsonDataFileDto>();
			foreach (CloudBlockBlob logBlob in blobs)
			{
				string emsBlobPath = $"{ cloudBlobContainer.Name}/{ logBlob.Name}";
				log.LogInformation($"  - Blob: {emsBlobPath}");
				string emsLogJsonText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, logBlob.Name);
				UsbdgJsonDataFileDto emsLog = DeserializeUsbdgLogText(logBlob.Name, emsLogJsonText);
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
		public static async Task<List<UsbdgJsonDataFileDto>> ValidateUsbdgLogBlobs(CloudBlobContainer cloudBlobContainer, List<UsbdgJsonDataFileDto> emsLogs, ILogger log)
		{
			List<UsbdgJsonDataFileDto> validatedEmsLogs = new List<UsbdgJsonDataFileDto>();

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

			foreach (UsbdgJsonDataFileDto emsLog in emsLogs)
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
					string validationResultString = BuildJsonValidationErrorString(errors);
					log.LogError($"    - Validated: No - {validationResultString}");
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "R85Y", EdiErrorsService.BuildErrorVariableArrayList(emsLog._SOURCE, validationResultString));
					throw new Exception(customErrorMessage);
				}
			}

			return validatedEmsLogs;
		}

		/// <summary>
		/// Builds a json validation error string from NJsonSchema.Validation.ValidationError
		/// </summary>
		/// <param name="errors">Collection of NJsonSchema.Validation.ValidationError objects</param>
		/// <returns>
		/// String results pulled from the first NJsonSchema.Validation.ValidationError in the collection
		/// </returns>
		private static string BuildJsonValidationErrorString(ICollection<ValidationError> errors)
		{
			string result = "";

			if (errors != null)
			{
				if (errors.Count > 0)
				{
					List<ValidationError> e = errors.ToList();
					ValidationError ve = e[0];

					if (ve != null)
					{
						result = ve.ToString();
					}
				}
			}

			return result;
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
		public static async Task<UsbdgJsonReportFileDto> DownloadUsbdgLogReportBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{
			UsbdgJsonReportFileDto emsLogMetadata = null;
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
	}
}
