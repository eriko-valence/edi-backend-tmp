
CREATE PROCEDURE [dbo].[getBatteryChargeTelerik]
(
	@ESER varchar(64),
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

	--declare @r0 varchar(MAX);

	WITH
	usbdg_event_cte as
	(
		SELECT 
			ESER, zutc_now, DATEADD(HOUR, DATEDIFF(HOUR, 0, zutc_now), 0) AS 'zutc_now_hour', 
			zbatt_chrg FROM [usbdg].[event] 
		WHERE 
			eser = @ESER
	)

	--select @r0 =
	--(
	SELECT 
	  ue.ESER,
	  cdh.CalendarDateHour as 'ZUTC_NOW',
	  AVG(ue.zbatt_chrg) as 'BEMD'
	  
	FROM 
	  [dbo].[DateSeq1hour] cdh
	LEFT OUTER JOIN usbdg_event_cte ue ON cdh.CalendarDateHour = zutc_now_hour
	GROUP BY
	  cdh.CalendarDateHour,
	  ue.ESER
	HAVING
	  cdh.CalendarDateHour >= @StartDate AND cdh.CalendarDateHour <= @EndDate
	ORDER BY 
	  cdh.CalendarDateHour
	--FOR JSON AUTO
	--)

	--select @r0



END