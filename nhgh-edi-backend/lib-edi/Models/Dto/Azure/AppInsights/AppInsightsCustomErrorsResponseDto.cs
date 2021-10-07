using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Azure.AppInsights
{
	public class AppInsightsCustomErrorsResponseDto
	{
		public AppInsightsCustomErrorsResponseDto()
		{

		}

		public List<AppInsightsCustomErrorsResponseDtoTable> tables { get; set; }
	}
}
