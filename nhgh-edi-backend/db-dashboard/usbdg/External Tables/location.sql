CREATE EXTERNAL TABLE [usbdg].[location] (
    [ESER] VARCHAR (50) NOT NULL,
    [zgps_utc] DATETIME2 (7) NOT NULL,
    [zgps_ang] SMALLINT NULL,
    [zgps_lat] DECIMAL (8, 5) NULL,
    [zgps_lng] DECIMAL (8, 5) NULL,
    [DATEADDED] DATETIME2 (7) NULL
)
    WITH (
    DATA_SOURCE = [emsDevSrc],
    SCHEMA_NAME = N'usbdg',
    OBJECT_NAME = N'location'
    );

