using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Edi.Functions
{
    public class EdiFunctionAppsEnum
    {
        public enum Name
        {
            #pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            NONE,
            CCDX_PROVIDER,
            CCDX_CONSUMER,
            ADF_TRANSFORM,
            EDI_MAINT
            
            #pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
