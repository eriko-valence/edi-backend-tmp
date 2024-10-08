﻿using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class Sl1EventRecord : EmsEventRecord
    {
        /*
         * ELECTRICAL PROPERTIES
         */
        [Name("DCCD")]
        public string DCCD { get; set; }
        [Name("DCSV")]
        public string DCSV { get; set; }
        [Name("FANS")]
        public string FANS { get; set; }
        [Name("SVA")]
        public string SVA { get; set; }

        /*
         * COMPRESSOR PROPERTIES
         */
        [Name("CNAM")]
        public string CNAM { get; set; }
        [Name("CSER")]
        public string CSER { get; set; }
        [Name("CSOF")]
        public string CSOF { get; set; }
        [Name("CMPR")]
        public string CMPR { get; set; }
        [Name("CMPS")]
        public string CMPS { get; set; }

        /*
         * OTHER PROPERTIES
         */
        [Name("DORF")]
        public string DORF { get; set; }
        [Name("TFRZ")]
        public string TFRZ { get; set; }
        [Name("ALRM")]
        public string ALRM { get; set; }
        [Name("BLOG")]
        public double? BLOG { get; set; }
        [Name("ZSTATE")]
        public int? ZSTATE { get; set; }
        [Name("ZVLVD")]
        public int? ZVLVD { get; set; }
        [Name("TAMB")]
        public double? TAMB { get; set; }
        [Name("TVC")]
        public double? TVC { get; set; }
        [Name("HOLD")]
        public double? HOLD { get; set; }
        [Name("DORV")]
        public string DORV { get; set; }
    }
}
