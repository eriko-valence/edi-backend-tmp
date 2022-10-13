CREATE PROCEDURE [usbdg].[uspUpdateDeviceComments]
  @eser varchar(50), @comments varchar(1000), @result INT OUTPUT
AS
BEGIN
	BEGIN TRY
		BEGIN TRANSACTION
			IF EXISTS(SELECT 1 FROM [usbdg].[device] WHERE ESER = @eser)
                BEGIN
                    UPDATE [usbdg].[device]
                    SET COMMENTS = @comments
                    WHERE
                    ESER = @eser
                    IF @@rowcount = 1
                        SET @result = 1 --successful update
                    ELSE
                        SET @result = 4 --unsuccessful update 
                END
                ELSE
                    SET @result = 5 --device not found
		COMMIT
	END TRY
	BEGIN CATCH
		SET @result = 6 --unsuccessful update
		ROLLBACK
	END CATCH	
END