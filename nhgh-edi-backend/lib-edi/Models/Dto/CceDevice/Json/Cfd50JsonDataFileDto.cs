using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Loggers
{
	public class Cfd50JsonDataFileDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string AMFR { get; set; }
		public string AMOD { get; set; }
		public string ASER { get; set; }
		public string ADOP { get; set; }
		public string APQS { get; set; }
		public string RNAM { get; set; }
		public string DNAM { get; set; }
		public string FNAM { get; set; }
		public string CID { get; set; }
		public string LAT { get; set; }
		public string LNG { get; set; }

		public IList<Cfd50JsonDataFileRecordDto> records { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	public class Cfd50JsonDataFileRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string ABST { get; set; }
		public string SVA { get; set; }
		public string HAMB { get; set; }
		public string TAMB { get; set; }
		public string ACCD { get; set; }
		public string TCON { get; set; }
		public string TVC { get; set; }
		public string BEMD { get; set; }
		public string HOLD { get; set; }
		public string DORV { get; set; }
		public string ALRM { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
