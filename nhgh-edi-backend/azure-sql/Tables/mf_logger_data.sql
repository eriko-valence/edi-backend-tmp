CREATE TABLE [ems_data].[mf_logger_data](
	[AMFR] [varchar](37) NOT NULL,
	[AMOD] [varchar](10) NOT NULL,
	[APQS] [varchar](8) NOT NULL,
	[ASER] [varchar](16) NOT NULL,
	[AID] [varchar](20) NOT NULL,
	[ADAT] [date] NOT NULL,
	[CID] [varchar](3) NOT NULL,
	[FID] [varchar](46) NOT NULL,
	[LOC] [varchar](15) NOT NULL,
	[ABST] [varchar](19) NOT NULL,
	[TAMB] [numeric](4, 1) NOT NULL,
	[TCLD] [numeric](3, 1) NOT NULL,
	[TVC] [numeric](3, 1) NOT NULL,
	[CMPR] [bit] NOT NULL,
	[SVA] [int] NOT NULL,
	[EVDC] [numeric](4, 1) NOT NULL,
	[CDRW] [numeric](4, 1) NOT NULL,
	[DOOR] [bit] NOT NULL,
	[HOLD] [int] NOT NULL,
	[BEMD] [numeric](4, 1) NOT NULL,
	[TCON] [numeric](4, 1) NOT NULL,
	[CMPS] [int] NOT NULL,
	[CSOF] [varchar](14) NOT NULL,
	[DateAdded] [datetime] NULL
) ON [PRIMARY]
GO