using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobLogger
    {
        public EdiJobLogger() 
        {
            Type = DataLoggerTypeEnum.Name.UNKNOWN;
        }
        public string LSER { get; set; }
        public string LMOD { get; set; }
        public DataLoggerTypeEnum.Name Type { get; set; }
    }
}
