using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class JobMonitorResult
    {
        //[JsonProperty("query_email")]
        public string QueryEmail { get; set; }
        //[JsonProperty("total_msgs")]
        public string TotalMessages { get; set; }
        public string JobState { get; set; }
        public string JobStatus { get; set; }
        //[JsonProperty("timestamp")]
        public string Timestamp { get; set; }
    }
}
