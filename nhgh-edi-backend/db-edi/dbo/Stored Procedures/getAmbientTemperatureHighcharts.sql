
CREATE PROCEDURE [dbo].[getAmbientTemperatureHighcharts]
(
	@ESER varchar(64),
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

	declare @r0 varchar(MAX);

	with
	usbdg_event_cte as
	(
		select ESER, zutc_now, DATEADD(HOUR, DATEDIFF(HOUR, 0, zutc_now), 0) as 'zutc_now_hour', TAMB from [usbdg].[event] where eser = @ESER
	)

	select @r0 =
	(
	SELECT 
	  datediff_big(MILLISECOND,{d '1970-01-01'},cdh.CalendarDateHour) as 'x',
	  AVG(ue.TAMB) as 'y'
	FROM 
	  [dbo].[DateSeq1hour] cdh
	LEFT OUTER JOIN usbdg_event_cte ue ON cdh.CalendarDateHour = zutc_now_hour
	GROUP BY
	  cdh.CalendarDateHour
	HAVING
	  cdh.CalendarDateHour >= @StartDate AND cdh.CalendarDateHour <= @EndDate
	ORDER BY 
	  cdh.CalendarDateHour
	FOR JSON AUTO
	)

	select @r0

END