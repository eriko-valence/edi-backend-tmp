using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Loggers
{
	public class Cfd50JsonDataFileDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string AMFR { get; set; } //1104
		public string AMOD { get; set; } //1104
		public string ASER { get; set; } //1104
		public string ADOP { get; set; } //1104
		public string APQS { get; set; } //1104
		public string RNAM { get; set; } //1104
		public string DNAM { get; set; } //1104
		public string FNAM { get; set; } //1104
		public string CID { get; set; } //1104
		public string LAT { get; set; } //1104
		public string LNG { get; set; } //1104

		public IList<Cfd50JsonDataFileRecordDto> records { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	public class Cfd50JsonDataFileRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public string ABST { get; set; } //1104
		public string SVA { get; set; } //1104
		public string HAMB { get; set; } //1104
		public string TAMB { get; set; } //1104
		public string ACCD { get; set; } //1104
		public string TCON { get; set; } //1104
		public string TVC { get; set; } //1104
		public string BEMD { get; set; } //1104
		public string HOLD { get; set; } //1104
		public string DORV { get; set; } //1104
		public string ALRM { get; set; } //1104
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
