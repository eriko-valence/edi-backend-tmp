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
                                if (prop.Name == "ADOP")
                                {
                                    var adatValue = prop.GetValue(csvEmsMetadata, null);
                                    if (adatValue != null)
                                    {
                                        DateTime adatDate = DateConverter.ConvertDateWithDashesString(adatValue.ToString());
                                        ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, adatDate);
                                    }
                                }
                                else
                                {
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.GetValue(csvEmsMetadata, null));
                                }
                            }
                            //Add collected event data to record
                            foreach (JProperty prop in z.Properties())
                            {
                                if (prop.Name == "ABST")
                                {
                                    DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, emdAbsoluteTime);
                                }
                                else
                                {
                                    ObjectManager.SetObjectValue(ref emsCsvRecord, prop.Name, prop.Value);
                                }
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
                UsbdgSimMetadata csvEmsMetadata = new UsbdgSimMetadata();
                foreach (KeyValuePair<string, JToken> x in JObjMetadataFile)
                {
                    if (x.Key == "ABST")
					{
                        DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(x.Value.ToString());
                        ObjectManager.SetObjectValue(ref csvEmsMetadata, x.Key, emdAbsoluteTime);
                    } else
					{
                        ObjectManager.SetObjectValue(ref csvEmsMetadata, x.Key, x.Value);
                    }   
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
                            UsbdgSimCsvRecordDto emsCsvRecord = new UsbdgSimCsvRecordDto();
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
                                    DateTime? emdAbsoluteTime = DateConverter.ConvertIso8601CompliantString(prop.Value.ToString());
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
