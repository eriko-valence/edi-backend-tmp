

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
		GETDATE())
	WHEN MATCHED THEN UPDATE SET
		t.[EDOP] = s.[EDOP],
		t.[EMFR] = s.[EMFR],
		t.[EMOD] = s.[EMOD],
		t.[EMSV] = s.[EMSV],
		t.[EPQS] = s.[EPQS],
		t.[ESER] = s.[ESER],
		t.[zcfg_ver] = s.[zcfg_ver],
		t.[zmcu_ver] = s.[zmcu_ver],
		t.[LASTMODIFIED] = GETDATE()
	;
END