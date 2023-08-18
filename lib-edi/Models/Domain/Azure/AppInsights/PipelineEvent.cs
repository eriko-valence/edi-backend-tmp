using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Emd;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Azure.AppInsights
{
	public class PipelineEvent
	{
		public PipelineStageEnum.Name StageName { get; set; }
		public PipelineEventEnum.Name EventName { get; set; }
		public DataLoggerTypeEnum.Name LoggerType { get; set; }
		public PipelineFailureTypeEnum.Name PipelineFailureType { get; set; }
		public PipelineFailureReasonEnum.Name PipelineFailureReason { get; set; }
		// NHGH-3057 1652 Add EMD type to app insights logging
		public EmdEnum.Name EmdType { get; set; }
		public string ReportFileName { get; set; }
		public string ErrorMessage { get; set; }
		public string ErrorCode { get; set; }
		public string ExceptionMessage { get; set; }
		public string ExceptionInnerMessage { get; set; }
	}
}
