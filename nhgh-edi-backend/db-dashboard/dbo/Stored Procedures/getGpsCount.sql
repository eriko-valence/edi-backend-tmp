



/*


	exec getGpsCount '40A36BCA695F', '2022-6-15', '2022-6-30'

*/

CREATE PROCEDURE [dbo].[getGpsCount]
(

	@DeviceId varchar(64),
	@startDte varchar(64),
	@stopDte varchar(64)
)

AS
BEGIN

    SET NOCOUNT ON
	--declare @dayWindow int = 15


		select 
		datediff_big(MILLISECOND,{d '1970-01-01'},CalendarDate) as x,
		ISNULL(ct,0) as y
		into #t
		FROM [dbo].[DateSeq1day] d
		left join
		(
			select count(*) as ct,
			cast(zgps_utc as date) as dte
			FROM [usbdg].[location]
			where ESER = @DeviceId
			group by cast(zgps_utc as date)
		) x
		on d.CalendarDate = x.dte
		--where CalendarDate >= DATEADD(day,-@dayWindow,@startDte) and CalendarDate < DATEADD(day,@dayWindow,@stopDte)
		where CalendarDate >= @startDte and CalendarDate < @stopDte

		--select * from #t

		declare @json varchar(max) = (select * from #t order by x FOR JSON AUTO)
		select @json

END