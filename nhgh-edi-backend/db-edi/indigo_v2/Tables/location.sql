CREATE TABLE [indigo_v2].[location] (
    [LSER]      VARCHAR (50)   NOT NULL,
    [zgps_abst] DATETIME2 (7)  NOT NULL,
    [zgps_ang]  SMALLINT       NULL,
    [zgps_lat]  DECIMAL (8, 5) NULL,
    [zgps_lng]  DECIMAL (8, 5) NULL,
    [DATEADDED] DATETIME2 (7)  NULL,
    CONSTRAINT [PK_indigo_v2_device] PRIMARY KEY CLUSTERED ([LSER] ASC, [zgps_abst] ASC)
);



