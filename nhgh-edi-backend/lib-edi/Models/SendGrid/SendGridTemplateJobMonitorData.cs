using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class SendGridTemplateJobMonitorData
    {
        public SendGridTemplateJobMonitorData()
        {
            Results = new List<JobMonitorResult>();
            Errors = new List<PogoLTAppError>();
        }

        public List<JobMonitorResult> Results { get; set; }
        public List<PogoLTAppError> Errors { get; set; }
        public int TotalJobsRun { get; set; }
        public int TotalErrors { get; set; }
        public string Subject { get; set; }
    }
}
