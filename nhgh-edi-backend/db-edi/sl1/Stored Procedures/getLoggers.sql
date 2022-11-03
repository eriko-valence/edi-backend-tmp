CREATE PROCEDURE [sl1].[getLoggers]

AS
BEGIN
    WITH
        SL1_LOGGERS_CTE AS
        (
            SELECT
                MAX([ABST_CALC]) AS 'ABST_CALC',
                [ESER],
                substring([ESER],9,4) AS ShortId,
                [LSER]
            FROM 
                [sl1].[event]
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
        SL1_LATEST_DG_CTE AS
        (
            SELECT * FROM (SELECT *, ROW_NUMBER() OVER (
                                PARTITION BY [LSER] 
                                ORDER BY [ABST_CALC] DESC
                        ) AS [ROW_NUMBER]
            FROM 
            SL1_LOGGERS_CTE) LOGGERS
            WHERE 
            ROW_NUMBER = 1
        )

        SELECT 
            t1.*, t2.LastData as 'LastUsbdgData'
        FROM 
            SL1_LATEST_DG_CTE t1
        LEFT OUTER JOIN
            DATAGRABBERS_CTE t2 ON t2.Id = t1.ESER
END