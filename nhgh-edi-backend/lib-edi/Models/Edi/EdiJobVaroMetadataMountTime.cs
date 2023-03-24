using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobVaroMetadataMountTime
    {
        public EdiJobVaroMetadataMountTime()
        {
            Calcs = new EdiJobEmdMountTimeCalcs();
        }
        /// <summary>
        /// Absolute timestamp determined by the EMD device at the point when a logger is mounted via USB. 
        /// EDI sources this from Varo collected SYNC file names.
        /// </summary>
        /// 
        /// HISTORY
        /// ----------------------------------------------------------
        /// 2023.03.15 1101 NHGH-2819 Added definition
        public string ABST { get; set; }
        /// <summary>
        /// Represents relative time (ISO 8601 duration format) determined by the logger at the point when 
        /// it is mounted by an EMD device via USB. The relative time/duration value is the time elapsed 
        /// since the logger was manufactured and activated/commissioned (likely at the factory). Sourced 
        /// from Varo collected SYNC file. 
        /// </summary>
        /// HISTORY
        /// ----------------------------------------------------------
        /// 2023.03.15 1101 NHGH-2819 Added definition
        public string RELT { get; set; }
        public EdiJobEmdMountTimeCalcs Calcs { get; set; }
        /// <summary>
        /// Source of EMD timestamps (RELT, ABST). Options include EMD report metadata records array (METADATA)
        /// or SYNC file name (SYNCFILENAME).
        /// </summary>
        /// <remarks>
        /// NHGH-2819 2023.03.14 1532 Added definition
        /// </remarks>
        public EmdTimeSource.Name SOURCE { get; set; }
    }
}
