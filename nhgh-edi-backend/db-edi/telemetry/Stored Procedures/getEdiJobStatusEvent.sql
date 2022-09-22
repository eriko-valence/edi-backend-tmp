

CREATE PROCEDURE [telemetry].[getEdiJobStatusEvent]
  @FilePackageName VARCHAR(50)
AS
BEGIN
  select * from [telemetry].[EdiJobStatus] where FilePackageName = @FilePackageName
END