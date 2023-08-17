CREATE TABLE [telemetry].[EdiPipelineEvents] (
    [EventTime]             DATETIME2 (7)  NOT NULL,
    [FilePackageName]       VARCHAR (100)  NOT NULL,
    [ESER]                  VARCHAR (50)   NOT NULL,
    [PipelineEvent]         VARCHAR (50)   NOT NULL,
    [PipelineStage]         VARCHAR (50)   NOT NULL,
    [PipelineFailureReason] VARCHAR (50)   NULL,
    [PipelineFailureType]   VARCHAR (50)   NULL,
    [DataLoggerType]        VARCHAR (50)   NULL,
    [ExceptionMessage]      VARCHAR (1500) NULL,
    [DateAdded]             DATETIME2 (7)  NOT NULL,
    [PipelineState]         VARCHAR (50)   NULL,
    [ErrorCode]             VARCHAR (10)   NULL,
    CONSTRAINT [PK_EdiPipelineEvents] PRIMARY KEY CLUSTERED ([EventTime] ASC, [FilePackageName] ASC, [PipelineStage] ASC, [PipelineEvent] ASC)
);








GO
CREATE NONCLUSTERED INDEX [IDX_EdiPipelineEvents_PipelineEvent]
    ON [telemetry].[EdiPipelineEvents]([PipelineEvent] ASC)
    INCLUDE([PipelineFailureReason], [PipelineFailureType], [PipelineState]);


GO
CREATE NONCLUSTERED INDEX [IDX_EdiPipelineEvents_LatestPipelineJobResults]
    ON [telemetry].[EdiPipelineEvents]([EventTime] ASC, [FilePackageName] ASC, [PipelineStage] ASC, [PipelineState] ASC);


GO
CREATE NONCLUSTERED INDEX [IDX_EdiPipelineEvents_FailedPipelineJobs]
    ON [telemetry].[EdiPipelineEvents]([EventTime] ASC, [PipelineStage] ASC, [PipelineState] ASC, [PipelineFailureReason] ASC, [PipelineEvent] ASC, [PipelineFailureType] ASC);

