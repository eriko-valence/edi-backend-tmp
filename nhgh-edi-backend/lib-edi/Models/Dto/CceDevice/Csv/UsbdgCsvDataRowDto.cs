using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class UsbdgCsvDataRowDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string RELT { get; set; }
		public string RTCW { get; set; }
		public double SV { get; set; }
		public double CDRW { get; set; }
		public double HCOM { get; set; }
		public double TCON { get; set; }
		public double CMPS { get; set; }
		public double FANS { get; set; }
		public double TPCB { get; set; }
		public double MSW { get; set; }
		public double TVC { get; set; }
		public double TFRZ { get; set; }
		public double T1 { get; set; }
		public double T2 { get; set; }
		public double T3 { get; set; }
		public double TF1 { get; set; }
		public double Hamb { get; set; }
		public string AMFR { get; set; }
		public string AMOD { get; set; }
		public string ASER { get; set; }
		public string ADAT { get; set; }
		public string APQS { get; set; }
		public string CNAM { get; set; }
		public string CSER { get; set; }
		public string CSOF { get; set; }
		public string CDAT { get; set; }
		public double SVA { get; set; }
		public double CMPR { get; set; }
		public double TCLD { get; set; }
		public double DOOR { get; set; }
		public double TAMB { get; set; }
		public string LOC { get; set; }
		public string AID { get; set; }
		public string LID { get; set; }
		public string EID { get; set; }
		public string CID { get; set; }
		public string FID { get; set; }
		public string EMFR { get; set; }
		public string EMOD { get; set; }
		public string ESER { get; set; }
		public string EDAT { get; set; }
		public string EMSV { get; set; }
		public string EPQS { get; set; }
		public string ABST { get; set; }
		public string BEMD { get; set; }
		public string LMFR { get; set; }
		public string LMOD { get; set; }
		public string LSER { get; set; }
		public string LDAT { get; set; }
		public string SWVER { get; set; }
		public string LPQS { get; set; }
		public double BLOG { get; set; }
		public int duration_secs { get; set; } // relative time (duration) in seconds (set by azure function)
		public int emd_relt { get; set; }
		public DateTime emd_abs { get; set; }
		public DateTime utc_timestamp { get; set; }
		[Ignore]
		public string Source { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
