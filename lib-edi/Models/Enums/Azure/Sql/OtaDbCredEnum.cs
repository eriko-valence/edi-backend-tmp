using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Azure.Sql
{
    public class OtaDbCredEnum
    {
        public enum Name
        {
            UNKNOWN,
            OTA_DB_MFOX_READER,
            OTA_DB_OTA_CFD50_IMPORTER,
            OTA_DB_OTA_APPINSIGHTS_IMPORTER,
            OTA_DB_DATA_IMPORTER_IMPORTER,
            EDI_LAW_IMPORTER
        }
    }
}
