using lib_edi.Services.Errors;
using Microsoft.Azure.Storage.Blob;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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
using lib_edi.Models.Azure.AppInsights;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Azure;
using lib_edi.Services.Loggers;
using lib_edi.Services.Ems;

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
                    // NHGH-2362 (2022.06.16) - only add Indigo V2 data files
                    if (EmsService.IsFileFromEmsLogger(logBlob.Name))
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
        public static List<EmsEventRecord> ConvertRelativeTimeToTotalSecondsForUsbdgLogRecords(List<EmsEventRecord> records)
        {
            foreach (EmsEventRecord record in records)
            {
                try
                {
                    TimeSpan ts = XmlConvert.ToTimeSpan(record.RELT);
                    record._RELT_SECS = Convert.ToInt32(ts.TotalSeconds);
                }
                catch (Exception e)
                {
                    string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "M34T", EdiErrorsService.BuildErrorVariableArrayList(record.RELT, record.EDI_SOURCE));
                    throw new Exception(customErrorMessage);
                }
            }
            return records;
        }

        /// <summary>
        /// Converts relative time to total seconds
        /// </summary>
        /// <param name="metadata">USBDG object holding relative time (e.g., P8DT30S)</param>
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
                relativeTime = GetKeyValutFromMetadataRecordsObject("RELT", metadata);
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

        /// <summary>
        /// Returns a key value from an array "records" in a JSON object 
        /// </summary>
        /// <param name="key">Name of property to find in array "records" of a JSON object</param>
        /// <param name="metadata">JSON object to search</param>
        /// <returns>
        /// Key value found
        /// </returns>
        public static string GetKeyValutFromMetadataRecordsObject(string key, dynamic metadata)
        {
            JObject sourceJObject = (JObject)metadata;
            string result = null;

            // Grab the log header properties from the source metadata file
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
        /// Calculates the absolute timestamp for each Indigo V2 records using the USBDG metadata absolute timestamp and relative time of records
        /// </summary>
        /// <param name="records">List of denormalized USBDG records </param>
        /// <param name="reportDurationSeconds">USBDG metadata duration seconds (converted from relative seconds)</param>
        /// <param name="reportMetadata">USBDG metadata file json object</param>
        /// <returns>
        /// Absolute timestamp (DateTime) of a Indigo V2 record; Exception (4Q5D) otherwise
        /// </returns>
        public static List<EmsEventRecord> CalculateAbsoluteTimeForUsbdgRecords(List<EmsEventRecord> records, int reportDurationSeconds, dynamic reportMetadata)
        {
            string absoluteTime = GetKeyValutFromMetadataRecordsObject("ABST", reportMetadata);

            foreach (EmsEventRecord record in records)
            {
                DateTime? dt = CalculateAbsoluteTimeForUsbdgRecord(absoluteTime, reportDurationSeconds, record.RELT, record.EDI_SOURCE);
                record.EDI_RECORD_ABST_CALC = dt;
            }
            return records;
        }

        /// <summary>
        /// Gets the absolute timestamp of a record using the report absolute timestamp and record relative time
        /// </summary>
        /// <param name="reportAbsoluteTime">Logger record absolute timestamp</param>
        /// <param name="reportDurationSeconds">USBDG metadata duration seconds (converted from relative seconds)</param>
        /// <param name="recordRelativeTime">Logger record relative time (e.g., P8DT30S)</param>
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
        public static async Task<string> WriteUsbdgLogRecordsToCsvBlob(CloudBlobContainer cloudBlobContainer, TransformHttpRequestMessageBodyDto requestBody, List<EdiSinkRecord> usbdgRecords, string loggerTypeName, ILogger log)
        {
            string blobName = "";
            string loggerType = loggerTypeName.ToString().ToLower();

            if (requestBody != null)
            {
                if (requestBody.Path != null)
                {
                    try
                    {
                        if (usbdgRecords.Count > 0)
                        {
                            log.LogInformation($"  - Determine object type using list of generic records");
                            var firstRecord = usbdgRecords.FirstOrDefault();
                            string recordType = firstRecord.GetType().Name;
                            log.LogInformation($"    - Record type: {recordType}");
                            if (recordType == "IndigoV2EventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_event.csv", loggerType);
                            }
                            else if (recordType == "Sl1EventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "sl1_event.csv", loggerType);
                            }
                            else if (recordType == "IndigoV2LocationRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "indigo_v2_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_location.csv", loggerType);
                            }
                            else if (recordType == "UsbdgDeviceRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_device.csv", loggerType);
                            }
                            else if (recordType == "UsbdgEventRecord")
                            {
                                log.LogInformation($"  - Is record type supported? Yes");
                                blobName = DataTransformService.BuildCuratedBlobPath(requestBody.Path, "usbdg_event.csv", loggerType);
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
                            else if (recordType == "UsbdgLocationRecord")
                            {
                                List<UsbdgLocationRecord> records = JsonConvert.DeserializeObject<List<UsbdgLocationRecord>>(serializedParent);
                                log.LogInformation($"  - Write list of usbdg location records to the CSV file");
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
                            }
                            else
                            {
                                log.LogInformation($"  - Unsupported record type. Will not write list of records to CSV file.");
                            }

                        } else
                        {
                            log.LogInformation($"  - Zero records found. CSV file will not be written to blog storage.");
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

        /// <summary>
        /// EMS ADF data transformation stage has succeeded
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogEmsTransformSucceededEventToAppInsights(string reportFileName, DataLoggerTypeEnum.Name loggerType, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.SUCCEEDED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.LoggerType = loggerType;
            pipelineEvent.ReportFileName = reportFileName;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
        }

        /// <summary>
        /// EMS ADF data transformation stage has started
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        public static void LogEmsTransformStartedEventToAppInsights(string reportFileName, ILogger log)
        {
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.STARTED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.ReportFileName = reportFileName;
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
        }

        /// <summary>
        /// Sends EMS ADF data transformatoin error event to App Insight
        /// </summary>
        /// <param name="reportFileName">Name of Cold chain telemetry file pulled from CCDX Kafka topic</param>
        /// <param name="log">Microsoft extension logger</param>
        /// <param name="e">Exception object</param>
        /// <param name="errorCode">Error code</param>
        public static void LogEmsTransformErrorEventToAppInsights(string reportFileName, ILogger log, Exception e, string errorCode)
        {
            string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList(reportFileName));
            PipelineEvent pipelineEvent = new PipelineEvent();
            pipelineEvent.EventName = PipelineEventEnum.Name.FAILED;
            pipelineEvent.StageName = PipelineStageEnum.Name.ADF_TRANSFORM;
            pipelineEvent.LoggerType = DataLoggerTypeEnum.Name.INDIGO_V2;
            pipelineEvent.PipelineFailureType = PipelineFailureTypeEnum.Name.ERROR;
            pipelineEvent.PipelineFailureReason = PipelineFailureReasonEnum.Name.UNKNOWN_EXCEPTION;
            pipelineEvent.ReportFileName = reportFileName;
            pipelineEvent.ErrorCode = errorCode;
            pipelineEvent.ErrorMessage = errorMessage;
            if (e != null)
            {
                pipelineEvent.ExceptionMessage = e.Message;
                pipelineEvent.ExceptionInnerMessage = EdiErrorsService.GetInnerException(e);
            }
            Dictionary<string, string> customProps = AzureAppInsightsService.BuildCustomPropertiesObject(pipelineEvent);
            AzureAppInsightsService.LogEntry(PipelineStageEnum.Name.ADF_TRANSFORM, customProps, log);
        }
    }
}
