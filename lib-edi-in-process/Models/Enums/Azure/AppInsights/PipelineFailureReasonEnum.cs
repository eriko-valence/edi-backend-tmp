using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Enums.Azure.AppInsights
{
    public class PipelineFailureReasonEnum
    {
        public enum Name
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            NONE,
            UNKNOWN_EXCEPTION,
            UNSUPPORTED_DATA_LOGGER,
            UNKNOWN_REPORT_PACKAGE,
            MISSING_CE_SUBJECT_HEADER,
            HTTP_STATUS_CODE_ERROR,
            UNSUPPORTED_EXTENSION,
            UNSUPPORTED_EMS_DEVICE,
            UNKNOWN_FILE_PACKAGE,
            WARN_INVALID_JSON,
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
