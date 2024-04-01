using Azure.Storage.Blobs.Models;
using lib_edi.Models.Edi;
using lib_edi.Models.Enums.Emd;
using lib_edi.Services.Loggers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Storage.Blob;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lib_edi.Services.Ems
{




    public class EmsService
    {

        // cross references logger models (LMOD json property) to well known logger model names 
        private static Dictionary<string, DataLoggerModelsEnum.Name> emsLoggerModelsSupported;
        

        /// <summary>
        /// Initializes supported EMS logger models dictionary
        /// </summary>
        /// <remarks>
        /// NHGH-2700 (2022.11.16) - Added supported EMS logger models dictionary initialization
        /// </remarks>
        public static void Initialize()
        {
            emsLoggerModelsSupported = new()
            {
                { "Indigo_Lid_201", DataLoggerModelsEnum.Name.INDIGO_V2 },
                { "L201", DataLoggerModelsEnum.Name.INDIGO_V2 },
                { "Indigo_Charger_C1", DataLoggerModelsEnum.Name.INDIGO_CHARGER_V2 },
                { "Demo EMS Logger", DataLoggerModelsEnum.Name.SL1 }
            };
        }

        /// <summary>
        /// Validate data logger type is supported by ETL pipeline
        /// </summary>
        /// <param name="loggerType">Blob path in string format</param>
        /// <remarks>
        /// NHGH-2698 (2022.11.16) - Added generic "ems" as supported logger type
        /// </remarks>
        public static bool ValidateCceDeviceType(string loggerType)
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
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.EMS.ToString())
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
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.INDIGO_CHARGER_V2.ToString())
                {
                    return DataLoggerTypeEnum.Name.INDIGO_CHARGER_V2;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.SL1.ToString())
                {
                    return DataLoggerTypeEnum.Name.SL1;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.NO_LOGGER.ToString())
                {
                    return DataLoggerTypeEnum.Name.NO_LOGGER;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.EMS.ToString())
                {
                    return DataLoggerTypeEnum.Name.EMS;
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
		/// Correlates string to an EMD type
		/// </summary>
		/// <param name="loggerType">string to check</param>
		public static EmdEnum.Name GetEmdType(string loggerType)
        {
            if (loggerType != null)
            {
                if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.EMS.ToString())
                {
                    return EmdEnum.Name.USBDG;
                }
                else if (loggerType.ToUpper() == EmdEnum.Name.VARO.ToString())
                {
                    return EmdEnum.Name.VARO;
                }
                else if (loggerType.ToUpper() == DataLoggerTypeEnum.Name.NO_LOGGER.ToString())
                {
					return EmdEnum.Name.USBDG;
				}
				else
                {
                    return EmdEnum.Name.UNKNOWN;
                }
            }
            else
            {
                return EmdEnum.Name.UNKNOWN;
            }
        }

        /// <summary>
        /// Returns EMS data logger model string associated with an EMS log LMOD property value
        /// </summary>
        /// <param name="loggerModel">EMS LMOD property value</param>
        public static EmsLoggerModelCheckResult GetEmsLoggerModelFromEmsLogLmodProperty(string loggerModel)
        {
            if (emsLoggerModelsSupported == null)
            {
                Initialize();
            }

            EmsLoggerModelCheckResult result = new();
            DataLoggerModelsEnum.Name wellKnownLoggerModel = DataLoggerModelsEnum.Name.UNKNOWN;
            bool isLoggerModelSupported = false;
            if (loggerModel != null)
            {
                // is this LMOD a supported logger model? 
                if (emsLoggerModelsSupported.ContainsKey(loggerModel))
                {
                    emsLoggerModelsSupported.TryGetValue(loggerModel, out wellKnownLoggerModel);
                    isLoggerModelSupported = true;
                }
            }
            result.LMOD = loggerModel;
            result.LoggerModelEnum = wellKnownLoggerModel;
            result.IsSupported = isLoggerModelSupported;
            return result;
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
					/*
                     * NHGH-3059 2023.08.11 0858 CURRENT_DATA file normally exists, but in rare scenarios it can be
                     * missing. For example, DATA files are archived every 60 days. If your logger is in standby mode, 
                     * it will not have a CURRENT_DATA record in memory for 24 hours. As a result, the CURRENT_DATA 
                     * file can be missing from a report package. This  is a rare, but valid scenario. Therefore a USBDG
                     * collected report package can have only a metadata file and a SYNC file. 
                     */
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
            if (EmsService.IsThisEmsDataFile(blobName))
            {
                result = true;
            }
            else if (EmsService.IsThisEmsCurrentDataFile(blobName))
            {
                result = true;
            }
            else if (EmsService.IsThisEmsSyncDataFile(blobName))
            {
                result = true;
            }
            return result;
        }

        /// <summary>
        /// Checks if blob name is an EMS logger data file
        /// </summary>
        /// <param name="blobName">blob name </param>
        /// <returns>
        /// True if yes; False if no
        /// </returns>
        public static bool IsThisEmsDataFile(string blobName)
        {
            bool result = false;
            if (Path.GetExtension(blobName) == ".json") {
                if ((!blobName.Contains("_CURRENT_DATA_")) && (blobName.Contains("_DATA_")))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if blob name is an EMS logger current data file
        /// </summary>
        /// <param name="blobName">blob name </param>
        /// <returns>
        /// True if yes; False if no
        /// </returns>
        public static bool IsThisEmsCurrentDataFile(string blobName)
        {
            bool result = false;
            if (Path.GetExtension(blobName) == ".json")
            {
                if (blobName.Contains("_CURRENT_DATA_"))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Checks if blob name is an EMS logger sync file
        /// </summary>
        /// <param name="blobName">blob name </param>
        /// <returns>
        /// True if yes; False if no
        /// </returns>
        public static bool IsThisEmsSyncDataFile(string blobName)
        {
            bool result = false;
            if (Path.GetExtension(blobName) == ".json")
            {
                if (blobName.Contains("_SYNC_"))
                {
                    result = true;
                }
            }
            return result;
        }

        /// <summary>
        /// Looks for an EMS SYNC file name regular expression match
        /// </summary>
        /// <param name="name">report file name</param>
        /// <remarks>
        /// nhgh-2819 2023-03-09 1755 Added function
        /// </remarks>
        /// <returns>
        /// Regular expresssion match result
        /// </returns>
        public static Match IsThisEmsSyncFile(string name)
        {
            string logFileNamePattern = "([A-Za-z0-9]+)_SYNC_([A-Za-z0-9]+)_([A-Za-z0-9]+)\\.json";
            Regex r = new Regex(logFileNamePattern);
            Match m = r.Match(name);
            return m;
        }
    }
}
