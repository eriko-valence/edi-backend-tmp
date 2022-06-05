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
using lib_edi.Helpers;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Domain.CceDevice;
using System.Reflection;
using System.Xml;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Ccdx;
using System.IO;
using System.Globalization;
using CsvHelper;
using lib_edi.Models.Loggers.Csv;

namespace lib_edi.Services.CceDevice
{
    public class IndigoDataTransformService : DataTransformService
    {

		/// <summary>
		/// Returns a list of Indigo V2 log blobs from a collection of blobs
		/// </summary>
		/// <param name="logDirectoryBlobs">Full list of blobs </param>
		/// <param name="blobPath">blob storage path</param>
		/// <returns>
		/// List containing only Indigo V2 log blobs; Exception (L91T)
		/// </returns>
		public static List<CloudBlockBlob> GetLogBlobs(IEnumerable<IListBlobItem> logDirectoryBlobs, string blobPath)
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

		/// <summary>
		/// Maps raw USBDG log file properties to csv columns
		/// </summary>
		/// <remarks>
		/// This function iterates a list of USBDG log files to map these records. Records
		/// from all log files are consolidated into one list of denormalized records.
		/// </remarks>
		/// <param name="usbdgLogFiles">A list of deserilized USBDG json log file</param>
		/// <returns>
		/// A consolidated list of csv USBDG MetaFridge log file records  if successful; Exception (D39Y) if any failures occur 
		/// </returns>
		/*
        public static List<UsbdgSimCsvRecordDto> MapSourceLogsToSinkColumnNames(List<dynamic> sourceLogs, dynamic sourceUsbdgMetadata)
		{


			List<UsbdgSimCsvRecordDto> sinkData = new List<UsbdgSimCsvRecordDto>();
			foreach (dynamic sourceLog in sourceLogs)
			{
                sinkData.AddRange(MapSourceLogToSinkColumnNames(sourceLog, sourceUsbdgMetadata));
			}

			return sinkData;
		}
        */

        public static List<IndigoV2EventRecord> MapSourceToSinkEvents(List<dynamic> sourceLogs)
        {


            List<IndigoV2EventRecord> sinkData = new List<IndigoV2EventRecord>();
            foreach (dynamic sourceLog in sourceLogs)
            {
                sinkData.AddRange(MapSourceToSinkEventColumns(sourceLog));
            }

            return sinkData;
        }

        

        /// <summary>
        /// Maps raw EMD and logger files to CSV compatible format
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the logger data file into records ready for CSV serialization.
        /// </remarks>
        /// <param name="emdLogFile">A deserilized logger data file</param>
        /// <param name="metadataFile">A deserilized EMD metadata file</param>
        /// <returns>
        /// A list of CSV compatible EMD + logger data records, if successful; Exception (D39Y) if any failures occur 
        /// </returns>
        private static List<IndigoV2EventRecord> MapSourceToSinkEventColumns(dynamic sourceLog)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<IndigoV2EventRecord> sinkEventRecords = new();

                JObject sourceLogJObject = (JObject)sourceLog;
                sourceFile = GetSourceFile(sourceLogJObject);
                
