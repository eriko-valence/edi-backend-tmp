using lib_edi.Helpers;
using lib_edi.Models.Edi;
using lib_edi.Models.Enums.Emd;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Services.CceDevice;
using lib_edi.Services.Ems;
using lib_edi.Services.Errors;
using lib_edi.Services.Loggers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using static Microsoft.ApplicationInsights.MetricDimensionNames.TelemetryContext;

namespace lib_edi.Services.Data.Transform
{
    /// <summary>
    /// NHGH-2819 03.0223 A class that provides methods processing Varo collected EMS report packages
    /// </summary>
    public class VaroDataProcessorService : DataTransformService
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
            List<CloudBlockBlob> reportBlobs = new();

            if (logDirectoryBlobs != null)
            {
                foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
                {
                    Match m = IsThisVaroReportMetadataFile(logBlob.Name);
                    if (m.Success)
                    {
                        reportBlobs.Add(logBlob);
                    }
                }
            }

            if (reportBlobs.Count == 0)
            {
                string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(null, "SVQJ", EdiErrorsService.BuildErrorVariableArrayList(blobPath));
                throw new Exception(customErrorMessage);
            }

            return reportBlobs.First();
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

        /// <summary>
        /// Checks if string is Varo EMD
        /// </summary>
        /// <param name="name">String to check</param>
        /// <remarks>
        /// NHGH-2835 (2023.03.02) - Added function
        /// </remarks>
        public static bool IsVaroEmd(string name)
        {
            bool result = false;

            if (name != null)
            {
                if (name.ToUpper() == EmdEnum.Name.VARO.ToString())
                {
                    result = true;
                }
            }
            return result;
        }

        public static bool IsReportPackageSupported(string incomingCcdxHeaderCeType, string incomingCcdxHeaderDxEmail)
        {
            bool dxEmailMatch = false;
            bool ceTypeMatch = false;

            string supportedCcdxHeaderDxEmail = Environment.GetEnvironmentVariable("SUPPORTED_CCDX_DX_EMAIL");
            string supportedCcdxHeaderCeType = Environment.GetEnvironmentVariable("SUPPORTED_CCDX_CE_TYPE");

            if (supportedCcdxHeaderDxEmail != null && incomingCcdxHeaderDxEmail != null)
            {
                if (supportedCcdxHeaderDxEmail.ToUpper() == incomingCcdxHeaderDxEmail.ToUpper())
                {
                    dxEmailMatch = true;
                }
            }

            if (supportedCcdxHeaderCeType != null && incomingCcdxHeaderCeType != null)
            {
                if (supportedCcdxHeaderCeType.ToUpper() == incomingCcdxHeaderCeType.ToUpper())
                {
                    ceTypeMatch = true;
                }
            }

            if (dxEmailMatch && ceTypeMatch)
            {
                return true;
            } else
            {
                return false;
            }
        }

        /// <summary>
        /// Checks if string is USBDG EMD
        /// </summary>
        /// <param name="name">String to check</param>
        /// <remarks>
        /// NHGH-2819 (2023.03.09) - Added function
        /// </remarks>
        public static bool IsUsbdgEmd(string name)
        {
            bool result = false;

            if (name != null)
            {
                if (name.ToUpper() == EmdEnum.Name.USBDG.ToString())
                {
                    result = true;
                }
            }
            return result;
        }

        // 
        /// <summary>
        /// Generates an EMS package name from the Varo report metadata file name
        /// </summary>
        /// <param name="attachments">list of Varo email report attachments</param>
        /// <remarks>
        /// nhgh-2815 2023-03-01 1203 Added function
        /// </remarks>
        /// <returns>
        /// Package name in format {TIMESTAMP}_{LOGGERID}_reports.tar.gz
        /// </returns>
        public static string GeneratePackageNameFromVaroReportFileName(dynamic attachments)
        {
            string name = null;

            if (attachments != null)
            {
                int i = 0;
                foreach (dynamic item in attachments)
                {
                    string elementName = item?.Name;
                    //Regex r = new Regex(varoReportFileNamePattern);
                    //Match m = r.Match(elementName);
                    Match m = IsThisVaroReportMetadataFile(elementName);
                    if (m.Success)
                    {
                        Group loggerId = m.Groups[1];
                        Group timeStamp = m.Groups[2];
                        name = $"{timeStamp.Value}_{loggerId.Value}_reports.tar.gz";
                    }
                    i++;
                }
            }
            return name;
        }

