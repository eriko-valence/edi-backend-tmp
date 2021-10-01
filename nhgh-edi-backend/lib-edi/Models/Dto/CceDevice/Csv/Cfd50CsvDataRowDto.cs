using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class Cfd50CsvDataRowDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		// Log Header
		public string AMFR { get; set; }
		public string AMOD { get; set; }
		public string APQS { get; set; }
		public string ASER { get; set; }
		public string AID { get; set; }
		public string ADAT { get; set; }
		public string CID { get; set; }
		public string FID { get; set; }
		public string LOC { get; set; }

		// Log Record
		public string ABST { get; set; }
		public string TAMB { get; set; }
		public string TCLD { get; set; }
		public string TVC { get; set; }
		public bool CMPR { get; set; }
		public string SVA { get; set; }
		public string EVDC { get; set; }
		public string CDRW { get; set; }
		public bool DOOR { get; set; }
		public string HOLD { get; set; }
		public string BEMD { get; set; }
		public string TCON { get; set; }
		public string CMPS { get; set; }
		public string CSOF { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
