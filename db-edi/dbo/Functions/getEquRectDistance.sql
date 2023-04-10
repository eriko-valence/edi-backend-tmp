/*
https://stackoverflow.com/questions/4913349/haversine-formula-in-python-bearing-and-distance-between-two-gps-points
https://www.geeksforgeeks.org/haversine-formula-to-find-distance-between-two-points-on-a-sphere/
https://www.movable-type.co.uk/scripts/latlong.html

JavaScript:	
const x = (λ2-λ1) * Math.cos((φ1+φ2)/2);
const y = (φ2-φ1);
const d = Math.sqrt(x*x + y*y) * R;



--	select [loc].[getHaversineDist] (51.5007,0.1246,40.6892,74.0445)
--   select * from loc.getHaversineBearingDistance  (51.5007,0.1246,40.6892,74.0445)

select * from loc.getHaversineBearingDistance  (50,-5.5,58,-3)
select * from loc.getHaversineBearingDistance  (50.0,-5.0,50.0,-5.0)




*/




--- NOT FINISHED !!!!!!!!!!!!!!!!!



CREATE FUNCTION [dbo].[getEquRectDistance]
(

	@lat1 float(53), @lon1 float(53), @lat2 float(53), @lon2 float(53)

)
RETURNS 

@t table
(
	distM numeric(12,0),
	distKm numeric(12,3),
	distMi numeric(12,3)
)

AS
BEGIN



	declare @distance float(53), 
	@dlat float(53), @dlon float(53), @a float(53), @rad float(53), @c float(53),
	@x float(53), @y float(53)

	-- distance
	-- avoid various floating point, divide by zero problems, yes this is a hack
	set @dlat = case when radians(@lat2 - @lat1) = 0 then .000000001 else radians(@lat2 - @lat1) end
	set @dlon = case when radians(@lon2 - @lon1) = 0 then .000000001 else radians(@lon2 - @lon1) end
	
	set @lat1 = radians(@lat1)
	set @lat2 = radians(@lat2)

	set @a = 
		power(sin(@dlat / 2.0),2) + 
		power(sin(@dlon / 2.0),2) * 
		cos(@lat1) * cos(@lat2)

	set @rad = 6371.0 * 1000.0
	set @c = 2.0 * asin(sqrt(@a))
	set @distance = @rad * @c


	-- results
	insert into @t select @distance, @distance / 1000.0, @distance * 0.621371 / 1000.0
	
	RETURN 
END