CREATE TABLE [telemetry].[EdiAdfActivity] (
    [EventTime]       DATETIME2 (7)  NOT NULL,
    [FilePackageName] VARCHAR (255)  NOT NULL,
    [Status]          VARCHAR (50)   NOT NULL,
    [ActivityName]    VARCHAR (100)  NOT NULL,
    [ActivityType]    VARCHAR (100)  NOT NULL,
    [PipelineName]    VARCHAR (100)  NOT NULL,
    [ErrorCode]       VARCHAR (100)  NULL,
    [ErrorMessage]    VARCHAR (2000) NULL,
    [DateAdded]       DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_EdiAdfActivity] PRIMARY KEY CLUSTERED ([EventTime] ASC, [Status] ASC, [ActivityName] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IDX_EdiAdfActivity_PackageName]
    ON [telemetry].[EdiAdfActivity]([FilePackageName] ASC);

