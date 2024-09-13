using lib_edi.Services.Errors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using lib_edi.Services.CceDevice;
using System.IO;
using lib_edi.Services.Ems;
using lib_edi.Models.Edi;
using System.Dynamic;
using System.Text.RegularExpressions;
using lib_edi.Helpers;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Models.Enums.Emd;
using System.Threading.Tasks;
using Azure.Storage.Blobs.Models;

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
		public static async Task<BlobItem> GetReportMetadataBlob(IEnumerable<BlobItem> logDirectoryBlobs, string blobPath)
		{
			List<BlobItem> usbdgLogReportBlobs = new();

			if (logDirectoryBlobs != null)
			{
				foreach (BlobItem logBlob in logDirectoryBlobs)
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
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(null, "RV62", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
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
		public static bool IsFilePackageUsbdgOnly(IEnumerable<BlobItem> logDirectoryBlobs)
		{
			bool result = false;
			bool emsCompliantLogFilesFound = false;
			bool usbdgMetaDataFound = false;
			if (logDirectoryBlobs != null)
			{
				foreach (BlobItem logBlob in logDirectoryBlobs)
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

        // 40A36BCA695F_20230313T141954Z_NoLogger_reports.tar.gz
        // 40A36BCA7463_20230323T160650Z_002200265547501820383131_reports.tar
        public static bool IsThisUsbdgGeneratedPackageName(string name)
        {
            bool result = false;
            string usbdgReportFileNamePattern = "([A-Z0-9]+)_(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)_([A-Za-z0-9]+)_reports\\.tar\\.gz";
            Regex r = new Regex(usbdgReportFileNamePattern);
            Match m = r.Match(name);
            if (m.Success)
            {
                result = true;
            }
            return result;
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
        public static async Task<EdiJob> PopulateEdiJobObject(dynamic sourceUsbdgMetadata, List<dynamic> sourceLogs, List<BlobItem> listLoggerFiles, BlobItem usbdgReportMetadataBlob, string packageName, string stagePath, EmdEnum.Name emdTypeEnum, DataLoggerTypeEnum.Name dataLoggerType)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;
            EdiJob ediJob = new();

            try
            {
                ediJob.Emd.Type = emdTypeEnum;
                // NHGH-2819 2023.03.15 1643 track report package file name (for debug purposes)
                ediJob.Emd.PackageFiles.ReportPackageFileName = packageName;
                ediJob.Emd.PackageFiles.StagedBlobPath = stagePath;

                if (sourceLogs != null)
                {
                    foreach (dynamic sourceLog in sourceLogs)
                    {
                        JObject sourceLogJObject = (JObject)sourceLog;

                        // NHGH-2819 2023.03.15 1638 track sync file name (for debug purposes)
                        var fileName = sourceLogJObject.SelectToken("EDI_SOURCE");
                        ediJob.Emd.PackageFiles.StagedFiles.Add(GetFileNameFromPath(fileName.ToString()));
                        if (EmsService.IsThisEmsSyncDataFile(fileName.ToString()))
                        {
                            ediJob.Emd.PackageFiles.SyncFileName = GetSyncFileNameFromBlobPath(fileName.ToString());
                        }

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
                }

                JObject sourceUsbdgMetadataJObject = (JObject)sourceUsbdgMetadata;
                var reportHeaderObject = new ExpandoObject() as IDictionary<string, Object>;

                foreach (KeyValuePair<string, JToken> log2 in sourceUsbdgMetadataJObject)
                {
                    if (log2.Value.Type != JTokenType.Array)
                    {
                        reportHeaderObject.Add(log2.Key, log2.Value);
                        ObjectManager.SetObjectValue(ediJob.Emd.Metadata.Usbdg, log2.Key, log2.Value);
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
                                ObjectManager.SetObjectValue(ediJob.Emd.Metadata.Usbdg.MountTime, prop.Name, prop.Value);
                            }
                        }
                    }
                }

                ediJob.Logger.Type = GetLoggerTypeFromEmsPackage(ediJob, dataLoggerType);

                // NHGH-2819 2023.03.15 1644 track report package file name (for debug purposes)
                ediJob.Emd.PackageFiles.ReportMetadataFileName = GetReportMetadataFileNameFromBlobPath(usbdgReportMetadataBlob.Name);
                if (ediJob.Emd.PackageFiles.ReportMetadataFileName != null) {
                    ediJob.Emd.PackageFiles.StagedFiles.Add(ediJob.Emd.PackageFiles.ReportMetadataFileName);
                }
                // NHGH-2819 2023.03.15 1335 Only grab the mount time if there are logger data files
                if (listLoggerFiles != null)
                {
                    ediJob.Emd.Metadata.Usbdg.MountTime = await GetUsbdgMountTime(listLoggerFiles);
                }
                
                ediJob.Emd.Metadata.Usbdg.CreationTime = await GetUsbdgReportCreationTime(usbdgReportMetadataBlob);

                return ediJob;
            }
            catch (Exception e)
            {
                throw new Exception(await EdiErrorsService.BuildExceptionMessageString(e, "D39Y", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }



        /// <summary>
        /// Returns absolute time from EMS report metadata. Falls back to the logger file name if missing from the metadata. 
        /// </summary>
        /// <param name="ediJob">EDI job object</param>
        /// <returns>
        /// Absolute time
        /// </returns>
        
        public static async Task<EdiJobUsbdgMetadataMountTime> GetUsbdgMountTimeFromReportMetadata(EdiJobUsbdgMetadata reportMetadata)
        {
            if (reportMetadata.MountTime.ABST != null && reportMetadata.MountTime.ABST != "")
            {
                if (reportMetadata.MountTime.RELT != null && reportMetadata.MountTime.RELT != "")
                {
                    reportMetadata.MountTime.Calcs.ABST_UTC = await DateConverter.ConvertIso8601CompliantString(reportMetadata.MountTime.ABST);
                    reportMetadata.MountTime.Calcs.RELT_ELAPSED_SECS = await DataTransformService.ConvertRelativeTimeStringToTotalSeconds(reportMetadata.MountTime.RELT);
                    reportMetadata.MountTime.SOURCE = EmdTimeSource.Name.EMD_REPORT_METADATA;
                } else
                {
                    // NHGH-2819 2023.13.15 1502 Mount times do not exist in report metadata 
                    reportMetadata.MountTime.SOURCE = EmdTimeSource.Name.NONE;
                }
            } else
            {
                // NHGH-2819 2023.13.15 1502 Mount times do not exist in report metadata 
                reportMetadata.MountTime.SOURCE = EmdTimeSource.Name.NONE;
            }
            return reportMetadata.MountTime;
        }

        /*
         * HISTORY
         *   - NHGH-3192 2023.11.06 1337 primary source of usbdg mount times should be SYNC file name, not report metadata
         *   - NHGH-2819 2023.03.15 1502 fall back to SYNC file name if mount times do not exist in report metadata 
         */
        public static async Task<EdiJobUsbdgMetadataMountTime> GetUsbdgMountTime(List<BlobItem> listLoggerFiles)
        {
            EdiJobUsbdgMetadataMountTime timeInfo = await GetUsbdgMountTimeFromSyncFileName(listLoggerFiles);
            return timeInfo;
        }

        public static async Task<EdiJobUsbdgMetadataCreationTime> GetUsbdgReportCreationTime(BlobItem usbdgReportMetadataBlob)
        {
            EdiJobUsbdgMetadataCreationTime timeInfo = await GetUsbdgReportCreationTimeFromMetadataFileName(usbdgReportMetadataBlob.Name);
            return timeInfo;
        }

        public static async Task<EdiJobUsbdgMetadataCreationTime> GetUsbdgReportCreationTimeFromMetadataFileName(string name)
        {
            EdiJobUsbdgMetadataCreationTime timeInfo = null;

            if (name != null)
            {
                string[] parts = name.Split("/"); ;
                string fileName = parts[parts.Length - 1];

                Match m = UsbdgDataProcessorService.IsUsbdgReportMetadataFile(fileName);
                if (m.Success)
                {
                    timeInfo ??= new EdiJobUsbdgMetadataCreationTime();
                    timeInfo.ABST = m.Groups[2].Value;
                    timeInfo.ABST_UTC = await DateConverter.ConvertIso8601CompliantString(m.Groups[2].Value); ;
                    timeInfo.RELT = m.Groups[1].Value;
                    timeInfo.SOURCE = EmdTimeSource.Name.EMD_REPORT_METADATA_FILENAME;
                }
            }
            return timeInfo;
        }




        /// <summary>
        /// Returns absolute time from EMS report metadata. Falls back to the logger file name if missing from the metadata. 
        /// </summary>
        /// <param name="ediJob">EDI job object</param>
        /// <returns>
        /// Absolute time
        /// </returns>
        /*
        public static string GetRelativeTimeFromEmsPackage(EdiJob ediJob)
        {
            string result = null;
            if (ediJob.UsbdgMetadata != null)
            {
                if (ediJob.UsbdgMetadata.RELT != null && ediJob.UsbdgMetadata.RELT != "")
                {
                    result = ediJob.UsbdgMetadata.RELT;
                }
                else if (ediJob.FileName_RELT != null)
                {
                    result = ediJob.FileName_RELT;
                }
            }
            return result;
        }
        */

        /// <summary>
        /// Calculates the absolute timestamp for each Indigo V2 records using the USBDG metadata absolute timestamp and relative time of records
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <param name="reportDurationSeconds">USBDG metadata duration seconds (converted from relative seconds)</param>
        /// <param name="reportMetadata">USBDG metadata file json object</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a Indigo V2 record; Exception (4Q5D) otherwise
        /// </returns>
        //public static List<EmsEventRecord> CalculateAbsoluteTimeForUsbdgRecords(List<EmsEventRecord> records, int reportDurationSeconds, dynamic reportMetadata, EdiJob ediJob)
        public static async Task<List<EmsEventRecord>> CalculateAbsoluteTimeForUsbdgRecords(List<EmsEventRecord> records, EdiJob ediJob)
        {
            //string absoluteTime = GetKeyValueFromMetadataRecordsObject("ABST", reportMetadata);
            string emdTimeAbst = GetUsbdgMountTimeAbst(ediJob);
            int emdTimeElapsedSecs = ediJob.Emd.Metadata.Usbdg.MountTime.Calcs.RELT_ELAPSED_SECS;

            foreach (EmsEventRecord record in records)
            {
                DateTime? dt = await DataTransformService.CalculateAbsoluteTimeForEmsRecord(emdTimeAbst, emdTimeElapsedSecs, record.RELT, record.EDI_SOURCE);
                record.EDI_ABST = dt;
            }
            return records;
        }

        public static Match IsUsbdgReportMetadataFile(string name)
        {
            // 40A36BCA69DD_20221221T224627Z_report.json
            // 40A36BCA692C_20230302T000314Z_report.json
            string varoReportFileNamePattern = "([A-Z0-9]+)_(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)_report\\.json";
            Regex r = new Regex(varoReportFileNamePattern);
            Match m = r.Match(name);
            return m;
        }

        public static async Task<EdiJobUsbdgMetadataMountTime> GetUsbdgMountTimeFromSyncFileName(List<BlobItem> listLoggerFiles)
        {
            EdiJobUsbdgMetadataMountTime timeInfo = null;

            if (listLoggerFiles != null)
            {
                foreach (BlobItem logBlob in listLoggerFiles)
                {
                    if (EmsService.IsThisEmsSyncDataFile(logBlob.Name))
                    {
                        string[] parts = logBlob.Name.Split("/"); ;
                        string logFileName = parts[parts.Length - 1];
                        Match m1 = EmsService.IsThisEmsSyncFile(logFileName);
                        if (m1.Success)
                        {
                            timeInfo ??= new EdiJobUsbdgMetadataMountTime();
                            timeInfo.ABST = m1.Groups[3].Value;
                            timeInfo.RELT = m1.Groups[2].Value;
                            timeInfo.Calcs.ABST_UTC = await DateConverter.ConvertIso8601CompliantString(m1.Groups[3].Value);
                            timeInfo.Calcs.RELT_ELAPSED_SECS = await DataTransformService.ConvertRelativeTimeStringToTotalSeconds(m1.Groups[2].Value);
                            timeInfo.SOURCE = EmdTimeSource.Name.EMS_SYNC_FILENAME;
                        }
                    }
                }
            }
            return timeInfo;
        }



        /// <summary>
        /// Returns absolute time from EMS report metadata. Falls back to the logger file name if missing from the metadata. 
        /// </summary>
        /// <param name="ediJob">EDI job object</param>
        /// <returns>
        /// Absolute time
        /// </returns>
        public static string GetUsbdgMountTimeAbst(EdiJob ediJob)
        {
            string result = null;
            if (ediJob != null)
            {
                if (ediJob.Emd.Metadata.Usbdg.MountTime.ABST != null)
                {
                    if (ediJob.Emd.Metadata.Usbdg.MountTime.ABST != null)
                    {
                        result = ediJob.Emd.Metadata.Usbdg.MountTime.ABST;
                    }
                }
            }
            return result;
        }

        public static string GetReportMetadataFileNameFromBlobPath(string blobPath)
        {
            string reportFileName = null;
            if (blobPath != null)
            {
                string[] parts = blobPath.Split("/"); ;
                string fileName = parts[parts.Length - 1];
                Match m = UsbdgDataProcessorService.IsUsbdgReportMetadataFile(fileName);
                if (m.Success)
                {
                    reportFileName = fileName;
                }
            }
            return reportFileName;
        }
    }
}
