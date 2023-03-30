using lib_edi.Models.Azure.Sql.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class SendGridEdiFailedJobsResults
    {
        public SendGridEdiFailedJobsResults()
        {
            Results = new List<SendGridEdiFailedJob>();
            //Errors = new List<PogoLTAppError>();
        }

        public List<SendGridEdiFailedJob> Results { get; set; }
        //public List<PogoLTAppError> Errors { get; set; }
        //public int TotalJobsRun { get; set; }
        //public int TotalErrors { get; set; }
        public string Subject { get; set; }
        public OverallEdiRunStat JobStats { get; set; }
    }
}
