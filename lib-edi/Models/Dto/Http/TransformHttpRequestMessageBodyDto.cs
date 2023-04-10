using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Http
{
	public class TransformHttpRequestMessageBodyDto
	{
		#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		[JsonProperty("path")]
		public string Path { get; set; }
        [JsonProperty("type")]
        public string LoggerType { get; set; }
        [JsonProperty("fileName")]
		public string FileName { get; set; }
		#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
