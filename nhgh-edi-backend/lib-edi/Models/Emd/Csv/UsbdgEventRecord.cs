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
        [Ignore]
        public string ABST { get; set; }
        [Name("ABST")]
        public DateTime? EDI_ABST_DATETIME { get; set; }
        [Name("BEMD")]
        public double BEMD { get; set; }
        [Name("EERR")]
        public string EERR { get; set; }
        [Name("ESER")]
        public string ESER { get; set; }
        [Name("zutc_now")]
        public DateTime? EDI_ZUTC_NOW_DATETIME { get; set; }
        [Ignore]
        public string ZUTC_NOW { get; set; }
        [Name("zcell_info")]
        public string ZCELL_INFO { get; set; }
        [Name("zbatt_volt")]
        public Int16 ZBATT_VOLT { get; set; }
        [Name("zbatt_chrg")]
        public Byte ZBATT_CHRG { get; set; }
    }
}
