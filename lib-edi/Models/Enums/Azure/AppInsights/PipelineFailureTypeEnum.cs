using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Enums.Azure.AppInsights
{
	public class PipelineFailureTypeEnum
	{
		public enum Name
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			NONE,
			VALIDATION,
			ERROR,
			WARN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
