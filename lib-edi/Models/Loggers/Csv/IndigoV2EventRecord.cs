using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class IndigoV2EventRecord : EmsEventRecord
    {

        [Name("HOLD")]
        public double? HOLD { get; set; }
        // NHGH-3191 2023.11.03 0928 New field introduced with logger firmware v1.0.5-C
        [Name("MSW")]
        public string MSW { get; set;}
    }
}
