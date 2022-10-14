using lib_edi.Helpers;
using lib_edi.Models.Csv;
using lib_edi.Models.Domain.CceDevice;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Models.Edi;
using lib_edi.Models.Emd.Csv;
using lib_edi.Models.Enums.Emd;
using lib_edi.Models.Loggers.Csv;
using lib_edi.Services.CceDevice;
using lib_edi.Services.Errors;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Reflection;
using System.Text;

namespace lib_edi.Services.Loggers
{
    /// <summary>
    /// Provides methods for mapping data models. 
    /// </summary>
    public class DataModelMappingService
    {
        /// <summary>
        /// Maps raw json MetaFridge log files to csv records
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the MetaFridge log file to get the records into a csv compatible format.
        /// </remarks>
        /// <param name="metaFridgeLogFile">A deserilized Metafridge json log file</param>
        /// <returns>
		/// A list of csv compatible MetaFridge log file records if successful; Exception (3B6U) if any there are any failures
		/// </returns>
        private static List<Cfd50CsvRecordDto> MapMetaFridgeLogFileRecords(dynamic metaFridgeLogFile)
        {
            try
            {

                List<Cfd50CsvRecordDto> cfd50CsvRecords = new();

                /* ######################################################################
                 * # Cast dynamic data file objects to JSON
                 * ###################################################################### */
                JObject JObjLoggerDataFile = (JObject)metaFridgeLogFile;

                /* ######################################################################
                 * # Merge EMD and logger metadata
                 * ###################################################################### */
                Cfd50Metadata csvEmsMetadata = new Cfd50Metadata();
                foreach (KeyValuePair<string, JToken> x in JObjLoggerDataFile)
                {
                    // Filter out records array
                    if (x.Value.Type != JTokenType.Array)
                    {
                        ObjectManager.SetObjectValue(ref csvEmsMetadata, x.Key, x.Value);
                    }
                }

                /* ######################################################################
                * # Format logger collection events for serialization to CSV 
                * ###################################################################### */
                foreach (KeyValuePair<string, JToken> x in JObjLoggerDataFile)
                {
                    if (x.Value.Type == JTokenType.Array)
                    {
                        //Iterate each logger collection event
                        foreach (JObject z in x.Value.Children<JObject>())
                        {
                            Cfd50CsvRecordDto emsCsvRecord = new Cfd50CsvRecordDto();
                            //Add metadata to each collected event record
                            foreach (PropertyInfo prop in csvEmsMetadata.GetType().GetProperties())
                            {
                                ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.GetValue(csvEmsMetadata, null));                                
                            }
                            //Add collected event data to record
                            foreach (JProperty prop in z.Properties())
                            {
                                ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.Value);
                            }
                            cfd50CsvRecords.Add(emsCsvRecord);
                        }
                    }
                }

                if (cfd50CsvRecords.Count == 0)
				{
                    throw new Exception(EdiErrorsService.BuildExceptionMessageString(null, "B98R", null));
                }

