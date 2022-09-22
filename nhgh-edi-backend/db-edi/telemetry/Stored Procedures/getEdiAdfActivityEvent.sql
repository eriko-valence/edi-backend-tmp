

CREATE PROCEDURE [telemetry].[getEdiAdfActivityEvent]
  @FilePackageName VARCHAR(50)
AS
BEGIN
  select * from [telemetry].[EdiAdfActivity] where FilePackageName = @FilePackageName
END