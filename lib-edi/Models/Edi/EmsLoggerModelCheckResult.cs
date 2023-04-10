using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EmsLoggerModelCheckResult
    {
        public EmsLoggerModelCheckResult()
        {
            LMOD = "";
            LoggerModelEnum = DataLoggerModelsEnum.Name.UNKNOWN;
            IsSupported = false;
        }
        public DataLoggerModelsEnum.Name LoggerModelEnum { get; set; }
        public bool IsSupported { get; set; }
        public string LMOD { get; set; }
    }
}
