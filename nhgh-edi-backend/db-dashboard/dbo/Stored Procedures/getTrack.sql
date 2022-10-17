


/*
	exec getTrack '40A36BCA695F'
*/

CREATE PROCEDURE [dbo].[getTrack]
(

	@DeviceId varchar(64),
	@startDte varchar(64),
	@stopDte varchar(64),
	@distFilter int = 500


)

AS
BEGIN

    SET NOCOUNT ON

	-- groups points by distance from first point in group, may be 1 to n points
	-- n should be reasonably low unless devices is in same location for extended period of time and moved occasionally 
	-- grouped points are averaged so they not match actual values

	-- the distance filter is measured from the initial value in a group until the last value in the group
	-- not one value to the next, as was done originally for simplification
	-- note that all gps values in the database are pre-filtered on the device as being valid
	-- but may be somewhat inaccurate - unfortuantely there is now accuracy metric, HDOP is uselss

	-- 500 meters seems about right for initial testing
	--declare @distfilter int = 500

	-- get values in a table for easier manipulation
	declare @r table (rn int, dtm datetime2(0), lat float, lng float, grp int)

	insert into @r 
	select
	ROW_NUMBER() over (order by zgps_utc),
	zgps_utc,
	cast(zgps_lat as float) lat,
	cast(zgps_lng as float) lng,
	null
	FROM [usbdg].[location]
	where ESER = @DeviceId 
	and zgps_utc >= @startDte and zgps_utc < @stopDte

	-- initial values

	declare @lat0 float, @lng0 float, @lat1 float, @lng1 float
	select @lat0 = lat from @r where rn = 1
	select @lng0 = lng from @r where rn = 1
	select @lat1 = lat from @r where rn = 2
	select @lng1 = lng from @r where rn = 2

	declare @rn int = 2
	declare @dist float = 0
	declare @grp int = 0
	update @r set grp = 0 where rn = 1
	declare @maxrn int = (select max(rn) from @r)


	-- a while loop is crude but ok since it only runs through the table once
	-- re-write as a cte? why bother
	
		while @rn <= @maxrn

		begin

			select @dist = distm from dbo.getHaversineDistance (@lat0, @lng0, @lat1, @lng1)

			if @dist < @distfilter 
			begin
				-- all in the same distance group
				select @lat1 = lat, @lng1 = lng from @r where rn = @rn + 1
			end

			else
			begin
				-- reinit for next distance group
				set @grp = @grp + 1
				select @lat0 = lat, @lng0 = lng from @r where rn = @rn
				select @lat1 = lat, @lng1 = lng from @r where rn = @rn + 1
			end
			
			update @r set grp = @grp where rn = @rn
			set @rn = @rn + 1

		end



	-- now get the bearing to the next point after grouping 
	-- this is to rotate the map marker so it points in the direction of travel
	-- doesn't look great for 10 min values but that's what you get with low sample rate

	; with a as
	(
		select 
		cast(FORMAT( min(dtm), 'MM/dd/yyyy HH:mm' ) as varchar(43)) as dtm,
		avg(lat) as lat, 
		avg(lng) as lng
		from @r
		group by grp
	)

	,b as
	(
		select 
		dtm,
		lat as lat0,
		lng as lng0,
		lag(lat) over (order by dtm) as lat1,
		lag(lng) over (order by dtm) as lng1
		from a
	) 

	select
	dtm, lat0 as lat, lng0 as lon,
	cast(ISNULL(bearing,0) as int)  as bearing
	from b
	cross apply dbo.getHaversineBearingDistance  (lat1, lng1, lat0, lng0)



END