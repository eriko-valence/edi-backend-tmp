
	CREATE PROCEDURE [telemetry].[uspCreateEdiPipelineEvent]
	@EventTime [datetime2],
    @FilePackageName [varchar](100),
	@ESER [varchar](50),
	@PipelineEvent [varchar](100),
	@PipelineStage [varchar](100),
	@PipelineFailureReason [varchar](100),
	@PipelineFailureType [varchar](100),
	@DataLoggerType [varchar](100),
	@ExceptionMessage [varchar](1500),
    @ErrorCode [varchar](10),
    @Result INT OUTPUT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN
                SELECT [FilePackageName] FROM [telemetry].[EdiPipelineEvents]
                    WHERE 
                        [FilePackageName] = @FilePackageName AND 
                        [EventTime] = @EventTime AND 
                        [PipelineStage] = @PipelineStage AND
                        [PipelineEvent] = @PipelineEvent
            END
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [telemetry].[EdiPipelineEvents] (
					[EventTime],
					[FilePackageName],
					[ESER],
                    [PipelineState],
					[PipelineEvent],
					[PipelineStage],
					[PipelineFailureReason],
					[PipelineFailureType],
					[DataLoggerType],
					[ExceptionMessage],
                    [ErrorCode],
					[DateAdded]) 
                VALUES(
					@EventTime,
					@FilePackageName,
					@ESER,
                    CASE @PipelineEvent
                        WHEN 'STARTED' THEN 'STARTED'   
                        ELSE 'COMPLETED'   
                    END,
					@PipelineEvent,
					@PipelineStage,
					@PipelineFailureReason,
					@PipelineFailureType,
					@DataLoggerType,
					@ExceptionMessage,
                    @ErrorCode,
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