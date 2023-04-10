

CREATE PROCEDURE [telemetry].[getEdiFunctionTraces]
  @FilePackageName VARCHAR(100)
AS
BEGIN
  select EventTime, OperationName, LogMessage, SeverityLevel from [telemetry].[EdiFunctionTrace] where FilePackageName = @FilePackageName
END