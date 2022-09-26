CREATE TABLE [telemetry].[EdiJobStatus] (
    [FilePackageName]      VARCHAR (255) NOT NULL,
    [ESER]                 VARCHAR (50)  NOT NULL,
    [BlobTimeStart]        DATETIME2 (7) NULL,
    [ProviderSuccessTime]  DATETIME2 (7) NULL,
    [ConsumerSuccessTime]  DATETIME2 (7) NULL,
    [TransformSuccessTime] DATETIME2 (7) NULL,
    [SQLSuccessTime]       DATETIME2 (7) NULL,
    [DurationSecs]         INT           NULL,
    [DateAdded]            DATETIME2 (7) NULL,
    [DateUpdated]          DATETIME2 (7) NULL,
    CONSTRAINT [PK_EdiJobStatus] PRIMARY KEY CLUSTERED ([FilePackageName] ASC)
);

