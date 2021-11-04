using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class Cfd50CsvRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		// Log Header
		public string AMFR { get; set; }
		public string AMOD { get; set; }
		public string ASER { get; set; }
		public string ADOP { get; set; }
		public double APQS { get; set; }
		public string RNAM { get; set; }
		public string DNAM { get; set; }
		public string FNAM { get; set; }
		public string CID { get; set; }
		public double LAT { get; set; }
		public double LNG { get; set; }


		// Log Record
		public DateTime ABST { get; set; }
		public int SVA { get; set; }
		public double HAMB { get; set; }
		public double TAMB { get; set; }
		public double ACCD { get; set; }
		public double TCON { get; set; }
		public double TVC { get; set; }
		public double BEMD { get; set; }
		public double HOLD { get; set; }
		public int DORV { get; set; }
		public string ALRM { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
