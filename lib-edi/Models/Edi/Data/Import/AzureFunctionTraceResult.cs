using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class AzureFunctionTraceResult
    {
        public DateTime? EventTime { get; set; }
        public string? FilePackageName { get; set; }
        public string? OperationName { get; set; }
        public byte? SeverityLevel { get; set; }
        public string? LogMessage { get; set; }
        public string? LogMessageMd5 { get; set; }
    }
}
