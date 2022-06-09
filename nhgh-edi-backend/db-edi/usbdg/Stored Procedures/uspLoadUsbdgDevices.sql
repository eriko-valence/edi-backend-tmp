
CREATE PROCEDURE [usbdg].[uspLoadUsbdgDevices] @usbdg_device [usbdg].[device_type] READONLY
AS
BEGIN
	MERGE [usbdg].[device] AS t
	USING @usbdg_device AS s
	ON t.[ESER] = s.[ESER]
	WHEN NOT MATCHED THEN 
	INSERT ([EDOP],[EMFR],[EMOD],[EMSV],[EPQS],[ESER],[zcfg_ver],[zmcu_ver],[DATEADDED]) 
	VALUES(
	s.[EDOP],
	s.[EMFR],
	s.[EMOD],
	s.[EMSV],
	s.[EPQS],
	s.[ESER],
	s.[zcfg_ver],
	s.[zmcu_ver],
	GETDATE());
END