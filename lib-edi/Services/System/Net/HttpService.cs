using lib_edi.Exceptions;
using lib_edi.Models.Dto.Http;
using lib_edi.Services.Errors;
using lib_edi.Services.System.IO;
using Microsoft.AspNetCore.Http; // Microsoft.NET.Sdk.Functions
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace lib_edi.Services.System.Net
{
	/// <summary>
	/// A class that provides methods related to manage http objects
	/// </summary>
	public class HttpService
	{
		/// <summary>
		/// Deserializes an EMS log transformation http request body (application/json)
		/// </summary>
		/// <param name="emsLogTransformationHttpRequest">EMS log transformation http request body</param>
		/// <returns>
		/// A deserialized object of the EMS log transformation http reseponse body if successful; Exception (SB49) otherwise
		/// </returns>
		public static async Task<TransformHttpRequestMessageBodyDto> DeserializeHttpRequestBody(HttpRequest emsLogTransformationHttpRequest)
		{
			try
			{
				string requestBody = String.Empty;
				using (StreamReader streamReader = new StreamReader(emsLogTransformationHttpRequest.Body))
				{
					requestBody = await streamReader.ReadToEndAsync();
				}
				return JsonConvert.DeserializeObject<TransformHttpRequestMessageBodyDto>(requestBody);
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "SB49", null);
				throw new Exception(customError);
			}
		}

		/// <summary>
		/// Serializes an EMS log transformation http response body
		/// </summary>
		/// <param name="csvBlobName">Name of csv azure storage blob</param>
		/// <returns>
		/// A serialized string of the EMS log transformation http reseponse body if successful; Exception (X83E) otherwise
		/// </returns>
		public static async Task<string> SerializeHttpResponseBody(string csvBlobName)
		{
			try
			{
				TransformHttpResponseMessageBodyDto emsLogResponseDto = new TransformHttpResponseMessageBodyDto();
				emsLogResponseDto.Path = csvBlobName;
				return JsonConvert.SerializeObject(emsLogResponseDto);
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "X83E", null);
				throw new Exception(customError);
			}
		}

		/// <summary>
		/// Gets a header value from HTTP request message object
		/// </summary>
		/// <remarks>
		/// An exception is thrown if the validation fails
		/// </remarks>
		/// <param name="hrm">HTTP request message</param>
		/// <param name="headerName">Header name</param>
		/// <returns>
		/// Header value as string
		/// </returns>
		public static string GetHeaderStringValue(HttpRequestMessage hrm, string headerName)
		{
			IEnumerable<string> ceid;
			if (hrm.Headers.TryGetValues(headerName, out ceid))
			{
				return ceid.FirstOrDefault();
			}
			else
			{
				return null;
			}
		}

		/// <summary>
		/// Validates an EMS log transformation http request body for required properties
		/// </summary>
		/// <remarks>
		/// An exception is thrown if the validation fails
		/// </remarks>
		/// <param name="body">EMS log http request body</param>
		/// <returns>
		/// True if validation passes; BadRequestException if body missing (SR54) or 'path' property missing (26ZZ)
		/// </returns>
		public static async Task<bool> ValidateHttpRequestBody(TransformHttpRequestMessageBodyDto body)
		{
			bool result;
			if (body != null)
			{
				if (body.Path != null && body.FileName != null)
				{
					result = true;
				}
				else
				{
					string customError = await EdiErrorsService.BuildExceptionMessageString(null, "26ZZ", null);
					throw new BadRequestException(customError);
				}
			}
			else
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(null, "SR54", null);
				throw new BadRequestException(customError);
			}
			return result;
		}

		/// <summary>
		/// Sends HTTP request message
		/// </summary>
		/// <param name="requestMessage">HTTP request message</param>
		/// <returns>
		/// Status code of HTTP reseponse
		/// </returns>
		public static async Task<HttpStatusCode> SendHttpRequestMessage(HttpRequestMessage requestMessage)
		{
			try
			{
				HttpClient httpClient = new HttpClient();
				HttpResponseMessage httpResponse = await httpClient.SendAsync(requestMessage, HttpCompletionOption.ResponseContentRead, CancellationToken.None);
				return httpResponse.StatusCode;
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "H39L", EdiErrorsService.BuildErrorVariableArrayList(requestMessage.Method.ToString(), requestMessage.RequestUri.ToString()));
				throw new BadRequestException(customError);
			}

		}

		/// <summary>
		/// Builds an HTTP multipart/form-data MIME container with byte array content.
		/// </summary>
		/// <param name="s">Stream of bytes to include in request message</param>
		/// <param name="name">HTTP content key name</param>
		/// <param name="fileName">HTTP content key file name</param>
		/// <returns>
		/// An HTTP multipart/form-data MIME container with byte array content.
		/// </returns>
		public static async Task<MultipartFormDataContent> BuildMultipartFormDataByteArrayContent(Stream s, string name, string fileName)
		{
			try
			{
				byte[] byteArray = await StreamService.ReadToEnd(s);
				string boundary = $"----{Guid.NewGuid()}";
				MultipartFormDataContent multiPartContent = new MultipartFormDataContent(boundary);
				ByteArrayContent byteArrayContent = new ByteArrayContent(byteArray);
				multiPartContent.Add(byteArrayContent, name, fileName);
				return multiPartContent;
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "YT86", EdiErrorsService.BuildErrorVariableArrayList(name, fileName));
				throw new BadRequestException(customError);
			}

		}

		/// <summary>
		/// Builds an HTTP multipart/form-data MIME container with string content.
		/// </summary>
		/// <param name="s">Stream of bytes to include in request message</param>
		/// <param name="name">HTTP content key name</param>
		/// <param name="fileName">HTTP content key file name</param>
		/// <returns>
		/// An HTTP multipart/form-data MIME container with string content.
		/// </returns>
		public static async Task<MultipartFormDataContent> BuildMultipartFormDataStringContent(string s, string name, string fileName)
		{
			try
			{
				string boundary = $"----{Guid.NewGuid()}";
				MultipartFormDataContent multiPartContent = new MultipartFormDataContent(boundary);
				StringContent stringContent = new StringContent(s);
				multiPartContent.Add(stringContent, name, fileName);
				return multiPartContent;
			}
			catch (Exception e)
			{
				string customError = await EdiErrorsService.BuildExceptionMessageString(e, "32L6", EdiErrorsService.BuildErrorVariableArrayList(name, fileName));
				throw new BadRequestException(customError);
			}

		}
	}
}
