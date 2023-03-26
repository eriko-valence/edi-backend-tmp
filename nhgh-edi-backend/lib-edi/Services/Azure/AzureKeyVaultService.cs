using Azure.Identity;
using Azure.Security.KeyVault.Secrets;
using lib_edi.Models.Azure.KeyVault;
using lib_edi.Models.Azure.Sql.Connection;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Enums.Azure.Sql;
using lib_edi.Models.Enums.Edi.Data.Import;
using lib_edi.Models.Enums.Edi.Functions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Azure
{
    public class AzureKeyVaultService
    {
        public static AzureSqlDbConnectInfo GetDatabaseCredentials(string appName, SqlDbSecretKeys secretKeys)
        {
            AzureSqlDbConnectInfo connectInfo = new();
            string vaultUrl = "https://" + Environment.GetEnvironmentVariable("AZURE_KEY_VAULT_NAME") + ".vault.azure.net/";
            try
            {
                var kvc = new SecretClient(vaultUri: new Uri(vaultUrl), credential: new DefaultAzureCredential());
                KeyVaultSecret l = kvc.GetSecret(secretKeys.SecretNameUserId);
                KeyVaultSecret p = kvc.GetSecret(secretKeys.SecretNameUserPw);
                KeyVaultSecret d = kvc.GetSecret(secretKeys.SecretNameDbName);
                KeyVaultSecret s = kvc.GetSecret(secretKeys.SecretNameDbServer);
                connectInfo.UserId = l.Value;
                connectInfo.Name = d.Value;
                connectInfo.Server = s.Value;
                connectInfo.Password = p.Value;
                return connectInfo;
            }
            catch (Exception)
            {
                throw;
            }

        }

    }
}
