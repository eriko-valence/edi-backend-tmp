CREATE TYPE [ems_data].[mf_logger_data_type] AS TABLE(
	[AMFR] [varchar](100) NULL,
	[AMOD] [varchar](100) NULL,
	[ASER] [varchar](50) NOT NULL,
	[ADOP] [varchar](100) NULL,
	[APQS] [varchar](100) NULL,
	[RNAM] [varchar](100) NULL,
	[DNAM] [varchar](100) NULL,
	[FNAM] [varchar](100) NULL,
	[CID] [varchar](10) NULL,
	[LAT] [numeric](7, 5) NULL,
	[LNG] [numeric](8, 5) NULL,

	[ABST] [datetime] NOT NULL,
	[SVA] [smallint] NULL,
	[HAMB] [numeric](3, 1) NULL,
	[TAMB] [numeric](3, 1) NULL,
	[ACCD] [numeric](3, 1) NULL,
	[TCON] [numeric](3, 1) NULL,
	[TVC] [numeric](3, 1) NULL,
	[BEMD] [numeric](5, 1) NULL,
	[HOLD] [numeric](4, 1) NULL,
	[DORV] [tinyint] NULL,
	[ALRM] [varchar](10) NULL
)
GO