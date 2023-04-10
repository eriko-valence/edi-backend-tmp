	CREATE PROCEDURE [telemetry].[uspCreateEdiMaintEvent]
	@EventTime [datetime2],
    @EventsLoaded int,
    @EventsQueried int,
	@EventsFailed int,
	@EventsExcluded int,
	@JobName [varchar](100),
	@JobStatus [varchar](100),
	@JobException [varchar](2000),
    @Result INT OUTPUT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN
                SELECT [EventTime], [JobName] FROM [telemetry].[EdiMaintEvent]
                    WHERE [JobName] = @JobName and [EventTime] = @EventTime
            END
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [telemetry].[EdiMaintEvent] (
					[EventTime],
					[EventsLoaded],
					[EventsQueried],
					[EventsFailed],
					[EventsExcluded],
					[JobName],
					[JobStatus],
                    [JobException],
					[DateAdded]) 
                VALUES(
					@EventTime,
					@EventsLoaded,
                    @EventsQueried,
					@EventsFailed,
					@EventsExcluded,
					@JobName,
					@JobStatus,
					@JobException,
                    getdate())
                IF @@ROWCOUNT = 1
                    SET @Result = 1 --successful insert
                ELSE
                    SET @Result = 3 --unsuccessful insert 
            END
			ELSE
            BEGIN
			    UPDATE [telemetry].[EdiMaintEvent]
                SET 
                    [EventTime] = @EventTime, 
                    [EventsLoaded] = @EventsLoaded,
                    [EventsQueried] = @EventsQueried,
                    [EventsFailed] = @EventsFailed,
                    [EventsExcluded] = @EventsExcluded,
                    [JobName] = @JobName,
                    [JobStatus] = @JobStatus,
                    [JobException] = @JobException,
                    [DateUpdated] = getdate()
                WHERE
                    [JobName] = @JobName and [EventTime] = @EventTime
                IF @@ROWCOUNT = 1
                    SET @Result = 1 --successful update
                ELSE
                    SET @Result = 3 --unsuccessful update 
            END
        COMMIT
    END TRY
    BEGIN CATCH
        SET @Result = 4 --unsuccessful insert (unknown)
        ROLLBACK
    END CATCH   
END