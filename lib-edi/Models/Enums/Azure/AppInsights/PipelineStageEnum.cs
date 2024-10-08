﻿using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Enums.Azure.AppInsights
{
	/// <summary>
	/// Holds enumerators for tracking telemetry processing in pipeline
	/// </summary>
	public class PipelineStageEnum
	{
		/// <summary>
		/// A pipeline stage enumerator for tracking progress of telemetry processing in pipeline
		/// </summary>
		public enum Name
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
			NONE,
			CCDX_PROVIDER,
			CCDX_PROVIDER_VARO,
			CCDX_CONSUMER,
			CCDX_CONSUMER_VARO,
			ADF_TRANSFORM,
			ADF_TRANSFORM_VARO,
			MAIL_COMPRESSOR_VARO
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
