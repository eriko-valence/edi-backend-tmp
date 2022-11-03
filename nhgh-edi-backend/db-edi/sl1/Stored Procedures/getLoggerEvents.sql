CREATE PROCEDURE [sl1].[getLoggerEvents]
(
	@LSER varchar(64),
	@StartDate [datetime2](7) = NULL,
	@EndDate [datetime2](7) = NULL
)
AS
BEGIN
    SET NOCOUNT ON
    SELECT 
        [ABST_CALC],
        --[ADOP],
        --[ALRM],
        --[AMOD],
        --[AMFR],
        --[APQS],
        --[ASER],
        [BLOG],
        --[DORV],
        [ESER],
        --[HOLD],
        --[LDOP],
        --[LERR],
        --[LMFR],
        --[LMOD],
        --[LPQS],
        [LSER],
        --[LSV],
        --[RELT],
        --[RTCW],
        [TAMB],
        [TVC]
        --[ZCHRG],
        --[ZSTATE],
        --[ZVLVD],
        --[_RELT_SECS],
        --[DATEADDED]
    FROM
        [sl1].[event]
    WHERE 
        [LSER] = @LSER AND
        ABST_CALC >= @StartDate AND 
        ABST_CALC <= @EndDate
END