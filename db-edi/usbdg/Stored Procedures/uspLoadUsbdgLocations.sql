
CREATE PROCEDURE [usbdg].[uspLoadUsbdgLocations] @usbdg_location [usbdg].[location_type] READONLY
AS
BEGIN
	MERGE [usbdg].[location] AS t
	USING @usbdg_location AS s
	ON t.[ESER] = s.[ESER] and t.[zgps_utc] = s.[zgps_utc]
    WHEN MATCHED THEN
        UPDATE SET
        t.[ESER] = s.[ESER],
        t.[zgps_utc] = t.[zgps_utc],
        t.[zgps_ang] = t.[zgps_ang],
        t.[zgps_lat] = t.[zgps_lat],
        t.[zgps_lng] = t.[zgps_lng],
        t.[DATEUPDATED] = GETDATE()
	WHEN NOT MATCHED THEN 
        INSERT ([ESER],[zgps_utc],[zgps_ang],[zgps_lat],[zgps_lng],[DATEADDED]) 
        VALUES(
        s.[ESER],
        s.[zgps_utc],
        s.[zgps_ang],
        s.[zgps_lat],
        s.[zgps_lng],
        GETDATE());
END