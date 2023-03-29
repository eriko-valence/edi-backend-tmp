using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Edi.Data.Import
{
    public class EdiMaintJobEventEnum
    {
        public enum Name
        {
            UNKNOWN,
            EDI_IMPORTER_RESULT,
            EDI_IMPORTER_MONITOR_RESULT,
            EDI_IMPORTER_MONITOR,
            EDI_IMPORTER_EXCEPTION,
            EDI_STATUS_EMAIL_RESULT
        }
    }
}
