





CREATE PROCEDURE [usbdg].[uspLoadUsbdgEvents] @usbdg_event [usbdg].[event_type] READONLY
AS
BEGIN
	MERGE [usbdg].[event] AS t
	USING @usbdg_event AS s
	ON t.[ESER] = s.[ESER] and t.[zutc_now] = s.[zutc_now]
	WHEN NOT MATCHED THEN 
	INSERT ([ABST],[BEMD],[EERR],[ESER],[zutc_now],[zcell_info],[zbatt_volt],[zbatt_chrg],[DATEADDED]) 
	VALUES(
	s.[ABST],
	s.[BEMD],
	s.[EERR],
	s.[ESER],
	s.[zutc_now],
	s.[zcell_info],
	NULLIF(s.[zbatt_volt], ''),
	NULLIF(s.[zbatt_chrg], ''),
	GETDATE());
END