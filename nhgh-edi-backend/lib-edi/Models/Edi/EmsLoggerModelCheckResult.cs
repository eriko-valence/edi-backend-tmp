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
        public DataLoggerModelsEnum.Name LoggerModel { get; set; }
        public bool IsSupported { get; set; }
    }
}
