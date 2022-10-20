
CREATE PROCEDURE [indigo_v2].[getIndigoLoggerStats]
(
	@LSER varchar(64) = NULL,
	@StartDate [datetime2](7),
	@EndDate [datetime2](7)
)
AS
BEGIN

    SELECT
        LSER, 
        MAX(ABST_CALC) as 'LAST_ACTIVITY',
        COUNT(*) as 'EVENTS',
        MAX(TAMB) as 'MAX_TAMB',
        MIN(TAMB) as 'MIN_TAMB'
    FROM
        [indigo_v2].[event]
    WHERE
        ABST_CALC >= @StartDate AND ABST_CALC <= @EndDate
    GROUP BY LSER
    HAVING
        LSER = @LSER

END