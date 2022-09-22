

CREATE PROCEDURE [telemetry].[getEdiFilePackagePiplelineEvents]
  @FilePackageName VARCHAR(100)
AS
BEGIN
  select * from [telemetry].[EdiPipelineEvents] where FilePackageName = @FilePackageName
END