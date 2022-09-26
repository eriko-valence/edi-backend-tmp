
	CREATE PROCEDURE [telemetry].[uspCreateEdiJobStatusEvent]
    @FilePackageName [varchar](255),
	@ESER [varchar](50),
	@BlobTimeStart [datetime2],
	@ProviderSuccessTime [datetime2],
	@ConsumerSuccessTime [datetime2],
	@TransformSuccessTime [datetime2],
	@SQLSuccessTime [datetime2] ,
	@DurationSecs [int],
    @Result INT OUTPUT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN
                SELECT [FilePackageName] FROM [telemetry].[EdiJobStatus]
                    WHERE [FilePackageName] = @FilePackageName
            END
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [telemetry].[EdiJobStatus] (
                    [FilePackageName],
					[ESER],
                    [BlobTimeStart],
                    [ProviderSuccessTime],
                    [ConsumerSuccessTime],
                    [TransformSuccessTime],
                    [SQLSuccessTime],
					[DurationSecs],
					[DateAdded]) 
                VALUES(
                    @FilePackageName,
					@ESER,
                    @BlobTimeStart,
                    @ProviderSuccessTime,
                    @ConsumerSuccessTime,
                    @TransformSuccessTime,
                    @SQLSuccessTime,
					@DurationSecs,
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