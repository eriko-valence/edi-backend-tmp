




/*


	exec getDgData '40A36BCA693B', '2022-7-12', '2022-7-15'

*/

CREATE PROCEDURE [dbo].[getDgData]
(

	@DeviceId varchar(64),
	@startDte varchar(64),
	@stopDte varchar(64)
)

AS
BEGIN

    SET NOCOUNT ON


		declare @r0 varchar(max)
		declare @r1 varchar(max)


		select @r0 =
		(
			select 
			datediff_big(MILLISECOND,{d '1970-01-01'},zutc_now) as x,
			TAMB as y
			FROM USBDG.event
			where zutc_now >= @startDte and zutc_now < @stopDte
			and ESER = @DeviceId
			order by x FOR JSON AUTO
		)

		select @r1 =
		(
			select 
			datediff_big(MILLISECOND,{d '1970-01-01'},zutc_now) as x,
			--cast(zbatt_volt / 1000 as numeric(8,3)) as y
			cast(cast(zbatt_volt  as float) / 1000 as numeric(8,3)) as y
			FROM USBDG.event
			where zutc_now >= @startDte and zutc_now < @stopDte
			and ESER = @DeviceId
			order by x FOR JSON AUTO
		)



		select isnull(@r0,'') + '|' + isnull(@r1,'')










END