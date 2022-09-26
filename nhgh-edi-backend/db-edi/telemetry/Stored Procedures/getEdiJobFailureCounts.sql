
CREATE PROCEDURE [telemetry].[getEdiJobFailureCounts]
(
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

    SELECT 
        count(*) as 'FailureCount', 
        PipelineEvent, 
        PipelineStage, 
        PipelineFailureReason, 
        PipelineFailureType
    FROM 
        [telemetry].[EdiPipelineEvents]
    WHERE
        PipelineEvent in ('FAILED') AND
        EventTime >= @StartDate AND
        EventTime <= @EndDate

    GROUP BY
        PipelineEvent, 
        PipelineStage, 
        PipelineFailureReason, 
        PipelineFailureType

END