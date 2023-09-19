using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using System.Threading.Tasks;
using lib_edi.Services.Errors;

namespace lib_edi.Helpers
{
	/// <summary>
	/// Helper class for converting date/time objects
	/// </summary>
	public class DateConverter
	{
		private static readonly DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

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
		public static async Task<DateTime?> ConvertIso8601CompliantString(string s)
		{
			try
			{
				string format = "yyyyMMddTHHmmssZ"; //20211018T164303Z
				var cultureInfo = new CultureInfo("en-US");
				DateTime? reportAbsoluteDateTime = DateTime.ParseExact(s, format, cultureInfo);
				
				if (reportAbsoluteDateTime.HasValue)
				{
					reportAbsoluteDateTime = ((DateTime)reportAbsoluteDateTime).ToUniversalTime();

                }
				
				return reportAbsoluteDateTime;
			}
			catch (ArgumentNullException ane)
			{
				s ??= "''";
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(ane, "D23W", EdiErrorsService.BuildErrorVariableArrayList(s));
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				s ??= "''";
				string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "HQ37", EdiErrorsService.BuildErrorVariableArrayList(s));
				throw new Exception(customErrorMessage);
			}

		}

		/// <summary>
		/// Converts a date or date/time string to DateTime object
		/// </summary>
		/// <param name="s">date/time string </param>
		/// <returns>
		/// A DateTime object representing the ISO 8601 compliant date/time string
		/// </returns>
		/// <example>
		/// date string with dashes: "yyyy-MM-dd": 2019-04-19
		/// date string without dashes: "yyyyMMdd": 20190419
		/// </example>
		public static DateTime ParseDateTimeString(string s)
		{
			try
			{
				string[] formats = { "yyyy-MM-dd","yyyyMMdd", "yyyyMMddTHHmmssZ" };
				//string format = "yyyy-MM-dd";
				var cultureInfo = new CultureInfo("en-US");
				DateTime reportAbsoluteDateTime = DateTime.ParseExact(s, formats, cultureInfo);
				return reportAbsoluteDateTime;
			}
			catch (ArgumentNullException ane)
			{
				s ??= "''";
				//string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(ane, "23EJ", EdiErrorsService.BuildErrorVariableArrayList(s));
				string customErrorMessage = "23EJ: An null valued exception was thrown while converting date {0}";
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				s ??= "''";
				//string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "7ZE5", EdiErrorsService.BuildErrorVariableArrayList(s));
				string customErrorMessage = "7ZE5: An exception was thrown while converting date {0}";
				throw new Exception(customErrorMessage);
			}
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
		/// Converts a date and time object to a ISO 8601 compliant date string representation
		/// </summary>
		/// <param name="s">Date and time object </param>
		/// <returns>
		/// An ISO 8601 compliant date string
		/// </returns>
		public static string ConverToDateString(DateTime? dt)
		{
			if (dt != null)
			{
				return ((DateTime)dt).ToString("yyyy-MM-dd");
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts a date and time object to a ISO 8601 compliant date time string representation
		/// </summary>
		/// <param name="s">Date and time object </param>
		/// <returns>
		/// An ISO 8601 compliant date/time string
		/// </returns>
		public static string ConverToDateTimeString(DateTime? dt)
		{
			if (dt != null)
			{
				return ((DateTime)dt).ToString("yyyy-MM-ddTHH:mm:ssZ");
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Converts a date and time object to a ISO 8601 compliant date time string representation
		/// </summary>
		/// <param name="s">Date and time object </param>
		/// <returns
		/// An ISO 8601 compliant date/time string
		/// </returns>
		public static string ConvertToUtcDateTimeNowString(string format)
		{
			//string format = "yyyyMMddTHHmmssZ"; //20211018T164303Z
			return DateTime.UtcNow.ToString(format);
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

		/// <summary>
		/// Converts Unix time to .NET DateTime object
		/// </summary>
		/// <returns>
		/// .NET DateTime object representing the Unix time
		/// </returns>
		public static DateTime FromUnixTimeSeconds(long unixTime)
		{
			return epoch.AddSeconds(unixTime);
		}

		public static DateTime FromUnixTimeMilliseconds(long unixTime)
		{
			return epoch.AddMilliseconds(unixTime);
		}

		
	}
}
