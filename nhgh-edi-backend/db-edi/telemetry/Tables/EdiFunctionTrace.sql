CREATE TABLE [telemetry].[EdiFunctionTrace] (
    [EventTime]       DATETIME2 (7)  NOT NULL,
    [FilePackageName] VARCHAR (255)  NULL,
    [OperationName]   VARCHAR (100)  NULL,
    [SeverityLevel]   TINYINT        NULL,
    [LogMessage]      VARCHAR (2000) NULL,
    [LogMessageMd5]   VARCHAR (50)   NOT NULL,
    [DateAdded]       DATETIME2 (7)  NOT NULL,
    CONSTRAINT [PK_EdiFunctionTrace] PRIMARY KEY CLUSTERED ([EventTime] ASC, [LogMessageMd5] ASC)
);


GO
CREATE NONCLUSTERED INDEX [IDX_EdiFunctionTrace_PackageName]
    ON [telemetry].[EdiFunctionTrace]([FilePackageName] ASC);

