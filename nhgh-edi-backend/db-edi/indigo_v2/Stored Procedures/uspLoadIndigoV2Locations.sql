CREATE PROCEDURE [indigo_v2].[uspLoadIndigoV2Locations] @indigo_v2_location [indigo_v2].[location_type] READONLY
AS
BEGIN
	MERGE [indigo_v2].[location] AS t
	USING @indigo_v2_location AS s
	ON t.[LSER] = s.[LSER] and t.[zgps_abst] = s.[zgps_abst]
	WHEN NOT MATCHED THEN 
	INSERT ([LSER],[zgps_abst],[zgps_ang],[zgps_lat],[zgps_lng], [DATEADDED]) 
	VALUES(
	s.[LSER],
	s.[zgps_abst],
	s.[zgps_ang],
	s.[zgps_lat],
	s.[zgps_lng],
	GETDATE());
END