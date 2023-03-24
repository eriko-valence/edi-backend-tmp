using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using lib_edi.Services.Azure;
using lib_edi.Models.SendGrid;
using lib_edi.Services.SendGrid;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Data.Import;
using lib_edi.Models.Enums.Azure.Sql;
using lib_edi.Models.Azure.Sql.Query;

namespace fa_maint
{
    public static class SendDailyStatusReportEmail
    {
        [FunctionName("send-daily-status-report-email")]
        public static async Task Run([TimerTrigger("%EDI_DAILY_STATUS_REPORT_TIMER_SCHEDULE%")] TimerInfo timerInfo, ILogger log)
        {
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: retrieving environment variables");

            OtaImportJob job = InitializeOtaImportJob(OtaJobImportFunctionEnum.Name.EDI_EVENTS);

            List<FailedEdiJob> result = await AzureSqlDatabaseService.GetFailedEdiJobsFromLast24Hours(job);

            string jobStatusProgressTableName = Environment.GetEnvironmentVariable("AZURE_STORAGE_TABLE_NAME_JOB_STATUS_PROGRESS");
            string jobStatusTableName = Environment.GetEnvironmentVariable("AZURE_STORAGE_TABLE_NAME_JOB_STATUS");
            string storageConnectionStringAppData = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION_STRING_APPDATA");
            string tableStorageQueryHours = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_QUERY_HOURS");
            int queyrHours = Int32.Parse(tableStorageQueryHours);
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: build sendgrid dynamic templdate dto");
            List<JobMonitorResult> results = await AzureTableStorageService.GetPogoLTJobResults(storageConnectionStringAppData, jobStatusProgressTableName, jobStatusTableName, queyrHours, false, log);
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: pull last 24 hours of app errors from app insights");
            List<PogoLTAppError> errors = AzureAppInsightsService.GetDailyAppInsightsAppErrors(GetAppInsightsApiQueryAppErrorsSettings(), log);
            log.LogInformation("- [monitor_job_status_time_trigger_function->run]: send pogo lt web job status email report via sendgrid");
            await SendGridService.SendJobMonitorEmailReport(results, errors, GetDailyStatusEmailReportSendGridSettings(), log);
        }

        /// <summary>
        /// Builds an App Insights API query app errors settings object
        /// </summary>
        /// <returns>
        /// An App Insights API query app errors settings object
        /// </returns>
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

        public static OtaImportJob InitializeOtaImportJob(OtaJobImportFunctionEnum.Name funcName)
        {
            OtaImportJob job = new()
            {
                FunctionName = funcName
            };

            job.JobId = Guid.NewGuid();
            job.EdiDb = AzureKeyVaultService.GetDatabaseCredentials(job.ApplicationName, funcName, OtaDbCredEnum.Name.EDI_LAW_IMPORTER, job);

            return job;
        }
    }
}
