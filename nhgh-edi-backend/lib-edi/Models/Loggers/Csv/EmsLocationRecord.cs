using CsvHelper.Configuration.Attributes;
using lib_edi.Models.Csv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class EmsLocationRecord : EdiSinkRecord
    {
        [Name("LSER")]
        public string LSER { get; set; }
        [Ignore]
        //public string zgps_abst { get; set; }
        public string ZGPS_ABST { get; set; }

        [Name("zgps_abst")]
        public DateTime? EDI_ZGPS_ABST_DATETIME { get; set; }

        [Name("zgps_ang")]
        public string ZGPS_ANG { get; set; }
        [Name("zgps_lat")]
        public string ZGPS_LAT { get; set; }
        [Name("zgps_lng")]
        public string ZGPS_LNG { get; set; }
    }
}
