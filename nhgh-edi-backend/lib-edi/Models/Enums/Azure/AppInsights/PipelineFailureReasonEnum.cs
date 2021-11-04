using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Enums.Azure.AppInsights
{
	public class PipelineFailureReasonEnum
	{
		public enum Name
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			NONE,
			UNKNOWN_EXCEPTION,
			UNSUPPORTED_DATA_LOGGER,
			MISSING_CE_SUBJECT_HEADER,
			HTTP_STATUS_CODE_ERROR,
			UNSUPPORTED_EXTENSION
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
