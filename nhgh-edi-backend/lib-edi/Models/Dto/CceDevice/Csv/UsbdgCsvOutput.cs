using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.CceDevice.Csv
{
	public class UsbdgCsvOutput
	{
		public List<UsbdgCsvDataRowDto> records {get; set;}
		public UsbdgCsvMetadataDto metadata { get; set; }
	}
}
