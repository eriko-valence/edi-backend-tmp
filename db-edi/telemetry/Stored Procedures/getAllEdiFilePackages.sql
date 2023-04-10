
CREATE PROCEDURE [telemetry].[getAllEdiFilePackages]
  @ESER varchar(64),
  @StartDate [datetime2](7),
  @EndDate [datetime2](7)
AS
BEGIN

	WITH 
    FilePackageLoggerTypeCTE
	AS
	(
SELECT 
    FilePackageName, 
    DataLoggerType
FROM 
    [telemetry].[EdiPipelineEvents] 
WHERE 
    PipelineStage = 'ADF_TRANSFORM' AND 
    DataLoggerType IS NOT NULL AND
    ESER = @ESER
GROUP BY 
    FilePackageName, DataLoggerType
	)


SELECT t1.*, t2.DataLoggerType
FROM 
    [telemetry].[EdiJobStatus] t1
LEFT OUTER JOIN 
    FilePackageLoggerTypeCTE t2 ON t2.FilePackageName = t1.FilePackageName
WHERE 
    t1.BlobTimeStart > @StartDate AND
    t1.BlobTimeStart < @EndDate AND
    t1.ESER = @ESER
ORDER BY 
    t1.BlobTimeStart DESC
END