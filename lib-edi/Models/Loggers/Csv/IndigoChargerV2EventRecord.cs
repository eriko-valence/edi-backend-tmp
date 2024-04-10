using CsvHelper.Configuration.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Loggers.Csv
{
    public class IndigoChargerV2EventRecord : EmsEventRecord
    {
        /*
        	-- z values, specific to indigo charger
			[ZSNAME] [varchar](16) NULL,
			[ZCHRG] [varchar](20) NULL,
			ZVCSER [varchar](50) NULL,
			ZTAMB [numeric](4, 1) NULL,
			ZTEVP [numeric](4, 1) NULL,
			ZTHTR [numeric](4, 1) NULL,
			ZTHTL [numeric](4, 1) NULL,
			ZTHBR [numeric](4, 1) NULL,
			ZTHBL [numeric](4, 1) NULL,
			ZTCLR [numeric](4, 1) NULL,
			ZACSV [numeric](4, 1) NULL,
			ZACCD [numeric](4, 2) NULL,
			ZACSF [numeric](4, 1) NULL,

			ZACPD [numeric](5, 1) NULL,
			ZACED [numeric](5, 1) NULL,

			ZLFS [smallint] NULL,
			ZLFC [smallint] NULL,
			ZIFC [smallint] NULL,

			ZLID [int] null,
			ZTCO [int] null,

			ZVENT [varchar](16) NULL
        */
        [Name("ZTAMB")]
        public double? ZTAMB { get; set; }

        [Name("ZSNAME")]
        public string ZSNAME { get; set; }
        //[Name("ZCHRG")]
        //public string ZCHRG { get; set; }
        [Name("ZVCSER")]
        public string ZVCSER { get; set; }
        [Name("ZTEVP")]
        public double? ZTEVP { get; set; }

        [Name("ZTHTR")]
        public double? ZTHTR { get; set; }

        [Name("ZTHTL")]
        public double? ZTHTL { get; set; }

        [Name("ZTHBR")]
        public double? ZTHBR { get; set; }

        [Name("ZTHBL")]
        public double? ZTHBL { get; set; }

        [Name("ZTCLR")]
        public double? ZTCLR { get; set; }

        [Name("ZACSV")]
        public double? ZACSV { get; set; }

        [Name("ZACCD")]
        public double? ZACCD { get; set; }

        [Name("ZACSF")]
        public double? ZACSF { get; set; }

        [Name("ZACPD")]
        public double? ZACPD { get; set; }

        [Name("ZACED")]
        public double? ZACED { get; set; }


        [Name("ZLFS")]
        public Int16? ZLFS { get; set; }

        [Name("ZLFC")]
        public Int16? ZLFC { get; set; }

        [Name("ZIFC")]
        public Int16? ZIFC { get; set; }

        [Name("ZLID")]
        public int ZLID { get; set; }

        [Name("ZTCO")]
        public int ZTCO { get; set; }

        [Name("ZVENT")]
        public string ZVENT { get; set; }

    }
}
