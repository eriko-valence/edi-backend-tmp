using System;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;
using lib_edi.Services.Azure;
using lib_edi.Models.SendGrid;
using lib_edi.Services.SendGrid;
using lib_edi.Models.Azure.Sql.Query;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.Azure.KeyVault;
using lib_edi.Services.Edi;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Data.Import;

namespace fa_maint
{
    public static class EdiEmailReport
    {
        [FunctionName("edi-maint-email-report")]
        public static async Task Run([TimerTrigger("%CRON_SCHEDULE_EDI_EMAIL_REPORT%"
            
            /*
            #if DEBUG
            , RunOnStartup=true
            #endif
            */
            
           
            )] TimerInfo timerInfo, ILogger log)
        //public static async Task Run(ILogger log)
        {
            string logPrefix = "- [edi-email-report->run]: ";

            try
            {
                log.LogInformation($"{logPrefix} retrieve azure sql and sendgrid connection information");
                EdiJobInfo job = EdiService.InitializeMaintJobSendReport(EdiFunctionsEnum.Name.EDI_MAINT_EMAIL_REPORT);
                log.LogInformation($"{logPrefix} - job.edisendgrid.templateid ..........: {job.EdiSendGrid.TemplateID}");
                log.LogInformation($"{logPrefix} - job.edisendgrid.emailreceipients ....: {job.EdiSendGrid.EmailReceipients}");
                log.LogInformation($"{logPrefix} - job.edisendgrid.emailsubjectline ....: {job.EdiSendGrid.EmailSubjectLine}");
                log.LogInformation($"{logPrefix} - job.edidb.name ......................: {job.EdiDb.Name}");
                log.LogInformation($"{logPrefix} - job.edidb.server ....................: {job.EdiDb.Server}");
                log.LogInformation($"{logPrefix} - job.edireportparameters.startdate ...: {job.EdiEmailReportParameters.StartDate}");
                log.LogInformation($"{logPrefix} - job.edireportparameters.enddate......: {job.EdiEmailReportParameters.EndDate}");
                log.LogInformation($"{logPrefix} get recent edi job runtime telemetry from azure sql");
                List<FailedEdiJob> results = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);
                log.LogInformation($"{logPrefix} get recent edi job runtime overall telemetry from azure sql");
                OverallEdiRunStat overallStats = await AzureSqlDatabaseService.GetOverallEdiJobRunStats(job);
                log.LogInformation($"{logPrefix} send edi daily job status email report via sendgrid");
                await SendGridService.SendEdiJobFailuresEmailReport(results, overallStats, EdiService.GetDailyStatusEmailReportSendGridSettings(), log);

                EdiMaintJobStats jobStatsSummarySucceeded = new()
                {
                    EdiFunctionApp = EdiFunctionAppsEnum.Name.EDI_MAINT,
                    EdiJobEventType = EdiMaintJobEventEnum.Name.EDI_STATUS_EMAIL_RESULT,
                    EdiJobName = EdiFunctionsEnum.Name.EDI_MAINT_EMAIL_REPORT,
                    EdiJobStatus = EdiMaintJobStatusEnum.Name.SUCCESS,
                };

                log.LogInformation($"{logPrefix} overall job runtime stats");
                log.LogInformation($"{logPrefix} - sucessful jobs count ....: {overallStats.SuccessfulJobs}");
                log.LogInformation($"{logPrefix} - failed job counts");
                log.LogInformation($"{logPrefix}   - failures at provider ..: {overallStats.FailedProvider}");
                log.LogInformation($"{logPrefix}   - failures at consumer ..: {overallStats.FailedConsumer}");
                log.LogInformation($"{logPrefix}   - failures at transform .: {overallStats.FailedTransform}");
                log.LogInformation($"{logPrefix}   - failures at sql .......: {overallStats.FailedSqlLoad}");
                AzureAppInsightsService.LogEvent(jobStatsSummarySucceeded);
                log.LogInformation($"{logPrefix} done");

            } catch (Exception ex)
            {
                log.LogInformation($"{logPrefix} exception thrown");
                log.LogError(ex.Message);
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
