CREATE TYPE [ems_data].[mf_logger_data_type] AS TABLE(
	[AMFR] [varchar](100) NOT NULL,
	[AMOD] [varchar](100) NOT NULL,
	[APQS] [varchar](50) NOT NULL,
	[ASER] [varchar](50) NOT NULL,
	[AID] [varchar](50) NOT NULL,
	[ADAT] [date] NOT NULL, 
	[CID] [varchar](10) NOT NULL,
	[FID] [varchar](100) NOT NULL,
	[LAT] [numeric](7, 5) NOT NULL,
	[LNG] [numeric](7, 5) NOT NULL,
	[ABST] [varchar](25) NOT NULL,
	[TAMB] [numeric](3, 1) NOT NULL,
	[TFRZ] [numeric](3, 1) NOT NULL,
	[TVC] [numeric](3, 1) NOT NULL,
	[CMPR] [tinyint] NOT NULL,
	[SVA] [smallint] NOT NULL,
	[EVDC] [numeric](3, 1) NOT NULL,
	[CDRW] [tinyint] NOT NULL,
	[DOOR] [tinyint] NOT NULL,
	[HOLD] [numeric](4, 1) NOT NULL,
	[BEMD] [numeric](5, 1) NOT NULL,
	[TCON] [numeric](3, 1) NOT NULL,
	[CMPS] [smallint] NOT NULL,
	[CSOF] [varchar](50) NOT NULL,
	[ALRM] [varchar](10) NULL
)
GO