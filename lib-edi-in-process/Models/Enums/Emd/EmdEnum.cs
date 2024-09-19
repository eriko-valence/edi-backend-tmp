using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Enums.Emd
{
    public class EmdEnum
    {
        public enum Name
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            NONE,
            USBDG,
            VARO,
            UNKNOWN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
