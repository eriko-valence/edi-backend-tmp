CREATE PROCEDURE [ems_data].[uspLoadMFLoggerData] @mf_logger_data [ems_data].[mf_logger_data_type] READONLY
AS
BEGIN
	MERGE [ems_data].[mf_logger_data] AS mld
	USING @mf_logger_data AS tvp
	ON mld.[ABST] = tvp.[ABST] AND mld.[ASER] = tvp.[ASER]
	WHEN NOT MATCHED THEN 
	INSERT ([AMFR],[AMOD],[APQS],[ASER],[AID],[ADAT],[CID],[FID],[LOC],[ABST],[TAMB],[TCLD],[TVC],[CMPR],[SVA],[EVDC],[CDRW],[DOOR],[HOLD],[BEMD],[TCON],[CMPS],[CSOF],[DateAdded]) 
	VALUES(
tvp.[AMFR],
tvp.[AMOD],
tvp.[APQS],
tvp.[ASER],
tvp.[AID],
tvp.[ADAT],
tvp.[CID],
tvp.[FID],
tvp.[LOC],
tvp.[ABST],
tvp.[TAMB],
tvp.[TCLD],
tvp.[TVC],
tvp.[CMPR],
tvp.[SVA],
tvp.[EVDC],
tvp.[CDRW],
tvp.[DOOR],
tvp.[HOLD],
tvp.[BEMD],
tvp.[TCON],
tvp.[CMPS],
tvp.[CSOF],
GETUTCDATE());
END
GO