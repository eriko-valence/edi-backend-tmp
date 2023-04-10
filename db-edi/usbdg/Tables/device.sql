CREATE TABLE [usbdg].[device] (
    [EDOP]         DATE           NULL,
    [EMFR]         VARCHAR (100)  NULL,
    [EMOD]         VARCHAR (100)  NULL,
    [EMSV]         VARCHAR (20)   NULL,
    [EPQS]         VARCHAR (20)   NULL,
    [ESER]         VARCHAR (50)   NOT NULL,
    [zcfg_ver]     VARCHAR (20)   NULL,
    [zmcu_ver]     VARCHAR (20)   NULL,
    [DATEADDED]    DATETIME2 (7)  NULL,
    [LASTMODIFIED] DATETIME2 (7)  NULL,
    [COMMENTS]     VARCHAR (1000) NULL,
    CONSTRAINT [PK_usbdg_device] PRIMARY KEY CLUSTERED ([ESER] ASC)
);





