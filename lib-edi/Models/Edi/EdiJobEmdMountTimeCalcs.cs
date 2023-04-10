using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Edi
{
    /// <summary>
    /// Holds time calculation results performed by EDI transformation functions.
    /// </summary>
    /// <remarks>
    /// NHGH-2819 2023.03.14 1532 Added definition
    /// </remarks>
    public class EdiJobEmdMountTimeCalcs
    {
        /// <summary>
        /// Represents EMD RELT timestamp as elapsed seconds.
        /// </summary>
        /// <remarks>
        /// NHGH-2819 2023.03.14 1532 Added definition
        /// </remarks>
        public int RELT_ELAPSED_SECS { get; set; }
        /// <summary>
        /// Represents EMD ABST timestamp as DateTime object. 
        /// </summary>
        /// <remarks>
        /// NHGH-2819 2023.03.14 1532 Added definition
        /// </remarks>
        public DateTime? ABST_UTC { get; set; }
    }
}
