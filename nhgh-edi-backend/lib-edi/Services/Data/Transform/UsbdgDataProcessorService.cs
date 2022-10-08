using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using lib_edi.Services.CceDevice;
using System.IO;
using lib_edi.Services.Ems;

namespace lib_edi.Services.Loggers
{
	/// <summary>
	/// A class that provides methods processing USBDG log files
	/// </summary>
	public class UsbdgDataProcessorService : DataTransformService
	{

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
					// NHGH-2362 (2022.06.16) - only add USBDG report metadata files
					if (IsFileUsbdgReportMetadata(logBlob.Name))
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
		/// Determines if a file package has only USBDG report metadata (i.e., "no logger" scenario)
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <returns>
		/// True if file package has USBDG report metdata exists and no logger data; False otherwise 
		/// </returns>
		public static bool IsFilePackageUsbdgOnly(IEnumerable<IListBlobItem> logDirectoryBlobs)
		{
			bool result = false;
			bool emsCompliantLogFilesFound = false;
			bool usbdgMetaDataFound = false;
			if (logDirectoryBlobs != null)
			{
				foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
				{
					string fileExtension = Path.GetExtension(logBlob.Name);
					if (EmsService.IsFileFromEmsLogger(logBlob.Name))
					{
                        emsCompliantLogFilesFound = true;
					}

					if (IsFileUsbdgReportMetadata(logBlob.Name))
					{
						usbdgMetaDataFound = true;
					}
				}
			}

			if ((emsCompliantLogFilesFound == false) && usbdgMetaDataFound)
			{
				result = true;
			}

			return result;
		}

		/// <summary>
		/// Checks if blob name a USBDB report metadata file
		/// </summary>
		/// <param name="blobName">blob name </param>
		/// <returns>
		/// True if yes; False if no
		/// </returns>
		public static bool IsFileUsbdgReportMetadata(string blobName)
		{
			bool result = false;
			if ((Path.GetExtension(blobName) == ".json") && (blobName.Contains("report")))
			{
				result = true;
			}
			return result;
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
