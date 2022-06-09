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
    }
}
