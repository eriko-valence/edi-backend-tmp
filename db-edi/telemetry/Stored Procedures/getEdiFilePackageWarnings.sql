
CREATE PROCEDURE [telemetry].[getEdiFilePackageWarnings]
  @StartDate [datetime2](7),
  @EndDate [datetime2](7)
AS
BEGIN
    SELECT [EventTime]
        ,[FilePackageName]
        ,[ESER]
        ,[PipelineEvent]
        ,[PipelineStage]
        ,[PipelineFailureReason]
        ,[PipelineFailureType]
        ,[DataLoggerType]
        ,[ExceptionMessage]
        ,[DateAdded]
        ,[PipelineState]
        ,[ErrorCode]
        ,[EmdType]
    FROM 
        [telemetry].[EdiPipelineEvents] 
    WHERE 
        PipelineEvent = 'WARN' AND 
        EventTime > @StartDate AND
        EventTime < @EndDate
    ORDER BY
        EventTime DESC
END