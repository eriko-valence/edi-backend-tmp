CREATE TABLE [usbdg].[event] (
    [ABST_last_mnt] DATETIME2 (7)  NULL,
    [BEMD]          DECIMAL (18)   NULL,
    [EERR]          VARCHAR (20)   NULL,
    [ESER]          VARCHAR (50)   NOT NULL,
    [zutc_now]      DATETIME2 (7)  NOT NULL,
    [zcell_info]    VARCHAR (100)  NULL,
    [zbatt_volt]    SMALLINT       NULL,
    [zbatt_chrg]    SMALLINT       NULL,
    [DATEADDED]     DATETIME2 (7)  NULL,
    [TAMB]          DECIMAL (3, 1) NULL,
    PRIMARY KEY CLUSTERED ([zutc_now] ASC, [ESER] ASC),
    CONSTRAINT [FK_usbdg_event.ESER] FOREIGN KEY ([ESER]) REFERENCES [usbdg].[device] ([ESER])
);











