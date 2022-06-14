using CsvHelper;
using lib_edi.Helpers;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Http;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.CceDevice;
using lib_edi.Services.Errors;
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
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Loggers
{
	/// <summary>
	/// A class that provides methods processing MetaFridge log files
	/// </summary>
	public class Cfd50DataProcessorService : DataTransformService
	{
		/// <summary>
		/// Deserializes the downloaded MetaFridge log blob's text
		/// </summary>
		/// <param name="blobName">Name of MetaFridge log blob from which the text was downloaded</param>
		/// <param name="blobText">MetaFridge log blob text to download</param>
		/// <returns>
		/// Deserialized MetaFridge log object if successful; Exception (X7Z1) otherwise
		/// </returns>
		private static dynamic DeserializeMetaFridgeLogText(string blobName, string blobText)
		{
			try
			{
				dynamic obj = JsonConvert.DeserializeObject<dynamic>(blobText);
				obj.EDI_SOURCE = blobName;
				return obj;
			}
			catch (Exception e)
			{
				string customError = EdiErrorsService.BuildExceptionMessageString(e, "X7Z1", EdiErrorsService.BuildErrorVariableArrayList(blobName));
				throw new Exception(customError);
			}
		}

		/// <summary>
		/// Returns a list of cloud block blobs located in this virtual directory
		/// </summary>
		/// <param name="logDirectoryBlobs">Virtual directory where blobs are located</param>
		/// <returns>
		/// A list of cloud block blobs located in this virtual directory; Exception (A21P) otherwise
		/// </returns>
		public static List<CloudBlockBlob> FindMetaFridgeLogBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
		{
			List<CloudBlockBlob> metaFridgeLogBlobs = new List<CloudBlockBlob>();

			foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
			{
				metaFridgeLogBlobs.Add(logBlob);
				/* NHGH-1788 No longer a requirement to validate CFD50 log files using the file name
				if (logBlob.Name.ToUpper().Contains("CFD50"))
				{
					metaFridgeLogBlobs.Add(logBlob);
				}
				*/
			}

			if (metaFridgeLogBlobs.Count == 0)
			{
				string customError = EdiErrorsService.BuildExceptionMessageString(null, "A21P", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customError);
			}

			return metaFridgeLogBlobs;
		}

		/// <summary>
		/// Writes denormalized MetaFridge log file csv records to Azure blob storage
		/// </summary>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="requestBody">MetaFridge log transformation http reqest object</param>
		/// <param name="metaFridgeRecords">A list of denormalized Metafridge log records</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// Blob name of MetaFridge csv formatted log file; Exception (PH77) otherwise
		/// </returns>
		public static async Task<string> WriteMetaFridgeLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<Cfd50CsvRecordDto> metaFridgeRecords, ILogger log)
		{
			string blobName = "";

			try
			{
				blobName = CcdxService.BuildCuratedCfd50BlobName(requestBody.Path);
				log.LogInformation($"  - Blob: {blobName}");
				CloudBlockBlob outBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
				using var writer = await outBlob.OpenWriteAsync();
				using var streamWriter = new StreamWriter(writer);
				using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
				csvWriter.WriteRecords(metaFridgeRecords);
				return blobName;
			}
			catch (Exception e)
			{
				string customError = EdiErrorsService.BuildExceptionMessageString(e, "PH77", EdiErrorsService.BuildErrorVariableArrayList(blobName, cloudBlobContainer.Name));
				throw new Exception(customError);
			}

		}

		/// <summary>
		/// Downloads and deserializes a list of MetaFridge logs stored in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of MetaFridge cloud block blobs located in this virtual directory</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized MetaFridge log objects that have been downloaded from Azure blob storage; Exception (3L4P) otherwise
		/// </returns>
		public static async Task<List<dynamic>> DownloadsAndDeserializesMetaFridgeLogBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{
			List<dynamic> metaFridgeLogFiles = new List<dynamic>();
			foreach (CloudBlockBlob logBlob in blobs)
			{
				log.LogInformation($"  - Blob: {cloudBlobContainer.Name}/{logBlob.Name}");
				string blobText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, logBlob.Name);
				metaFridgeLogFiles.Add(DeserializeMetaFridgeLogText(logBlob.Name, blobText));
			}

			if (metaFridgeLogFiles.Count == 0)
			{
				string customError = EdiErrorsService.BuildExceptionMessageString(null, "3L4P", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customError);
			}

			return metaFridgeLogFiles;
		}

		/// <summary>
		/// Validates 
		/// </summary>
		/// <param name="emsLogs">A list of downloaded CFD50 log objects</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of validated CFD50 log objects; Exception thrown if at least one report fails validation (TV79) or if the json definition file failed to be retrieved (P2G3)
		/// </returns>
		public static async Task<List<dynamic>> ValidateCfd50LogBlobs(CloudBlobContainer cloudBlobContainer, List<dynamic> emsLogs, ILogger log)
		{
			List<dynamic> validatedEmsLogs = new List<dynamic>();

			string cfd50ConfigBlobName;
			string cfd50ConfigBlobJson;
			JsonSchema emsLogJsonSchema;

			try
			{
				cfd50ConfigBlobName = Environment.GetEnvironmentVariable("CFD50_LOG_JSON_SCHEMA_DEFINITION_FILE_NAME");
				cfd50ConfigBlobJson = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, cfd50ConfigBlobName);
				emsLogJsonSchema = await JsonSchema.FromJsonAsync(cfd50ConfigBlobJson);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "P2G3", null);
				throw new Exception(customErrorMessage);
			}

			foreach (dynamic emsLog in emsLogs)
			{
				string emsLogText = SerializeCfd50LogText(emsLog);

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
					string source = ObjectManager.GetJObjectPropertyValueAsString(emsLog, "EDI_SOURCE");
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "TV79", EdiErrorsService.BuildErrorVariableArrayList(source, validationResultString));
					throw new Exception(customErrorMessage);
				}
			}

			return validatedEmsLogs;
		}

		/// <summary>
		/// Serializes USBDG log
		/// </summary>
		/// <param name="emsLog">EMS log object </param>
		/// <returns>
		/// Serialized USBDG log text; Exception (EL33) otherwise
		/// </returns>
		private static string SerializeCfd50LogText(dynamic emsLog)
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
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "EL33", null);
				throw new Exception(customErrorMessage);
			}
		}
	}
}
