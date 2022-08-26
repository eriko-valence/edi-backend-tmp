CREATE TABLE [usbdg].[location] (
    [ESER]        VARCHAR (50)   NOT NULL,
    [zgps_utc]    DATETIME2 (7)  NOT NULL,
    [zgps_ang]    SMALLINT       NULL,
    [zgps_lat]    DECIMAL (8, 5) NULL,
    [zgps_lng]    DECIMAL (8, 5) NULL,
    [DATEADDED]   DATETIME2 (7)  NULL,
    [DATEUPDATED] DATETIME2 (7)  NULL,
    PRIMARY KEY CLUSTERED ([ESER] ASC, [zgps_utc] ASC),
    CONSTRAINT [FK_usbdg_location_ESER] FOREIGN KEY ([ESER]) REFERENCES [usbdg].[device] ([ESER])
);



