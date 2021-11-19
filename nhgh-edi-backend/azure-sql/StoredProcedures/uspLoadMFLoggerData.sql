CREATE PROCEDURE [ems_data].[uspLoadMFLoggerData] @mf_logger_data [ems_data].[mf_logger_data_type] READONLY
AS
BEGIN
	MERGE [ems_data].[mf_logger_data] AS mld
	USING @mf_logger_data AS tvp
	ON mld.[ABST] = tvp.[ABST] AND mld.[ASER] = tvp.[ASER]
	WHEN NOT MATCHED THEN 
	INSERT ([AMFR],[AMOD],[ASER],[ASER_HEX],[ADOP],[APQS],[RNAM],[DNAM],[FNAM],[CID],[LAT],[LNG],[ABST],[SVA],[HAMB],[TAMB],[ACCD],[TCON],[TVC],[BEMD],[HOLD],[DORV],[ALRM],[EMSV],[EERR],[CMPR],[ACSV],[AID],[CMPS],[DCCD],[DCSV],[DATEADDED]) 
	VALUES(
tvp.[AMFR],
tvp.[AMOD],
tvp.[ASER],
tvp.[ASER_HEX],
tvp.[ADOP],
tvp.[APQS],
tvp.[RNAM],
tvp.[DNAM],
tvp.[FNAM],
tvp.[CID],
tvp.[LAT],
tvp.[LNG],

tvp.[ABST],
tvp.[SVA],
tvp.[HAMB],
tvp.[TAMB],
tvp.[ACCD],
tvp.[TCON],
tvp.[TVC],
tvp.[BEMD],
tvp.[HOLD],
tvp.[DORV],
tvp.[ALRM],
tvp.[EMSV],
tvp.[EERR],
tvp.[CMPR],
tvp.[ACSV],
tvp.[AID],
tvp.[CMPS],
tvp.[DCCD],
tvp.[DCSV],
GETDATE());
END
GO