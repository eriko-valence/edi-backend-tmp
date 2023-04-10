CREATE TYPE [usbdg].[device_type] AS TABLE (
    [EDOP]     DATE          NULL,
    [EMFR]     VARCHAR (100) NULL,
    [EMOD]     VARCHAR (100) NULL,
    [EMSV]     VARCHAR (20)  NULL,
    [EPQS]     VARCHAR (20)  NULL,
    [ESER]     VARCHAR (50)  NULL,
    [zcfg_ver] VARCHAR (20)  NULL,
    [zmcu_ver] VARCHAR (20)  NULL);

