CREATE PROCEDURE [dbo].[getDevices]

AS
BEGIN

    SET NOCOUNT ON

    select 
        a.eser as Id,
        --stuff(substring(a.eser,8,99),3,0,'-')  as ShortId,
		substring(a.eser,9,4) as ShortId,
        a.LASTMODIFIED as LastData,
        substring(a.emsv,0,len(a.EMSV)-5) as EMSV,
        a.COMMENTS
    from [usbdg].[device] a
END