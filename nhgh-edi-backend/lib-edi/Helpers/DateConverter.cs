﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using lib_edi.Services.Errors;

namespace lib_edi.Helpers
{
	/// <summary>
	/// Helper class for converting date/time objects
	/// </summary>
	public class DateConverter
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
		public static DateTime? ConvertIso8601CompliantString(string s)
		{
			try
			{
				string format = "yyyyMMddTHHmmssZ"; //20211018T164303Z
				var cultureInfo = new CultureInfo("en-US");
				DateTime? reportAbsoluteDateTime = DateTime.ParseExact(s, format, cultureInfo);
				return reportAbsoluteDateTime;
			}
			catch (ArgumentNullException ane)
			{
				s ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(ane, "D23W", EdiErrorsService.BuildErrorVariableArrayList(s));
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				s ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "HQ37", EdiErrorsService.BuildErrorVariableArrayList(s));
				throw new Exception(customErrorMessage);
			}

		}

		/// <summary>
		/// Converts a date string with dashes
		/// </summary>
		/// <param name="s">date/time string in format "yyyy-MM-dd" </param>
		/// <returns>
		/// A DateTime object representing the ISO 8601 compliant date/time string
		/// </returns>
		/// <example>
		/// date string with dashes in format "yyyy-MM-dd": 2019-04-19
		/// </example>
		public static DateTime ConvertDateWithDashesString(string s)
		{
			try
			{
				string format = "yyyy-MM-dd";
				var cultureInfo = new CultureInfo("en-US");
				DateTime reportAbsoluteDateTime = DateTime.ParseExact(s, format, cultureInfo);
				return reportAbsoluteDateTime;
			}
			catch (ArgumentNullException ane)
			{
				s ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(ane, "23EJ", EdiErrorsService.BuildErrorVariableArrayList(s));
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				s ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "7ZE5", EdiErrorsService.BuildErrorVariableArrayList(s));
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