                foreach (KeyValuePair<string, JToken> x in sourceLogJObject)
                {
                    if (x.Value.Type == JTokenType.Array)
                    {
                        foreach (JObject z in x.Value.Children<JObject>())
                        {
                            IndigoV2EventRecord sinkRecord = new();
                            foreach (JProperty prop in z.Properties())
                            {
                                propName = prop.Name;
                                propValue = (string)prop.Value;
                                if (prop.Name == "ABST")
                                {
                                    DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ObjectManager.SetObjectValue(ref sinkRecord, prop.Name, emdAbsoluteTime);

                                }
                                else
                                {
                                    ObjectManager.SetObjectValue(ref sinkRecord, prop.Name, prop.Value);
                                }

                            }
                            sinkEventRecords.Add(sinkRecord);
                        }
                    }
                }
                return sinkEventRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "D39Y", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="usbdgSimMetadata"> The UsbdgSimMetadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimMetadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref UsbdgSimEmdMetadata usbdgSimMetadata, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimMetadata.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimMetadata, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a System.Object value
        /// </summary>
        /// <param name="usbdgSimCsvRecordDto"> The UsbdgSimCsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimCsvRecordDto object that will be set with the System.Object value</param>
        /// <param name="obj">The new System.Object property value</param>
        public static void SetObjectValue(ref UsbdgSimCsvRecordDto usbdgSimCsvRecordDto, string propertyName, object obj)
        {
            try
            {
                if (obj != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimCsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimCsvRecordDto, obj, null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw e;
            }
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="usbdgSimCsvRecordDto"> The UsbdgSimCsvRecordDto object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimCsvRecordDto object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref UsbdgSimCsvRecordDto usbdgSimCsvRecordDto, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = usbdgSimCsvRecordDto.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(usbdgSimCsvRecordDto, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception e)
            {
                throw;
            }
        }

        /// <summary>
        /// Calculates record total duration in seconds using relative time (e.g., P8DT30S) of that record
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <returns>
        /// List of denormalized USBDG records (with the calculated duration seconds); Exception (M34T) otherwise
        /// </returns>
        public static List<IndigoV2EventRecord> ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(List<IndigoV2EventRecord> records)
        {
            foreach (IndigoV2EventRecord record in records)
            {
                try
                {
                    TimeSpan ts = XmlConvert.ToTimeSpan(record.RELT);
                    record._RELT_SECS = Convert.ToInt32(ts.TotalSeconds);
                }
                catch (Exception e)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "M34T", EdiErrorsService.BuildErrorVariableArrayList(record.RELT, record.Source));
                    throw new Exception(customErrorMessage);
                }
            }
            return records;
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="relativeTime">Relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static int ConvertRelativeTimeStringToTotalSeconds(dynamic metadata)
        {
            string relativeTime = null;

            try
            {
                relativeTime = ObjectManager.GetJObjectPropertyValueAsString(metadata, "RELT");
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                return Convert.ToInt32(ts.TotalSeconds);
            }
            catch (Exception e)
            {
                if (relativeTime != null)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
                    throw new Exception(customErrorMessage);
                }


            }
        }

        /// <summary>
        /// Gets the absolute timestamp of USBDG records using the report absolute timestamp and relative time of records
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <param name="reportDurationSeconds">USBDG log report duration seconds</param>
        /// <param name="reportAbsoluteTimestamp">USBDG log report record relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a USBDG record; Exception (4Q5D) otherwise
        /// </returns>
        public static List<IndigoV2EventRecord> CalculateAbsoluteTimeForUsbdgRecords(List<IndigoV2EventRecord> records, int reportDurationSeconds, dynamic reportAbsoluteTimestamp)
        {
            string absoluteTime = ObjectManager.GetJObjectPropertyValueAsString(reportAbsoluteTimestamp, "ABST");

            foreach (IndigoV2EventRecord record in records)
            {
                DateTime? dt = CalculateAbsoluteTimeForUsbdgRecord(absoluteTime, reportDurationSeconds, record.RELT, record.Source);
                record.ABST_CALC = dt;
            }
            return records;
        }

