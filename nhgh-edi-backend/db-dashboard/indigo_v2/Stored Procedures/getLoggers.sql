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
        ),
        DATAGRABBERS_CTE AS
        (
            select 
            a.eser as Id,
            a.LASTMODIFIED as LastData
            from [usbdg].[device] a
        ),
        INDIGO_LATEST_DG_CTE AS
        (
            SELECT * FROM (SELECT *, ROW_NUMBER() OVER (
                                PARTITION BY [LSER] 
                                ORDER BY [ABST_CALC] DESC
                        ) AS [ROW_NUMBER]
            FROM 
            INDIGO_LOGGERS_CTE) LOGGERS
            WHERE 
            ROW_NUMBER = 1
        )

        SELECT 
            t1.*, t2.LastData as 'LastUsbdgData'
        FROM 
            INDIGO_LATEST_DG_CTE t1
        LEFT OUTER JOIN
            DATAGRABBERS_CTE t2 ON t2.Id = t1.ESER


END