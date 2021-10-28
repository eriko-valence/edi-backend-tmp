CREATE TABLE [ems_data].[logger_data](
	[AMFR] [varchar](100) NULL,
	[AMOD] [varchar](100) NULL,
	[APQS] [varchar](50) NULL,
	[ASER] [varchar](50) NULL,
	[AID] [varchar](50) NULL,
	[CID] [varchar](10) NULL,
	[FID] [varchar](100) NULL,
	[LAT] [numeric](7, 5) NULL,
	[LNG] [numeric](8, 5) NULL,
	[ABST] [datetime] NULL,
	[TAMB] [numeric](3, 1) NULL,
	[TFRZ] [numeric](3, 1) NULL,
	[TVC] [numeric](3, 1) NULL,
	[CMPR] [tinyint] NULL,
	[SVA] [smallint] NULL,
	[HOLD] [numeric](4, 1) NULL,
	[BEMD] [numeric](5, 1) NULL,
	[TCON] [numeric](4, 1) NULL,
	[CMPS] [smallint] NULL,
	[CSOF] [varchar](50) NULL,
	[ALRM] [varchar](10) NULL,
	[ADOP] [date] NULL,
	[CDAT] [date] NULL,
	[CNAM] [varchar](20) NULL,
	[CSER] [varchar](20) NULL,
	[DNAM] [varchar](20) NULL,
	[EDOP] [date] NULL,
	[EID] [varchar](20) NULL,
	[EMFR] [varchar](20) NULL,
	[EMOD] [varchar](20) NULL,
	[EMSV] [varchar](20) NULL,
	[EPQS] [varchar](20) NULL,
	[ESER] [varchar](20) NULL,
	[FNAM] [varchar](20) NULL,
	[LDOP] [date] NULL,
	[LID] [varchar](20) NULL,
	[LMFR] [varchar](20) NULL,
	[LMOD] [varchar](20) NULL,
	[LPQS] [varchar](20) NULL,
	[LSER] [varchar](20) NOT NULL,
	[LSV] [varchar](20) NULL,
	[RNAM] [varchar](20) NULL,
	[ACCD] [decimal](4, 2) NULL,
	[ACSV] [decimal](4, 1) NULL,
	[BLOG] [decimal](5, 1) NULL,
	[DCCD] [decimal](4, 1) NULL,
	[DCSV] [decimal](4, 1) NULL,
	[DORF] [int] NULL,
	[DORV] [int] NULL,
	[EERR] [varchar](20) NULL,
	[FANS] [decimal](4, 1) NULL,
	[HAMB] [decimal](4, 1) NULL,
	[HCOM] [decimal](4, 1) NULL,
	[LERR] [varchar](20) NULL,
	[MSW] [tinyint] NULL,
	[RELT] [varchar](20) NOT NULL,
	[RTCW] [varchar](20) NULL,
	[TPCB] [decimal](5, 2) NULL,
	[_RELT_SECS] [varchar](20) NULL,
	[_ABST] [varchar](20) NULL,
	[DATEADDED] [datetime] NOT NULL,
PRIMARY KEY CLUSTERED 
(
	[LSER] ASC,
	[RELT] ASC
)WITH (STATISTICS_NORECOMPUTE = OFF, IGNORE_DUP_KEY = OFF) ON [PRIMARY]
) ON [PRIMARY]
GO