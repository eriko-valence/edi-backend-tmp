﻿
CREATE PROCEDURE [telemetry].[getEdiJobFailureCounts]
(
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

-- Need to account for EDI ADF jobs that have been re-run. These job will produce multiple pipeline stage 
-- names for a file package. We want to make sure only the latest stage record is retrieved. For example,
-- the ADF_TRANSFORM stage completed with a FAILED state. After the job was re-run, ADF_TRANSFORM completed
-- with a SUCCEEDED. We want this last result as it depicts the current pipeline state of that file package. 
WITH 
LatestEdiPipelineJobResultsCTE
AS
(
    SELECT 
        MAX(EventTime) as 'EventTime',
        FilePackageName, PipelineStage, PipelineState
    FROM 
        [telemetry].[EdiPipelineEvents]
    WHERE
        EventTime >= @StartDate AND
        EventTime <= @EndDate --AND
    GROUP BY
        FilePackageName,
        PipelineStage, 
        PipelineState
),

FailedEdiPipelineJobResultsCTE
AS
(
SELECT 
    t1.FilePackageName,
    t1.PipelineEvent, 
    t1.PipelineStage, 
    t1.PipelineFailureReason, 
    t1.PipelineFailureType,
    t1.PipelineState
FROM 
    [telemetry].[EdiPipelineEvents] t1,
    LatestEdiPipelineJobResultsCTE t2
WHERE
    t1.EventTime = t2.EventTime AND
    t1.PipelineStage = t2.PipelineStage AND
    t1.PipelineState = t2.PipelineState AND
    t1.PipelineEvent in ('FAILED')
),

UpdatedFailedEdiPipelineJobResultsCTE
AS
(

select * from FailedEdiPipelineJobResultsCTE

-- Need to also monitor file packages that never trigger a EDI pipeline job to start
-- These file packages upload to blob storage but the CCDX provider azure function fails to trigger
-- This separate UNION statement is needed becuase all events in the table [EdiPipelineEvents]
-- are sourced from azure function telemetry. There is no azure function involved in uploading 
-- the file packages to the ccdx-provider blob container. So this is a blind spot that is
-- accounted for by using this UNION statement. 
UNION
    SELECT 
        FilePackageName,
        'FAILED' as 'PipelineEvent',
        'CCDX_PROVIDER' as 'PipelineStage',
        'CCDX_PROVIDER_NOT_TRIGGERED' as 'PipelineFailureReason',
        'NOT STARTED' AS 'PipelineFailureType',
        'COMPLETED' as 'PipelineState'
    FROM 
        [telemetry].[EdiJobStatus] 
    WHERE 
        ProviderSuccessTime IS NULL AND 
        DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing provider telemetry)
        JobStartTime > @StartDate AND
        -- NHGH-2948 (2023.06.07 1049AM) - a provider should only be marked as "not triggered" if it did not have an error (an error implies the provider was triggered)
        --                               - without this filter, a report package would have duplicate failure entries (CCDX_PROVIDER_NOT_TRIGGERED and HTTP_STATUS_CODE_ERROR)
        JobStartTime < @EndDate and FilePackageName not in (
            select FilePackageName from FailedEdiPipelineJobResultsCTE where PipelineStage IN('CCDX_PROVIDER', 'CCDX_PROVIDER_VARO') and PipelineEvent = 'FAILED')

    -- Need to also monitor file packages that fail to load into the SQL database
    -- This separate UNION statement is needed becuase all events in the table [EdiPipelineEvents]
    -- are sourced from azure function telemetry. There is no azure function involved in loading 
    -- the file packages into the SQL database. So this is a blind spot that is accounted for by 
    -- using this UNION statement. 
    UNION
        SELECT 
        FilePackageName,
        'FAILED' as 'PipelineEvent',
        'SQL_LOAD' as 'PipelineStage',
        'SQL_LOAD_ERROR' as 'PipelineFailureReason',
        'ERROR' AS 'PipelineFailureType',
        'COMPLETED' as 'PipelineState'
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            ProviderSuccessTime IS NOT NULL AND 
            ConsumerSuccessTime IS NOT NULL AND 
            TransformSuccessTime IS NOT NULL AND
            SQLSuccessTime IS NULL AND
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing provider telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate

    -- NHGH-2948 2023.06.07 1135 Need to monitor file packages that never get pulled out of ccdx by the consumer
    
    UNION
        SELECT 
        FilePackageName,
        'FAILED' as 'PipelineEvent',
        'CCDX_CONSUMER' as 'PipelineStage',
        'CCDX_CONSUMER' as 'PipelineFailureReason',
        'NOT STARTED' AS 'PipelineFailureType',
        'COMPLETED' as 'PipelineState'
        FROM 
            [telemetry].[EdiJobStatus] 
        WHERE 
            ProviderSuccessTime IS NOT NULL AND 
            ConsumerSuccessTime IS NULL AND 
            DurationSecs IS NULL AND -- make sure the file package also never successfully completed (this accounts for the possibiliy of missing provider telemetry)
            JobStartTime > @StartDate AND
            JobStartTime < @EndDate AND
            FilePackageName not in (
            select FilePackageName from [telemetry].[EdiPipelineEvents] where PipelineStage IN('CCDX_CONSUMER', 'CCDX_CONSUMER_VARO') and PipelineEvent = 'FAILED'))

--select * from UpdatedFailedEdiPipelineJobResultsCTE where FilePackageName = '40A36BCA6956_20230413T212554Z_001f00265547501820333333_reports.tar.gz'

--select * from UpdatedFailedEdiPipelineJobResultsCTE where PipelineStage IN('CCDX_CONSUMER') order by PipelineFailureReason


    SELECT 
        count(*) as 'FailureCount', 
        t1.PipelineEvent, 
        t1.PipelineStage, 
        t1.PipelineFailureReason, 
        t1.PipelineFailureType,
        t1.PipelineState
    FROM 
        UpdatedFailedEdiPipelineJobResultsCTE t1
    WHERE
        t1.PipelineEvent in ('FAILED')
    GROUP BY
        t1.PipelineState,
        t1.PipelineEvent, 
        t1.PipelineStage, 
        t1.PipelineFailureReason, 
        t1.PipelineFailureType


END