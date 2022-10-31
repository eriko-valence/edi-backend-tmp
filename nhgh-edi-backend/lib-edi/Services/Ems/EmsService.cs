using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Loggers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Ems
{
    public class EmsService
    {
        /// <summary>
        /// Validate data logger type is supported by ETL pipeline
        /// </summary>
        /// <param name="loggerType">Blob path in string format</param>
        public static bool ValidateLoggerType(string loggerType)
        {
            bool result = false;

            if (loggerType != null)
            {
                if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.USBDG_DATASIM.ToString())
                {
                    result = true;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.CFD50.ToString())
                {
                    result = true;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_V2.ToString())
                {
                    result = true;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.SL1.ToString())
                {
                    result = true;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.NO_LOGGER.ToString())
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Returns data logger type enum
        /// </summary>
        /// <param name="loggerType">Blob path in string format</param>
        public static DataLoggerTypeEnum.Name GetDataLoggerType(string loggerType)
        {
            if (loggerType != null)
            {
                if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.USBDG_DATASIM.ToString())
                {
                    return DataLoggerTypeEnum.Name.USBDG_DATASIM;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.CFD50.ToString())
                {
                    return DataLoggerTypeEnum.Name.CFD50;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_V2.ToString())
                {
                    return DataLoggerTypeEnum.Name.INDIGO_V2;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.SL1.ToString())
                {
                    return DataLoggerTypeEnum.Name.SL1;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.NO_LOGGER.ToString())
                {
                    return DataLoggerTypeEnum.Name.NO_LOGGER;
                }
                else
                {
                    return DataLoggerTypeEnum.Name.UNKNOWN;
                }
            } else
            {
                return DataLoggerTypeEnum.Name.UNKNOWN;
            }
        }

        /// <summary>
        /// An EMS file package contains one or more EMS files and a USBDG report file
        /// </summary>
        /// <param name="logDirectoryBlobs">Full list of blobs </param>
        /// <returns>
        /// Return true if yes; false if no
        /// </returns>
        public static bool IsFilePackageContentsEms(IEnumerable<IListBlobItem> logDirectoryBlobs)
        {
            bool result = false;
            bool emsCompliantLogFilesFound = false;
            bool usbdgMetaDataFound = false;
            if (logDirectoryBlobs != null)
            {
                foreach (CloudBlockBlob logBlob in logDirectoryBlobs)
                {
                    string fileExtension = Path.GetExtension(logBlob.Name);
                    if (IsFileFromEmsLogger(logBlob.Name))
                    {
                        emsCompliantLogFilesFound = true;
                    }

                    if (UsbdgDataProcessorService.IsFileUsbdgReportMetadata(logBlob.Name))
                    {
                        usbdgMetaDataFound = true;
                    }
                }
            }

            if (emsCompliantLogFilesFound && usbdgMetaDataFound)
            {
                result = true;
            }

            return result;
        }

        public static bool IsLoggerTypeIndigoV2(string loggerType)
        {
            bool result = false;
            if (loggerType != null)
            {
                if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_V2.ToString())
                {
                    result = true;
                }
            }
            return result;
        }

        public static bool IsLoggerTypeStationaryLogger(string loggerType)
        {
            bool result = false;
            if (loggerType != null)
            {
                if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.SL1.ToString())
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if blob name is from an EMS logger
        /// </summary>
        /// <param name="blobName">blob name </param>
        /// <returns>
        /// True if yes; False if no
        /// </returns>
        public static bool IsFileFromEmsLogger(string blobName)
        {
            bool result = false;
            if ((Path.GetExtension(blobName) == ".json") && (blobName.Contains("DATA") || blobName.Contains("CURRENT")))
            {
                result = true;
            }
            return result;
        }
    }
}
