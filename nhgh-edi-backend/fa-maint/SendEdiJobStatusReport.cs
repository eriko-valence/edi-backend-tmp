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
    public static class SendEdiJobStatusReport
    {
        [FunctionName("send-daily-status-report-email")]
        public static async Task Run([TimerTrigger("%EDI_DAILY_STATUS_REPORT_TIMER_SCHEDULE%")] TimerInfo timerInfo, ILogger log)
        {
            try
            {
                string logPrefix = "- [monitor_job_status_time_trigger_function->run]: ";
                log.LogInformation($"{logPrefix} retrieve azure sql database connection information from azure key vault");
                EdiJobInfo job = EdiService.InitializeMaintJobSendReport(EdiFunctionsEnum.Name.EDI_DAILY_STATUS_EMAIL_REPORT);
                log.LogInformation($"{logPrefix} get the most recent (last 24 hours) failed edi jobs from the database");
                List<FailedEdiJob> results = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);
                log.LogInformation($"{logPrefix} send edi daily job status email report via sendgrid");
                await SendGridService.SendEdiJobFailuresEmailReport(results, EdiService.GetDailyStatusEmailReportSendGridSettings(), log);

                EdiImportJobStats jobStatsSummarySucceeded = new()
                {
                    EdiJobEventType = EdiJobImportEventEnum.Name.EDI_SEND_EMAIL_REPORT_RESULT,
                    EdiJobName = EdiJobImportFunctionEnum.Name.EDI_MAINT,
                    EdiJobStatus = EdiJobImportStatusNameEnum.Name.SUCCESS,
                };
                AzureAppInsightsService.LogEvent(jobStatsSummarySucceeded);

            } catch (Exception ex)
            {
                EdiImportJobStats jobStatsSummaryFailed = new()
                {
                    EdiJobEventType = EdiJobImportEventEnum.Name.EDI_SEND_EMAIL_REPORT_RESULT,
                    EdiJobName = EdiJobImportFunctionEnum.Name.EDI_MAINT,
                    EdiJobStatus = EdiJobImportStatusNameEnum.Name.FAILED,
                    ExceptionMessage = ex.Message
                };
                AzureAppInsightsService.LogEvent(jobStatsSummaryFailed);
            }

        }
    }
}
