CREATE TABLE [telemetry].[EdiPipelineEvents] (
    [EventTime]             DATETIME2 (7)  NOT NULL,
    [FilePackageName]       VARCHAR (255)  NOT NULL,
    [ESER]                  VARCHAR (50)   NOT NULL,
    [PipelineEvent]         VARCHAR (100)  NOT NULL,
    [PipelineStage]         VARCHAR (100)  NOT NULL,
    [PipelineFailureReason] VARCHAR (100)  NULL,
    [PipelineFailureType]   VARCHAR (100)  NULL,
    [DataLoggerType]        VARCHAR (100)  NULL,
    [ExceptionMessage]      VARCHAR (1500) NULL,
    [DateAdded]             DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_EdiPipelineEvents] PRIMARY KEY CLUSTERED ([EventTime] ASC, [FilePackageName] ASC, [PipelineStage] ASC, [PipelineEvent] ASC)
);

