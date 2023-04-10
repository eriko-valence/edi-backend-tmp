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
        public DateTime? BlobTimeStart { get; set; }
        public string DataLoggerType { get; set; }
        public string PipelineFailureLocation { get; set; }
    }
}
