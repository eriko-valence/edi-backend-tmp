

CREATE PROCEDURE [telemetry].[getEdiFilePackageAdfActivity]
  @FilePackageName VARCHAR(100)
AS
BEGIN
  select * from [telemetry].[EdiAdfActivity] where FilePackageName = @FilePackageName
END