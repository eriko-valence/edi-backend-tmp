CREATE PROCEDURE [indigo_v2].[getLoggers]

AS
BEGIN
    SET NOCOUNT ON
    SELECT
        MAX([ABST_CALC]) as 'ABST_CALC',
        [ESER],
        substring([ESER],9,4) as ShortId,
        [LSER]
    FROM 
        [indigo_v2].[event]
    GROUP BY
        [ESER],
        [LSER]
END