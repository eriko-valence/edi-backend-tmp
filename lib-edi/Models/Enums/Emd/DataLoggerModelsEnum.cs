using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Enums.Emd
{
    public class DataLoggerModelsEnum
    {
        /// <summary>
        /// LMOD property value pulled from SYNC/CURRENT json file
        /// </summary>
        public enum Name
        {
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            UNKNOWN,
            INDIGO_V2,
            SL1
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
    }
}
