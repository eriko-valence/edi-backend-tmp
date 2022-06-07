using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class IndigoV2EventRecord : EdiSinkRecord
    {
        [Name("ABST_CALC")]
        public DateTime? EDI_RECORD_ABST_CALC { get; set; }
		/*
		 * APPLIANCE PROPERTIES
		 */
		[Name("ADOP")]
		public string ADOP { get; set; }
		[Name("ALRM")]
		public string ALRM { get; set; }
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
		[Name("BLOG")]
		public double BLOG { get; set; }
		[Name("DORV")]
		public int DORV { get; set; }

		[Name("ESER")]
		public string ESER { get; set; }
		[Name("HOLD")]
		public double HOLD { get; set; }

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
		 * OTHER PROPERTIES
		 */
		[Name("RELT")]
		public string RELT { get; set; }
		[Name("RTCW")]
		public string RTCW { get; set; }
		[Name("TAMB")]
		public double TAMB { get; set; }
		[Name("TVC")]
		public double TVC { get; set; }
		[Name("ZCHRG")]
		public string ZCHRG { get; set; }
		[Name("ZSTATE")]
		public bool ZSTATE { get; set; }
		[Name("ZVLVD")]
		public bool ZVLVD { get; set; }
		[Name("_RELT_SECS")]
		public int _RELT_SECS { get; set; } // relative time (duration) in seconds (set by azure function)
											//public int emd_relt { get; set; }
											//public DateTime emd_abs { get; set; }

		[Ignore]
		public string Source { get; set; }
	}
}
