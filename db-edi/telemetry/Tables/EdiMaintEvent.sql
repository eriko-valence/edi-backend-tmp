CREATE TABLE [telemetry].[EdiMaintEvent] (
    [EventTime]      DATETIME2 (7)  NOT NULL,
    [EventsLoaded]   INT            NOT NULL,
    [EventsQueried]  INT            NOT NULL,
    [EventsFailed]   INT            NOT NULL,
    [EventsExcluded] INT            NOT NULL,
    [JobName]        VARCHAR (100)  NOT NULL,
    [JobStatus]      VARCHAR (100)  NULL,
    [JobException]   VARCHAR (2000) NULL,
    [DateAdded]      DATETIME       NULL,
    [DateUpdated]    DATETIME       NULL,
    CONSTRAINT [PK_EdiMaintEvent] PRIMARY KEY CLUSTERED ([EventTime] ASC, [JobName] ASC)
);

