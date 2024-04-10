using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Enums.Emd
{
	/// <summary>
	/// A data logger type enumerator for app insights logging
	/// </summary>
	public class DataLoggerTypeEnum
	{
		/// <summary>
		/// A data logger type enumerator for app insights logging
		/// </summary>
		public enum Name
		{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
            NONE,
            CFD50,
            EMS,
            INDIGO_V2,
            INDIGO_CHARGER_V2,
            NO_LOGGER,
            SL1,
            USBDG_DATASIM,
            UNKNOWN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
        }
	}
}
