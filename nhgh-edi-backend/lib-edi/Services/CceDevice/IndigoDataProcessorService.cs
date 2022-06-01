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
    public class IndigoDataProcessorService : DataProcessorService
    {

		/// <summary>
		/// Returns a list of only Indigo V2 log blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <param name="blobPath">blob storage path</param>
		/// <returns>
		/// List containing only Indigo V2 log blobs; Exception (L91T)
		/// </returns>
		public static List<CloudBlockBlob> FindLogBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
		{
			List<CloudBlockBlob> listBlobs = new();

			if (logDirectoryBlobs != null)
			{
				foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
				{
					if (logBlob.Name.Contains("DATA") || logBlob.Name.Contains("CURRENT"))
					{
						listBlobs.Add(logBlob);
					}
				}
				if (listBlobs.Count == 0)
				{
					string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "L91T", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
					throw new Exception(customErrorMessage);
				}
			}
			return listBlobs;
		}


	}
}
