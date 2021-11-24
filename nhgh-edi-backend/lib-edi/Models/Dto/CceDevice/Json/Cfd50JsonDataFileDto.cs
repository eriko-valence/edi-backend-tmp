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
		public double? LAT { get; set; }
		public double? LNG { get; set; }

		public IList<Cfd50JsonDataFileRecordDto> records { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	public class Cfd50JsonDataFileRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string ABST { get; set; }
		public double? SVA { get; set; }
		public double? HAMB { get; set; }
		public double? TAMB { get; set; }
		public double? ACCD { get; set; }
		public double? TCON { get; set; }
		public double? TVC { get; set; }
		public double? BEMD { get; set; }
		public double? HOLD { get; set; }
		public double? DORV { get; set; }
		public string ALRM { get; set; }
		public string EMSV { get; set; }
		public string EERR { get; set; }
		public double? CMPR { get; set; }
		public double? ACSV { get; set; }
		public string AID { get; set; }
		public double? CMPS { get; set; }
		public double? DCCD { get; set; }
		public double? DCSV { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
