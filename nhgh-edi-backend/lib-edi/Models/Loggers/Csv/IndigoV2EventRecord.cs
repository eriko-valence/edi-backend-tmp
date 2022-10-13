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
        [Name("DORV")]
        public int? DORV { get; set; }

        [Name("HOLD")]
        public double? HOLD { get; set; }
    }
}
