CREATE TABLE [usbdg].[event] (
    [ABST]       DATETIME2 (7) NOT NULL,
    [BEMD]       DECIMAL (18)  NULL,
    [EERR]       VARCHAR (20)  NULL,
    [ESER]       VARCHAR (50)  NOT NULL,
    [zcell_info] VARCHAR (100) NULL,
    [DATEADDED]  DATETIME2 (7) NULL,
    PRIMARY KEY CLUSTERED ([ABST] ASC, [ESER] ASC),
    CONSTRAINT [FK_usbdg_event.ESER] FOREIGN KEY ([ESER]) REFERENCES [usbdg].[device] ([ESER])
);

