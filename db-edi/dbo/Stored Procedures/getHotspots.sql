/*
	exec getHotspots '40A36BCA6983'

	exec getHotspots '40A36BCA695F'

*/

CREATE PROCEDURE [dbo].[getHotspots]
(

	@DeviceId varchar(64),
	@startDte varchar(64),
	@stopDte varchar(64)
)

AS
BEGIN

    SET NOCOUNT ON

	declare @r table (lat float, lon float, groupNumber int)

	declare @distfilter int = 500
	declare @hotCount int = 8
		
	;with a as
	(
		select 
		zgps_utc,
		cast(zgps_lat as float) lat1,
		cast(zgps_lng as float) lon1,

		lead(zgps_lat) over (order by zgps_utc) as lat2,
		lead(zgps_lng) over (order by zgps_utc) as lon2

		FROM [usbdg].[location]
		where ESER = @DeviceId
		and zgps_utc >= @startDte and zgps_utc < @stopDte

	)
	
	,b as
	(
		select 
		zgps_utc,
		lat1,
		lon1,
		lat2,
		lon2,
		distM
		from a	
		cross apply dbo.getHaversineDistance  (a.lat1, a.lon1, a.lat2, a.lon2)
	)

	,c as
	(
		select 
		ROW_NUMBER() over (order by zgps_utc) as rn,
		*,
		case when distm > @distfilter then 0 else 1 end as dxx
		from b 
	)


	 ,e as
	 (
		select *,
		rn - (RANK() OVER(PARTITION BY dxx ORDER BY zgps_utc)) as groupNumber
		from c
		where dxx = 1
	  )


		insert into @r
		select lat1, lon1, groupNumber from e

		select  --groupNumber,  count(*) as ct,
		(select avg(lat)) as lat,
		(select avg(lon)) as lon
		from @r 
		group by groupNumber
		having count(*) > @hotCount


END