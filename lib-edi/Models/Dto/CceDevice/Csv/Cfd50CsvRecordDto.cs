using CsvHelper.Configuration.Attributes;
using lib_edi.Helpers;
using lib_edi.Services.Errors;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Numerics;
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
		public BigInteger _ASER { get; set; }
		[Name("ASER_HEX")]
		public string ASER
		{
			get
			{
				return IntegerConverter.ConvertToHexadecimal(_ASER);
			}

			set
			{
				try
				{
					_ASER = IntegerConverter.ConvertToBigInteger(value);
				} catch (Exception e)
				{
					//string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "3C68", EdiErrorsService.BuildErrorVariableArrayList("ASER"));
					string customErrorMessage = "3C68: An exception was thrown while setting property 'ASER'";
					throw new Exception(customErrorMessage);
				}
				
			}
		}
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
		public string APQS { get; set; }
		[Name("RNAM")]
		public string RNAM { get; set; }
		[Name("DNAM")]
		public string DNAM { get; set; }
		[Name("FNAM")]
		public string FNAM { get; set; }
		[Name("CID")]
		public string CID { get; set; }
		[Name("LAT")]
		public double? LAT { get; set; }
		[Name("LNG")]
		public double? LNG { get; set; }
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
		public int? SVA { get; set; }
		[Name("HAMB")]
		public double? HAMB { get; set; }
		[Name("TAMB")]
		public double? TAMB { get; set; }
		[Name("ACCD")]
		public double? ACCD { get; set; }
		[Name("TCON")]
		public double? TCON { get; set; }
		[Name("TVC")]
		public double? TVC { get; set; }
		[Name("BEMD")]
		public double? BEMD { get; set; }
		[Name("HOLD")]
		public double? HOLD { get; set; }
		[Name("DORV")]
		public int? DORV { get; set; }
		[Name("ALRM")]
		public string ALRM { get; set; }
		[Name("EMSV")]
		public string EMSV { get; set; }
		[Name("EERR")]
		public string EERR { get; set; }
		[Name("CMPR")]
		public int? CMPR { get; set; }
		[Name("ACSV")]
		public double? ACSV { get; set; }
		[Name("AID")]
		public string AID { get; set; }
		[Name("CMPS")]
		public int? CMPS { get; set; }
		[Name("DCCD")]
		public double? DCCD { get; set; }
		[Name("DCSV")]
		public double? DCSV { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
