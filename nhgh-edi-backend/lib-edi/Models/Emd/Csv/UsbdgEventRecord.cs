using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Emd.Csv
{
    public class UsbdgEventRecord : EdiSinkRecord
    {
        [Name("ABST")]
        public DateTime? ABST { get; set; }
        [Name("BEMD")]
        public double BEMD { get; set; }
        [Name("EERR")]
        public string EERR { get; set; }
        [Name("ESER")]
        public string ESER { get; set; }
        [Name("zcell_info")]
        public string zcell_info { get; set; }
    }
}
