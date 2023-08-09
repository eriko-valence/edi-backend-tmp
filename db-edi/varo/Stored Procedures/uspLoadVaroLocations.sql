CREATE PROCEDURE [varo].[uspLoadVaroLocations] @varo_location [varo].[location_type] READONLY
AS
BEGIN
	MERGE [varo].[location] AS t
	USING @varo_location AS s
	ON t.[ASER] = s.[ASER] and t.[REPORTTIME] = s.[REPORTTIME]
    WHEN MATCHED THEN
        UPDATE SET
        t.[ASER] = s.[ASER],
        t.[REPORTTIME] = t.[REPORTTIME],
        t.[ACCURACY] = t.[ACCURACY],
        t.[LATITUDE] = t.[LATITUDE],
        t.[LONGITUDE] = t.[LONGITUDE],
        t.[DATEUPDATED] = GETDATE()
	WHEN NOT MATCHED THEN 
        INSERT ([ASER],[REPORTTIME],[ACCURACY],[LATITUDE],[LONGITUDE],[DATEADDED]) 
        VALUES(
        s.[ASER],
        s.[REPORTTIME],
        s.[ACCURACY],
        s.[LATITUDE],
        s.[LONGITUDE],
        GETDATE());
END