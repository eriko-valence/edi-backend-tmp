using lib_edi.Helpers;
using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobUsbdgMetadataMountTime
    {
        public EdiJobUsbdgMetadataMountTime()
        {
            Calcs = new EdiJobEmdMountTimeCalcs();
        }
        /// <summary>
        /// Absolute timestamp determined by the EMD device at the point when a logger is mounted via USB. 
        /// EDI sources USBDG ABST from records array in report metadata file. If ABST is null, EDI sources
        /// from SYNC file name. 
        /// </summary>
        /// NOTES
        /// ----------------------------------------------------------
        /// - The ABST and RELT values from the metadata can be used as a "point in time reference" association 
        /// between the two timestamps for transformation purposes if needed.
        /// - The logger has a 7 to 10 year battery backup for the real time clock.
        /// - A USBDG EMD uses a local state file to read from to populate the USBDG metadata file. For brand 
        /// new USBDG EMDs (at the first logger connection time), this local state file does not exist. In this 
        /// case null values will be present in the metadata file and ABST and RELT are pulled from the SYNC 
        /// file name in the cloud for transformation purposes.
        /// 
        /// HISTORY
        /// ----------------------------------------------------------
        /// 2023.03.14 1532 NHGH-2819 Added definition
        public string ABST { get; set; }
        /// <summary>
        /// Represents relative time (ISO 8601 duration format) determined by the logger at the point when 
        /// it is mounted by an EMD device via USB. The relative time/duration value is the time elapsed 
        /// since the logger was manufactured and activated/commissioned (likely at the factory). Sourced 
        /// from records array in EMD (USBDG) report metadata file or SYNC file (USBDG, Varo). 
        /// </summary>
        /// HISTORY
        /// ----------------------------------------------------------
        /// 2023.03.14 1532 NHGH-2819 Added definition
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
