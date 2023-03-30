using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Edi.Data.Import
{
    public class EdiJobImportFunctionEnum
    {
        public enum Name
        {
            UNKNOWN,
            OTA_EVENTS,
            CFD50_MFOX_EXPORT,
            EDI_EVENTS,
            IMPORT_EVENTS,
            EDI_MAINT,
            EDI_MAINT_IMPORTER,
            EDI_MAINT_IMPORTER_MONITOR,
            EDI_MAINT_EMAIL_REPORT
        }
    }
}
