	CREATE PROCEDURE [telemetry].[uspCreateEdiAdfPipelineEvent]
	@EventTime [datetime2],
    @FilePackageName [varchar](100),
    @Status [varchar](100),
	@ActivityName [varchar](100),
	@ActivityType [varchar](100),
	@PipelineName [varchar](100),
	@ErrorCode [varchar](100),
	@ErrorMessage [varchar](2000),
    @Result INT OUTPUT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN
                SELECT [FilePackageName] FROM [telemetry].[EdiAdfActivity]
                    WHERE [FilePackageName] = @FilePackageName and [EventTime] = @EventTime and [Status] = @Status
            END
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [telemetry].[EdiAdfActivity] (
					[EventTime],
					[FilePackageName],
					[Status],
					[ActivityName],
					[ActivityType],
					[PipelineName],
					[ErrorCode],
					[ErrorMessage],
					[DateAdded]) 
                VALUES(
					@EventTime,
					@FilePackageName,
                    @Status,
					@ActivityName,
					@ActivityType,
					@PipelineName,
					@ErrorCode,
					@ErrorMessage,
                    getdate())
                IF @@ROWCOUNT = 1
                    SET @Result = 1 --successful insert
                ELSE
                    SET @Result = 3 --unsuccessful insert 
            END
			ELSE
			    SET @Result = 2 --entry already exists
        COMMIT
    END TRY
    BEGIN CATCH
        SET @Result = 4 --unsuccessful insert (unknown)
        ROLLBACK
    END CATCH   
END