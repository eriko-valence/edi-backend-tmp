using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class EmsEventRecord : EdiSinkRecord
    {
        /*
		 * APPLIANCE PROPERTIES
		 */
        [Name("ADOP")]
        public string ADOP { get; set; }
        [Name("AMOD")]
        public string AMOD { get; set; }
        [Name("AMFR")]
        public string AMFR { get; set; }
        [Name("APQS")]
        public string APQS { get; set; }
        [Name("ASER")]
        public string ASER { get; set; }

        /*
		 * OTHER PROPERTIES
		 */

        /*
		 * LOGGER PROPERTIES
		 */
        [Name("LDOP")]
        public string LDOP { get; set; }
        [Name("LERR")]
        public string LERR { get; set; }
        [Name("LMFR")]
        public string LMFR { get; set; }
        [Name("LMOD")]
        public string LMOD { get; set; }
        [Name("LPQS")]
        public string LPQS { get; set; }
        [Name("LSER")]
        public string LSER { get; set; }
        [Name("LSV")]
        public string LSV { get; set; }

        /*
         * USBDG REPORT PROPERTIES
         */
        [Name("ESER")]
        public string ESER { get; set; }
        [Name("ZCHRG")]
        public string ZCHRG { get; set; }

        [Name("RELT")]
        public string RELT { get; set; }
        [Name("RTCW")]
        public string RTCW { get; set; }
        [Name("EMD_TYPE")]
        public string EMD_TYPE { get; set; }

        /*
		 * OTHER PROPERTIES
		 */
        [Name("ABST_CALC")]
        public DateTime? EDI_ABST { get; set; }
        [Name("_RELT_SECS")]
        public int EDI_RELT_ELAPSED_SECS { get; set; } // relative time (duration) in seconds (set by azure function)
                                            //public int emd_relt { get; set; }
                                            //public DateTime emd_abs { get; set; }
    }
}
