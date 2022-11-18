CREATE PROCEDURE [sl1].[getLoggers]

AS
BEGIN
    WITH
        -- get aggregated stationary logger event data by ESER and LSER
        SL1_LOGGERS_CTE AS
        (
            SELECT
                MAX([ABST_CALC]) AS 'ABST_CALC',
                [ESER],
                substring([ESER],9,4) AS ShortId,
                [LSER],
                concat(substring([LSER],3,2),substring([LSER],7,2)) AS LoggerShortId,
                [LSV]
            FROM 
                [sl1].[event]
            GROUP BY
                [ESER],
                [LSER],
                [LSV]
        ),
        -- get USBDG device list
        DATAGRABBERS_CTE AS
        (
            select 
            a.eser as Id,
            a.LASTMODIFIED as LastData
            from [usbdg].[device] a
        ),
        -- get the latest logger event record for each logger
        SL1_LATEST_CTE AS
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
        -- return results
        SELECT 
            t1.*, t2.LastData as 'LastUsbdgData'
        FROM 
            SL1_LATEST_CTE t1
        LEFT OUTER JOIN
            DATAGRABBERS_CTE t2 ON t2.Id = t1.ESER
END