        /// <summary>
        /// Looks for a Varo report file name regular expression match
        /// </summary>
        /// <param name="name">file name</param>
        /// <remarks>
        /// nhgh-2819 2023-03-08 1511 Added function
        /// </remarks>
        /// <returns>
        /// Regular expresssion match result
        /// </returns>
        public static Match IsThisVaroReportMetadataFile(string name)
        {
            string varoReportFileNamePattern = "([a-z0-9]+)_(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)\\.json";
            Regex r = new Regex(varoReportFileNamePattern);
            Match m = r.Match(name);
            return m;
        }
        // 20230227T212312Z_03b00274630501120363837_reports.tar.gz
        public static bool IsThisVaroGeneratedPackageName(string name)
        {
            bool result = false;
            string varoReportFileNamePattern = "(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)_([a-z0-9]+)_reports\\.tar\\.gz";
            Regex r = new Regex(varoReportFileNamePattern);
            Match m = r.Match(name);
            if (m.Success)
            {
                result = true;
            }
            return result;
        }



        /// <summary>
        /// Inspects contents of report package to see if it is Varo generated 
        /// </summary>
        /// <param name="list">list of report package blobs</param>
        /// <remarks>
        /// nhgh-2819 2023-03-08 1511 Added function
        /// </remarks>
        /// <returns>
        /// True if Varo; False otherwise
        /// </returns>
        public static bool IsThisVaroCollectedEmsReportPackage(IEnumerable<IListBlobItem> list, string loggerType)
        {
            bool result = false;
            bool sFile = false;
            bool cFile = false;
            bool rFile = false;
            bool isVaro = false;

            foreach (CloudBlockBlob blob in list)
            {
                if (VaroDataProcessorService.IsVaroEmd(loggerType))
                {
                    isVaro = true;
                }
                if (EmsService.IsThisEmsCurrentDataFile(blob.Name))
                {
                    cFile = true;
                }
                if (EmsService.IsThisEmsSyncDataFile(blob.Name))
                {
                    sFile = true;
                }
                Match m = IsThisVaroReportMetadataFile(blob.Name);
                if (m.Success)
                {
                    rFile = true;
                }
            }

            if (sFile && cFile && rFile && isVaro)
            {
                result = true;
            }

            return result;
        }

