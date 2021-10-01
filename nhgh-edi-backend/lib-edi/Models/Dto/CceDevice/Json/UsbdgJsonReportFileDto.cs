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
		[JsonProperty("country")]
		public string country { get; set; }

		[JsonProperty("district")]
		public string district { get; set; }

		[JsonProperty("managerName")]
		public string managerName { get; set; }

		[JsonProperty("managerPhone")]
		public string managerPhone { get; set; }

		[JsonProperty("name")]
		public string name { get; set; }

		[JsonProperty("state")]
		public string state { get; set; }

		[JsonProperty("accuracy")]
		public string accuracy { get; set; }

		[JsonProperty("latitude")]
		public string latitude { get; set; }

		[JsonProperty("longitude")]
		public string longitude { get; set; }

		[JsonProperty("emd_abs")]
		public string emd_abs { get; set; }

		[JsonProperty("emd_relt")]
		public string emd_relt { get; set; }

		[JsonProperty("ESER")]
		public string ESER { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
