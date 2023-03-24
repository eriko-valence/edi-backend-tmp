using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    public class EdiJobVaroMetadataCreationTime
    {
        /// <summary>
        /// Absolute timestamp of EMD when the file is created on the EMD filesystem. EMD 
        /// timestamp source for Varo is whatever the phone thinks is absolute time. 
        /// </summary>
        /// NOTES
        /// ----------------------------------------------------------
        /// - The ABST and RELT values from the filename can be used as a "point in time reference" 
        /// association between the two timestamps for transformation purposes if needed. 
        /// - Timestamp is strictly "at time of file creation" and not "upload" since upload may fail… 
        /// so it reflects the time when the file package was created only.There may be multiple files 
        /// sitting on the file system with different times (when they were created) if upload 
        /// continuously fails for some reason.
        /// 
        /// HISTORY
        /// ----------------------------------------------------------
        /// 2023.03.15 1101 NHGH-2819 Added definition
        public string ABST { get; set; }
        public DateTime? ABST_UTC { get; set; }
        public EmdTimeSource.Name SOURCE { get; set; }
    }
}
