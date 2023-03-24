using lib_edi.Models.Enums.Edi.Data.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class OtaImportJobStats
    {
        [JsonPropertyName("job_name")]
        public OtaJobImportFunctionEnum.Name OtaJobName { get; set; }
        [JsonPropertyName("job_result")]
        public OtaJobImportStatusNameEnum.Name OtaJobStatus { get; set; }
        [JsonPropertyName("job_event_type")]
        public OtaJobImportEventEnum.Name OtaJobEventType { get; set; }
        [JsonPropertyName("job_exception")]
        public string ExceptionMessage { get; set; }
        [JsonPropertyName("events_queried")]
        public int Queried { get; set; }
        [JsonPropertyName("events_loaded")]
        public int Loaded { get; set; }
        [JsonPropertyName("events_excluded")]
        public int Skipped { get; set; }
        [JsonPropertyName("events_failed")]
        public int Failed { get; set; }
    }
}
