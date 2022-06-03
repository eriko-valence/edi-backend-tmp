using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class UsbdgSimCsvRecordDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		[Name("ABST")]
		public DateTime ABST { get; set; }
		[Name("ACCD")]
		public double ACCD { get; set; }
		[Name("ACSV")]
		public double ACSV { get; set; }
		[Name("ALRM")]
		public string ALRM { get; set; }
		[Name("BEMD")]
		public double BEMD { get; set; }
		[Name("BLOG")]
		public double BLOG { get; set; }
		[Name("CMPR")]
		public double CMPR { get; set; }
		[Name("CMPS")]
		public double CMPS { get; set; }
		[Name("DCCD")]
		public double DCCD { get; set; }
		[Name("DCSV")]
		public double DCSV { get; set; }
		[Name("DORF")]
		public double DORF { get; set; }
		[Name("DORV")]
		public double DORV { get; set; }
		[Name("EERR")]
		public string EERR { get; set; }
		[Name("FANS")]
		public double FANS { get; set; }
		[Name("HAMB")]
		public double HAMB { get; set; }
		[Name("HCOM")]
		public double HCOM { get; set; }
		[Name("HOLD")]
		public double HOLD { get; set; }
		[Name("LAT")]
		public double LAT { get; set; }
		[Name("LERR")]
		public string LERR { get; set; }
		[Name("LNG")]
		public double LNG { get; set; }
		[Name("MSW")]
		public double MSW { get; set; }
		[Name("RELT")]
		public string RELT { get; set; }
		[Name("RTCW")]
		public string RTCW { get; set; }
		[Name("SVA")]
		public double SVA { get; set; }
		[Name("TAMB")]
		public double TAMB { get; set; }
		[Name("TCON")]
		public double TCON { get; set; }
		[Name("TFRZ")]
		public double TFRZ { get; set; }
		[Name("TPCB")]
		public double TPCB { get; set; }
		[Name("TVC")]
		public double TVC { get; set; }

		[Name("ADOP")]
		public string ADOP { get; set; }
		[Name("AID")]
		public string AID { get; set; }
		[Name("AMFR")]
		public string AMFR { get; set; }
		[Name("AMOD")]
		public string AMOD { get; set; }
		[Name("APQS")]
		public string APQS { get; set; }
		[Name("ASER")]
		public string ASER { get; set; }
		[Name("CDAT")]
		public string CDAT { get; set; }
		[Name("CID")]
		public string CID { get; set; }
		[Name("CNAM")]
		public string CNAM { get; set; }
		[Name("CSER")]
		public string CSER { get; set; }
		[Name("CSOF")]
		public string CSOF { get; set; }
		[Name("DNAM")]
		public string DNAM { get; set; }
		[Name("EDOP")]
		public string EDOP { get; set; }
		[Name("EID")]
		public string EID { get; set; }
		[Name("EMFR")]
		public string EMFR { get; set; }
		[Name("EMOD")]
		public string EMOD { get; set; }
		[Name("EMSV")]
		public string EMSV { get; set; }
		[Name("EPQS")]
		public string EPQS { get; set; }
		[Name("ESER")]
		public string ESER { get; set; }
		[Name("FID")]
		public string FID { get; set; }
		[Name("FNAM")]
		public string FNAM { get; set; }
		[Name("LDOP")]
		public string LDOP { get; set; }
		[Name("LID")]
		public string LID { get; set; }
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
		[Name("RNAM")]
		public string RNAM { get; set; }
		[Name("_RELT_SECS")]
		public int _RELT_SECS { get; set; } // relative time (duration) in seconds (set by azure function)
											//public int emd_relt { get; set; }
											//public DateTime emd_abs { get; set; }
		[Name("ABST_CALC")]
		public DateTime? ABST_CALC { get; set; }
		[Ignore]
		public string _SOURCE { get; set; }
		[Ignore]
		public string Source { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
