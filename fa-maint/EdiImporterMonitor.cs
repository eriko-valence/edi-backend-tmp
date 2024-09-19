using System;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Services.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using lib_edi.Models.Edi.Job;
using lib_edi.Services.Edi;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Models.Enums.Edi.Data.Import;
using Microsoft.Azure.Functions.Worker;

namespace fa_maint
{
    public class EdiImporterMonitor
    {
        [Function("edi-maint-importer-monitor")]
        public async Task RunAsync([TimerTrigger("%CRON_SCHEDULE_EDI_IMPORTER_MONITOR%"
            
            /*
            #if DEBUG
            , RunOnStartup=true
            #endif
            */
           
            
            
            )] TimerInfo schedule, ILogger log)
        {
            // track import job results using these objects
            EdiMaintJobStats r1 = new();

            string logPrefix = "- [edi_importer_monitor->run]: ";

            //OtaLoggerService logger = new(log);
            //if (schedule is null)
            //{
            //    throw new ArgumentNullException(nameof(schedule));
            //}

            log.LogInformation($"[DataImportTelemetryImporter:RunAsync] Triggered at: {DateTime.Now}");
            try
            {
                log.LogInformation($"{logPrefix} retrieve azure sql and log analytics connection information");
                EdiJobInfo job = EdiService.InitializeMaintJobImportEvents(EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER_MONITOR);
                log.LogInformation($"{logPrefix} - job.edilaw.workspaceid ..............: {job.EdiLaw.WorkspaceId}");
                log.LogInformation($"{logPrefix} - job.edilaw.azurebloburiccdxprovider .: {job.EdiLaw.AzureBlobUriCcdxProvider}");
                log.LogInformation($"{logPrefix} - job.edilaw.queryhours ...............: {job.EdiLaw.QueryHours}");
                log.LogInformation($"{logPrefix} - job.edidb.name ......................: {job.EdiDb.Name}");
                log.LogInformation($"{logPrefix} - job.edidb.server ....................: {job.EdiDb.Server}");
                log.LogInformation($"{logPrefix} retrieve telmetry from azure log analytics");
                List<DataImporterAppEvent> l1 = await AzureMonitorService.QueryWorkspaceForEdiMaintEvents(job);
                if (l1.Count > 0)
                {
                    log.LogInformation($"{logPrefix} insert telmetry into azure sql");
                    r1 = await AzureSqlDatabaseService.InsertEdiDataImporterJobResults(job, l1);
                }
                else
                {
                    log.LogInformation($"{logPrefix} no telmetry found in azure log analytics");
                }

                EdiMaintJobStats jobStatsSummarySucceeded = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_IMPORTER_MONITOR_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER_MONITOR,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.SUCCESS,
                    Queried = r1.Queried,
                    Loaded = r1.Loaded,
                    Skipped = r1.Skipped,
                    Failed = r1.Failed
                };

                log.LogInformation($"{logPrefix} - function app name ........: {jobStatsSummarySucceeded.EdiFunctionApp}");
                log.LogInformation($"{logPrefix} - import results");
                log.LogInformation($"{logPrefix}   - total telemetry events .: {jobStatsSummarySucceeded.Queried}");
                log.LogInformation($"{logPrefix}     - loaded ...............: {jobStatsSummarySucceeded.Loaded}");
                log.LogInformation($"{logPrefix}     - failed ...............: {jobStatsSummarySucceeded.Failed}");

                AzureAppInsightsService.LogEvent(jobStatsSummarySucceeded);
            }
            catch (Exception ex)
            {
                EdiMaintJobStats jobStatsSummaryFailed = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_IMPORTER_MONITOR_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER_MONITOR,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.FAILED,
                    ExceptionMessage = ex.Message,
                    Queried = r1.Queried,
                    Loaded = r1.Loaded,
                    Skipped = r1.Skipped,
                    Failed = r1.Failed
                };
                AzureAppInsightsService.LogEvent(jobStatsSummaryFailed);
            }
        }
    }
}
