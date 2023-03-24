using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobUsbdgMetadata
    {
        public EdiJobUsbdgMetadata()
        {
            MountTime = new EdiJobUsbdgMetadataMountTime();
            CreationTime = new EdiJobUsbdgMetadataCreationTime();
        }
        public string ALRM { get; set; }
        public string ESER { get; set; }
        public string ABST { get; set; }
        public string RELT { get; set; }
        public string RTCW { get; set; }
        public string EDI_SOURCE { get; set; }
        public EdiJobUsbdgMetadataMountTime MountTime { get; set; }
        public EdiJobUsbdgMetadataCreationTime CreationTime { get; set; }
    }
}
