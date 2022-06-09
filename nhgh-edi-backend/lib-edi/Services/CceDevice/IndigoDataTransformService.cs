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
using lib_edi.Models.Csv;
using lib_edi.Models.Emd.Csv;
using System.Dynamic;
using System.Collections;
using lib_edi.Models.Edi;

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

        /*
        public static List<EdiSinkRecord> MapSourceToSinkEvents(List<dynamic> sourceLogs)
        {
            foreach (dynamic sourceLog in sourceLogs)
            {
                sinkRecords.AddRange(MapSourceToSinkEventColumns(sourceLog));
            }

            return sinkRecords;
        }
        */


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
        public static List<IndigoV2EventRecord> MapIndigoV2Events(List<dynamic> sourceLogs, EdiJob ediJob)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<IndigoV2EventRecord> sinkCsvEventRecords = new();

                foreach (dynamic sourceLog in sourceLogs)
                {
                    JObject sourceLogJObject = (JObject)sourceLog;
                    sourceFile = GetSourceFile(sourceLogJObject);
                    
                    

                    // Grab the log header properties from the source log file
                    var logHeaderObject = new ExpandoObject() as IDictionary<string, Object>;
                    foreach (KeyValuePair<string, JToken> log1 in sourceLogJObject)
                    {
                        if (log1.Value.Type != JTokenType.Array)
                        {
                            logHeaderObject.Add(log1.Key, log1.Value);
                        }
                    }

                    // Map csv record objects from source log file
                    foreach (KeyValuePair<string, JToken> log2 in sourceLogJObject)
                    {
                        // Load log record properties into csv record object
                        if (log2.Value.Type == JTokenType.Array && log2.Key == "records")
                        {
                            

                            //ObjectManager.SetObjectValue(ref sinkCsvEventRecord, "RELT", ediJob.RELT);
                            // Iterate each log record
                            foreach (JObject z in log2.Value.Children<JObject>())
                            {
                                EdiSinkRecord sinkCsvEventRecord = new IndigoV2EventRecord();
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ESER", ediJob.UsbdgMetadata.ESER);
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ALRM", ediJob.UsbdgMetadata.ALRM);

                                // Load log header properties into csv record object
                                foreach (var logHeader in logHeaderObject)
                                {
                                    ObjectManager.SetObjectValue(sinkCsvEventRecord, logHeader.Key, logHeader.Value);
                                }

                                // Load each log record property
                                foreach (JProperty prop in z.Properties())
                                {
                                    propName = prop.Name;
                                    if (propName == "RELT")
                                    {
                                        Console.WriteLine("debug");
                                    }
                                    propValue = (string)prop.Value;
                                    ObjectManager.SetObjectValue(sinkCsvEventRecord, prop.Name, prop.Value);
                                }

                                sinkCsvEventRecords.Add((IndigoV2EventRecord)sinkCsvEventRecord);
                            }
                        }
                        else
                        {
                            // ObjectManager.SetObjectValue(ref sink, log2.Key, log2.Value);
                        }
                        
                    }
                }
                return sinkCsvEventRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "D39Y", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
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
        public static List<EdiSinkRecord> MapIndigoV2Locations(dynamic sourceUsbdgMetadata, EdiJob ediJob)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EdiSinkRecord> sinkCsvLocationsRecords = new();
                JObject sourceJObject = (JObject)sourceUsbdgMetadata;
                sourceFile = GetSourceFile(sourceJObject);
                

                // Grab the log header properties from the source metadata file
                var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
                foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
                {
                    if (log1.Value.Type != JTokenType.Array)
                    {
                        sourceHeaders.Add(log1.Key, log1.Value);
                    }
                }

                // Map csv record objects from source metadata file
                foreach (KeyValuePair<string, JToken> log2 in sourceJObject)
                {
                    // Load log record properties into csv record object
                    if (log2.Value.Type == JTokenType.Array && log2.Key == "zgps_data")
                    {
                        // Iterate each array
                        foreach (JObject z in log2.Value.Children<JObject>())
                        {
                            EdiSinkRecord sinkCsvLocationsRecord = new IndigoV2LocationRecord();
                            ObjectManager.SetObjectValue(sinkCsvLocationsRecord, "LSER", ediJob.Logger.LSER);

                            // Load each log record property
                            foreach (JProperty prop in z.Properties())
                            {
                                propName = prop.Name;
                                propValue = (string)prop.Value;
                                ObjectManager.SetObjectValue(sinkCsvLocationsRecord, prop.Name, prop.Value);

                                if (propName == "zgps_abst")
                                {
                                    long zgps_abst_long = long.Parse(propValue);
                                    ((IndigoV2LocationRecord)sinkCsvLocationsRecord).EDI_ZGPS_ABST_DATETIME = DateConverter.FromUnixTimeSeconds(zgps_abst_long);
                                }
                            }
                            sinkCsvLocationsRecords.Add(sinkCsvLocationsRecord);
                        }
                    }
                    else
                    {
                        // ObjectManager.SetObjectValue(ref sink, log2.Key, log2.Value);
                    }
                    
                }
                return sinkCsvLocationsRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "MTJV", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
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
            catch (Exception)
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
            catch (Exception)
            {
                throw;
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
            catch (Exception)
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
            int result = 0;

            try
            {
                JObject sourceJObject = (JObject)metadata;
                Console.WriteLine("debug");
                relativeTime = GetKeyValutFromMetadataRecordsObject("RELT", metadata);
                //jTokenObject.GetValue(propertyName).Value<string>();
                TimeSpan ts = XmlConvert.ToTimeSpan(relativeTime); // parse iso 8601 duration string to timespan
                result = Convert.ToInt32(ts.TotalSeconds);
                return result;

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

        public static string GetKeyValutFromMetadataRecordsObject(string key, dynamic metadata)
        {

            JObject sourceJObject = (JObject)metadata;

            string result = null;

            // Grab the log header properties from the source metadata file
            var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
            foreach (KeyValuePair<string, JToken> source in sourceJObject)
            {
                if (source.Value.Type == JTokenType.Array && source.Key == "records")
                {
                    // Iterate each log record
                    foreach (JObject z in source.Value.Children<JObject>())
                    {
                        // Load each log record property
                        foreach (JProperty prop in z.Properties())
                        {
                            string propName = prop.Name;
                            
                            if (propName == key)
                            {
                                result = (string)prop.Value;
                            }
                        }
                    }
                }


            }

            return result;

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
        public static List<IndigoV2EventRecord> CalculateAbsoluteTimeForUsbdgRecords(List<IndigoV2EventRecord> records, int reportDurationSeconds, dynamic reportMetadata)
        {
            //string absoluteTime = ObjectManager.GetJObjectPropertyValueAsString(reportAbsoluteTimestamp, "ABST");
            string absoluteTime = GetKeyValutFromMetadataRecordsObject("ABST", reportMetadata);

            foreach (IndigoV2EventRecord record in records)
            {
                DateTime? dt = CalculateAbsoluteTimeForUsbdgRecord(absoluteTime, reportDurationSeconds, record.RELT, record.Source);
                record.EDI_RECORD_ABST_CALC = dt;
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
        public static async Task<string> WriteUsbdgLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<EdiSinkRecord> usbdgRecords, ILogger log)
        {
            string blobName = "";

            if (requestBody != null)
            {
                if (requestBody.Path != null)
                {
                    try
                    {
                        log.LogInformation($"  - Determine object type using list of generic records");
                        var firstRecord = usbdgRecords.FirstOrDefault();
                        string recordType = firstRecord.GetType().Name;
                        log.LogInformation($"    - Record type: {recordType}");
                        if (recordType == "IndigoV2EventRecord")
                        {
                            log.LogInformation($"  - Is record type supported? Yes");
                            blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path, "indigo_v2_event.csv");
                        }
                        else if (recordType == "IndigoV2LocationRecord")
                        {
                            log.LogInformation($"  - Is record type supported? Yes");
                            blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path, "indigo_v2_location.csv");
                        }
                        else if (recordType == "UsbdgDeviceRecord")
                        {
                            log.LogInformation($"  - Is record type supported? Yes");
                            blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path, "usbdg_device.csv");
                        } 
                        else if (recordType == "UsbdgEventRecord")
                        {
                            log.LogInformation($"  - Is record type supported? Yes");
                            blobName = CcdxService.BuildCuratedCcdxConsumerBlobPath(requestBody.Path, "usbdg_event.csv");
                        }
                        else
                        {
                            log.LogInformation($"  - Is record type supported? No");
                            return blobName;
                        }
                        log.LogInformation($"  - Blob: {blobName}");
                        log.LogInformation($"  - Get block blob reference");
                        CloudBlockBlob outBlob = cloudBlobContainer.GetBlockBlobReference(blobName);
                        log.LogInformation($"  - Open stream for writing to the blob");
                        using var writer = await outBlob.OpenWriteAsync();
                        log.LogInformation($"  - Initialize new instance of stream writer");
                        using var streamWriter = new StreamWriter(writer);
                        log.LogInformation($"  - Initialize new instance of csv writer");
                        using var csvWriter = new CsvWriter(streamWriter, CultureInfo.InvariantCulture);
                        log.LogInformation($"  - Pull child records from list");
                        var serializedParent = JsonConvert.SerializeObject(usbdgRecords);
                        if (recordType == "IndigoV2EventRecord")
                        {
                            List<IndigoV2EventRecord> records = JsonConvert.DeserializeObject<List<IndigoV2EventRecord>>(serializedParent);
                            log.LogInformation($"  - Write list of indigo v2 event records to the CSV file");
                            csvWriter.WriteRecords(records);
                        }
                        else if (recordType == "IndigoV2LocationRecord")
                        {
                            List<IndigoV2LocationRecord> records = JsonConvert.DeserializeObject<List<IndigoV2LocationRecord>>(serializedParent);
                            log.LogInformation($"  - Write list of indigo v2 location records to the CSV file");
                            csvWriter.WriteRecords(records);
                        }
                        else if (recordType == "UsbdgDeviceRecord")
                        {
                            List<UsbdgDeviceRecord> records = JsonConvert.DeserializeObject<List<UsbdgDeviceRecord>>(serializedParent);
                            log.LogInformation($"  - Write list of usbdg device records to the CSV file");
                            csvWriter.WriteRecords(records);
                        } 
                        else if (recordType == "UsbdgEventRecord")
                        {
                            List<UsbdgEventRecord> records = JsonConvert.DeserializeObject<List<UsbdgEventRecord>>(serializedParent);
                            log.LogInformation($"  - Write list of usbdg event records to the CSV file");
                            csvWriter.WriteRecords(records);
                        } else
                        {
                            log.LogInformation($"  - Unsupported record type. Will not write list of records to CSV file.");
                        }
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
    }
}
