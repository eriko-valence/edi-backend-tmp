using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobUsbdgMetadata
    {
        public string ALRM { get; set; } // Required EMD object, so it will come from the USBDG metadata file
        public string ESER { get; set; }
        public string ABST { get; set; }
        public string RELT { get; set; }
        public string RTCW { get; set; }
    }
}
