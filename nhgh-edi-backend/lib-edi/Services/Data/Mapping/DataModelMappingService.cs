using lib_edi.Helpers;
using lib_edi.Models.Csv;
using lib_edi.Models.Domain.CceDevice;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Models.Edi;
using lib_edi.Models.Emd.Csv;
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
        /// Maps raw MetaFridge log file records to csv compatible format
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the MetaFridge log file to get the records into a csv compatible format.
        /// </remarks>
        /// <param name="metaFridgeLogFile">A deserilized Metafridge json log file</param>
        /// <returns>
		/// A list of csv compatible MetaFridge log file records if successful; Exception (3B6U) if any there are any failures
		/// </returns>
        public static List<Cfd50CsvRecordDto> MapMetaFridgeLogFileRecords(dynamic metaFridgeLogFile)
        {
            try
            {

                List<Cfd50CsvRecordDto> cfd50CsvRecords = new List<Cfd50CsvRecordDto>();

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
        /// Maps raw Metafridge log files to csv compatible format
        /// </summary>
        /// <remarks>
        /// This function iterates a list of MetaFridge log files to map these records. Records from each 
        /// log file are consolidated.  
        /// </remarks>
        /// <param name="metaFridgeLogFiles">A list of deserilized MetaFridge json log files</param>
        /// <returns>
        /// A consolidated list of csv compatible MetaFridge log file records if successful; Exception (3B6U) if any there are any failures
        /// </returns>
        public static List<Cfd50CsvRecordDto> MapMetaFridgeLogs(List<dynamic> metaFridgeLogFiles)
        {
            List<Cfd50CsvRecordDto> metaFridgeCsvRow = new List<Cfd50CsvRecordDto>();
            foreach (dynamic metaFridgeLogFile in metaFridgeLogFiles)
            {
                metaFridgeCsvRow = DataModelMappingService.MapMetaFridgeLogFileRecords(metaFridgeLogFile);
            }

            return metaFridgeCsvRow;
        }

        /// <summary>
        /// Maps raw USBDG log files to denormalized, csv compatible format
        /// </summary>
        /// <remarks>
        /// This function iterates a list of USBDG log files to map these records. Records
        /// from all log files are consolidated into one list of denormalized records.
        /// </remarks>
        /// <param name="usbdgLogFiles">A list of deserilized USBDG json log file</param>
        /// <returns>
        /// A consolidated list of csv USBDG MetaFridge log file records  if successful; Exception (D39Y) if any failures occur 
        /// </returns>
        public static List<UsbdgSimCsvRecordDto> MapUsbdgLogs(List<dynamic> usbdgLogFiles, dynamic reportFile)
        {


            List<UsbdgSimCsvRecordDto> usbdbLogCsvRows = new List<UsbdgSimCsvRecordDto>();
            /*
            foreach (dynamic usbdbLog in usbdgLogFiles)
            {
                usbdbLogCsvRows.AddRange(MapUsbdgLogFileRecords(usbdbLog, reportFile));
            }
            */

            return usbdbLogCsvRows;
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
                    else
					{
                        //ObjectManager.SetObjectValue(ref usbdgSimEmdMetadata, x.Key, x.Value);
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
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "D39Y", null));
            }

        }

        /// <summary>
        /// Converts string bit value ("0" or "1") to boolean equiavlent
        /// </summary>
        /// <param name="bitValue">Bit value represented as string ("0" or "1")</param>
        /// <returns>
        /// Boolean equivalent of bit value string
        /// </returns>

        private static bool ConvertStringBitToBool(string bitValue)
        {
            return (bitValue == "1");
        }

        private static string BuildEmdKeyName(string s)
		{
            return $"EMD_{s}";
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

                EdiSinkRecord sinkUsbdgDeviceRecord = new UsbdgEventRecord();

                // Grab the log header properties from the source metadata file
                var sourceHeaders = new ExpandoObject() as IDictionary<string, Object>;
                foreach (KeyValuePair<string, JToken> log1 in sourceJObject)
                {
                    if (log1.Value.Type != JTokenType.Array)
                    {
                        sourceHeaders.Add(log1.Key, log1.Value);
                        ObjectManager.SetObjectValue(sinkUsbdgDeviceRecord, log1.Key, log1.Value);
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
                                if (propName == "ABST")
                                {
                                    DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ((UsbdgEventRecord)sinkUsbdgDeviceRecord).EDI_ABST_DATETIME = emdAbsoluteTime;
                                    Console.WriteLine("debug");
                                }
                            }
                        }
                    }
                }




                sinkCsvLocationsRecords.Add(sinkUsbdgDeviceRecord);
                return sinkCsvLocationsRecords;
            }
            catch (Exception e)
            {
                throw new Exception(EdiErrorsService.BuildExceptionMessageString(e, "DYUF", EdiErrorsService.BuildErrorVariableArrayList(propName, propValue, sourceFile)));
            }
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

    }
}
