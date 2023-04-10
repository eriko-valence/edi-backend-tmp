using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Ccdx
{
	/// <summary>
	/// Data model for CCDX provider http metadata dummy data
	/// </summary>
	public class CcdxProviderSampleHeadersDto
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member
		public CcdxProviderSampleHeadersDto()
		{
			Facility = new CcdxProviderSampleHeadersFacility();
			Fridge = new CcdxProviderSampleHeadersFridge();
			Location = new CcdxProviderSampleHeadersLocation();
			Logger = new CcdxProviderSampleHeadersLogger();
		}
		[JsonProperty("facility")]
		public CcdxProviderSampleHeadersFacility Facility { get; set; }
		[JsonProperty("fridge")]
		public CcdxProviderSampleHeadersFridge Fridge { get; set; }
		[JsonProperty("location")]
		public CcdxProviderSampleHeadersLocation Location { get; set; }
		[JsonProperty("logger")]
		public CcdxProviderSampleHeadersLogger Logger { get; set; }
#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member

	}

	/// <summary>
	/// Data model for CCDX provider http metadata facility dummy data
	/// </summary>
	public class CcdxProviderSampleHeadersFacility
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		[JsonProperty("dx-facility-name")]
		public string FacilityName { get; set; }

		[JsonProperty("dx-facility-contact-name")]
		public string ContactName { get; set; }

		[JsonProperty("dx-facility-contact-phone")]
		public string ContactPhone { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Data model for CCDX provider http metadata fridge dummy data
	/// </summary>
	public class CcdxProviderSampleHeadersFridge
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		[JsonProperty("dx-fridge-manufacturer")]
		public string Manufacturer { get; set; }

		[JsonProperty("dx-fridge-model")]
		public string Model { get; set; }

		[JsonProperty("dx-fridge-assigned-id")]
		public string AssignedID { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Data model for CCDX provider http metadata location dummy data
	/// </summary>
	public class CcdxProviderSampleHeadersLocation
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		[JsonProperty("dx-location-latitude")]
		public string Latitude { get; set; }

		[JsonProperty("dx-location-longitude")]
		public string Longitude { get; set; }

		[JsonProperty("dx-location-accuracy")]
		public string Accuracy { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}

	/// <summary>
	/// Data model for CCDX provider http metadata logger dummy data
	/// </summary>
	public class CcdxProviderSampleHeadersLogger
	{
#pragma warning disable CS1591 // Missing XML comment for publicly visible type or member

		[JsonProperty("dx-tdl-manufacturer")]
		public string Manufacturer { get; set; }

		[JsonProperty("dx-tdl-model")]
		public string Model { get; set; }

		[JsonProperty("dx-tdl-serial")]
		public string Serial { get; set; }

#pragma warning restore CS1591 // Missing XML comment for publicly visible type or member
	}
}
