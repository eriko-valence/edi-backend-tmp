using lib_edi_in_process.Models.Enums.Emd;

namespace lib_edi_in_process.Services.Ems
{
    public class EmsService
    {
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
    }
}
