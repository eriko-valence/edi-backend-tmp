
CREATE PROCEDURE [telemetry].[getEdiFilePackagesOverallStats]
(
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

    DECLARE @FailedProvider varchar(50);
    DECLARE @FailedConsumer varchar(50);
    DECLARE @FailedTransform varchar(50);
    DECLARE @FailedSqlLoad varchar(50);
    DECLARE @SuccessfulJobs varchar(50);

	WITH 
    FailedProviderCTE
	AS
	(
        SELECT 
            COUNT(*) AS 'Count' 
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            ProviderSuccessTime IS NULL AND 
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing provider telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate
	)
    SELECT @FailedProvider = Count FROM FailedProviderCTE;

	WITH 
    FailedConsumerCTE
	AS
	(
        SELECT 
            COUNT(*) AS 'Count' 
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            ConsumerSuccessTime IS NULL AND
            ProviderSuccessTime IS NOT NULL AND
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing consumer telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate
	)
    SELECT @FailedConsumer = Count FROM FailedConsumerCTE;
    
	WITH 
    FailedTransformCTE
	AS
	(
        SELECT 
            COUNT(*) AS 'Count' 
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            TransformSuccessTime IS NULL AND
            ConsumerSuccessTime IS NOT NULL AND 
            ProviderSuccessTime IS NOT NULL AND
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing transform telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate
	)
    SELECT @FailedTransform = Count FROM FailedTransformCTE;

    WITH 
    FailedSqlLoadCTE
	AS
	(
        SELECT 
            COUNT(*) AS 'Count' 
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            SqlSuccessTime IS NULL AND  
            ConsumerSuccessTime IS NOT NULL AND 
            ProviderSuccessTime IS NOT NULL AND 
            TransformSuccessTime IS NOT NULL AND
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing sql load telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate
	)
    SELECT @FailedSqlLoad = Count FROM FailedSqlLoadCTE;

        DECLARE @SucceededCount varchar(50);

	WITH 
    SucceededFilePackagesCTE
	AS
	(
        SELECT 
            count(*) AS 'SucceededCount'
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            DurationSecs IS NOT NULL AND
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate
	)
    SELECT @SuccessfulJobs = SucceededCount FROM SucceededFilePackagesCTE;

    SELECT 
        @FailedProvider AS 'FailedProvider', 
        @FailedConsumer as 'FailedConsumer',
        @FailedTransform as 'FailedTransform',
        @FailedSqlLoad as 'FailedSqlLoad',
        @SuccessfulJobs as 'SuccessfulJobs';

END