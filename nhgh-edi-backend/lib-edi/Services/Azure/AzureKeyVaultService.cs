using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Azure.Sql;
using lib_edi.Models.Enums.Edi.Data.Import;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Azure
{
    public class AzureKeyVaultService
    {

        public static OtaImportJobDb GetDatabaseCredentials(string appName, OtaJobImportFunctionEnum.Name funcName, OtaDbCredEnum.Name dbCreds, OtaImportJob job)
        {
            OtaImportJobDb jobDb = new();

            string SecretNameUserId = null;
            string SecretNameUserPw = null;
            string SecretNameDbName = null;
            string SecretNameDbServer = null;

            if (funcName.ToString() == OtaJobImportFunctionEnum.Name.EDI_EVENTS.ToString())
            {
                if (dbCreds.ToString() == OtaDbCredEnum.Name.EDI_LAW_IMPORTER.ToString())
                {
                    SecretNameUserId = "AzureSqlServerLoginName-Edi";
                    SecretNameUserPw = "AzureSqlServerLoginPass-Edi";
                    SecretNameDbName = "AzureSqlDatabaseName-Edi";
                    SecretNameDbServer = "AzureSqlServerName-Edi";
                }
            }

            string vaultUrl = "https://" + Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_NAME") + ".vault.azure.net/";

            //logger.LogInfo("Retrieve Connection String", job);
            //logger.LogInfo(" Key Vault Url: " + vaultUrl, job);

            try
            {
                //logger.LogInfo("Initialize New Instance of Microsoft.Azure.Services.AppAuthentication.AzureServiceTokenProvider", job);
                //var azureServiceTokenProvider = new AzureServiceTokenProvider();
                //logger.LogInfo("Initialize New Instance of Azure.Security.KeyVault.Secrets.SecretClient", job);
                var kvc = new SecretClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());
                //logger.LogInfo("Get Secrets", job);
                KeyVaultSecret l = kvc.GetSecret(SecretNameUserId);
                KeyVaultSecret p = kvc.GetSecret(SecretNameUserPw);
                KeyVaultSecret d = kvc.GetSecret(SecretNameDbName);
                KeyVaultSecret s = kvc.GetSecret(SecretNameDbServer);

                jobDb.UserId = l.Value;
                jobDb.Name = d.Value;
                jobDb.Server = s.Value;
                jobDb.Password = p.Value;

                jobDb.ConnectionString = AzureSqlDatabaseService.BuildConnectionString(appName, jobDb);

                //logger.LogInfo(" User ID  : " + l.Value, job);
                //logger.LogInfo(" Database : " + d.Value, job);
                //logger.LogInfo(" Server   : " + s.Value, job);
                return jobDb;
            }
            catch (Exception e)
            {
                //logger.LogError($"Exception thrown while getting db creds from azure key vault", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureKeyVaultService", "GetSecret", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }

        }

    }
}
