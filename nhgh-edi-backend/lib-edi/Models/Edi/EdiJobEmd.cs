using lib_edi.Models.Enums.Emd;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobEmd
    {
        public EdiJobEmd()
        {
            Metadata = new EdiJobEmdMetadata();
            Type = EmdEnum.Name.UNKNOWN;
        }

        public EdiJobEmdMetadata Metadata { get; set; }
        public EmdEnum.Name Type { get; set; }
    }
}
