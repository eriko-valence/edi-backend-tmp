using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NJsonSchema;
using NJsonSchema.Validation;

namespace lib_edi.Services.CceDevice
{
    public class DataProcessorService
    {
		/// <summary>
		/// Downloads and deserializes a list of CCE device blobs residing in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of CCE device blobs</param>
		/// <param name="cloudBlobContainer">Microsoft Azure Blob service container holding the logs</param>
		/// <param name="blobPath">Path to log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized CCE device logsobjects that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
		/// </returns>
		public static async Task<List<dynamic>> DownloadAndDeserializeJsonBlobs(List<CloudBlockBlob> blobs, CloudBlobContainer cloudBlobContainer, string blobPath, ILogger log)
		{

			List<dynamic> listLogs = new();
			foreach (CloudBlockBlob logBlob in blobs)
			{
				string blobSource = $"{ cloudBlobContainer.Name}/{ logBlob.Name}";
				log.LogInformation($"  - Blob: {blobSource}");
				string logBlobText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, logBlob.Name);
				dynamic logBlobJson = DeserializeJsonText(logBlob.Name, logBlobText);
				logBlobJson._SOURCE = $"{blobSource}";
				listLogs.Add(logBlobJson);
			}

			if (listLogs.Count == 0)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "C26Z", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}

			return listLogs;
		}

		/// <summary>
		/// Downloads and deserializes a single CCE device blob residing in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of CCE device blobs</param>
		/// <param name="cloudBlobContainer">Microsoft Azure Blob service container holding the logs</param>
		/// <param name="blobPath">Path to log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized CCE device logsobjects that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
		/// </returns>
		public static async Task<dynamic> DownloadAndDeserializeJsonBlob(CloudBlockBlob blob, CloudBlobContainer blobContainer, string blobPath, ILogger log)
		{
			log.LogInformation($"  - Blob: {blobContainer.Name}/{blob.Name}");
			string logBlobText = await AzureStorageBlobService.DownloadBlobTextAsync(blobContainer, blob.Name);
			dynamic logBlobJson = DeserializeJsonText(blob.Name, logBlobText);

			if (logBlobJson == null)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "P76H", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}

			return logBlobJson;
		}

		/// <summary>
		/// Deserializes JSON string
		/// </summary>
		/// <param name="blobName">Blob name of JSON string </param>
		/// <param name="blobText">Downloaded text of JSON string </param>
		/// <returns>
		/// Deserialized object of JSON string; Exception (582N) otherwise
		/// </returns>
		public static JObject DeserializeJsonText(string blobName, string blobText)
		{
			try
			{
				dynamic results = JsonConvert.DeserializeObject<dynamic>(blobText);
				return results;
			}
			catch (Exception e)
			{
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "582N", EdiErrorsService.BuildErrorVariableArrayList(blobName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Serializes JSON object
		/// </summary>
		/// <param name="emsLog">Name of JSON object </param>
		/// <returns>
		/// Text of serialized JSOn object; Exception (48TV) otherwise
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
		/// Validates CCE device log JSON objects against schema
		/// </summary>
		/// <param name="emsLogs">A list validated JSON objects</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list validated CCE device log JSON objects; Exception thrown if at least one report fails validation (R85Y) or if the json definition file failed to be retrieved 
		/// </returns>
		public static async Task<List<dynamic>> ValidateLogJsonObjects(CloudBlobContainer cloudBlobContainer, List<dynamic> emsLogs, string jsonSchemaBlobName, ILogger log)
		{
			List<dynamic> validatedJsonObjects = new();

			//string configBlobName;
			string configBlobJsonText;
			JsonSchema configJsonSchema;

			try
			{
				//jsonSchemaBlobName = Environment.GetEnvironmentVariable("EMS_LOG_JSON_SCHEMA_DEFINITION_FILE_NAME");
				configBlobJsonText = await AzureStorageBlobService.DownloadBlobTextAsync(cloudBlobContainer, jsonSchemaBlobName);
				configJsonSchema = await JsonSchema.FromJsonAsync(configBlobJsonText);
			}
			catch (Exception e)
			{
				log.LogError($"    - Validated: No");
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "FY84", null);
				throw new Exception(customErrorMessage);
			}

			foreach (dynamic emsLog in emsLogs)
			{
				string emsLogText = SerializeJsonObject(emsLog);

				ICollection<ValidationError> errors = configJsonSchema.Validate(emsLogText);
				if (errors.Count == 0)
				{
					log.LogInformation($"    - Validated: Yes");
					validatedJsonObjects.Add(emsLog);
				}
				else
				{
					string validationResultString = EdiErrorsService.BuildJsonValidationErrorString(errors);
					log.LogError($"    - Validated: No - {validationResultString}");
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "R85Y", EdiErrorsService.BuildErrorVariableArrayList(emsLog._SOURCE, validationResultString));
					throw new Exception(customErrorMessage);
				}
			}

			return validatedJsonObjects;
		}
	}
}
