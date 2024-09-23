using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using lib_edi.Services.Azure;
using lib_edi.Services.SendGrid;
using lib_edi.Models.Azure.Sql.Query;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Services.Edi;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Data.Import;
using Microsoft.Azure.Functions.Worker;

namespace fa_maint
{
    public class EdiEmailReport
    {
        private readonly ILogger<EdiEmailReport> _logger;

        public EdiEmailReport(ILogger<EdiEmailReport> logger)
        {
            _logger = logger;
        }

        [Function("edi-maint-email-report")]
        public async Task Run([TimerTrigger("%CRON_SCHEDULE_EDI_EMAIL_REPORT%"
            
            /*
            #if DEBUG
            , RunOnStartup=true
            #endif
            */
            
           
            )] TimerInfo timerInfo)
        //public static async Task Run(ILogger log)
        {
            string logPrefix = "- [edi-email-report->run]: ";

            try
            {
                _logger.LogInformation($"{logPrefix} retrieve azure sql and sendgrid connection information");
                EdiJobInfo job = EdiService.InitializeMaintJobSendReport(EdiFunctionsEnum.Name.EDI_MAINT_EMAIL_REPORT);
                _logger.LogInformation($"{logPrefix} - job.edisendgrid.templateid ..........: {job.EdiSendGrid.TemplateID}");
                _logger.LogInformation($"{logPrefix} - job.edisendgrid.emailreceipients ....: {job.EdiSendGrid.EmailReceipients}");
                _logger.LogInformation($"{logPrefix} - job.edisendgrid.emailsubjectline ....: {job.EdiSendGrid.EmailSubjectLine}");
                _logger.LogInformation($"{logPrefix} - job.edidb.name ......................: {job.EdiDb.Name}");
                _logger.LogInformation($"{logPrefix} - job.edidb.server ....................: {job.EdiDb.Server}");
                _logger.LogInformation($"{logPrefix} - job.query.startdate (utc) ...........: {job.EdiEmailReportParameters.StartDate}");
                _logger.LogInformation($"{logPrefix} - job.query.enddate (utc)..............: {job.EdiEmailReportParameters.EndDate}");
                _logger.LogInformation($"{logPrefix} get recent edi job runtime telemetry from azure sql");
                List<FailedEdiJob> results = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);
                _logger.LogInformation($"{logPrefix} get recent edi job runtime telemetry from azure sql");
				List<EdiPipelineEvent> warnEvents = await AzureSqlDatabaseService.GetEdiJobWarningEventsFromLast24Hours(job);
                _logger.LogInformation($"{logPrefix} get recent edi job runtime overall telemetry from azure sql");
                OverallEdiRunStat overallStats = await AzureSqlDatabaseService.GetOverallEdiJobRunStats(job);
                _logger.LogInformation($"{logPrefix} send edi daily job status email report via sendgrid");
                await SendGridService.SendEdiJobFailuresEmailReport(results, warnEvents, overallStats, EdiService.GetDailyStatusEmailReportSendGridSettings(), _logger);

                EdiMaintJobStats jobStatsSummarySucceeded = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_STATUS_EMAIL_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_EMAIL_REPORT,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.SUCCESS,
                };

                _logger.LogInformation($"{logPrefix} overall job runtime stats");
                _logger.LogInformation($"{logPrefix} - total jobs count ........: {overallStats.TotalJobs}");
                _logger.LogInformation($"{logPrefix}   - sucessful jobs count ....: {overallStats.SuccessfulJobs}");
                _logger.LogInformation($"{logPrefix}   - failed job counts .......: {overallStats.TotalFailedJobs}");
                _logger.LogInformation($"{logPrefix}     - failures at provider ..: {overallStats.FailedProvider}");
                _logger.LogInformation($"{logPrefix}     - failures at consumer ..: {overallStats.FailedConsumer}");
                _logger.LogInformation($"{logPrefix}     - failures at transform .: {overallStats.FailedTransform}");
                _logger.LogInformation($"{logPrefix}     - failures at sql .......: {overallStats.FailedSqlLoad}");
                _logger.LogInformation($"{logPrefix} - warning events count ......: {warnEvents.Count}");
				AzureAppInsightsService.LogEvent(jobStatsSummarySucceeded);
                _logger.LogInformation($"{logPrefix} done");

            } catch (Exception ex)
            {
                _logger.LogInformation($"{logPrefix} exception thrown");
                _logger.LogError(ex.Message);
                EdiMaintJobStats jobStatsSummaryFailed = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_STATUS_EMAIL_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_EMAIL_REPORT,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.FAILED,
                    ExceptionMessage = ex.Message
                };
                AzureAppInsightsService.LogEvent(jobStatsSummaryFailed);
            }

        }
    }
}
