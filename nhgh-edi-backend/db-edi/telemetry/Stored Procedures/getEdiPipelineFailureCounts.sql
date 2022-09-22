
CREATE PROCEDURE [telemetry].[getEdiPipelineFailureCounts]
(
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN
	WITH 
    EdiPipelineStagesCTE
	AS
	(
        SELECT DISTINCT
            PipelineStage
        FROM 
            [telemetry].[EdiPipelineEvents] 
	),
    EdiPipelineFailureCountsCTE
	AS
	(
        SELECT
            PipelineStage,
            count(*) as 'EventCount'
        FROM 
            [telemetry].[EdiPipelineEvents]
        WHERE 
            PipelineEvent = 'FAILED' AND
            EventTime > @StartDate AND
            EventTime < @EndDate
        GROUP BY 
            PipelineStage
	)
    SELECT 
        t1.PipelineStage, 
        ISNULL(t2.EventCount, 0) as 'FailedEvents'
    FROM
        EdiPipelineStagesCTE t1
    LEFT OUTER JOIN
        EdiPipelineFailureCountsCTE t2 ON t2.PipelineStage = t1.PipelineStage
END