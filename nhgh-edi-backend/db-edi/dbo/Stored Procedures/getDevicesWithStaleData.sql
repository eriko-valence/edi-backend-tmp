CREATE PROCEDURE [dbo].[getDevicesWithStaleData]
AS
BEGIN

    DECLARE @StaleCount varchar(50);
    DECLARE @RecentCount varchar(50);

	WITH 
    StaleCountCTE
	AS
	(
        SELECT 
            count(*) AS 'StaleCount'
        FROM 
            [usbdg].[device] 
        WHERE 
            LASTMODIFIED < DATEADD(DAY, -3, GETDATE())

	)
    SELECT @StaleCount = StaleCount FROM StaleCountCTE;

	WITH 
    RecentCountCTE
	AS
	(
        SELECT 
            count(*) AS 'RecentCount'
        FROM 
            [usbdg].[device] 
        WHERE 
            LASTMODIFIED > DATEADD(DAY, -3, GETDATE())

	)
    SELECT @RecentCount = RecentCount FROM RecentCountCTE;
    
    SELECT @StaleCount AS 'StaleCount', @RecentCount as 'RecentCount';

END