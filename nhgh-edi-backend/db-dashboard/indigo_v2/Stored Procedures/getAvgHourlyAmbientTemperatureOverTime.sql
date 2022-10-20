

CREATE PROCEDURE [indigo_v2].[getAvgHourlyAmbientTemperatureOverTime]
(
	@LSER varchar(64),
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

	with
	indigo_event_cte as
	(
		SELECT 
            LSER, 
            ABST_CALC, 
            DATEADD(HOUR, DATEDIFF(HOUR, 0, ABST_CALC), 0) AS 'ABST_CALC_HOUR',
            TAMB
        FROM 
            [indigo_v2].[event]
        WHERE
            LSER = @LSER
	)

	SELECT 
	  cdh.CalendarDateHour as 'EventData',
	  AVG(ue.TAMB) as 'AvgTamb',
      ue.LSER
	FROM 
	  [dbo].[DateSeq1hour] cdh
	LEFT OUTER JOIN indigo_event_cte ue ON cdh.CalendarDateHour = ABST_CALC_HOUR
	GROUP BY
	  ue.LSER,
      cdh.CalendarDateHour
	HAVING
	  cdh.CalendarDateHour >= @StartDate AND cdh.CalendarDateHour <= @EndDate
	ORDER BY 
	  cdh.CalendarDateHour

END