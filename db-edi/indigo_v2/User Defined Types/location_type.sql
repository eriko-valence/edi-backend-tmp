CREATE TYPE [indigo_v2].[location_type] AS TABLE (
    [LSER]      VARCHAR (50)   NOT NULL,
    [zgps_abst] DATETIME2 (7)  NOT NULL,
    [zgps_ang]  SMALLINT       NULL,
    [zgps_lat]  DECIMAL (8, 5) NULL,
    [zgps_lng]  DECIMAL (8, 5) NULL);

