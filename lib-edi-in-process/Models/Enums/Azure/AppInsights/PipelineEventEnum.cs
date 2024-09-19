using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Enums.Azure.AppInsights
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
