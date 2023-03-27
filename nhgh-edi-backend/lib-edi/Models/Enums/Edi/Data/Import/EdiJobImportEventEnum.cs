using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Edi.Data.Import
{
    public class EdiJobImportEventEnum
    {
        public enum Name
        {
            UNKNOWN,
            EDI_IMPORT_JOB_RESULT,
            EDI_IMPORT_EXCEPTION,
            EDI_SEND_EMAIL_REPORT_RESULT
        }
    }
}
