using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobEmdMetadataVaro
    {
        public EdiJobEmdMetadataVaro()
        {
            MountTime = new EdiJobVaroMetadataMountTime();
            CreationTime = new EdiJobVaroMetadataCreationTime();
        }
        public long? timestamp { get; set; } //varo timestamp utc (Epoch)
        public DateTime? TimestampDateTime { get; set; } //varo timestamp utc (human readable date time)
        public EdiJobVaroMetadataMountTime MountTime { get; set; }
        public EdiJobVaroMetadataCreationTime CreationTime { get; set; }
    }
}
