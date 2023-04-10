using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Azure.AppInsights
{
	public class PipelineJobStatusEntry
	{
		public string TimeStamp { get; set; }
		public string PipelineStageName { get; set; }
		public string FileName { get; set; }
	}
}
