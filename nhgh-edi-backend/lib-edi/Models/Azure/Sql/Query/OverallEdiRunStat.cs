using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.Sql.Query
{
    public class OverallEdiRunStat
    {
        public int FailedProvider { get; set; }
        public int FailedConsumer { get; set; }
        public int FailedTransform { get; set; }
        public int FailedSqlLoad { get; set; }
        public int SuccessfulJobs { get; set; }
        public int TotalFailedJobs { get; set; }
        public int TotalJobs { get; set; }
    }
}
