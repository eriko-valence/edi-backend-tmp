using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Csv
{
    public class EdiSinkRecord
    {
        [Ignore]
        public string EDI_SOURCE { get; set; }
    }
}
