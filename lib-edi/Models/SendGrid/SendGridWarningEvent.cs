using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
	public class SendGridWarningEvent
	{
		public DateTime? EventTime { get; set; }
		public string FilePackageName { get; set; }
		public string PipelineEvent { get; set; }
		public string PipelineStage { get; set; }
		public string PipelineFailureReason { get; set; }
		public string PipelineFailureType { get; set; }
		public string DataLoggerType { get; set; }
		public string ErrorCode { get; set; }
		public string EmdType { get; set; }
		public string ExceptionMessage { get; set; }
	}
}
