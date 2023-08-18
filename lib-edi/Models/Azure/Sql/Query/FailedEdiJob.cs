using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.Sql.Query
{
    public class FailedEdiJob
    {
        public string FilePackageName { get; set; }
        public string ESER { get; set; }
        public string ErrorCode { get; set; } // NHGH-3056 2023.08.17 1503 daily edi status email report
		public string EmdType { get; set; } // NHGH-3056 2023.08.18 1503 daily edi status email report
		public DateTime? JobStartTime { get; set; }
        public DateTime? ProviderSuccessTime { get; set; }
        public DateTime? ConsumerSuccessTime { get; set; }
        public DateTime? TransformSuccessTime { get; set; }
        public DateTime? SQLSuccessTime { get; set; }
        public int DurationSecs { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        public string DataLoggerType { get; set; }
        public string PipelineFailureLocation { get; set; }
    }
}
