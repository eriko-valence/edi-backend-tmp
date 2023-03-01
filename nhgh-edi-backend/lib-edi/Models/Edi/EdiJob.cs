using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJob
    {
        public EdiJob()
        {
            UsbdgMetadata = new EdiJobUsbdgMetadata();
            Logger = new EdiJobLogger();
        }
        
        public string BlobContainerName { get; set; }
        public string BlobFilePackagePath { get; set; }
        public EdiJobUsbdgMetadata UsbdgMetadata { get; set; }
        public EdiJobLogger Logger { get; set; }
        public string FileName_ABST { get; set; } // NHGH-2838 (2022.02.28) Use file name as fallback source of ABST
        public string FileName_RELT { get; set; } // NHGH-2838 (2022.02.28) Use file name as fallback source of RELT
    }
}
