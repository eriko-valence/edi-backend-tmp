CREATE PROCEDURE [dbo].[getDeviceStats]
(
	@ESER varchar(64) = NULL,
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

	WITH 
	DevicesEventsCTE
	AS
	(
		SELECT
		  ESER, 
		  COUNT(zutc_now) as 'EVENTS',
          MIN(zbatt_chrg) as 'MIN_ZBATT_CHRG',
          MAX(TAMB) as 'MAX_TAMB',
          MIN(TAMB) as 'MIN_TAMB'
		FROM
		  [usbdg].[event]
		WHERE
		  zutc_now >= @StartDate AND zutc_now <= @EndDate
		GROUP BY ESER
		HAVING
		  ESER = @ESER
	)

	SELECT 
		d.ESER as 'ESER',
		d.LASTMODIFIED as 'LASTMODIFIED',
		e.EVENTS as 'EVENTS',
        e.MIN_ZBATT_CHRG,
        e.MAX_TAMB,
        e.MIN_TAMB
	FROM 
		[usbdg].[device] d
	LEFT OUTER JOIN DevicesEventsCTE e ON d.ESER = e.ESER
	WHERE
		d.ESER = @ESER
END