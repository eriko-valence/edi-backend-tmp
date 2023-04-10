CREATE TYPE [usbdg].[location_type] AS TABLE (
    [ESER]     VARCHAR (50)   NOT NULL,
    [zgps_utc] DATETIME2 (7)  NOT NULL,
    [zgps_ang] SMALLINT       NULL,
    [zgps_lat] DECIMAL (8, 5) NULL,
    [zgps_lng] DECIMAL (8, 5) NULL);