        /// <summary>
        /// Populates an EDI job object from logger data and Varo report metadata files
        /// </summary>
        /// <remarks>
        /// This EDI object holds properties useful further downstream in the processing
        /// </remarks>
        /// <param name="varoReportFileObject">A deserialized Varo report metadata file</param>
        /// <param name="sourceLogs">A list of deserialized logger data files</param>
        /// <returns>
        /// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
        /// </returns>
        public static EdiJob PopulateEdiJobObject(dynamic varoReportFileObject, List<dynamic> sourceLogs, List<CloudBlockBlob> listLoggerFiles, CloudBlockBlob varoReportMetadataBlob, CloudBlockBlob usbdgReportMetadataBlob, string packageName, string stagePath, EmdEnum.Name emdTypeEnum, DataLoggerTypeEnum.Name dataLoggerType)
        {
            string sourceFile = null;
            EdiJob ediJob = new();
            // NHGH-2819 2023.03.16 1020 track report package file name (for debug purposes)
            ediJob.ReportPackageFileName = packageName;
            ediJob.StagedBlobPath = stagePath;

            try
            {
                ediJob.Emd.Type = emdTypeEnum;
                bool aserFound = false;
                foreach (var sourceLog in sourceLogs)
                {
                    if (!aserFound)
                    {
						JObject sourceLogJObject = (JObject)sourceLog;
						JToken AserToken = sourceLogJObject.SelectToken("ASER");
						if (AserToken != null)
						{
							ediJob.Emd.ASER = AserToken.ToString();
							aserFound = true;
						}
					}
				}

				if (sourceLogs != null)
                {
                    foreach (dynamic sourceLog in sourceLogs)
                    {
                        JObject sourceLogJObject = (JObject)sourceLog;

                        // NHGH-2819 2023.03.16 1020 track sync file name (for debug purposes)
                        var fileName = sourceLogJObject.SelectToken("EDI_SOURCE");
                        ediJob.StagedFiles.Add(GetFileNameFromPath(fileName.ToString()));
                        if (EmsService.IsThisEmsSyncDataFile(fileName.ToString()))
                        {
                            ediJob.SyncFileName = GetSyncFileNameFromBlobPath(fileName.ToString());
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

				/*
                 * NHGH-922 Pull gps coordinates from "Used" location object if report format version is 2 or higher.
                 *	 * If location[”gpsCorrectionUsed”] does not exist, assume the report is a “version 1” report and 
                 *	 just process GPS in the same way it is currently processed	  
                 *	 * If location[“gpsCorrectionUsed”] exists (regardless of whether it is true or false):
                 *	   - If used is not present, just use the GPS values in the files “as is”.
                 *	   - If used is present, always use the used values for GPS records.
                 */
				JObject varoReportFileJObject = (JObject)varoReportFileObject;
				JToken gpsCorrectedionUsedObject = varoReportFileJObject.SelectToken("$.location.gpsCorrectionUsed");
				JToken locationObject = varoReportFileJObject.SelectToken("$.location");
				if (gpsCorrectedionUsedObject != null)
                {
					JToken locationUsedObject = varoReportFileJObject.SelectToken("$.location.used");
					if (locationUsedObject != null)
                    {
						ediJob.Emd.Metadata.Varo.Location = locationUsedObject.ToObject<EdiJobVaroMetadataLocation>();
					} else if (locationObject != null)
                    {
						ediJob.Emd.Metadata.Varo.Location = locationObject.ToObject<EdiJobVaroMetadataLocation>();
					}
                } else
                {
					ediJob.Emd.Metadata.Varo.Location = locationObject.ToObject<EdiJobVaroMetadataLocation>();
				}

				JToken timestampObject = varoReportFileJObject.SelectToken("$.timestamp");
                if (timestampObject != null)
                {
					ediJob.Emd.Metadata.Varo.ReportTime = DateConverter.FromUnixTimeMilliseconds(timestampObject.ToObject<long>());
				}

				// ediJob.Emd.Metadata.Varo.ReportTime = DateConverter.FromUnixTimeMilliseconds(123421432);

				var varoReportFileHeaderObject = new ExpandoObject() as IDictionary<string, Object>;
				foreach (KeyValuePair<string, JToken> log2 in varoReportFileJObject)
                {
                    if (log2.Value.Type != JTokenType.Array)
                    {
                        varoReportFileHeaderObject.Add(log2.Key, log2.Value);
                        ObjectManager.SetObjectValue(ediJob.Emd.Metadata.Varo, log2.Key, log2.Value);
                    }
                }

                // NHGH-2819 2023.03.15 1644 track report package file name (for debug purposes)
                ediJob.ReportMetadataFileName = GetReportMetadataFileNameFromBlobPath(usbdgReportMetadataBlob.Name);
                if (ediJob.ReportMetadataFileName != null)
                {
                    ediJob.StagedFiles.Add(ediJob.ReportMetadataFileName);
                }
                ediJob.Logger.Type = GetLoggerTypeFromEmsPackage(ediJob, dataLoggerType);
                ediJob.Emd.Metadata.Varo.MountTime = GetTimeFromEmsSyncFileName(listLoggerFiles);
				ediJob.Emd.Metadata.Varo.CreationTime = GetVaroReportCreationTimeFromMetadataFileName(varoReportMetadataBlob.Name);
				return ediJob;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "TTCW", EdiErrorsService.BuildErrorVariableArrayList(packageName)));
            }
        }

        public static void LogEmsPackageInformation_OLD(ILogger log, List<EmsEventRecord> records, EdiJob ediJob)
        {
            int first = (records.Count - 1);
            int last = (0);
            log.LogInformation($"  - EMD report creation time ");
            log.LogInformation($"    - Absolute UTC timestamp (ISO 8601 string format) ...........: {ediJob.Emd.Metadata.Varo.CreationTime.ABST}");
            log.LogInformation($"    - Absolute UTC timestamp (date/time object) .................: {ediJob.Emd.Metadata.Varo.CreationTime.ABST_UTC}");
            log.LogInformation($"  - EMD logger mount time ");
            log.LogInformation($"    - Absolute UTC timestamp (ISO 8601 string format) ...........: {ediJob.Emd.Metadata.Varo.MountTime.ABST}");
            log.LogInformation($"    - Absolute UTC timestamp (date/time object) .................: {ediJob.Emd.Metadata.Varo.MountTime.Calcs.ABST_UTC}");
            log.LogInformation($"    - Relative timestamp (ISO 8601 duration format) .............: {ediJob.Emd.Metadata.Varo.MountTime.RELT ?? ""}");
            log.LogInformation($"    - Relative timestamp (elapsed seconds since activation) .....: {ediJob.Emd.Metadata.Varo.MountTime.Calcs.RELT_ELAPSED_SECS}");
            log.LogInformation($"  - EDI logger event record time calculations (sample only) ");
            if (records.Count > 1)
            {
                log.LogInformation($"    - records[{last}] ");
                log.LogInformation($"      - Relative timestamp (ISO 8601 duration format) ...........: {records[last].RELT}");
                log.LogInformation($"      - Relative timestamp (elapsed seconds since activation) ...: {records[last].EDI_RELT_ELAPSED_SECS}");
                log.LogInformation($"      - Relative timestamp (elapsed seconds since mount time) ...: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Varo.MountTime.RELT, records[last].RELT)}");
                log.LogInformation($"      - Absolute UTC timestamp (date/time object) ...............: {records[last].EDI_ABST}");
                log.LogInformation($"    - records[{first}] ");
                log.LogInformation($"      - Relative timestamp (ISO 8601 duration format) ...........: {records[first].RELT}");
                log.LogInformation($"      - Relative timestamp (elapsed seconds since activation) ...: {records[first].EDI_RELT_ELAPSED_SECS}");
                log.LogInformation($"      - Relative timestamp (elapsed seconds since mount time) ...: {DataTransformService.CalculateElapsedSecondsFromLoggerMountRelativeTime(ediJob.Emd.Metadata.Varo.MountTime.RELT, records[first].RELT)}");
                log.LogInformation($"      - Absolute UTC timestamp (date/time object) ...............: {records[first].EDI_ABST}");
            }
        }

        public static EdiJobVaroMetadataMountTime GetTimeFromEmsSyncFileName(List<CloudBlockBlob> listLoggerFiles)
        {
            EdiJobVaroMetadataMountTime timeInfo = null;

            if (listLoggerFiles != null)
            {
                foreach (CloudBlockBlob logBlob in listLoggerFiles)
                {
                    if (EmsService.IsThisEmsSyncDataFile(logBlob.Name))
                    {
                        string[] parts = logBlob.Name.Split("/"); ;
                        string logFileName = parts[parts.Length - 1];
                        Match m1 = EmsService.IsThisEmsSyncFile(logFileName);
                        if (m1.Success)
                        {
                            timeInfo ??= new EdiJobVaroMetadataMountTime();
                            timeInfo.ABST = m1.Groups[3].Value;
                            timeInfo.RELT = m1.Groups[2].Value;
                            timeInfo.Calcs.ABST_UTC = DateConverter.ConvertIso8601CompliantString(m1.Groups[3].Value);
                            timeInfo.Calcs.RELT_ELAPSED_SECS = DataTransformService.ConvertRelativeTimeStringToTotalSeconds(m1.Groups[2].Value);
                            timeInfo.SOURCE = EmdTimeSource.Name.EMS_SYNC_FILENAME;
                        }
                    }
                }
            }
            return timeInfo;
        }

        public static string GetVaroMountTimeAbst(EdiJob ediJob)
        {
            string result = null;
            if (ediJob != null)
            {
                if (ediJob.Emd.Metadata.Varo.MountTime.ABST != null)
                {
                    if (ediJob.Emd.Metadata.Varo.MountTime.ABST != null)
                    {
                        result = ediJob.Emd.Metadata.Varo.MountTime.ABST;
                    }
                }
            }
            return result;
        }

        public static EdiJobVaroMetadataCreationTime GetVaroReportCreationTimeFromMetadataFileName(string name)
        {
            EdiJobVaroMetadataCreationTime timeInfo = null;

            if (name != null)
            {
                string[] parts = name.Split("/"); ;
                string fileName = parts[parts.Length - 1];

                Match m = VaroDataProcessorService.IsThisVaroReportMetadataFile(fileName);
                if (m.Success)
                {
                    timeInfo ??= new EdiJobVaroMetadataCreationTime();
                    timeInfo.ABST = m.Groups[2].Value;
                    timeInfo.ABST_UTC = DateConverter.ConvertIso8601CompliantString(m.Groups[2].Value);
                    timeInfo.SOURCE = EmdTimeSource.Name.EMD_REPORT_METADATA_FILENAME;
                }
            }
            return timeInfo;
        }

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
        public static List<EmsEventRecord> CalculateAbsoluteTimeForVaroCollectedRecords(List<EmsEventRecord> records, EdiJob ediJob)
        {
            //string absoluteTime = GetKeyValueFromMetadataRecordsObject("ABST", reportMetadata);
            string emdTimeAbst = GetVaroMountTimeAbst(ediJob);
            int emdTimeElapsedSecs = ediJob.Emd.Metadata.Varo.MountTime.Calcs.RELT_ELAPSED_SECS;

            foreach (EmsEventRecord record in records)
            {
                DateTime? dt = DataTransformService.CalculateAbsoluteTimeForEmsRecord(emdTimeAbst, emdTimeElapsedSecs, record.RELT, record.EDI_SOURCE);
                record.EDI_ABST = dt;
            }
            return records;
        }

        public static string GetReportMetadataFileNameFromBlobPath(string blobPath)
        {
            string reportFileName = null;
            if (blobPath != null)
            {
                string[] parts = blobPath.Split("/"); ;
                string fileName = parts[parts.Length - 1];
                Match m = VaroDataProcessorService.IsThisVaroReportMetadataFile(fileName);
                if (m.Success)
                {
                    reportFileName = fileName;
                }
            }
            return reportFileName;
        }
    }
}
