using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class EdiAdfActivityResult
    {
        public DateTimeOffset? EventTime { get; set; }
        public string? PackageName { get; set; }
        public string? Status { get; set; }
        public string? ActivityName { get; set; }
        public string? ActivityType { get; set; }
        public string? PipelineName { get; set; }
        public string? ErrorCode { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
