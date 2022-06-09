using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Emd.Csv
{
    public class UsbdgDeviceRecord : EdiSinkRecord
	{
		[Name("EDOP")]
		public string EDOP { get; set; }

		[Name("EMFR")]
		public string EMFR { get; set; }
		[Name("EMOD")]
		public string EMOD { get; set; }
		[Name("EMSV")]
		public string EMSV { get; set; }
		[Name("EPQS")]
		public string EPQS { get; set; }
		[Name("ESER")]
		public string ESER { get; set; }
		[Name("zcfg_ver")]
		public string zcfg_ver { get; set; }
		[Name("zmcu_ver")]
		public string zmcu_ver { get; set; }
	}
}
