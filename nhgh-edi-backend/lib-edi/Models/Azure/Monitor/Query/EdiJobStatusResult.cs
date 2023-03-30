using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.Monitor.Query
{
    public class EdiJobStatusResult
    {
        public DateTimeOffset TimeStamp { get; set; }
        public string? fileName { get; set; }
        public DateTime? BlobTimeStart { get; set; }
        public DateTime? ProviderSuccessTime { get; set; }
        public DateTime? ConsumerSuccessTime { get; set; }
        public DateTime? TransformSuccessTime { get; set; }
        public DateTime? SQLSuccessTime { get; set; }
        public TimeSpan? Duration { get; set; }
    }
}
