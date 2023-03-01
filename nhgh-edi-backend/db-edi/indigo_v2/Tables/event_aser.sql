CREATE TABLE [indigo_v2].[event_aser] (
    [ABST_CALC] DATETIME2 (3)  NOT NULL,
    [ALRM]      VARCHAR (100)  NULL,
    [ASER]      VARCHAR (50)   NOT NULL,
    [BLOG]      DECIMAL (5, 1) NULL,
    [DORV]      INT            NULL,
    [ESER]      VARCHAR (50)   NULL,
    [HOLD]      NUMERIC (4, 1) NULL,
    [LERR]      VARCHAR (20)   NULL,
    [LSER]      VARCHAR (50)   NULL,
    [LSV]       VARCHAR (20)   NULL,
    [TAMB]      NUMERIC (4, 2) NULL,
    [TVC]       NUMERIC (3, 1) NULL,
    [ZCHRG]     VARCHAR (20)   NULL,
    [ZSTATE]    BIT            NULL,
    [ZVLVD]     BIT            NULL,
    [DATEADDED] DATETIME2 (3)  NULL,
    CONSTRAINT [PK_indigo_v2_event_aser] PRIMARY KEY CLUSTERED ([ABST_CALC] ASC, [ASER] ASC)
);

