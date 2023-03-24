using lib_edi.Models.Enums.Edi.Data.Import;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi.Data.Import
{
    public class OtaImportJob
    {
        public OtaImportJob()
        {
            OtaDb = new OtaImportJobDb();
            MfoxDb = new OtaImportJobDb();
            EdiDb = new OtaImportJobDb();
            ApplicationName = "OTA Data Importer";
        }
        public Guid JobId { get; set; }
        public string ApplicationName { get; set; }
        public OtaJobImportFunctionEnum.Name FunctionName { get; set; }
        public OtaImportJobDb OtaDb { get; set; }
        public OtaImportJobDb MfoxDb { get; set; }
        public OtaImportJobDb EdiDb { get; set; }
        public ILogger Logger { get; set; }
    }
}
