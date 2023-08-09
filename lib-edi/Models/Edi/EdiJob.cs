using Microsoft.Azure.Storage.Blob.Protocol;
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
            Logger = new EdiJobLogger();
            Emd = new EdiJobEmd();
        }
        public EdiJobLogger Logger { get; set; }
        public EdiJobEmd Emd { get; set; }
    }
}
