using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Emd.Csv
{
	public class VaroLocationRecord : EdiSinkRecord
	{
		public VaroLocationRecord() { 

		}

		[Name("ASER")]
		public string ASER { get; set; }

		[Name("REPORTTIME")]
		public DateTime REPORTTIME { get; set; }

		[Name("ACCURACY")]
		public string ACCURACY { get; set; }
		[Name("LATITUDE")]
		public string LATITUDE { get; set; }
		[Name("LONGITUDE")]
		public string LONGITUDE { get; set; }
	}
}
