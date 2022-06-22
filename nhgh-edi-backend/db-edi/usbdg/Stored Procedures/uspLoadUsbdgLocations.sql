
CREATE PROCEDURE [usbdg].[uspLoadUsbdgLocations] @usbdg_location [usbdg].[location_type] READONLY
AS
BEGIN
	MERGE [usbdg].[location] AS t
	USING @usbdg_location AS s
	ON t.[ESER] = s.[ESER] and t.[zgps_utc] = s.[zgps_utc]
	WHEN NOT MATCHED THEN 
	INSERT ([ESER],[zgps_utc],[zgps_ang],[zgps_lat],[zgps_lng],[usb_id], [DATEADDED]) 
	VALUES(
	s.[ESER],
	s.[zgps_utc],
	s.[zgps_ang],
	s.[zgps_lat],
	s.[zgps_lng],
	s.[usb_id],
	GETDATE());
END