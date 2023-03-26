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

namespace fa_maint
{
    public static class SendDailyStatusReportEmail
    {
        [FunctionName("send-daily-status-report-email")]
        public static async Task Run([TimerTrigger("%EDI_DAILY_STATUS_REPORT_TIMER_SCHEDULE%")] TimerInfo timerInfo, ILogger log)
        {
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: retrieving environment variables");
            EdiJobsStatusReportInfo job = InitializeJob(EdiFunctionsEnum.Name.EDI_DAILY_STATUS_EMAIL_REPORT);
            List<FailedEdiJob> results = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: build sendgrid dynamic templdate dto");
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: send edi daily job status email report via sendgrid");
            await SendGridService.SendEdiJobFailuresEmailReport(results, GetDailyStatusEmailReportSendGridSettings(), log);
        }

        /// <summary>
        /// Builds an App Insights API query app errors settings object
        /// </summary>
        /// <returns>
        /// An App Insights API query app errors settings object
        /// </returns>
        /*
        public static AppInsightsApiQueryAppErrorsSettings GetAppInsightsApiQueryAppErrorsSettings()
        {
            AppInsightsApiQueryAppErrorsSettings settings = new AppInsightsApiQueryAppErrorsSettings();
            settings.AppID = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_APP_ID");
            settings.ApiKey = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_KEY");
            settings.ErrorQueryInterval = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_ERROR_QUERY_INTERVAL");
            settings.ErrorQueryUrl = Environment.GetEnvironmentVariable("AZURE_APP_INSIGHTS_API_ERROR_QUERY_URL");

            if (settings.AppID == null) { return null; }
            if (settings.ApiKey == null) { return null; }
            if (settings.ErrorQueryInterval == null) { return null; }
            if (settings.ErrorQueryUrl == null) { return null; }

            return settings;
        }
        */

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

        public static EdiJobsStatusReportInfo InitializeJob(EdiFunctionsEnum.Name funcName)
        {
            EdiJobsStatusReportInfo job = new()
            {
                FunctionName = funcName
            };
            SqlDbSecretKeys sqlDbSecretKeys = new()
            {
                SecretNameUserId = "AzureSqlServerLoginName-Edi",
                SecretNameUserPw = "AzureSqlServerLoginPass-Edi",
                SecretNameDbName = "AzureSqlDatabaseName-Edi",
                SecretNameDbServer = "AzureSqlServerName-Edi"
            };
            job.JobId = Guid.NewGuid();
            job.EdiDb = AzureKeyVaultService.GetDatabaseCredentials(job.ApplicationName, sqlDbSecretKeys);
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(job.ApplicationName, job.EdiDb); ;
            return job;
        }
    }
}
