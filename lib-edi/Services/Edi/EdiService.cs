using Azure.Storage.Blobs.Models;
using lib_edi.Models.Azure.KeyVault;
using lib_edi.Models.Azure.LogAnalytics;
using lib_edi.Models.Azure.Sql.Connection;
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
            /*
                SqlDbSecretKeys sqlDbSecretKeys = new()
                {
                    SecretNameUserId = "AzureSqlServerLoginName-Edi",
                    SecretNameUserPw = "AzureSqlServerLoginPass-Edi",
                    SecretNameDbName = "AzureSqlDatabaseName-Edi",
                    SecretNameDbServer = "AzureSqlServerName-Edi"
                };
                job.JobId = Guid.NewGuid();
                job.EdiDb = AzureKeyVaultService.GetDatabaseCredentials(funcName.ToString(), sqlDbSecretKeys);
            */
            job.EdiDb = GetDatabaseCredentials();
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(funcName.ToString(), job.EdiDb);
            job.EdiSendGrid = GetDailyStatusEmailReportSendGridSettings();
            job.EdiEmailReportParameters = GetEdiEmailReportParameters();
            return job;
        }

        public static EdiJobInfo InitializeMaintJobImportEvents(EdiFunctionsEnum.Name funcName)
        {
            EdiJobInfo job = new()
            {
                FunctionName = funcName,
                EdiLaw = GetAzureLogAnalyticsInfo()
            };
            /*
            SqlDbSecretKeys sqlDbSecretKeys = new()
            {
                SecretNameUserId = "AzureSqlServerLoginName-Edi",
                SecretNameUserPw = "AzureSqlServerLoginPass-Edi",
                SecretNameDbName = "AzureSqlDatabaseName-Edi",
                SecretNameDbServer = "AzureSqlServerName-Edi"
            };
            */
            job.JobId = Guid.NewGuid();
            //job.EdiDb = AzureKeyVaultService.GetDatabaseCredentials(job.ApplicationName, sqlDbSecretKeys);
            job.EdiDb = GetDatabaseCredentials();
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(funcName.ToString(), job.EdiDb);

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
                azureLogAnalyticsInfo.QueryHours = Convert.ToInt32(queryHours);
            }

            if (azureLogAnalyticsInfo.AzureBlobUriCcdxProvider == null) { return null; }
            if (queryHours == null) { return null; }
            if (azureLogAnalyticsInfo.WorkspaceId == null) { return null; }

            return azureLogAnalyticsInfo;

        }

        public static SendGridConnectInfo GetDailyStatusEmailReportSendGridSettings()
        {
            SendGridConnectInfo settings = new SendGridConnectInfo();
            settings.ApiKey = Environment.GetEnvironmentVariable("EDI_SENDGRID_KEY");
            settings.TemplateID = Environment.GetEnvironmentVariable("EDI_SENDGRID_JOB_MONITOR_TEMPLATE_ID");
            settings.FromEmailAddress = Environment.GetEnvironmentVariable("EDI_SENDGRID_FROM_EMAIL_ADDRESS");
            settings.EmailReceipients = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_RECEIPIENTS");
            settings.EmailSubjectLine = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_SUBJECT_LINE");
            if (settings.ApiKey == null) { return null; }
            if (settings.TemplateID == null) { return null; }
            if (settings.FromEmailAddress == null) { return null; }
            if (settings.EmailReceipients == null) { return null; }
            if (settings.EmailSubjectLine == null) { return null; }
            return settings;
        }

        public static AzureSqlDbConnectInfo GetDatabaseCredentials()
        {
            AzureSqlDbConnectInfo azureSqlDbConnectInfo = new AzureSqlDbConnectInfo();
            azureSqlDbConnectInfo.Name = Environment.GetEnvironmentVariable("EDI_AZURE_SQL_DATABASE_NAME");
            azureSqlDbConnectInfo.Server = Environment.GetEnvironmentVariable("EDI_AZURE_SQL_SERVER_NAME");
            azureSqlDbConnectInfo.UserId = Environment.GetEnvironmentVariable("EDI_AZURE_SQL_USERID");
            azureSqlDbConnectInfo.Password = Environment.GetEnvironmentVariable("EDI_AZURE_SQL_PASSWD");
            if (azureSqlDbConnectInfo.Name == null) { return null; }
            if (azureSqlDbConnectInfo.Server == null) { return null; }
            if (azureSqlDbConnectInfo.UserId == null) { return null; }
            if (azureSqlDbConnectInfo.Password == null) { return null; }
            return azureSqlDbConnectInfo;
        }

        public static EmailReportParameters GetEdiEmailReportParameters()
        {
            EmailReportParameters parameters = new();
            string queryHours = Environment.GetEnvironmentVariable("EDI_DAILY_STATUS_REPORT_QUERY_HOURS");

            if (queryHours != null)
            {
                /*
                 * NHGH-2855 (2023.03.30 1032) - The EDI Email Report function pulls EDI job tuntime telemetry
                 * from the EDI Azure SQL database. It takes a few hours for this EDI job runtime telemetry to
                 * emit to Azure Log Analytics and then Azure SQL. This queryHourOffset variable is used to
                 * account for this delay. 
                 */
                int queryReportHoursOffset = 0;
                int queryReportHours = ((Convert.ToInt32(queryHours) + queryReportHoursOffset) * -1);
                parameters.StartDate = DateTime.Now.AddHours(queryReportHours);
                parameters.EndDate = DateTime.Now.AddHours(queryReportHoursOffset * -1);
            }

            if (parameters.StartDate == null) { return null; }
            if (parameters.EndDate == null) { return null; }

            return parameters;
        }
    }
}