        /// <summary>
        /// Gets the absolute timestamp of a record using the report absolute timestamp and record relative time
        /// </summary>
        /// <param name="reportAbsoluteTime">Log report absolute timestamp</param>
        /// <param name="reportDurationSeconds">Log report duration seconds</param>
        /// <param name="recordRelativeTime">Log report record relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a USBDG record; Exception (4Q5D) otherwise
        /// </returns>
        private static DateTime? CalculateAbsoluteTimeForUsbdgRecord(string reportAbsoluteTime, int reportDurationSeconds, string recordRelativeTime, string sourceLogFile)
        {
            try
            {
                int recordDurationSeconds = ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = reportDurationSeconds - recordDurationSeconds; // How far away time wise is this record compared to the absolute time

                DateTime? reportAbsoluteDateTime = DateConverter.ConvertIso8601CompliantString(reportAbsoluteTime);

                TimeSpan ts = TimeSpan.FromSeconds(elapsedSeconds);
                if (reportAbsoluteDateTime != null)
                {
                    DateTime reportAbsDateTime = (DateTime)reportAbsoluteDateTime;
                    DateTime UtcTime = reportAbsDateTime.Subtract(ts);
                    return UtcTime;
                }
                else
                {
                    return null;
                }
            }
            catch (Exception e)
            {
                string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
                throw new Exception(customErrorMessage);
            }
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="relativeTime">Relative time (e.g., P8DT30S)</param>
        /// <returns>
        /// Total seconds calculated from relative time; Exception (A89R) or (EZ56) otherwise
        /// </returns>
        public static int ConvertRelativeTimeStringToTotalSeconds(string relativeTime)
        {
            try
            {
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                return Convert.ToInt32(ts.TotalSeconds);
            }
            catch (Exception e)
            {
                if (relativeTime != null)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "A89R", EdiErrorsService.BuildErrorVariableArrayList(relativeTime));
                    throw new Exception(customErrorMessage);
                }
                else
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "EZ56", null);
                    throw new Exception(customErrorMessage);
                }
            }
        }

        /// <summary>
        /// Calculate a record's elapsed time (in seconds) since the logger activation relative time
        /// </summary>
        /// <param name="loggerActivationRelativeTime">Logger activation relative time</param>
        /// <param name="recordRelativeTime">Record's relative time</param>
        /// <returns>
        /// Seconds that elapsed snce logger activation relative time
        /// </returns>
        public static int CalculateElapsedSecondsFromLoggerActivationRelativeTime(string loggerActivationRelativeTime, string recordRelativeTime)
        {
            try
            {
                int loggerActivationRelativeTimeSecs = ConvertRelativeTimeStringToTotalSeconds(loggerActivationRelativeTime); // convert timespan to seconds
                int recordRelativeTimeSecs = ConvertRelativeTimeStringToTotalSeconds(recordRelativeTime);
                int elapsedSeconds = loggerActivationRelativeTimeSecs - recordRelativeTimeSecs; // How far away time wise is this record compared to the absolute time
                return elapsedSeconds;
            }
            catch (Exception)
            {
                //string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "4Q5D", EdiErrorsService.BuildErrorVariableArrayList(reportAbsoluteTime, recordRelativeTime, sourceLogFile));
                //throw new Exception(customErrorMessage);
                throw;
            }
        }

        /// <summary>
        /// Writes denormalized USBDG log file csv records to Azure blob storage
        /// </summary>
        /// <param name="cloudBlobContainer">A container in the Microsoft Azure Blob service</param>
        /// <param name="requestBody">EMS log transformation http reqest object</param>
        /// <param name="usbdgRecords">A list of denormalized USBDG log records</param>
        /// <param name="log">Azure function logger object</param>
        /// <returns>
        /// Blob name of USBDG csv formatted log file; Exception (Q25U)
        /// </returns>
        public static async Task<string> WriteUsbdgLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<IndigoV2EventRecord> usbdgRecords, ILogger log)
        {
            string blobName = "";
            if (requestBody != null)
            {
                if (requestBody.Path != null)
                {
                    try
                    {
                        //string loggerType = CcdxService.GetDataLoggerTypeFromBlobPath(requestBody.Path);
                        //string dateFolder = DateTime.UtcNow.ToString("yyyy-MM-dd/HH");
                        //string guidFolder = CcdxService.GetGuidFromBlobPath(requestBody.Path);

                        blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path);

                        //blobName = $"{dateFolder}/{guidFolder}/out.csv";
                        log.LogInformation($"  - Blob: {blobName}");
                        log.LogInformation($"  - Get block blob reference");
                        CloudBlockBlob outBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                        log.LogInformation($"  - Open stream for writing to the blob");
                        using var writer = await outBlob.OpenWriteAsync();
                        log.LogInformation($"  - Initialize new instance of stream writer");
                        using var streamWriter = new StreamWriter(writer);
                        log.LogInformation($"  - Initialize new instance of csv writer");
                        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                        log.LogInformation($"  - Write list of records to the CSV file");
                        csvWriter.WriteRecords(usbdgRecords);
                    }
                    catch (Exception e)
                    {
                        string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "Q25U", EdiErrorsService.BuildErrorVariableArrayList(blobName, cloudBlobContainer.Name));
                        throw new Exception(customErrorMessage);
                    }
                }
            }
            return blobName;
        }

        /// <summary>
        /// Sets the property value of a specified object with a JToken value
        /// </summary>
        /// <param name="eventRecord"> The UsbdgSimMetadata object whose property value will be set</param>
        /// <param name="propertyName">The property name of the UsbdgSimMetadata object that will be set with the JToken value</param>
        /// <param name="token">The new JToken property value</param>
        public static void SetObjectValue(ref IndigoV2EventRecord eventRecord, string propertyName, JToken token)
        {
            try
            {
                if (token != null)
                {
                    if (propertyName != null)
                    {
                        PropertyInfo propertyInfo = eventRecord.GetType().GetProperty(propertyName);
                        if (propertyInfo != null)
                        {
                            propertyInfo.SetValue(eventRecord, Convert.ChangeType(token, propertyInfo.PropertyType), null);
                        }
                    }
                }
            }
            catch (Exception)
            {
                throw;
            }
        }

        private static string GetSourceFile(JObject jo)
        {
            string sourceFile = null;
            if (jo != null)
            {
                sourceFile = ObjectManager.GetJObjectPropertyValueAsString(jo, "_SOURCE");
            }
            return sourceFile;
        }
    }
}
