using lib_edi.Models.Domain.Errors;
using Newtonsoft.Json;
using NJsonSchema.Validation;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace lib_edi.Services.Errors
{
	public class EdiErrorsService
	{
		static EmsErrors emsErrors = null;

		/// <summary>
		/// Deserializes EMS error code definition file
		/// </summary>
		public static void Initialize()
		{
			if (emsErrors == null)
			{
				//var emsErrorCodeFile = Path.Combine(context.FunctionAppDirectory, "Config", "ems_error_codes.json");
				var binpath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
				var rootpath = Path.GetFullPath(Path.Combine(binpath, ".."));
				var emsErrorCodeFile = Path.Combine(rootpath, "Config", "edi_error_codes.json");

				try
				{
					using StreamReader r = new StreamReader(emsErrorCodeFile);
					string jsonString = r.ReadToEnd();
					emsErrors = JsonConvert.DeserializeObject<EmsErrors>(jsonString);
				}
				catch (Exception e)
				{
					Console.WriteLine($"Unable to load the EMS error code json file {emsErrorCodeFile}: {e.Message}");
				}
			}
		}

		/// <summary>
		/// Looks up EMS error object in EMS error code definition file
		/// </summary>
		/// <param name="emsErrorCode">EMS error code as string</param>
		/// <returns>
		/// EMS error object matching provided EMS error code
		/// </returns>
		public static EmsError GetError(string emsErrorCode)
		{
			EmsError emsError;
			Initialize();
			if (emsErrors != null)
			{
				bool hasValue = emsErrors.list.TryGetValue(emsErrorCode, out emsError);
				if (hasValue)
				{
					return emsError;
				}
				else
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
		/// Builds a list of custom EMS variable values
		/// </summary>
		/// <param name="variable1">EMS custom error variable value #1</param>
		/// <param name="variable2">EMS custom error variable value #2</param>
		/// <param name="variable3">EMS custom error variable value #3</param>
		/// <param name="variable4">EMS custom error variable value #4</param>
		/// <returns>
		/// A list of custom EMS variable values
		/// </returns>
		public static ArrayList BuildErrorVariableArrayList(string variable1 = null, string variable2 = null, string variable3 = null, string variable4 = null)
		{
			ArrayList listErrorVariables = new ArrayList();
			if (variable1 != null)
			{
				listErrorVariables.Add(variable1);
			}

			if (variable2 != null)
			{
				listErrorVariables.Add(variable2);
			}

			if (variable3 != null)
			{
				listErrorVariables.Add(variable3);
			}

			if (variable4 != null)
			{
				listErrorVariables.Add(variable4);
			}

			return listErrorVariables;
		}

		public static string AppendAllExceptionMessages(Exception e)
		{
			if (e != null)
			{
				string appendedMessages = BuildExceptionString(e);
				while (e.InnerException != null)
				{
					e = e.InnerException;
					appendedMessages += BuildExceptionString(e);
				}
				return appendedMessages;
			} else
			{
				return "";
			}
		}

		public static string BuildExceptionString(Exception e)
		{
			if (e != null)
			{
				return $"Message: {e.Message}, Class: {e.GetType()}, HResult: {e.HResult}, Source: {e.Source}";
			} else
			{
				return null;
			}
		}



		/// <summary>
		/// Builds a custom exception message string with the provided exception object and custom error information
		/// </summary>
		/// <param name="e">Excepion object</param>
		/// <param name="customErrorCode">Custom error code</param>
		/// <param name="errorVariables">Custom error variable values</param>
		/// <returns>
		/// A custom exception message string
		/// </returns>
		public static string BuildExceptionMessageString(Exception e, string customErrorCode, ArrayList errorVariables)
		{
			EmsError emsError = EdiErrorsService.GetError(customErrorCode);
			string customErrorMessage = BuildCustomErrorMessage(emsError, errorVariables);

			string message = null;
			if (e != null && customErrorMessage != null && customErrorCode != null)
			{
				message = $"{customErrorCode}: {customErrorMessage}. {AppendAllExceptionMessages(e)}";
			}
			else if (customErrorMessage != null && customErrorCode != null)
			{
				message = $"{customErrorCode}: {customErrorMessage}.";
			}
			return message;
		}

		/// <summary>
		/// Builds a custtom error message string using EMS error message template and variable values 
		/// </summary>
		/// <param name="emsError">EMS error object pulled from EMS error definition file</param>
		/// <param name="errorVariables">A list of custom EMS variable values</param>
		/// <returns>
		/// A custom error message string
		/// </returns>
		public static string BuildCustomErrorMessage(EmsError emsError, ArrayList errorVariables)
		{
			string customErrorMessage = null;

			if (emsError != null)
			{
				if (errorVariables != null)
				{
					if (errorVariables.Count > 0 && errorVariables.Count < 5)
					{
						if (errorVariables.Count == 1)
						{
							string abc = (string)errorVariables[0];
							customErrorMessage = string.Format(emsError.Message, errorVariables[0]);
							Console.WriteLine("debug");
						}
						else if (errorVariables.Count == 2)
						{
							customErrorMessage = string.Format(emsError.Message, errorVariables[0], errorVariables[1]);
						}
						else if (errorVariables.Count == 3)
						{
							customErrorMessage = string.Format(emsError.Message, errorVariables[0], errorVariables[1], errorVariables[2]);
						}
						else if (errorVariables.Count == 4)
						{
							customErrorMessage = string.Format(emsError.Message, errorVariables[0], errorVariables[1], errorVariables[2], errorVariables[3]);
						}
					}
					else
					{
						customErrorMessage = emsError.Message;
					}
				}
				else
				{
					customErrorMessage = emsError.Message;
				}
			}
			else
			{
				customErrorMessage = "There was a problem looking up the EMS error code";
			}

			return customErrorMessage;
		}

		/// <summary>
		/// Gets the inner most exception message of an Exception object; Return null if no inner exception
		/// </summary>
		public static string GetInnerException(Exception e1)
		{
			string exceptionMessage = null;

			/*
			if (e1 != null)
			{
				exceptionMessage = $"Class: {e1.GetType()}, HResult: {e1.HResult}, Source: {e1.Source}, Message: {e1.Message}";
			}
			*/

			while (e1.InnerException != null)
			{
				exceptionMessage = $"Class: {e1.InnerException.GetType()}, HResult: {e1.InnerException.HResult}, Source: {e1.InnerException.Source}, Message: {e1.InnerException.Message}";
				e1 = e1.InnerException;
			}
			return exceptionMessage;
		}

		/// <summary>
		/// Builds a json validation error string from NJsonSchema.Validation.ValidationError
		/// </summary>
		/// <param name="errors">Collection of NJsonSchema.Validation.ValidationError objects</param>
		/// <returns>
		/// String results pulled from the first NJsonSchema.Validation.ValidationError in the collection
		/// </returns>
		public static string BuildJsonValidationErrorString(ICollection<ValidationError> errors)
		{
			string result = "";

			if (errors != null)
			{
				if (errors.Count > 0)
				{
					List<ValidationError> e = errors.ToList();
					ValidationError ve = e[0];

					if (ve != null)
					{
						result = ve.ToString();
					}
				}
			}

			return result;
		}
	}
}
