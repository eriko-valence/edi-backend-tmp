using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi_in_process.Models.Dto.Azure.AppInsights
{
    public class AppInsightsCustomErrorsResponseDtoTableColumn
    {
        public AppInsightsCustomErrorsResponseDtoTableColumn()
        {

        }
        public string name { get; set; }
        public string type { get; set; }
    }
}
