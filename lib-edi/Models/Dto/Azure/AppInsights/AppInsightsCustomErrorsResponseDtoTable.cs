using System;
using System.Collections.Generic;
using System.Text;

namespace lib_edi.Models.Dto.Azure.AppInsights
{
	public class AppInsightsCustomErrorsResponseDtoTable
	{
		public AppInsightsCustomErrorsResponseDtoTable()
		{

		}
		public string name { get; set; }
		public List<AppInsightsCustomErrorsResponseDtoTableColumn> columns { get; set; }
		public List<List<object>> rows { get; set; }
	}
}
