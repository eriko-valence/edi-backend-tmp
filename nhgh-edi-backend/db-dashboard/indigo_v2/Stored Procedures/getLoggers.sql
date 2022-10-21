CREATE PROCEDURE [indigo_v2].[getLoggers]

AS
BEGIN
    WITH
        INDIGO_LOGGERS_CTE AS
        (
            SELECT
                MAX([ABST_CALC]) AS 'ABST_CALC',
                [ESER],
                substring([ESER],9,4) AS ShortId,
                [LSER]
            FROM 
                [indigo_v2].[event]
            GROUP BY
                [ESER],
                [LSER]
        )

    SELECT * FROM (SELECT *, ROW_NUMBER() OVER (
                        PARTITION BY [LSER] 
                        ORDER BY [ABST_CALC] DESC
                ) AS [ROW_NUMBER]
    FROM 
    INDIGO_LOGGERS_CTE) LOGGERS
    WHERE 
    ROW_NUMBER = 1
END