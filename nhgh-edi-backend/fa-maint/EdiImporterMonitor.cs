using System;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Services.Azure;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;
using lib_edi.Models.Edi.Job;
using lib_edi.Services.Edi;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Models.Enums.Edi.Data.Import;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.WebJobs.Extensions.Http;

namespace fa_maint
{
    public class EdiImporterMonitor
    {
        [FunctionName("edi-maint-importer-monitor")]
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

            //OtaLoggerService logger = new(log);
            //if (schedule is null)
            //{
            //    throw new ArgumentNullException(nameof(schedule));
            //}

            log.LogInformation($"[DataImportTelemetryImporter:RunAsync] Triggered at: {DateTime.Now}");
            try
            {
                EdiJobInfo job = EdiService.InitializeMaintJobImportEvents(EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER_MONITOR);
                List<DataImporterAppEvent> l1 = await AzureMonitorService.QueryWorkspaceForEdiMaintEvents(job);
                if (l1.Count > 0)
                {
                    r1 = await AzureSqlDatabaseService.InsertEdiDataImporterJobResults(job, l1);
                }
                else
                {
                    log.LogInformation($"[DataImportTelemetryImporter:RunAsync] No EDI job status records found in the Log Analytics workspace");
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
