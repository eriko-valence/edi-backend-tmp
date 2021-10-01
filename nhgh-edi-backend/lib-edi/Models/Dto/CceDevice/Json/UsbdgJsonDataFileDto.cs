using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Loggers
{
	public class UsbdgJsonDataFileDto
	{
        #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
        public string AMFR { get; set; }
        public string AMOD { get; set; }
        public string ASER { get; set; }
        public string ADAT { get; set; }
        public string APQS { get; set; }
        public string CNAM { get; set; }
        public string CSER { get; set; }
        public string CSOF { get; set; }
        public string CDAT { get; set; }
        public string LMFR { get; set; }
        public string LMOD { get; set; }
        public string LSER { get; set; }
        public string LDAT { get; set; }
        public string SWVER { get; set; }
        public string LPQS { get; set; }
        public IList<UsbdgJsonReportFileDto> records { get; set; }
        public string Source { get; set; }
        #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
    }

    public class UsbdgJsonReportFileDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		[JsonProperty("RELT")]
		public string RELT { get; set; }

		[JsonProperty("RTCW")]
		public string RTCW { get; set; }

		[JsonProperty("SV")]
		public double SV { get; set; }

		[JsonProperty("CDRW")]
		public double CDRW { get; set; }

		[JsonProperty("HCOM")]
		public double HCOM { get; set; }
		[JsonProperty("TCON")]
		public double TCON { get; set; }
		[JsonProperty("CMPS")]
		public double CMPS { get; set; }

		[JsonProperty("FANS")]
		public double FANS { get; set; }

		[JsonProperty("TPCB")]
		public double TPCB { get; set; }

		[JsonProperty("MSW")]
		public double MSW { get; set; }

		[JsonProperty("TVC")]
		public double TVC { get; set; }

		[JsonProperty("TFRZ")]
		public double TFRZ { get; set; }

		[JsonProperty("T1")]
		public double T1 { get; set; }

		[JsonProperty("T2")]
		public double T2 { get; set; }

		[JsonProperty("T3")]
		public double T3 { get; set; }

		[JsonProperty("TF1")]
		public double TF1 { get; set; }

		[JsonProperty("Hamb")]
		public double Hamb { get; set; }

		[JsonProperty("SVA")]
		public double SVA { get; set; }

		[JsonProperty("CMPR")]
		public double CMPR { get; set; }

		[JsonProperty("TCLD")]
		public double TCLD { get; set; }

		[JsonProperty("DOOR")]
		public double DOOR { get; set; }

		[JsonProperty("TAMB")]
		public double TAMB { get; set; }

		[JsonProperty("BLOG")]
		public double BLOG { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
