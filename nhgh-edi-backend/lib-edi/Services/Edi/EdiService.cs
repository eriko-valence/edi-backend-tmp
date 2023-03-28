using lib_edi.Models.Azure.KeyVault;
using lib_edi.Models.Azure.LogAnalytics;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.Enums.Edi.Functions;
using lib_edi.Models.SendGrid;
using lib_edi.Services.Azure;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Edi
{
    public class EdiService
    {
        public static EdiJobInfo InitializeMaintJobSendReport(EdiFunctionsEnum.Name funcName)
        {
            EdiJobInfo job = new()
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
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(job.ApplicationName, job.EdiDb);
            job.EdiSendGrid = GetDailyStatusEmailReportSendGridSettings();
            return job;
        }

        public static EdiJobInfo InitializeMaintJobImportEvents(EdiFunctionsEnum.Name funcName)
        {
            EdiJobInfo job = new()
            {
                FunctionName = funcName,
                EdiLaw = GetAzureLogAnalyticsInfo()
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
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(job.ApplicationName, job.EdiDb);

            return job;
        }

        public static AzureLogAnalyticsInfo GetAzureLogAnalyticsInfo()
        {
            string queryHours = Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_EDI_APP_EVENTS");

            AzureLogAnalyticsInfo azureLogAnalyticsInfo = new()
            {
                AzureBlobUriCcdxProvider = Environment.GetEnvironmentVariable("AZURE_STORAGE_URI_EDI_CCDX_PROVIDER"),
                WorkspaceId = Environment.GetEnvironmentVariable("AZURE_MONITOR_LOG_ANALYTICS_WORKSPACE_ID_EDI")
            };

            if (queryHours != null)
            {
                azureLogAnalyticsInfo.QueryHours = Convert.ToDouble(queryHours);
            }

            if (azureLogAnalyticsInfo.AzureBlobUriCcdxProvider == null) { return null; }
            if (queryHours == null) { return null; }
            if (azureLogAnalyticsInfo.WorkspaceId == null) { return null; }

            return azureLogAnalyticsInfo;

        }

        public static SendGridConnectInfo GetDailyStatusEmailReportSendGridSettings()
        {
            SendGridConnectInfo settings = new SendGridConnectInfo();
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
