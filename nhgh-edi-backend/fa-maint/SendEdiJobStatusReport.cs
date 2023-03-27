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

namespace fa_maint
{
    public static class SendEdiJobStatusReport
    {
        [FunctionName("send-daily-status-report-email")]
        public static async Task Run([TimerTrigger("%EDI_DAILY_STATUS_REPORT_TIMER_SCHEDULE%")] TimerInfo timerInfo, ILogger log)
        {
            string logPrefix = "- [monitor_job_status_time_trigger_function->run]: ";
            log.LogInformation($"{logPrefix} retrieve azure sql database connection information from azure key vault");
            EdiJobInfo job = EdiService.InitializeMaintJobSendReport(EdiFunctionsEnum.Name.EDI_DAILY_STATUS_EMAIL_REPORT);
            log.LogInformation($"{logPrefix} get the most recent (last 24 hours) failed edi jobs from the database");
            List<FailedEdiJob> results = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);
            log.LogInformation($"{logPrefix} send edi daily job status email report via sendgrid");
            await SendGridService.SendEdiJobFailuresEmailReport(results, GetDailyStatusEmailReportSendGridSettings(), log);
        }

        public static DailyStatusEmailReportSendGridSettings GetDailyStatusEmailReportSendGridSettings()
        {
            DailyStatusEmailReportSendGridSettings settings = new DailyStatusEmailReportSendGridSettings();
            settings.ApiKey = Environment.GetEnvironmentVariable("SENDGRID_KEY");
            settings.TemplateID = Environment.GetEnvironmentVariable("SENDGRID_JOB_MONITOR_TEMPLATE_ID");
            settings.FromEmailAddress = Environment.GetEnvironmentVariable("SENDGRID_FROM_EMAIL_ADDRESS");
            settings.EmailReceipients = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_RECEIPIENTS");
            settings.EmailSubjectLine = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_SUBJECT_LINE");
            if (settings.ApiKey == null) { return null; }
            if (settings.TemplateID == null) { return null; }
            if (settings.FromEmailAddress == null) { return null; }
            if (settings.EmailReceipients == null) { return null; }
            if (settings.EmailSubjectLine == null) { return null; }
            return settings;
        }


    }
}
