	CREATE PROCEDURE [telemetry].[uspCreateEdiFunctionTrace]
    @EventTime datetime2(7),
    @FilePackageName varchar(255),
    @OperationName varchar(100),
    @SeverityLevel tinyint,
    @LogMessage varchar(2000),
    @LogMessageMd5 varchar(50),
    @Result INT OUTPUT
AS
BEGIN
    BEGIN TRY
        BEGIN TRANSACTION
            BEGIN
                SELECT [EventTime] FROM [telemetry].[EdiFunctionTrace]
                    WHERE [EventTime] = @EventTime AND [LogMessageMd5] = @LogMessageMd5
            END
            IF @@ROWCOUNT = 0
            BEGIN
                INSERT INTO [telemetry].[EdiFunctionTrace] (
                    [EventTime],
                    [FilePackageName],
                    [OperationName],
                    [SeverityLevel],
                    [LogMessage],
                    [LogMessageMd5],
                    [DateAdded])
                VALUES(
                    @EventTime,
                    @FilePackageName,
                    @OperationName,
                    @SeverityLevel,
                    @LogMessage,
                    @LogMessageMd5,
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