using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class EmsCfd50CsvRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		// Log Header
		public string AMFR { get; set; }
		public string AMOD { get; set; }
		public double APQS { get; set; }
		public string ASER { get; set; }
		public string AID { get; set; }
		public string ADAT { get; set; }
		public string CID { get; set; }
		public string FID { get; set; }
		public double LAT { get; set; }
		public double LNG { get; set; }

		// Log Record
		public DateTime ABST { get; set; }
		public double TAMB { get; set; }
		public double TFRZ { get; set; }
		public double TVC { get; set; }
		public int CMPR { get; set; }
		public int SVA { get; set; }
		public int EVDC { get; set; }
		public int CDRW { get; set; }
		public int DOOR { get; set; }
		public double HOLD { get; set; }
		public double BEMD { get; set; }
		public double TCON { get; set; }
		public int CMPS { get; set; }
		public string CSOF { get; set; }
		public string ALRM { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
