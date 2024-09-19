using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
//using lib_edi.Models.Edi;
//using lib_edi.Models.Enums.Azure.AppInsights;
//using lib_edi.Models.Enums.Emd;
//using lib_edi.Services.CceDevice;
//using lib_edi.Services.Errors;
//using lib_edi.Services.System.IO;
using lib_edi_in_process.Services.Errors;
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

namespace lib_edi_in_process.Services.Azure
{
    public class AzureStorageBlobService
    {
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
    }
}
