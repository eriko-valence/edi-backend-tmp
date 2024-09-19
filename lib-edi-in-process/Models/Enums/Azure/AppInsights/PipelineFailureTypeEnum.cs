using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Enums.Azure.AppInsights
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
