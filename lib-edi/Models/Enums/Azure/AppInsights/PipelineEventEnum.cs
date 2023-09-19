using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Enums.Azure.AppInsights
{
	/// <summary>
	/// Holds enumerators for app insights pipeline event type
	/// </summary>
	public class PipelineEventEnum
	{
		/// <summary>
		/// A pipeline event type enumerator for app insights logging
		/// </summary>
		public enum Name
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			NONE,
			STARTED,
			SUCCEEDED,
			FAILED,
			WARN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
