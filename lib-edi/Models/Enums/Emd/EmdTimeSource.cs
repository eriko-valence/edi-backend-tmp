using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Emd
{
    public class EmdTimeSource
    {
        public enum Name
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            NONE,
            EMD_REPORT_METADATA,
            EMS_SYNC_FILENAME,
            EMD_REPORT_METADATA_FILENAME,
            UNKNOWN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
