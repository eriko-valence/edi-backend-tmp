

CREATE PROCEDURE [sl1].[uspLoadSl1Events] @sl1_event [sl1].[event_type] READONLY
AS
BEGIN
	MERGE [sl1].[event] AS t
	USING @sl1_event AS s
	ON t.[RELT] = s.[RELT] and t.[LSER] = s.[LSER]
	WHEN NOT MATCHED THEN 
	INSERT ([ABST_CALC],[ADOP],[ALRM],[AMOD],[AMFR],[APQS],[ASER],[BLOG],[ESER],[LDOP],[LERR],[LMFR],[LMOD],[LPQS],[LSER],[LSV],[RELT],[RTCW],[TAMB],[TVC],[ZCHRG],[ZSTATE],[ZVLVD],[CNAM],[CSER],[CSOF],[CMPR],[CMPS],[DCCD],[DCSV],[FANS],[SVA],[DORV],[DORF],[TFRZ],[_RELT_SECS],[DATEADDED]) 
	VALUES(
	s.[ABST_CALC],
	s.[ADOP],
	s.[ALRM],
	s.[AMOD],
	s.[AMFR],
	s.[APQS],
	s.[ASER],
	s.[BLOG],
	s.[ESER],
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
    s.[CNAM],
    s.[CSER],
    s.[CSOF],
    s.[CMPR],
    s.[CMPS],
    s.[DCCD],
    s.[DCSV],
    s.[FANS],
    s.[SVA],
    s.[DORV], 
    s.[DORF], 
    s.[TFRZ],
	s.[_RELT_SECS],
	GETDATE());
END