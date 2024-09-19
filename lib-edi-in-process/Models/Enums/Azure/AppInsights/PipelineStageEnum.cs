﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Enums.Azure.AppInsights
{
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
