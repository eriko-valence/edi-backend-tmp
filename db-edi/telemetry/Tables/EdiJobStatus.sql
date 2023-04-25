CREATE TABLE [telemetry].[EdiJobStatus] (
    [FilePackageName]      VARCHAR (255) NOT NULL,
    [ESER]                 VARCHAR (50)  NOT NULL,
    [JobStartTime]         DATETIME2 (7) NULL,
    [ProviderSuccessTime]  DATETIME2 (7) NULL,
    [ConsumerSuccessTime]  DATETIME2 (7) NULL,
    [TransformSuccessTime] DATETIME2 (7) NULL,
    [SQLSuccessTime]       DATETIME2 (7) NULL,
    [DurationSecs]         INT           NULL,
    [DateAdded]            DATETIME2 (7) NULL,
    [DateUpdated]          DATETIME2 (7) NULL,
    [EMDType]              VARCHAR (50)  NULL,
    CONSTRAINT [PK_EdiJobStatus] PRIMARY KEY CLUSTERED ([FilePackageName] ASC)
);








GO
CREATE NONCLUSTERED INDEX [IDX_EdiJobStatus_FailedEdiJobsOverTime]
    ON [telemetry].[EdiJobStatus]([JobStartTime] ASC, [DurationSecs] ASC)
    INCLUDE([ESER], [ProviderSuccessTime], [ConsumerSuccessTime], [TransformSuccessTime], [SQLSuccessTime], [DateAdded], [DateUpdated]);



