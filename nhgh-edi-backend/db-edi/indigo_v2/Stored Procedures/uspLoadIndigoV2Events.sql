

CREATE PROCEDURE [indigo_v2].[uspLoadIndigoV2Events] @indigo_v2_event [indigo_v2].[event_type] READONLY
AS
BEGIN
	MERGE [indigo_v2].[event] AS t
	USING @indigo_v2_event AS s
	ON t.[RELT] = s.[RELT] and t.[LSER] = s.[LSER]
    WHEN MATCHED THEN
        UPDATE SET
		t.[ZSTATE] = s.[ZSTATE],
		t.[ZVLVD] = s.[ZVLVD],
        t.[LASTMODIFIED] = GETDATE()
	WHEN NOT MATCHED THEN 
	INSERT ([ABST_CALC],[ADOP],[ALRM],[AMOD],[AMFR],[APQS],[ASER],[BLOG],[DORV],[ESER],[HOLD],[LDOP],[LERR],[LMFR],[LMOD],[LPQS],[LSER],[LSV],[RELT],[RTCW],[TAMB],[TVC],[ZCHRG],[ZSTATE],[ZVLVD],[_RELT_SECS],[DATEADDED]) 
	VALUES(
	s.[ABST_CALC],
	s.[ADOP],
	s.[ALRM],
	s.[AMOD],
	s.[AMFR],
	s.[APQS],
	s.[ASER],
	s.[BLOG],
	s.[DORV],
	s.[ESER],
	s.[HOLD],
	s.[LDOP],
	s.[LERR],
	s.[LMFR],
	s.[LMOD],
	s.[LPQS],
	s.[LSER],
	s.[LSV],
	s.[RELT],
	s.[RTCW],
	s.[TAMB],
	s.[TVC],
	s.[ZCHRG],
	s.[ZSTATE],
	s.[ZVLVD],
	s.[_RELT_SECS],
	GETDATE());
END