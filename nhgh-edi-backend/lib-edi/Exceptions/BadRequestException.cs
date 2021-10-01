using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Exceptions
{
	/// <summary>
	/// A derived exception class for bad http requests
	/// </summary>
	[Serializable]
	public class BadRequestException : Exception
	{
		/// <summary>
		/// A derived exception class for bad http requests
		/// </summary>
		/// <param name="errorMessage">Custom error message</param>
		/// <returns>
		/// A derived exception object with a custom error
		/// </returns>
		public BadRequestException(string errorMessage): base(String.Format(errorMessage))
		{

		}
	}
}
