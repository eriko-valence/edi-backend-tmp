using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class SendGridEdiFailedJob
    {
        public string FilePackageName { get; set; }
        public DateTime? JobStartTime { get; set; }
        public string DataLoggerType { get; set; }
        public string PipelineFailureLocation { get; set; }
        public string ErrorCode { get; set; } // NHGH-3056 2023.08.17 1640 daily edi status email report
        public string EmdType { get; set; } // NHGH-3057 2023.08.18 0944 daily edi status email report
    }
}
