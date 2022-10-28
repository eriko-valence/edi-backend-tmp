CREATE TABLE [indigo_v2].[event] (
    [ABST_CALC]  DATETIME2 (7)  NOT NULL,
    [ADOP]       VARCHAR (100)  NULL,
    [ALRM]       VARCHAR (100)  NULL,
    [AMOD]       VARCHAR (100)  NULL,
    [AMFR]       VARCHAR (100)  NULL,
    [APQS]       VARCHAR (20)   NULL,
    [ASER]       VARCHAR (50)   NULL,
    [BLOG]       DECIMAL (5, 1) NULL,
    [DORV]       INT            NULL,
    [ESER]       VARCHAR (50)   NULL,
    [HOLD]       NUMERIC (4, 1) NULL,
    [LDOP]       VARCHAR (100)  NULL,
    [LERR]       VARCHAR (20)   NULL,
    [LMFR]       VARCHAR (20)   NULL,
    [LMOD]       VARCHAR (20)   NULL,
    [LPQS]       VARCHAR (20)   NULL,
    [LSER]       VARCHAR (50)   NOT NULL,
    [LSV]        VARCHAR (20)   NULL,
    [RELT]       VARCHAR (20)   NOT NULL,
    [RTCW]       VARCHAR (20)   NULL,
    [TAMB]       NUMERIC (4, 2) NULL,
    [TVC]        NUMERIC (3, 1) NULL,
    [ZCHRG]      VARCHAR (20)   NULL,
    [ZSTATE]     BIT            NULL,
    [ZVLVD]      BIT            NULL,
    [_RELT_SECS] VARCHAR (20)   NULL,
    [DATEADDED]  DATETIME2 (7)  NULL,
    CONSTRAINT [PK_indigo_v2_event] PRIMARY KEY CLUSTERED ([RELT] ASC, [LSER] ASC),
    CONSTRAINT [FK_indigo_v2_event_ESER] FOREIGN KEY ([ESER]) REFERENCES [usbdg].[device] ([ESER])
);









