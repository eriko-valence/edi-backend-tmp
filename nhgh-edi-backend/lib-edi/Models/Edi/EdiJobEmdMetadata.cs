using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobEmdMetadata
    {
        public EdiJobEmdMetadata()
        {
            Usbdg = new EdiJobUsbdgMetadata();
            Varo = new EdiJobVaroMetadata();
        }

        public EdiJobUsbdgMetadata Usbdg { get; set; }
        public EdiJobVaroMetadata Varo { get; set; }
    }
}
