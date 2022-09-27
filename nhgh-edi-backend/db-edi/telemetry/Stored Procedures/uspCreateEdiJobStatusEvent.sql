
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
            BEGIN
                UPDATE [telemetry].[EdiJobStatus]
                SET 
                    ESER = @ESER, 
                    BlobTimeStart = @BlobTimeStart,
                    ProviderSuccessTime = @ProviderSuccessTime,
                    ConsumerSuccessTime = @ConsumerSuccessTime,
                    TransformSuccessTime = @TransformSuccessTime,
                    SQLSuccessTime = @SQLSuccessTime,
                    DurationSecs = @DurationSecs,
                    DateUpdated = getdate()
                WHERE 
                    FilePackageName = @FilePackageName;
                IF @@ROWCOUNT = 1
                    SET @Result = 2 --successful upsert
                ELSE
                    SET @Result = 4 --unsuccessful upsert 
            END
        COMMIT
    END TRY
    BEGIN CATCH
        SET @Result = 5 --unsuccessful insert (unknown)
        ROLLBACK
    END CATCH   
END