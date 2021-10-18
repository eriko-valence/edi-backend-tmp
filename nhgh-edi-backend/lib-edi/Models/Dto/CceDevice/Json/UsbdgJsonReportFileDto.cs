using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Loggers
{
	/// <summary>
	/// A data transfer object for a JSON formatted USBDG report file
	/// </summary>
	public class UsbdgJsonReportFileDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		[JsonProperty("ABST")]
		public string ABST { get; set; }

		[JsonProperty("AID")]
		public string AID { get; set; }

		[JsonProperty("ALRM")]
		public string ALRM { get; set; }

		[JsonProperty("BEMD")]
		public string BEMD { get; set; }

		[JsonProperty("CID")]
		public string CID { get; set; }

		[JsonProperty("DNAM")]
		public string DNAM { get; set; }

		[JsonProperty("EDOP")]
		public string EDOP { get; set; }

		[JsonProperty("EERR")]
		public string EERR { get; set; }

		[JsonProperty("EID")]
		public string EID { get; set; }

		[JsonProperty("EMFR")]
		public string EMFR { get; set; }

		[JsonProperty("EMOD")]
		public string EMOD { get; set; }

		[JsonProperty("EMSV")]
		public string EMSV { get; set; }

		[JsonProperty("EPQS")]
		public string EPQS { get; set; }

		[JsonProperty("ESER")]
		public string ESER { get; set; }

		[JsonProperty("FID")]
		public string FID { get; set; }

		[JsonProperty("FNAM")]
		public string FNAM { get; set; }

		[JsonProperty("LAT")]
		public string LAT { get; set; }

		[JsonProperty("LNG")]
		public string LNG { get; set; }

		[JsonProperty("RNAM")]
		public string RNAM { get; set; }

		[JsonProperty("RELT")]
		public string RELT { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
