using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Domain.Errors
{
	public class EmsErrors
	{
		public EmsErrors()
		{
			list = new Dictionary<string, EmsError>();
		}
		public Dictionary<string, EmsError> list { get; set; }
	}

	public class EmsError
	{
		[JsonProperty("error-code")]
		public string Code { get; set; }
		[JsonProperty("error-message")]
		public string Message { get; set; }
		[JsonProperty("error-source")]
		public string Source { get; set; }
		[JsonProperty("error-mitigation")]
		public string Mitigation { get; set; }

	}
}
