using lib_edi.Models.Azure.LogAnalytics;
using lib_edi.Models.Azure.Sql.Connection;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Models.SendGrid;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Job
{
    public class EdiJobInfo
    {
        public EdiJobInfo()
        {
            EdiDb = new AzureSqlDbConnectInfo();
            //ApplicationName = "EDI Maintenance";
            EdiLaw = new AzureLogAnalyticsInfo();
        }
        public Guid JobId { get; set; }
        //public string ApplicationName { get; set; }
        public EdiFunctionsEnum.Name FunctionName { get; set; }
        public AzureSqlDbConnectInfo EdiDb { get; set; }
        public AzureLogAnalyticsInfo EdiLaw { get; set; }
        public SendGridConnectInfo EdiSendGrid { get; set; }
        public EmailReportParameters EdiEmailReportParameters { get; set; }

        //public ILogger Logger { get; set; }
    }
}
