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
		/// Validates CCE device log JSON objects against schema
		/// </summary>
		/// <param name="emsLogs">A list validated JSON objects</param>
		/// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list validated CCE device log JSON objects; Exception thrown if at least one report fails validation (R85Y) or if the json definition file failed to be retrieved (FY84)
		/// </returns>
		public static async Task<List<dynamic>> ValidateLogJsonObjects(CloudBlobContainer cloudBlobContainer, List<dynamic> emsLogs, string jsonSchemaBlobName, ILogger log)
		{
			List<dynamic> validatedJsonObjects = new();

			string configBlobJsonText;
			JsonSchema configJsonSchema;

			try
			{
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

		/// <summary>
		/// Gets EMD source property from JSON object
		/// </summary>
		/// <param name="jo">JSON object</param>
		/// <returns>
		/// A string value of the EMD source property; "unknown" otherwise
		/// </returns>
		private static string GetSourceFile(JObject jo)
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
	}
}
