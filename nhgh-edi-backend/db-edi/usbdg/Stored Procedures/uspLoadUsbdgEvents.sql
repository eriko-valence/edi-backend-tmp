
CREATE PROCEDURE [usbdg].[uspLoadUsbdgEvents] @usbdg_event [usbdg].[event_type] READONLY
AS
BEGIN
	MERGE [usbdg].[event] AS t
	USING @usbdg_event AS s
	ON t.[ESER] = s.[ESER]
	WHEN NOT MATCHED THEN 
	INSERT ([ABST],[BEMD],[EERR],[ESER],[zcell_info],[DATEADDED]) 
	VALUES(
	s.[ABST],
	s.[BEMD],
	s.[EERR],
	s.[ESER],
	s.[zcell_info],
	GETDATE());
END