                return cfd50CsvRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "3B6U", null));
            }


        }

        /// <summary>
        /// Maps raw json MetaFridge log files to csv records
        /// </summary>
        /// <remarks>
        /// This function iterates a list of MetaFridge log files to create Metafridge data records in csv format
        /// </remarks>
        /// <param name="metaFridgeLogFiles">A list of deserialized MetaFridge json log files</param>
        /// <returns>
        /// A list of csv compatible MetaFridge log file records if successful; Exception (3B6U) if any there are any failures
        /// </returns>
        public static List<Cfd50CsvRecordDto> MapMetaFridgeLogs(List<dynamic> metaFridgeLogFiles)
        {
            List<Cfd50CsvRecordDto> metaFridgeCsvRow = new();
            foreach (dynamic metaFridgeLogFile in metaFridgeLogFiles)
            {
                metaFridgeCsvRow = DataModelMappingService.MapMetaFridgeLogFileRecords(metaFridgeLogFile);
            }

            return metaFridgeCsvRow;
        }

        /// <summary>
        /// Maps raw USBDG data simulator produced log files to CSV compatible format
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the logger data file into records ready for CSV serialization.
        /// </remarks>
        /// <param name="emdLogFile">A deserialized USBDG data simulator produced logger data file</param>
        /// <param name="metadataFile">A deserialized USBDG metadata file</param>
        /// <returns>
        /// A list of CSV compatible EMD + logger data records, if successful; Exception (HUA2) if any failures occur 
        /// </returns>
        public static List<UsbdgSimCsvRecordDto> MapUsbdgLogFileRecords(dynamic emdLogFile, dynamic metadataFile)
        {
            try
            {
                List<UsbdgSimCsvRecordDto> usbdbCsvRecords = new List<UsbdgSimCsvRecordDto>();

                /* ######################################################################
                 * # Cast dynamic data file objects to JSON
                 * ###################################################################### */
                JObject JObjLoggerDataFile = (JObject)emdLogFile;
                JObject JObjMetadataFile = (JObject)metadataFile;

                /* ######################################################################
                 * # Merge EMD and logger metadata
                 * ###################################################################### */
                UsbdgSimEmdMetadata usbdgSimEmdMetadata = new UsbdgSimEmdMetadata();

                // Get the EMD metadata fields
                foreach (KeyValuePair<string, JToken> x in JObjMetadataFile)
                {
                    if (x.Key == "ABST")
					{
                        DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(x.Value.ToString());
                        //ObjectManager.SetObjectValue(ref usbdgSimEmdMetadata, x.Key, emdAbsoluteTime);
                    }  
                }

                // Get the logger header fields
                foreach (KeyValuePair<string, JToken> x in JObjLoggerDataFile)
                {
                    // Filter out records array
                    if (x.Value.Type != JTokenType.Array)
                    {
                        //ObjectManager.SetObjectValue(ref usbdgSimEmdMetadata, x.Key, x.Value);
                    }
                }

                /* ######################################################################
                 * # Format logger collection events for serialization to CSV 
                 * ###################################################################### */
                foreach (KeyValuePair<string, JToken> x in JObjLoggerDataFile)
                {
                    if (x.Value.Type == JTokenType.Array)
                    {
                        //Iterate each logger collection event
                        foreach (JObject z in x.Value.Children<JObject>())
                        {
                            UsbdgSimCsvRecordDto csvUsbdgSimRecordDto = new UsbdgSimCsvRecordDto();
                            //Add metadata to each collected event record
                            foreach (PropertyInfo prop in usbdgSimEmdMetadata.GetType().GetProperties())
                            {
                                ObjectManager.SetObjectValue(ref csvUsbdgSimRecordDto, prop.Name, prop.GetValue(usbdgSimEmdMetadata, null));
                            }
                            //Add collected event data to record
                            foreach (JProperty prop in z.Properties())
                            {
                                if (prop.Name == "ABST")
								{
                                    DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ObjectManager.SetObjectValue(ref csvUsbdgSimRecordDto, prop.Name, emdAbsoluteTime);

                                } else
								{
                                    ObjectManager.SetObjectValue(ref csvUsbdgSimRecordDto, prop.Name, prop.Value);
                                }
                                
                            }
                            usbdbCsvRecords.Add(csvUsbdgSimRecordDto);
                        }
                    }
                }
                return usbdbCsvRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "HUA2", null));
            }

        }

        /// <summary>
        /// Maps raw json USBDG metadata file to USBDG device csv record
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the raw json USBDG metadata file into a USBDG device record ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceUsbdgMetadata">A  raw json USBDG metadata file</param>
        /// <returns>
        /// A list of one USBDG device csv record, if successful; Exception (CPA8) if any failures occur 
        /// </returns>
        public static List<EdiSinkRecord> MapUsbdgDevice(dynamic sourceUsbdgMetadata)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EdiSinkRecord> sinkCsvLocationsRecords = new();
                JObject sourceJObject = (JObject)sourceUsbdgMetadata;
                sourceFile = DataTransformService.GetSourceFile(sourceJObject);

                EdiSinkRecord sinkUsbdgDeviceRecord = new UsbdgDeviceRecord();

                // Grab the log header properties from the source metadata file
                var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
                foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
                {
                    if (log1.Value.Type != JTokenType.Array)
                    {
                        sourceHeaders.Add(log1.Key, log1.Value);
                        ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
                    }
                }
                sinkCsvLocationsRecords.Add(sinkUsbdgDeviceRecord);
                return sinkCsvLocationsRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "CPA8", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        /// <summary>
        /// Maps raw json USBDG metadata file to USBDG event csv record
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the raw json USBDG metadata file into a USBDG event record ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceUsbdgMetadata">A  raw json USBDG metadata file</param>
        /// <returns>
        /// A list of one USBDG event csv record, if successful; Exception (DYUF) if any failures occur 
        /// </returns>
        public static List<EdiSinkRecord> MapUsbdgEvent(dynamic sourceUsbdgMetadata)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EdiSinkRecord> sinkCsvLocationsRecords = new();
                JObject sourceJObject = (JObject)sourceUsbdgMetadata;
                sourceFile = DataTransformService.GetSourceFile(sourceJObject);

                UsbdgEventRecord sinkUsbdgDeviceRecord = new UsbdgEventRecord();

                // Grab the log header properties from the source metadata file
                var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
                foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
                {
                    if (log1.Value.Type != JTokenType.Array)
                    {
                        propName = log1.Key;
                        propValue = (string)log1.Value;

                        sourceHeaders.Add(log1.Key, log1.Value);
                        ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
                        if (log1.Key == "zutc_now" && log1.Value != null)
                        {
                            string strZutcNow = log1.Value.ToString();
                            if (strZutcNow != "")
                            {
                                DateTime? zutcNow = DateConverter.ConvertIso8601CompliantString(log1.Value.ToString());
                                ((UsbdgEventRecord)sinkUsbdgDeviceRecord).EDI_ZUTC_NOW_DATETIME = zutcNow;
                            }
                        }
                    }

                    // Load log record properties into csv record object
                    if (log1.Value.Type == JTokenType.Array && log1.Key == "records")
                    {
                        // Iterate each log record
                        foreach (JObject z in log1.Value.Children<JObject>())
                        {
                            // Load each log record property
                            foreach (JProperty prop in z.Properties())
                            {
                                propName = prop.Name;
                                propValue = (string)prop.Value;
                                ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, prop.Name, prop.Value);
                                if (propName == "ABST" && prop.Value != null)
                                {
                                    string strAbst = prop.Value.ToString();
                                    if (strAbst != "")
                                    {
                                        DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                        ((UsbdgEventRecord)sinkUsbdgDeviceRecord).EDI_ABST_DATETIME = emdAbsoluteTime;
                                    }
                                }
                            }
                        }
                    }
                }

                if (sinkUsbdgDeviceRecord.EDI_ZUTC_NOW_DATETIME == null)
                {
                    sinkUsbdgDeviceRecord.EDI_ZUTC_NOW_DATETIME = sinkUsbdgDeviceRecord.EDI_ABST_DATETIME;
                }

                sinkCsvLocationsRecords.Add(sinkUsbdgDeviceRecord);
                return sinkCsvLocationsRecords;
            }
            catch (Exception e)
            {
                // 2022.07.07 stringify null for BuildErrorVariableArrayList
                if (propValue == null)
                {
                    propValue = "null";
                }
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "DYUF", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        /// <summary>
        /// Maps raw Indigo V2 data log files to csv event records
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the logger data file into records ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceLogs">A list of deserilized logger data file objects</param>
        /// <param name="sourceEdiJob">A deserilized EDI job object holding the USBDG serial number and ABST</param>
        /// <returns>
        /// A list of CSV compatible Indigo V2 event records, if successful; Exception (HKTJ) if any failures occur 
        /// </returns>
        public static List<IndigoV2EventRecord> MapIndigoV2Events(List<dynamic> sourceLogs, EdiJob sourceEdiJob)
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
                    sourceFile = DataTransformService.GetSourceFile(sourceLogJObject);



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
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ESER", sourceEdiJob.UsbdgMetadata.ESER);
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ALRM", sourceEdiJob.UsbdgMetadata.ALRM);

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
                                        //Console.WriteLine("debug");
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
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "HKTJ", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        /// <summary>
        /// Maps raw Indigo V2 data log files to csv event records
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the logger data file into records ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceLogs">A list of deserilized logger data file objects</param>
        /// <param name="sourceEdiJob">A deserilized EDI job object holding the USBDG serial number and ABST</param>
        /// <returns>
        /// A list of CSV compatible Indigo V2 event records, if successful; Exception (HKTJ) if any failures occur 
        /// </returns>
        public static List<EmsEventRecord> MapEmsLoggerEvents(List<dynamic> sourceLogs, string loggerType, EdiJob sourceEdiJob)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EmsEventRecord> sinkCsvEventRecords = new();

                foreach (dynamic sourceLog in sourceLogs)
                {
                    JObject sourceLogJObject = (JObject)sourceLog;
                    sourceFile = DataTransformService.GetSourceFile(sourceLogJObject);

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
                                EmsEventRecord sinkCsvEventRecord = CreateNewEmsEventRecord(loggerType);
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ESER", sourceEdiJob.UsbdgMetadata.ESER);
                                ObjectManager.SetObjectValue(sinkCsvEventRecord, "ALRM", sourceEdiJob.UsbdgMetadata.ALRM);

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
                                        //Console.WriteLine("debug");
                                    }
                                    propValue = (string)prop.Value;
                                    ObjectManager.SetObjectValue(sinkCsvEventRecord, prop.Name, prop.Value);
                                }

                                sinkCsvEventRecords.Add((EmsEventRecord)sinkCsvEventRecord);
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
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "HKTJ", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        public static EmsEventRecord CreateNewEmsEventRecord(string loggerType)
        {
            if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.SL1.ToString())
            {
                return new Sl1EventRecord();
            } else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_V2.ToString())
            {
                return new IndigoV2EventRecord();
            } else
            {
                return null;
            }
                
        }

        /// <summary>
        /// Maps raw Indigo V2 data log and USBDG metadata files to csv location records
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the Indigo V2 data log and USBDG metadata files into location records ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceUsbdgMetadata">A list of deserilized logger data file objects</param>
        /// <param name="sourceEdiJob">A deserilized EDI job object holding the Indigo V2 logger serial number</param>
        /// <returns>
        /// A list of CSV compatible Indigo V2 location records, if successful; Exception (DVKA) if any failures occur 
        /// </returns>
        public static List<EdiSinkRecord> MapIndigoV2Locations(dynamic sourceUsbdgMetadata, EdiJob sourceEdiJob)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EdiSinkRecord> sinkCsvLocationsRecords = new();
                JObject sourceJObject = (JObject)sourceUsbdgMetadata;
                sourceFile = DataTransformService.GetSourceFile(sourceJObject);


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
                            ObjectManager.SetObjectValue(sinkCsvLocationsRecord, "LSER", sourceEdiJob.Logger.LSER);

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
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "DVKA", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

        /// <summary>
        /// Maps raw USBDG metadata files to csv usbdg location records
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the USBDG metadata files into location records ready for CSV serialization.
        /// </remarks>
        /// <param name="sourceUsbdgMetadata">A list of deserilized logger data file objects</param>
        /// <returns>
        /// A list of CSV compatible USBDG location records, if successful; Exception (DVKA) if any failures occur 
        /// </returns>
        public static List<EdiSinkRecord> MapUsbdgLocations(dynamic sourceUsbdgMetadata, EdiJob sourceEdiJob)
        {
            string propName = null;
            string propValue = null;
            string sourceFile = null;

            try
            {
                List<EdiSinkRecord> sinkCsvLocationsRecords = new();
                JObject sourceJObject = (JObject)sourceUsbdgMetadata;
                sourceFile = DataTransformService.GetSourceFile(sourceJObject);


                // Grab the log header properties from the source metadata file
                /*
                var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
                foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
                {
                    if (log1.Value.Type != JTokenType.Array)
                    {
                        sourceHeaders.Add(log1.Key, log1.Value);
                        //ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
                    }
                }
                */

                // Map csv record objects from source metadata file
                foreach (KeyValuePair<string, JToken> log2 in sourceJObject)
                {
                    // Load log record properties into csv record object
                    if (log2.Value.Type == JTokenType.Array && log2.Key == "zgps_data")
                    {
                        // Iterate each array
                        foreach (JObject z in log2.Value.Children<JObject>())
                        {
                            EdiSinkRecord sinkCsvLocationsRecord = new UsbdgLocationRecord();
                            ObjectManager.SetObjectValue(sinkCsvLocationsRecord, "ESER", sourceEdiJob.UsbdgMetadata.ESER);
                            //ObjectManager.SetObjectValue(sinkCsvLocationsRecord, "usb_id", sourceEdiJob.Logger.LSER);
                            ObjectManager.SetObjectValue(sinkCsvLocationsRecord, "EDI_SOURCE", sourceEdiJob.UsbdgMetadata.EDI_SOURCE);

                            // Load each log record property
                            foreach (JProperty prop in z.Properties())
                            {
                                propName = prop.Name;
                                propValue = (string)prop.Value;
                                ObjectManager.SetObjectValue(sinkCsvLocationsRecord, prop.Name, prop.Value);

                                if (propName == "zgps_abst")
                                {
                                    long zgps_abst_long = long.Parse(propValue);
                                    ((UsbdgLocationRecord)sinkCsvLocationsRecord).EDI_ZGPS_ABST_DATETIME = DateConverter.FromUnixTimeSeconds(zgps_abst_long);
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
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "DVKA", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
        }

    }
}
