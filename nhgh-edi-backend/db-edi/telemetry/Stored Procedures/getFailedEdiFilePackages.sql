
CREATE PROCEDURE [telemetry].[getFailedEdiFilePackages]
  @StartDate [datetime2](7),
  @EndDate [datetime2](7)
AS
BEGIN
  SELECT * 
  FROM 
    [telemetry].[EdiJobStatus] 
  WHERE 
    BlobTimeStart > @StartDate AND
    BlobTimeStart < @EndDate AND
    DurationSecs IS NULL
END