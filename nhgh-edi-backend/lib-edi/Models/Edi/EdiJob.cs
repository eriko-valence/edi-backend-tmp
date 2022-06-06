using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJob
    {
        public string ESER { get; set; }
        public string LSER { get; set; }
        public string BlobContainerName { get; set; }
        public string BlobFilePackagePath { get; set; }
    }
}
