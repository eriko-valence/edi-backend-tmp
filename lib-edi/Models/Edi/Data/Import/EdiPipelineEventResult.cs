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
    }
}
