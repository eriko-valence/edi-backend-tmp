
CREATE PROCEDURE [telemetry].[getEdiFailedFilePackagesOverTime]
(
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN
    WITH 
    DayRangeCTE
    AS
    (
        SELECT DATEADD(DAY,0, DATEDIFF(day,0, @StartDate)) AS dt
        UNION ALL
            SELECT DATEADD(dd, 1, dt) as 'Day'
                FROM DayRangeCTE s
                WHERE DATEADD(dd, 1, dt) <= DATEADD(DAY,-1, DATEDIFF(day,0, @EndDate))
    ),
    FailedJobsCTE
    AS
    (
        SELECT 
            COUNT(*) as 'FailedJobs',
            DATEADD(DAY,0, DATEDIFF(day,0, t1.BlobTimeStart)) as 'BlobTimeStart'
        FROM
            [telemetry].[EdiJobStatus] t1
        WHERE
            t1.DurationSecs IS NULL AND
            t1.BlobTimeStart >= @StartDate AND
            t1.BlobTimeStart <= DATEADD(DAY,1, DATEDIFF(day,0, @EndDate))
        GROUP BY
            DATEADD(DAY,0, DATEDIFF(day,0, t1.BlobTimeStart))
    ),
    SuccessfulJobsCTE
    AS
    (
        SELECT 
            COUNT(*) as 'SuccessfulJobs',
            DATEADD(DAY,0, DATEDIFF(day,0, t1.BlobTimeStart)) as 'BlobTimeStart'
        FROM
            [telemetry].[EdiJobStatus] t1
        WHERE
            t1.DurationSecs IS NOT NULL AND
            t1.BlobTimeStart >= @StartDate AND
            t1.BlobTimeStart <= DATEADD(DAY,1, DATEDIFF(day,0, @EndDate))
        GROUP BY
            DATEADD(DAY,0, DATEDIFF(day,0, t1.BlobTimeStart))
    )
    SELECT
        t2.FailedJobs as 'FailedJobs',
        t3.SuccessfulJobs as 'SuccessfulJobs',
        DATEADD(DAY,0, DATEDIFF(day,0, t1.dt)) as 'EventDate'
    FROM
        DayRangeCTE t1
    LEFT OUTER JOIN
        FailedJobsCTE t2 on DATEADD(DAY,0, DATEDIFF(day,0, t2.BlobTimeStart)) = t1.dt
    LEFT OUTER JOIN
        SuccessfulJobsCTE t3 on DATEADD(DAY,0, DATEDIFF(day,0, t3.BlobTimeStart)) = t1.dt
END