using lib_edi.Services.Errors;
using System;
using System.Collections.Generic;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

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
				//string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(ane, "G375", EdiErrorsService.BuildErrorVariableArrayList(hex));
				string customErrorMessage = "G375: An null valued exception was thrown while converting hex to bigint";
				throw new Exception(customErrorMessage);
			}
			catch (Exception e)
			{
				hex ??= "''";
				//string customErrorMessage = await EdiErrorsService.BuildExceptionMessageString(e, "T216", EdiErrorsService.BuildErrorVariableArrayList(hex));
				string customErrorMessage = "T216: An exception was thrown while converting hex to bigint";
				throw new Exception(customErrorMessage);
			}
		} 

		public static string ConvertToHexadecimal(BigInteger bi)
		{
			return bi.ToString("X");
		}
	}
}
