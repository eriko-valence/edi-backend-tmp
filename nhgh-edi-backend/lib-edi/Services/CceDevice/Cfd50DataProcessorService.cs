using CsvHelper;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Http;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Services.Azure;
using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
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
	public class Cfd50DataProcessorService
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
				return JsonConvert.DeserializeObject<dynamic>(blobText);
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
				if (logBlob.Name.Contains("CFD50"))
				{
					metaFridgeLogBlobs.Add(logBlob);
				}
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
		/// Blob name of MetaFridge csv formatted log file; Exception (Q25U) otherwise
		/// </returns>
		public static async Task<string> WriteMetaFridgeLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<Cfd50CsvRecordDto> metaFridgeRecords, ILogger log)
		{
			string blobName = "";

			try
			{
				blobName = $"{requestBody.Path}out_mf.csv";
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
				string customError = EdiErrorsService.BuildExceptionMessageString(e, "Q25U", EdiErrorsService.BuildErrorVariableArrayList(blobName, cloudBlobContainer.Name));
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
	}
}
