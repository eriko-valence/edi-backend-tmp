using lib_edi.Models.Azure.Sql.Connection;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Functions;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Job.EmailReport
{
    public class EdiJobsStatusReportInfo
    {
        public EdiJobsStatusReportInfo()
        {
            EdiDb = new AzureSqlDbConnectInfo();
            ApplicationName = "EDI Jobs Status Email Report";
        }
        public Guid JobId { get; set; }
        public string ApplicationName { get; set; }
        public EdiFunctionsEnum.Name FunctionName { get; set; }
        public AzureSqlDbConnectInfo EdiDb { get; set; }
        public ILogger Logger { get; set; }
    }
}
