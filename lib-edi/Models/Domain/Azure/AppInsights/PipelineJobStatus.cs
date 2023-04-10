using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Azure.AppInsights
{
	public class PipelineJobStatus
	{
		public PipelineJobStatus()
		{
			JobStageResults = new Dictionary<string, List<string>>();
		}

		public Dictionary<string, List<string>> JobStageResults { get; set; }
	}
}
