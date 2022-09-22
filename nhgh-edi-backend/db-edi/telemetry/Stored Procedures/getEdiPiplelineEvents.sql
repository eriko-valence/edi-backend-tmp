

CREATE PROCEDURE [telemetry].[getEdiPiplelineEvents]
  @eser VARCHAR(50)
AS
BEGIN
  select * from [telemetry].[EdiPipelineEvents] where ESER = @eser
END