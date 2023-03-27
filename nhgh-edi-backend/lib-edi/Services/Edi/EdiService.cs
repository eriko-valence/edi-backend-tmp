using lib_edi.Models.Azure.KeyVault;
using lib_edi.Models.Azure.LogAnalytics;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.Enums.Edi.Functions;
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
            job.EdiDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(job.ApplicationName, job.EdiDb); ;
            return job;
        }

        public static EdiJobInfo InitializeMaintJobImportEvents(EdiFunctionsEnum.Name funcName)
        {
            EdiJobInfo job = new()
            {
                FunctionName = funcName
            };
            job.EdiLaw = new AzureLogAnalyticsInfo
            {
                AzureBlobUriCcdxProvider = Environment.GetEnvironmentVariable("AZURE_MONITOR_LOG_ANALYTICS_WORKSPACE_ID_EDI"),
                QueryHours = Convert.ToDouble(Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_EDI_APP_EVENTS")),
                WorkspaceId = Environment.GetEnvironmentVariable("AZURE_MONITOR_LOG_ANALYTICS_WORKSPACE_ID_EDI")
            };
            return job;
        }
    }
}
