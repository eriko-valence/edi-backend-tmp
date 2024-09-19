using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Dto.Azure.AppInsights
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
