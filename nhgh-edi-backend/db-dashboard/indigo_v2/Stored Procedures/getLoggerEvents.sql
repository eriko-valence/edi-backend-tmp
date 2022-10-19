CREATE PROCEDURE [indigo_v2].[getLoggerEvents]
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
        [DORV],
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
        [indigo_v2].[event]
    WHERE 
        [LSER] = @LSER
END