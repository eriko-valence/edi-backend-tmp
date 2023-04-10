

CREATE PROCEDURE [telemetry].[getEdiFilePackageJobStatus]
  @FilePackageName VARCHAR(100)
AS
BEGIN
  select * from [telemetry].[EdiJobStatus] where FilePackageName = @FilePackageName
END