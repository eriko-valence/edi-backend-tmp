
CREATE PROCEDURE [dbo].[getDeviceEvents]
(
	@ESER varchar(64),
	@StartDate [datetime2](7) = NULL,
	@EndDate [datetime2](7) = NULL
)
AS
BEGIN

    IF @StartDate IS NOT NULL AND @EndDate IS NOT NULL
    BEGIN
        WITH 
        ZBattChrgCTE
        AS
        (
            SELECT
                ESER,
                zutc_now, 
                zbatt_volt,
                zbatt_chrg, 
                Lag(zbatt_chrg, 1) OVER(ORDER BY DATEADDED asc) AS last_zbatt_chrg,
                tamb
            FROM 
                [usbdg].[event]
            WHERE ESER = @ESER AND zbatt_chrg IS NOT NULL
        )

        SELECT 
            ESER, 
            zutc_now AS 'ZUTC_NOW', 
            zbatt_volt AS 'ZBATT_VOLT',
            zbatt_chrg AS 'ZBATT_CHRG',
            tamb AS 'TAMB',
            last_zbatt_chrg AS 'ZBATT_CHRG_LAST',
            (zbatt_chrg - last_zbatt_chrg) AS 'ZBATT_CHRG_CHANGE'
        FROM 
            ZBattChrgCTE 
        WHERE 
            zutc_now > @StartDate 
            AND zutc_now < @EndDate
            AND last_zbatt_chrg is not null
        ORDER BY 
            zutc_now DESC

    END
    ELSE
    BEGIN
        WITH 
        ZBattChrgCTE
        AS
        (
            SELECT
                ESER,
                zutc_now, 
                zbatt_volt,
                zbatt_chrg, 
                Lag(zbatt_chrg, 1) OVER(ORDER BY DATEADDED asc) AS last_zbatt_chrg,
                tamb
            FROM 
                [usbdg].[event]
            WHERE ESER = @ESER AND zbatt_chrg IS NOT NULL
        )

        SELECT 
            ESER, 
            zutc_now AS 'ZUTC_NOW', 
            zbatt_volt AS 'ZBATT_VOLT',
            zbatt_chrg AS 'ZBATT_CHRG',
            tamb AS 'TAMB',
            last_zbatt_chrg AS 'ZBATT_CHRG_LAST',
            (zbatt_chrg - last_zbatt_chrg) AS 'ZBATT_CHRG_CHANGE'
        FROM 
            ZBattChrgCTE 
        WHERE 
            last_zbatt_chrg is not null
    END


END