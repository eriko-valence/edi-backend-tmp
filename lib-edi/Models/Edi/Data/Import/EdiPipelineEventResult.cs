using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class EdiPipelineEventResult
    {
        public DateTime? EventTime { get; set; }
        public string? FileName { get; set; }
        public string? PipelineEvent { get; set; }
        public string? PipelineStage { get; set; }
        public string? PipelineFailureReason { get; set; }
        public string? PipelineFailureType { get; set; }
        public string? DataLoggerType { get; set; }
        public string? ExceptionMessage { get; set; }
		public string? ErrorCode { get; set; } // NHGH-3056 2023.08.17 1308 add error code to hourly edi import jobs
        public string? EmdType { get; set; } // NHGH-3057 2023.08.18 0907 add emd type to hourly edi import job
	}
}
