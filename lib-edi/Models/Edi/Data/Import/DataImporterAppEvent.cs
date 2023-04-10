using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class DataImporterAppEvent
    {
        public DateTime? EventTime { get; set; }
        public int? EventsLoaded { get; set; }
        public int? EventsQueried { get; set; }
        public int? EventsFailed { get; set; }
        public int? EventsExcluded { get; set; }
        public string? JobStatus { get; set; }
        public string? JobName { get; set; }
        public string? JobException { get; set; }
    }
}
