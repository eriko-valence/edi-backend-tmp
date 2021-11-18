using CsvHelper.Configuration.Attributes;
using lib_edi.Helpers;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class Cfd50CsvRecordDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		// Log Header
		[Name("AMFR")]
		public string AMFR { get; set; }
		[Name("AMOD")]
		public string AMOD { get; set; }
		[Name("ASER")]
		public string ASER { get; set; }
		[Ignore]
		public DateTime? _ADOP { get; set; }
		[Name("ADOP")]
		public string ADOP
		{
			get {
				return DateConverter.ConverToDateString(_ADOP);
			}

			set
			{
				_ADOP = DateConverter.ParseDateTimeString(value);
			}
		}
		[Name("APQS")]
		public double APQS { get; set; }
		[Name("RNAM")]
		public string RNAM { get; set; }
		[Name("DNAM")]
		public string DNAM { get; set; }
		[Name("FNAM")]
		public string FNAM { get; set; }
		[Name("CID")]
		public string CID { get; set; }
		[Name("LAT")]
		public double LAT { get; set; }
		[Name("LNG")]
		public double LNG { get; set; }
		[Ignore]
		DateTime? _ABST { get; set; }

		// Log Record
		[Name("ABST")]
		public string ABST
		{
			get
			{
				return DateConverter.ConverToDateTimeString(_ABST);
			}

			set
			{
				_ABST = DateConverter.ParseDateTimeString(value);
			}
		}
		[Name("SVA")]
		public int SVA { get; set; }
		[Name("HAMB")]
		public double HAMB { get; set; }
		[Name("TAMB")]
		public double TAMB { get; set; }
		[Name("ACCD")]
		public double ACCD { get; set; }
		[Name("TCON")]
		public double TCON { get; set; }
		[Name("TVC")]
		public double TVC { get; set; }
		[Name("BEMD")]
		public double BEMD { get; set; }
		[Name("HOLD")]
		public double HOLD { get; set; }
		[Name("DORV")]
		public int DORV { get; set; }
		[Name("ALRM")]
		public string ALRM { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
