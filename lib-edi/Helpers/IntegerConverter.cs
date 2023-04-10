using lib_edi.Services.Errors;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;

namespace lib_edi.Helpers
{
	public class IntegerConverter
	{
		public static BigInteger ConvertToBigInteger(string hex)
		{
			try
			{
				return BigInteger.Parse(hex, System.Globalization.NumberStyles.HexNumber);
			}
			catch (ArgumentNullException ane)
			{
				hex ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(ane, "G375", EdiErrorsService.BuildErrorVariableArrayList(hex));
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				hex ??= "''";
				string customErrorMessage = EdiErrorsService.BuildExceptionMessageString(e, "T216", EdiErrorsService.BuildErrorVariableArrayList(hex));
				throw new Exception(customErrorMessage);
			}
		} 

		public static string ConvertToHexadecimal(BigInteger bi)
		{
			return bi.ToString("X");
		}
	}
}
