using lib_edi.Helpers;
using lib_edi.Models.Domain.CceDevice;
using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Loggers;
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
        public static List<EmsCfd50CsvRecordDto> MapMetaFridgeLogFileRecords(dynamic metaFridgeLogFile)
        {
            try
            {

                List<EmsCfd50CsvRecordDto> usbdbCsvRecords = new List<EmsCfd50CsvRecordDto>();

                /* ######################################################################
                 * # Cast dynamic data file objects to JSON
                 * ###################################################################### */
                JObject JObjLoggerDataFile = (JObject)metaFridgeLogFile;

                /* ######################################################################
                 * # Merge EMD and logger metadata
                 * ###################################################################### */
                EmsCfd50Metadata csvEmsMetadata = new EmsCfd50Metadata();
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
                            EmsCfd50CsvRecordDto emsCsvRecord = new EmsCfd50CsvRecordDto();
                            //Add metadata to each collected event record
                            foreach (PropertyInfo prop in csvEmsMetadata.GetType().GetProperties())
                            {
                                //if (prop.PropertyType.Name)

                                if (prop.Name == "ADAT")
                                {
                                    DateTime adatDate = DateConverter.ConvertDateWithDashesString(prop.GetValue(csvEmsMetadata, null).ToString());
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, adatDate);
                                } else
								{
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.GetValue(csvEmsMetadata, null));
                                }

                                
                            }
                            //Add collected event data to record
                            foreach (JProperty prop in z.Properties())
                            {
                                if (prop.Name == "ABST")
                                {
                                    DateTime emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, emdAbsoluteTime);
                                }
                                else
                                {
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.Value);
                                }
                            }
                            usbdbCsvRecords.Add(emsCsvRecord);
                        }
                    }
                }
                return usbdbCsvRecords;

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
        public static List<EmsCfd50CsvRecordDto> MapMetaFridgeLogs(List<dynamic> metaFridgeLogFiles)
        {
            List<EmsCfd50CsvRecordDto> metaFridgeCsvRow = new List<EmsCfd50CsvRecordDto>();
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
        public static List<EmsUsbdgSimCsvRecordDto> MapUsbdgLogs(List<dynamic> usbdgLogFiles, dynamic reportFile)
        {


            List<EmsUsbdgSimCsvRecordDto> usbdbLogCsvRows = new List<EmsUsbdgSimCsvRecordDto>();
            foreach (dynamic usbdbLog in usbdgLogFiles)
            {
                usbdbLogCsvRows.AddRange(MapUsbdgLogFileRecords(usbdbLog, reportFile));
            }

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
        public static List<EmsUsbdgSimCsvRecordDto> MapUsbdgLogFileRecords(dynamic emdLogFile, dynamic metadataFile)
        {
            try
            {
                List<EmsUsbdgSimCsvRecordDto> usbdbCsvRecords = new List<EmsUsbdgSimCsvRecordDto>();

                /* ######################################################################
                 * # Cast dynamic data file objects to JSON
                 * ###################################################################### */
                JObject JObjLoggerDataFile = (JObject)emdLogFile;
                JObject JObjMetadataFile = (JObject)metadataFile;

                /* ######################################################################
                 * # Merge EMD and logger metadata
                 * ###################################################################### */
                EmsUsbdgSimMetadata csvEmsMetadata = new EmsUsbdgSimMetadata();
                foreach (KeyValuePair<string, JToken> x in JObjMetadataFile)
                {
                    ObjectManager.SetObjectValue(ref csvEmsMetadata, x.Key, x.Value);
                }
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
                            EmsUsbdgSimCsvRecordDto emsCsvRecord = new EmsUsbdgSimCsvRecordDto();
                            //Add metadata to each collected event record
                            foreach (PropertyInfo prop in csvEmsMetadata.GetType().GetProperties())
                            {
                                ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.GetValue(csvEmsMetadata, null));
                            }
                            //Add collected event data to record
                            foreach (JProperty prop in z.Properties())
                            {
                                if (prop.Name == "ABST")
								{
                                    DateTime emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, emdAbsoluteTime);

                                } else
								{
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.Value);
                                }
                                
                            }
                            usbdbCsvRecords.Add(emsCsvRecord);
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

    }
}
