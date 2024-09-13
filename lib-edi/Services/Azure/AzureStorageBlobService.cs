using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using lib_edi.Models.Edi;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.CceDevice;
using lib_edi.Services.Errors;
using lib_edi.Services.System.IO;
//using Microsoft.Azure.Storage; // Microsoft.Azure.WebJobs.Extensions.Storage
//using Microsoft.Azure.Storage.Blob; // Microsoft.Azure.WebJobs.Extensions.Storage
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Azure
{
	/// <summary>
	/// A class that provides methods for interacting with the Azure Blob Storage service.
	/// </summary>
	/// <summary>
	/// A class that provides methods for interacting with the Azure Blob Storage service.
	/// </summary>
	public class AzureStorageBlobService
	{
		/// <summary>
		/// Returns a list of azure storage blobs 
		/// </summary>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="directoryPath">Azure blob storage virtual directory</param>
		/// <param name="fullBlobPath">Azure blob storage virtual directory</param>
		/// <returns>
		/// An enumerable collection of objects that implement IListBlobItem if successful; Exception (K3E5) otherwise
		/// </returns>
		/// <example>
		/// directoryPath = "usbdg/2021-11-10/22/e190f06b-8de8-494e-8fbc-20599f14a9b7/"
		/// fullBlobPath = ""
		/// </example>
		public static async Task<List<BlobItem>> GetListOfBlobsInDirectory(BlobContainerClient blobContainerClient, string directoryPath, string fullBlobPath)
		{
			List<BlobItem> listBlockBlobClient = new();
			try
			{
                //var logDirectory = BlobContainerClient.GetDirectoryReference(directoryPath);
                //IAsyncEnumerable<BlobHierarchyItem> results = BlobContainerClient.GetBlobsByHierarchyAsync(prefix:directoryPath);

                IAsyncEnumerable<BlobItem> results = blobContainerClient.GetBlobsAsync(prefix: directoryPath);

                //IEnumerable <IListBlobItem> listBlobs = logDirectory.;
				await foreach (BlobItem item in results)
				{
					listBlockBlobClient.Add(item);
				}
				return listBlockBlobClient;
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "K3E5", EdiErrorsService.BuildErrorVariableArrayList(blobContainerClient.Name, fullBlobPath));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Retrieves blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Azure blob storage virtual directory</param>
		/// <returns>
		/// An enumerable collection of objects that implement IListBlobItem if successful; Exception (K3E5) otherwise
		/// </returns>
		public static List<BlobItem> GetUsbdgBlobs(IEnumerable<BlobItem> logDirectoryBlobs)
		{
			List<BlobItem> listBlockBlobClient = new();
			foreach (BlobItem logBlob in logDirectoryBlobs)
			{
				if (logBlob.Name.Contains("DATA") || logBlob.Name.Contains("CURRENT"))
				{
					listBlockBlobClient.Add(logBlob);
				}
			}
			return listBlockBlobClient;
		}

		/// <summary>
		/// Initiates an asychronous operation to download the blob's contents as a string
		/// </summary>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="blobName">Azure blob name to download</param>
		/// <returns>
		/// Blob's contents as a string if successful; Exception (9L96) otherwise
		/// </returns>
		public static async Task<string> DownloadBlobTextAsync(BlobContainerClient blobContainerClient, string blobName)
		{
			try
			{
                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);
                BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
                string downloadedData = downloadResult.Content.ToString();
				return downloadedData;
            }
			catch (Exception e)
			{
				blobName ??= ""; // set blob name to an empty string value as error code 9L96 expects blob name to not be null
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "9L96", EdiErrorsService.BuildErrorVariableArrayList(blobName, blobContainerClient.Name));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Downloads blob content to a stream
		/// </summary>
		/// <param name="BlobContainerClient">A container in the Microsoft Azure Blob service</param>
		/// <param name="blobName">Azure blob name to download</param>
		/// <returns>
		/// Blob's contents as Stream object
		/// </returns>
		public static async Task<Stream> DownloadBlobContent(BlobContainerClient blobContainerClient, string blobName)
		{
			try
			{
				using Stream mem = new MemoryStream();
                //BlockBlobClient BlockBlobClient = BlobContainerClient.GetBlockBlobReference(blobName);

                BlobClient blobClient = blobContainerClient.GetBlobClient(blobName);

                //BlockBlobClient.DownloadToStream(mem);
                await blobClient.DownloadToAsync(mem);
                return mem;
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "21SQ", EdiErrorsService.BuildErrorVariableArrayList(blobName, blobContainerClient.Name));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Uploads blob stream to Azure Blob Storage using the Azure Blob Storage REST api
		/// </summary>
		/// <param name="s">A blob, as a Stream object, to be uploaded</param>
		/// <param name="name">Azure blob name to upload</param>
		/// <param name="containerName">Azure blob container name</param>
		/// <returns>
		/// bool value of 'true' if upload is successful; otherwise a bool value of 'false'
		/// </returns>
		/// <remarks>
		/// A SAS token is currently used here. It needs to be updated with a valid one before using this method.
		/// </remarks>
		public static async Task<bool> UploadBlobToContainerUsingRestApi(Stream s, string name, string containerName)
		{
			try
			{
				string SasToken = "?sv=2020-04-08&st=2021-07-13T16%3A52%3A34Z&se=2021-07-17T16%3A52%3A00Z&sr=c&sp=racwdxlt&sig=16EmXUO8MlmdaiBx5Sx9YplpzkvMGPgcoPhsYMPLILE%3D";
				bool isUploaded = false;
				string baseUri = "https://usbdatagrabberstorage.blob.core.windows.net";
				string URI = $"{baseUri}/{containerName}/{name}{SasToken}";
				byte[] requestPayload = await StreamService.ReadToEnd(s);
				HttpRequestMessage httpRequestMessage = new HttpRequestMessage(HttpMethod.Put, URI);
				httpRequestMessage.Content = (requestPayload == null) ? null : new ByteArrayContent(requestPayload);

				// Add the request headers for x-ms-date and x-ms-version.
				DateTime now = DateTime.UtcNow;
				httpRequestMessage.Headers.Add("x-ms-date", now.ToString("R", CultureInfo.InvariantCulture));
				httpRequestMessage.Headers.Add("x-ms-version", "2017-07-29");
				httpRequestMessage.Headers.Add("contentType", "application/zip");
				httpRequestMessage.Headers.Add("x-ms-blob-type", "BlockBlob");

				// Send the request.
				using (HttpResponseMessage httpResponseMessage =
				  await new HttpClient().SendAsync(httpRequestMessage))
				{
					// If successful (status code = 200),
					//   parse the XML response for the container names.
					if (httpResponseMessage.StatusCode == HttpStatusCode.Created)
					{
						isUploaded = true;
						String content = await httpResponseMessage.Content.ReadAsStringAsync();
					}
				}

				return isUploaded;
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "L72L", EdiErrorsService.BuildErrorVariableArrayList(name, containerName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Uploads blob byte array to Azure Blob Storage using the Azure Blob Storage SDK
		/// </summary>
		/// <param name="requestPayload">A byte array of the stream object</param>
		/// <param name="storageConnString">Azure blob storage connection string</param>
		/// <param name="containerName">Azure blob container name</param>
		/// <param name="fileName">File name to uploaded to Azure Blob Storage</param>
		/// <returns>
		/// bool value of 'true' if upload is successful; otherwise a bool value of 'false'
		/// </returns>
		public static async Task UploadBlobToContainerUsingSdk(byte[] requestPayload, string storageConnString, string containerName, string fileName)
		{
			try
			{
				BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnString);
				BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
				BlobClient blobClient = containerClient.GetBlobClient(fileName);
				using Stream stream = new MemoryStream(requestPayload);
				await blobClient.UploadAsync(stream, true);
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "R6C5", EdiErrorsService.BuildErrorVariableArrayList(fileName, containerName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Download blob content as string
		/// </summary>
		/// <param name="storageConnString">Azure blob storage connection string</param>
		/// <param name="containerName">Azure blob container name</param>
		/// <param name="fileName">File name to uploaded to Azure Blob Storage</param>
		/// <returns>
		/// Blob content as string
		/// </returns>
		public static async Task<string> DownloadBlobTextAsync(string storageConnString, string containerName, string fileName)
		{
			BlobServiceClient blobServiceClient = null;
			string downloadedData = "";
            try
			{
				if (storageConnString != null)
				{
					blobServiceClient = new BlobServiceClient(storageConnString);
					BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);
					BlobClient blobClient = containerClient.GetBlobClient(fileName);
					BlobDownloadResult downloadResult = await blobClient.DownloadContentAsync();
					downloadedData = downloadResult.Content.ToString();
					return downloadedData;
				}
				else
				{
					return downloadedData;
				}
            }
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "747H", EdiErrorsService.BuildErrorVariableArrayList(fileName, containerName, blobServiceClient.AccountName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Deletes blob
		/// </summary>
		/// <param name="storageConnString">Azure blob storage connection string</param>
		/// <param name="containerName">Azure blob container name</param>
		/// <param name="fileName">File name to uploaded to Azure Blob Storage</param>
		public static async Task DeleteBlob(string storageConnString, string containerName, string fileName)
		{
			try
			{
				BlobServiceClient blobServiceClient = new BlobServiceClient(storageConnString);
				BlobContainerClient blobContainerClient = blobServiceClient.GetBlobContainerClient(containerName);
				await blobContainerClient.CreateIfNotExistsAsync();
				Response response = await blobContainerClient.DeleteBlobAsync(fileName);
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "Q9W9", EdiErrorsService.BuildErrorVariableArrayList(fileName, containerName));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Deletes blob folder
		/// </summary>
		/// <param name="container">Azure blob container object</param>
		/// <param name="path">Azure blob folder path</param>
		public static async Task DeleteFolder(BlobContainerClient container, string path)
		{
			try
			{
                IAsyncEnumerable<BlobItem> results = container.GetBlobsAsync(prefix: path);
                await foreach (BlobItem blobItem in results)
				{
                    var blob = container.GetBlobClient(blobItem.Name);
                    await blob.DeleteIfExistsAsync();
				}
			} catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "RH84", EdiErrorsService.BuildErrorVariableArrayList(path, container.Name));
				throw new Exception(customErrorMessage);
			}
		}

		/// <summary>
		/// Downloads and deserializes a list of CCE device blobs residing in Azure blob storage
		/// </summary>
		/// <param name="blobs">A list of CCE device blobs</param>
		/// <param name="BlobContainerClient">Microsoft Azure Blob service container holding the logs</param>
		/// <param name="blobPath">Path to log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized CCE device logs in JObject format that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
		/// </returns>
		public static async Task<List<dynamic>> DownloadAndDeserializeJsonBlobs(List<BlobItem> blobs, BlobContainerClient BlobContainerClient, string blobPath, ILogger log, string reportPackageName, EmdEnum.Name emdNum, DataLoggerTypeEnum.Name dataLoggerType)
		{

			List<dynamic> listLogs = new();
			foreach (BlobItem logBlob in blobs)
			{
				string blobSource = $"{ BlobContainerClient.Name}/{ logBlob.Name}";
				string logBlobText = await AzureStorageBlobService.DownloadBlobTextAsync(BlobContainerClient, logBlob.Name);

				// NHGH-3078 20230824 1441 Add custom error to capture blobs with empty strings 
				if (logBlobText == "")
				{
					log.LogError($"- {reportPackageName} - json data file is empty");
					log.LogError($"- {reportPackageName}   - data file : " + blobSource);
					log.LogError($"- {reportPackageName}   - action    : skip this empty json data file and continue processing other files in this report package ");
					string errorCode = "ATDM";
					string errorMessage = await EdiErrorsService.BuildExceptionMessageString(null, errorCode, EdiErrorsService.BuildErrorVariableArrayList(logBlob.Name));
					DataTransformService.LogEmsTransformWarningEventToAppInsights(reportPackageName, emdNum, PipelineStageEnum.Name.ADF_TRANSFORM, log, null, errorCode, errorMessage, dataLoggerType, PipelineFailureReasonEnum.Name.WARN_INVALID_JSON);
				} else
				{
					try
					{
						dynamic logBlobJson = await DeserializeJsonText(logBlob.Name, logBlobText);
						logBlobJson.EDI_SOURCE = $"{blobSource}";
						listLogs.Add(logBlobJson);
					}
					catch (Exception e)
					{
						log.LogError($"- {reportPackageName} - error deserializing data file");
						log.LogError($"- {reportPackageName}   - data file : " + blobSource);
						log.LogError($"- {reportPackageName}   - action    : skip this invalid json data file and continue processing other files in this report package ");
						string errorCode = "QTAF";
						string errorMessage = await EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(logBlob.Name));
						DataTransformService.LogEmsTransformWarningEventToAppInsights(reportPackageName, emdNum, PipelineStageEnum.Name.ADF_TRANSFORM, log, null, errorCode, errorMessage, dataLoggerType, PipelineFailureReasonEnum.Name.WARN_INVALID_JSON);
					}
				}
			}

			if (listLogs.Count == 0)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "C26Z", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
				throw new Exception(customErrorMessage);
			}
			return listLogs;
		}

		/// <summary>
		/// Downloads and deserializes a single CCE device blob residing in Azure blob storage
		/// </summary>
		/// <param name="blob">A list of CCE device blobs</param>
		/// <param name="blobContainer">Microsoft Azure Blob service container holding the logs</param>
		/// <param name="blobPath">Path to log blobs</param>
		/// <param name="log">Azure function logger object</param>
		/// <returns>
		/// A list of deserialized CCE device logsobjects that have been downloaded from Azure blob storage; Exception (C26Z) if no blobs found
		/// </returns>
		public static async Task<dynamic> DownloadAndDeserializeJsonBlob(BlobItem blob, BlobContainerClient blobContainer, string blobPath, ILogger log)
		{
			string emsBlobPath = $"{ blobContainer.Name}/{ blob.Name}";
			string logBlobText = await AzureStorageBlobService.DownloadBlobTextAsync(blobContainer, blob.Name);
			dynamic logBlobJson = await DeserializeJsonText(blob.Name, logBlobText);
			logBlobJson.EDI_SOURCE = emsBlobPath;

			if (logBlobJson == null)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "P76H", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
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
		private static async Task<JObject> DeserializeJsonText(string blobName, string blobText)
		{
			try
			{
				dynamic results = JsonConvert.DeserializeObject<dynamic>(blobText);
				return results;
			}
			catch (Exception e)
			{
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "582N", EdiErrorsService.BuildErrorVariableArrayList(blobName));
				throw new Exception(customErrorMessage);
			}
		}
	}
}
