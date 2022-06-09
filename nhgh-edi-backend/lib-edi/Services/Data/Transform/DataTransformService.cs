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
using System.Reflection;
using lib_edi.Models.Dto.Http;
using Microsoft.AspNetCore.Http;
using System.IO;
using lib_edi.Helpers;
using lib_edi.Models.Edi;
using System.Dynamic;

namespace lib_edi.Services.CceDevice
{
    public class DataTransformService
    {
		/// <summary>
		/// Downloads and deserializes a list of CCE device blobs residing in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of CCE device blobs</param>
		/// <param name="cloudBlobContainer">Microsoft Azure Blob service container holding the logs</param>
		/// <param name="blobPath">Path to log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized CCE device logs in JObject format that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
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
			//return (List<dynamic>)listLogs.Cast<JObject>();
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
			string emsBlobPath = $"{ blobContainer.Name}/{ blob.Name}";
			log.LogInformation($"  - Blob: {emsBlobPath}");
			string logBlobText = await AzureStorageBlobService.DownloadBlobTextAsync(blobContainer, blob.Name);
			dynamic logBlobJson = DeserializeJsonText(blob.Name, logBlobText);
			logBlobJson._SOURCE = emsBlobPath;

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


		public static string GetSourceFile(JObject jo)
		{
			string sourceFile = null;
			if (jo != null)
			{
				sourceFile = ObjectManager.GetJObjectPropertyValueAsString(jo, "_SOURCE");
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
							ObjectManager.SetObjectValue(ediJob.Logger, log1.Key, log1.Value);
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

							//sinkCsvEventRecords.Add((IndigoV2EventRecord)sinkCsvEventRecord);
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

	}
}
