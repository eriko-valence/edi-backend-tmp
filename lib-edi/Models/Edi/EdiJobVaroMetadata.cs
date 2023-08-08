using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobVaroMetadata
    {
        public EdiJobVaroMetadata()
        {
            MountTime = new EdiJobVaroMetadataMountTime();
            CreationTime = new EdiJobVaroMetadataCreationTime();
        }
        public long? timestamp { get; set; } //varo timestamp utc (Epoch)
        public EdiJobVaroMetadataMountTime MountTime { get; set; }
        public EdiJobVaroMetadataCreationTime CreationTime { get; set; }

        //public long? Timestamp { get; set; } //varo timestamp utc (Epoch)
        public DateTime? TimestampDateTime { get; set; } //varo timestamp utc (human readable date time)
        public string EDI_SOURCE { get; set; }
        public EdiJobVaroMetadataLocation Location { get; set; }
        public DateTime? ReportTime { get; set; }
	}
}
