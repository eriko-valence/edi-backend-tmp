using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using lib_edi.Models.Azure.Monitor.Query;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Models.Enums.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Services.Azure;
using lib_edi.Services.Edi;
using Microsoft.Extensions.Logging;
using Microsoft.Azure.Functions.Worker;

namespace fa_maint
{
    public class EdiImporter
    {
        private readonly ILogger<EdiImporter> _logger;

        public EdiImporter(ILogger<EdiImporter> logger)
        {
            _logger = logger;
        }

        [Function("edi-maint-importer")]
        public async Task Run([TimerTrigger("%CRON_SCHEDULE_EDI_IMPORTER%"
         
            /*
            #if DEBUG
            , RunOnStartup=true
            #endif
            */

            )] TimerInfo schedule)
        {
            string logPrefix = "- [edi_importer->run]: ";

            _logger.LogInformation($"{logPrefix} retrieve azure sql database connection information from azure key vault");
            

            // track import job results using these objects
            EdiMaintJobStats r1 = new();
            EdiMaintJobStats r2 = new();
            EdiMaintJobStats r3 = new();
            EdiMaintJobStats r4 = new();

            //OtaLoggerService logger = new(log);
            //if (schedule is null)
            //{
            //    throw new ArgumentNullException(nameof(schedule));
            //}

            try
            {
                EdiJobInfo job = EdiService.InitializeMaintJobImportEvents(EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER);
                _logger.LogInformation($"{logPrefix} - job.edilaw.workspaceid .............. : {job.EdiLaw.WorkspaceId}");
                _logger.LogInformation($"{logPrefix} - job.edilaw.azurebloburiccdxprovider . : {job.EdiLaw.AzureBlobUriCcdxProvider}");
                _logger.LogInformation($"{logPrefix} - job.edilaw.queryhours ............... : {job.EdiLaw.QueryHours}");
                _logger.LogInformation($"{logPrefix} - job.edidb.name ...................... : {job.EdiDb.Name}");
                _logger.LogInformation($"{logPrefix} - job.edidb.server .................... : {job.EdiDb.Server}");

                _logger.LogInformation($"{logPrefix} query log analytics workspace for edi usbdg job results (high level status)");
                List<EdiJobStatusResult> l1a = await AzureMonitorService.QueryWorkspaceForEdiUsbdgJobsStatus(job, _logger);
                _logger.LogInformation($"{logPrefix} query log analytics workspace for edi varo job results (high level status)");
				List<EdiJobStatusResult> l1b = await AzureMonitorService.QueryWorkspaceForEdiVaroJobsStatus(job, _logger);
                _logger.LogInformation($"{logPrefix} merge edi usbdg and varo job results (high level status)");
                l1a.AddRange(l1b);

				if (l1a.Count > 0)
                {
                    _logger.LogInformation($"{logPrefix} insert these edi (usbdg & varo) job results (high level status) into azure sql");
                    r1 = await AzureSqlDatabaseService.InsertEdiJobStatusEvents(job, l1a, _logger);
                }
                else
                {
                    _logger.LogInformation($"{logPrefix} no edi (usbdg & varo) job status records found in the log analytics workspace");
                }

                _logger.LogInformation($"{logPrefix} query log analytics workspace for edi job results (azure function app events)");
                List<EdiPipelineEventResult> l2 = await AzureMonitorService.QueryWorkspaceForEdiPipelineEvents(job);
                if (l2.Count > 0)
                {
                    _logger.LogInformation($"{logPrefix} insert these edi job results (azure function app events) into azure sql");
                    r2 = await AzureSqlDatabaseService.InsertEdiPipelineEvents(job, l2);
                }
                else
                {
                    _logger.LogInformation($"{logPrefix} no edi pipeline records found in the log analytics workspace");
                }

                _logger.LogInformation($"{logPrefix} query log analytics workspace for edi job results (adf pipeline)");
                List<EdiAdfActivityResult> l3 = await AzureMonitorService.QueryWorkspaceForEdiAdfActivityData(job);
                if (l3.Count > 0)
                {
                    _logger.LogInformation($"{logPrefix} insert these edi job results (adf pipeline) into azure sql");
                    r3 = await AzureSqlDatabaseService.InsertEdiAdfActivityEvents(job, l3);
                }
                else
                {
                    _logger.LogInformation($"{logPrefix} no edi adf pipeline activity records found in the log analytics workspace");
                }

                _logger.LogInformation($"{logPrefix} query log analytics workspace for edi job results (azure function traces)");
                List<AzureFunctionTraceResult> l4 = await AzureMonitorService.QueryWorkspaceForAzureFunctionTraceData(job);
                if (l4.Count > 0)
                {
                    _logger.LogInformation($"{logPrefix} insert these edi job results (azure function traces) into azure sql");
                    r4 = await AzureSqlDatabaseService.InsertEdiAzureFunctionTraceRecords(job, l4);
                }
                else
                {
                    _logger.LogInformation($"{logPrefix} no edi adf pipeline activity records found in the log analytics workspace");
                }

				List<EdiPipelineEventResult> warnEvents = l2.Where(x => x.PipelineEvent == PipelineEventEnum.Name.WARN.ToString()).ToList();

				EdiMaintJobStats results = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_IMPORTER_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.SUCCESS,
                    Queried = r1.Queried + r2.Queried + r3.Queried + r4.Queried,
                    Loaded = r1.Loaded + r2.Loaded + r3.Loaded + r4.Loaded,
                    Skipped = r1.Skipped + r2.Skipped + r3.Skipped + r4.Skipped,
                    Failed = r1.Failed + r2.Failed + r3.Failed + r4.Failed
                };
                _logger.LogInformation($"{logPrefix} overall import results");
                _logger.LogInformation($"{logPrefix} - function app name ......................: {results.EdiFunctionApp}");
                _logger.LogInformation($"{logPrefix} - maint job event type ...................: {results.EdiJobEventType}");
                _logger.LogInformation($"{logPrefix} - maint job name .........................: {results.EdiJobName}");
                _logger.LogInformation($"{logPrefix} - maint job status .......................: {results.EdiJobStatus}");
                _logger.LogInformation($"{logPrefix} - total events ...........................: {results.Queried}");
                _logger.LogInformation($"{logPrefix} - events loaded into db ..................: {results.Loaded}");
                _logger.LogInformation($"{logPrefix} - events not loaded into db (duplicates) .: {results.Skipped}");
                _logger.LogInformation($"{logPrefix} - events not loaded into db (failed) .....: {results.Failed}");
                _logger.LogInformation($"{logPrefix} - total warning events ...................: {warnEvents.Count}");

				AzureAppInsightsService.LogEvent(results);
                _logger.LogInformation($"{logPrefix} done");
            }
            catch (Exception ex)
            {
                EdiMaintJobStats jobStatsSummaryFailed = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_IMPORTER_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_IMPORTER,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.FAILED,
                    ExceptionMessage = ex.Message,
                    Queried = r1.Queried + r2.Queried + r3.Queried + r4.Queried,
                    Loaded = r1.Loaded + r2.Loaded + r3.Loaded + r4.Loaded,
                    Skipped = r1.Skipped + r2.Skipped + r3.Skipped + r4.Skipped,
                    Failed = r1.Failed + r2.Failed + r3.Failed + r4.Failed
                };
                AzureAppInsightsService.LogEvent(jobStatsSummaryFailed);
            }

        }
    }
}
