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

        [Name("ALRM")]
        public string ALRM { get; set; }
        [Name("TVC")]
        public double? TVC { get; set; }
        [Name("HOLD")]
        public double? HOLD { get; set; }
        [Name("DORV")]
        public string DORV { get; set; }
        [Name("BLOG")]
        public double? BLOG { get; set; }
        [Name("ZSTATE")]
        public int? ZSTATE { get; set; }
        [Name("ZVLVD")]
        public int? ZVLVD { get; set; }
        [Name("TAMB")]
        public double? TAMB { get; set; }

        // NHGH-3191 2023.11.03 0928 New field introduced with logger firmware v1.0.5-C
        [Name("MSW")]
        public string MSW { get; set;}
        // NHGH-3287 2024.02.07 1614 New Indigo logger object
        [Name("ZXCLC")]
        public double? ZXCLC { get; set; }
        // NHGH-3287 2024.02.07 1614 New Indigo logger object
        [Name("DRCV")]
        public int DRCV { get; set; }
    }
}
