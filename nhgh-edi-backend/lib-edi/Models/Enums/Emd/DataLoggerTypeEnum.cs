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
			USBDG_DATASIM,
			INDIGO_V2,
			CFD50,
			UNKNOWN
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
		}
	}
}
