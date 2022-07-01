CREATE TYPE [usbdg].[event_type] AS TABLE (
    [ABST]       DATETIME2 (7) NULL,
    [BEMD]       DECIMAL (18)  NULL,
    [EERR]       VARCHAR (20)  NULL,
    [ESER]       VARCHAR (50)  NULL,
    [zutc_now]   DATETIME2 (7) NULL,
    [zcell_info] VARCHAR (100) NULL,
    [zbatt_volt] SMALLINT      NULL,
    [zbatt_chrg] TINYINT       NULL);





