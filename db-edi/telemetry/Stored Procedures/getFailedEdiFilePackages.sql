
CREATE PROCEDURE [telemetry].[getFailedEdiFilePackages]
  @StartDate [datetime2](7),
  @EndDate [datetime2](7)
AS
BEGIN

    /*
        NHGH-2860 (2023.03.21 @ 2143) - Added this CTE as the EDI pipeline events partition 
        query (below) should only run against data within the specified date range. Without 
        this CTE, the partitioning was being performed on the entire dataset. This resulted
        in slower than desired execution times.  
    */
	WITH 
    RecentFilePackagesCTE
	AS
	(
        SELECT * FROM [telemetry].[EdiPipelineEvents]
        WHERE     
        EventTime > @StartDate AND
        EventTime < @EndDate
    ),


    FilePackageLoggerTypeCTE
	AS
	(
        /*
            NHGH-2653 (2022.11.17 @ 11:10AM) - This partition is needed to account for  ADF pipeline 
            jobs that are re-run. We want the most recent result.
        */
        SELECT * FROM (SELECT *, ROW_NUMBER() OVER (
                                PARTITION BY [FilePackageName] 
                                ORDER BY [EventTime] DESC
                        ) AS [ROW_NUMBER]
        FROM 
        RecentFilePackagesCTE) EVENTS
        WHERE 
        ROW_NUMBER = 1
	)


SELECT t1.*, t2.DataLoggerType
FROM 
    [telemetry].[EdiJobStatus] t1
LEFT OUTER JOIN 
    FilePackageLoggerTypeCTE t2 ON t2.FilePackageName = t1.FilePackageName
WHERE 
    t1.BlobTimeStart > @StartDate AND
    t1.BlobTimeStart < @EndDate AND
    t1.DurationSecs IS NULL
ORDER BY
    t1.BlobTimeStart DESC
END