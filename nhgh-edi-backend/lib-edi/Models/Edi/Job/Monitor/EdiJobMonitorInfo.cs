using lib_edi.Models.Azure.Sql.Connection;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Data.Import;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Job.Monitor
{
    public class EdiJobMonitorInfo
    {
        public EdiJobMonitorInfo()
        {
            EdiDb = new AzureSqlDbConnectInfo();
            ApplicationName = "EDI Job Monitor Report";
        }
        public Guid JobId { get; set; }
        public string ApplicationName { get; set; }
        public OtaJobImportFunctionEnum.Name FunctionName { get; set; }
        public OtaImportJobDb OtaDb { get; set; }
        public OtaImportJobDb MfoxDb { get; set; }
        public AzureSqlDbConnectInfo EdiDb { get; set; }
        public ILogger Logger { get; set; }

    }
}
