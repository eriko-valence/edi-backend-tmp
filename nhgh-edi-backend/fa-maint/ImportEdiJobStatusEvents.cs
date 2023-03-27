using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using lib_edi.Models.Azure.Monitor.Query;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.Enums.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Services.Azure;
using lib_edi.Services.Edi;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Host;
using Microsoft.Extensions.Logging;

namespace fa_maint
{
    public class ImportEdiJobStatusEvents
    {
        [FunctionName("ImportEdiJobStatusEvents")]
        public static async Task Run([TimerTrigger("%EDI_DAILY_STATUS_REPORT_TIMER_SCHEDULE%")] TimerInfo schedule, ILogger log)
        {
            string logPrefix = "- [import_edi_job_status_events->run]: ";
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            log.LogInformation($"{logPrefix} retrieve azure sql database connection information from azure key vault");
            

            // track import job results using these objects
            OtaImportJobStats r1 = new();
            OtaImportJobStats r2 = new();
            OtaImportJobStats r3 = new();
            OtaImportJobStats r4 = new();

            //OtaLoggerService logger = new(log);
            if (schedule is null)
            {
                throw new ArgumentNullException(nameof(schedule));
            }

            log.LogInformation($"[EdiLawImporter:RunAsync] Triggered at: {DateTime.Now}");
            try
            {
                //OtaImportJob job = OtaImporterService.InitializeOtaImportJob(OtaJobImportFunctionEnum.Name.EDI_EVENTS, logger);
                EdiJobInfo job = EdiService.InitializeMaintJobImportEvents(EdiFunctionsEnum.Name.EDI_JOB_STATUS_IMPORTER);

                // Query Log Analytics workspace for overall EDI job status results
                List<EdiJobStatusResult> l1 = await AzureMonitorService.QueryWorkspaceForEdiJobsStatus(job);
                if (l1.Count > 0)
                {
                    r1 = await AzureSqlDatabaseService.InsertEdiJobStatusEvents(job, l1);
                }
                else
                {
                    log.LogInformation($"[EdiLawImporter:RunAsync] No EDI job status records found in the Log Analytics workspace");
                }

                // Query Log Analytics workspace for EDI job pipeline events
                List<EdiPipelineEventResult> l2 = await AzureMonitorService.QueryWorkspaceForEdiPipelineEvents(job);
                if (l2.Count > 0)
                {
                    r2 = await AzureSqlDatabaseService.InsertEdiPipelineEvents(job, l2);
                }
                else
                {
                    log.LogInformation($"[EdiLawImporter:RunAsync] No EDI pipeline records found in the Log Analytics workspace");
                }

                // Query Log Analytics workspace for EDI ADF pipeline activity events
                List<EdiAdfActivityResult> l3 = await AzureMonitorService.QueryWorkspaceForEdiAdfActivityData(job);
                if (l3.Count > 0)
                {
                    r3 = await AzureSqlDatabaseService.InsertEdiAdfActivityEvents(job, l3);
                }
                else
                {
                    log.LogInformation($"[EdiLawImporter:RunAsync] No EDI ADF pipeline activity records found in the Log Analytics workspace");
                }

                // Query Log Analytics workspace for EDI Azure Function trace events
                List<AzureFunctionTraceResult> l4 = await AzureMonitorService.QueryWorkspaceForAzureFunctionTraceData(job);
                if (l4.Count > 0)
                {
                    r4 = await AzureSqlDatabaseService.InsertEdiAzureFunctionTraceRecords(job, l4);
                }
                else
                {
                    log.LogInformation($"[EdiLawImporter:RunAsync] No EDI ADF pipeline activity records found in the Log Analytics workspace");
                }

                OtaImportJobStats jobStatsSummarySucceeded = new()
                {
                    OtaJobEventType = OtaJobImportEventEnum.Name.OTA_IMPORT_JOB_RESULT,
                    OtaJobName = OtaJobImportFunctionEnum.Name.EDI_EVENTS,
                    OtaJobStatus = OtaJobImportStatusNameEnum.Name.SUCCESS,
                    Queried = r1.Queried + r2.Queried + r3.Queried + r4.Queried,
                    Loaded = r1.Loaded + r2.Loaded + r3.Loaded + r4.Loaded,
                    Skipped = r1.Skipped + r2.Skipped + r3.Skipped + r4.Skipped,
                    Failed = r1.Failed + r2.Failed + r3.Failed + r4.Failed
                };
                //AzureAppInsightsService.LogEvent(jobStatsSummarySucceeded);
            }
            catch (Exception ex)
            {
                OtaImportJobStats jobStatsSummaryFailed = new()
                {
                    OtaJobEventType = OtaJobImportEventEnum.Name.OTA_IMPORT_JOB_RESULT,
                    OtaJobName = OtaJobImportFunctionEnum.Name.EDI_EVENTS,
                    OtaJobStatus = OtaJobImportStatusNameEnum.Name.FAILED,
                    ExceptionMessage = ex.Message,
                    Queried = r1.Queried + r2.Queried + r3.Queried + r4.Queried,
                    Loaded = r1.Loaded + r2.Loaded + r3.Loaded + r4.Loaded,
                    Skipped = r1.Skipped + r2.Skipped + r3.Skipped + r4.Skipped,
                    Failed = r1.Failed + r2.Failed + r3.Failed + r4.Failed
                };
                //AzureAppInsightsService.LogEvent(jobStatsSummaryFailed);
            }

        }
    }
}
