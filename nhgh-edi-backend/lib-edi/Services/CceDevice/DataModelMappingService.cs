using lib_edi.Helpers;
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
        public static List<Cfd50CsvDataRowDto> MapMetaFridgeLogFileRecords(Cfd50JsonDataFileDto metaFridgeLogFile)
        {
            try
            {
                List<Cfd50CsvDataRowDto> metaFridgeCsvRows = new List<Cfd50CsvDataRowDto>();

                foreach (Cfd50JsonDataFileRecordDto metaFridgeLogRecord in metaFridgeLogFile.records)
                {
                    Cfd50CsvDataRowDto metaFridgeCsvRow = new Cfd50CsvDataRowDto();

                    metaFridgeCsvRow.AMFR = metaFridgeLogFile.AMFR;
                    metaFridgeCsvRow.AMOD = metaFridgeLogFile.AMOD;
                    metaFridgeCsvRow.APQS = metaFridgeLogFile.APQS;
                    metaFridgeCsvRow.ASER = metaFridgeLogFile.ASER;
                    metaFridgeCsvRow.AID = metaFridgeLogFile.AID;
                    metaFridgeCsvRow.ADAT = metaFridgeLogFile.ADAT;
                    metaFridgeCsvRow.CID = metaFridgeLogFile.CID;
                    metaFridgeCsvRow.FID = metaFridgeLogFile.FID;
                    metaFridgeCsvRow.LOC = metaFridgeLogFile.LOC;

                    metaFridgeCsvRow.ABST = metaFridgeLogRecord.ABST;
                    metaFridgeCsvRow.TAMB = metaFridgeLogRecord.TAMB;
                    metaFridgeCsvRow.TCLD = metaFridgeLogRecord.TCLD;
                    metaFridgeCsvRow.TVC = metaFridgeLogRecord.TVC;
                    metaFridgeCsvRow.CMPR = ConvertStringBitToBool(metaFridgeLogRecord.CMPR);
                    metaFridgeCsvRow.SVA = metaFridgeLogRecord.SVA;
                    metaFridgeCsvRow.EVDC = metaFridgeLogRecord.EVDC;
                    metaFridgeCsvRow.CDRW = metaFridgeLogRecord.CDRW;
                    metaFridgeCsvRow.DOOR = ConvertStringBitToBool(metaFridgeLogRecord.DOOR);
                    metaFridgeCsvRow.HOLD = metaFridgeLogRecord.HOLD;
                    metaFridgeCsvRow.BEMD = metaFridgeLogRecord.BEMD;
                    metaFridgeCsvRow.TCON = metaFridgeLogRecord.TCON;
                    metaFridgeCsvRow.CMPS = metaFridgeLogRecord.CMPS;
                    metaFridgeCsvRow.CSOF = metaFridgeLogRecord.CSOF;

                    metaFridgeCsvRows.Add(metaFridgeCsvRow);
                }

                return metaFridgeCsvRows;
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
        public static List<Cfd50CsvDataRowDto> MapMetaFridgeLogs(List<Cfd50JsonDataFileDto> metaFridgeLogFiles)
        {
            List<Cfd50CsvDataRowDto> metaFridgeCsvRow = new List<Cfd50CsvDataRowDto>();
            foreach (Cfd50JsonDataFileDto metaFridgeLogFile in metaFridgeLogFiles)
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
        public static List<EmsCsvRecordDto> MapUsbdgLogs(List<dynamic> usbdgLogFiles, dynamic reportFile)
        {


            List<EmsCsvRecordDto> usbdbLogCsvRows = new List<EmsCsvRecordDto>();
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
        public static List<EmsCsvRecordDto> MapUsbdgLogFileRecords(dynamic emdLogFile, dynamic metadataFile)
        {
            try
            {
                List<EmsCsvRecordDto> usbdbCsvRecords = new List<EmsCsvRecordDto>();

                /* ######################################################################
                 * # Cast dynamic data file objects to JSON
                 * ###################################################################### */
                JObject JObjLoggerDataFile = (JObject)emdLogFile;
                JObject JObjMetadataFile = (JObject)metadataFile;

                /* ######################################################################
                 * # Merge EMD and logger metadata
                 * ###################################################################### */
                UsbdgCsvMetadataDto csvEmsMetadata = new UsbdgCsvMetadataDto();
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
                            EmsCsvRecordDto emsCsvRecord = new EmsCsvRecordDto();
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
