CREATE EXTERNAL TABLE [usbdg].[event] (
    [ABST_last_mnt] DATETIME2 (7) NULL,
    [BEMD] DECIMAL (18) NULL,
    [EERR] VARCHAR (20) NULL,
    [ESER] VARCHAR (50) NOT NULL,
    [zutc_now] DATETIME2 (7) NOT NULL,
    [zcell_info] VARCHAR (100) NULL,
    [zbatt_volt] SMALLINT NULL,
    [zbatt_chrg] SMALLINT NULL,
    [DATEADDED] DATETIME2 (7) NULL,
    [TAMB] DECIMAL (3, 1) NULL
)
    WITH (
    DATA_SOURCE = [emsDevSrc],
    SCHEMA_NAME = N'usbdg',
    OBJECT_NAME = N'event'
    );

