using lib_edi.Models.Dto.CceDevice.Csv;
using lib_edi.Models.Dto.Loggers;
using lib_edi.Services.Errors;
using System;
using System.Collections.Generic;
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
        public static List<UsbdgCsvDataRowDto> MapUsbdgLogs(List<UsbdgJsonDataFileDto> usbdgLogFiles, UsbdgJsonReportFileDto reportFile)
        {
            List<UsbdgCsvDataRowDto> usbdbLogCsvRows = new List<UsbdgCsvDataRowDto>();
            foreach (UsbdgJsonDataFileDto usbdbLog in usbdgLogFiles)
            {
                usbdbLogCsvRows.AddRange(MapUsbdgLogFileRecords(usbdbLog, reportFile));
            }

            return usbdbLogCsvRows;
        }


        /// <summary>
        /// Maps raw USBDG log file records to csv compatible format
        /// </summary>
        /// <remarks>
        /// This mapping denormalizes the USBDG log file to get the records into a csv compatible format.
        /// </remarks>
        /// <param name="usbdgLog">A deserilized USBDG json log file</param>
        /// <returns>
        /// A list of csv compatible USBDG log file records, if successful; Exception (D39Y) if any failures occur 
        /// </returns>
        public static List<UsbdgCsvDataRowDto> MapUsbdgLogFileRecords(UsbdgJsonDataFileDto usbdgLog, UsbdgJsonReportFileDto reportFile)
        {
            try
            {
                List<UsbdgCsvDataRowDto> usbdbCsvRows = new List<UsbdgCsvDataRowDto>();
             
                foreach (UsbdgJsonReportFileRecordDto usbdgLogRecord in usbdgLog.records)
                {
                    UsbdgCsvDataRowDto csvEmsLogRecord = new UsbdgCsvDataRowDto();
                    csvEmsLogRecord.ABST = reportFile.ABST;
                    csvEmsLogRecord.ADOP = usbdgLog.ADOP;
                    csvEmsLogRecord.AID = reportFile.AID;
                    csvEmsLogRecord.AMFR = usbdgLog.AMFR;
                    csvEmsLogRecord.AMOD = usbdgLog.AMOD;
                    csvEmsLogRecord.APQS = usbdgLog.APQS;
                    csvEmsLogRecord.ASER = usbdgLog.ASER;
                    csvEmsLogRecord.CDAT = usbdgLog.CDAT;
                    csvEmsLogRecord.CID = reportFile.CID;
                    csvEmsLogRecord.CNAM = usbdgLog.CNAM;
                    csvEmsLogRecord.CSER = usbdgLog.CSER;
                    csvEmsLogRecord.CSOF = usbdgLog.CSOF;
                    csvEmsLogRecord.DNAM = reportFile.DNAM;
                    csvEmsLogRecord.EDOP = reportFile.EDOP;
                    csvEmsLogRecord.EID = reportFile.EID;
                    csvEmsLogRecord.EMFR = reportFile.EMFR;
                    csvEmsLogRecord.EMOD = reportFile.EMOD;
                    csvEmsLogRecord.EMSV = reportFile.EMSV;
                    csvEmsLogRecord.EPQS = reportFile.EPQS;
                    csvEmsLogRecord.ESER = reportFile.ESER;
                    csvEmsLogRecord.FID = reportFile.FID;
                    csvEmsLogRecord.FNAM = reportFile.FNAM;
                    csvEmsLogRecord.LDOP = usbdgLog.LDOP;
                    csvEmsLogRecord.LID = usbdgLog.LID;
                    csvEmsLogRecord.LMFR = usbdgLog.LMFR;
                    csvEmsLogRecord.LMOD = usbdgLog.LMOD;
                    csvEmsLogRecord.LPQS = usbdgLog.LPQS;
                    csvEmsLogRecord.LSER = usbdgLog.LSER;
                    csvEmsLogRecord.LSV = usbdgLog.LSV;
                    csvEmsLogRecord.RNAM = reportFile.RNAM;
                    csvEmsLogRecord.ALRM = reportFile.ALRM;
                    csvEmsLogRecord.EERR = reportFile.EERR;

                    // json record properties
                    csvEmsLogRecord.ACCD = usbdgLogRecord.ACCD;
                    csvEmsLogRecord.ACSV = usbdgLogRecord.ACSV;
                    csvEmsLogRecord.BEMD = usbdgLogRecord.BEMD;
                    csvEmsLogRecord.BLOG = usbdgLogRecord.BLOG;
                    csvEmsLogRecord.CMPR = usbdgLogRecord.CMPR;
                    csvEmsLogRecord.CMPS = usbdgLogRecord.CMPS;
                    csvEmsLogRecord.DCCD = usbdgLogRecord.DCCD;
                    csvEmsLogRecord.DCSV = usbdgLogRecord.DCSV;
                    csvEmsLogRecord.DORF = usbdgLogRecord.DORF;
                    csvEmsLogRecord.DORV = usbdgLogRecord.DORV;
                    
                    csvEmsLogRecord.FANS = usbdgLogRecord.FANS;
                    csvEmsLogRecord.HAMB = usbdgLogRecord.HAMB;
                    csvEmsLogRecord.HCOM = usbdgLogRecord.HCOM;
                    csvEmsLogRecord.HOLD = usbdgLogRecord.HOLD;
                    csvEmsLogRecord.LAT = usbdgLogRecord.LAT;
                    csvEmsLogRecord.LERR = usbdgLogRecord.LERR;
                    csvEmsLogRecord.LNG = usbdgLogRecord.LNG;
                    csvEmsLogRecord.MSW = usbdgLogRecord.MSW;
                    csvEmsLogRecord.RELT = usbdgLogRecord.RELT;
                    csvEmsLogRecord.RTCW = usbdgLogRecord.RTCW;
                    csvEmsLogRecord.SVA = usbdgLogRecord.SVA;
                    csvEmsLogRecord.TAMB = usbdgLogRecord.TAMB;
                    csvEmsLogRecord.TCON = usbdgLogRecord.TCON;
                    csvEmsLogRecord.TFRZ = usbdgLogRecord.TFRZ;
                    csvEmsLogRecord.TPCB = usbdgLogRecord.TPCB;
                    csvEmsLogRecord.TVC = usbdgLogRecord.TVC;
                    csvEmsLogRecord.Source = usbdgLog._SOURCE;
                    usbdbCsvRows.Add(csvEmsLogRecord);
                }

                return usbdbCsvRows;
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
