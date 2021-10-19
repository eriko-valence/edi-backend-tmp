using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace lib_edi.Services.System
{
	public class DateTimeService
	{
		/// <summary>
		/// Converts ISO 8601 compliant date/time string to DateTime object
		/// </summary>
		/// <param name="s">ISO 8601 compliant date/time string in format "yyyyMMddTHHmmssZ" </param>
		/// <returns>
		/// A DateTime object representing the ISO 8601 compliant date/time string
		/// </returns>
		/// <example>
		/// ISO 8601 compliant date/time string in format "yyyyMMddTHHmmssZ": 20211018T164303Z
		/// </example>
		public static DateTime ConvertIso8601CompliantString(string s)
		{
			string format = "yyyyMMddTHHmmssZ"; //20211018T164303Z
			var cultureInfo = new CultureInfo("en-US");
			DateTime reportAbsoluteDateTime = DateTime.ParseExact(s, format, cultureInfo);
			return reportAbsoluteDateTime;
		}

		/// <summary>
		/// Converts the specified string representation of a date and time to an equivalent date and time value
		/// </summary>
		/// <param name="s">Date and time string </param>
		/// <returns>
		/// A DateTime object representing the date and time string
		/// </returns>
		public static DateTime? ConvertStringToDateTime(string s)
		{
			if (s != null)
			{
				try
				{
					DateTime dt = Convert.ToDateTime(s);
					return (DateTime?)(dt);
				}
				catch (Exception)
				{
					return null;
				}
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts .NET DateTime object to a display friendly date string.
		/// </summary>
		/// <returns>
		/// Display friendly date string. Example: "31 Oct 2020".
		/// </returns>
		public static string getDisplayDateFormat(DateTime dt)
		{
			return dt.ToString("d MMM yyyy", CultureInfo.InvariantCulture);
		}

		/// <summary>
		/// Converts .NET DateTime object to a UTC date string.
		/// </summary>
		/// <returns>
		/// UTC date string. Example: "1 Dec 2020 02:04:17 GMT".
		/// </returns>
		public static string getDisplayDateUtcFormat(DateTime dt)
		{
			return dt.ToString("d MMM yyyy HH:mm:ss 'GMT'", CultureInfo.InvariantCulture);
		}
	}
}
