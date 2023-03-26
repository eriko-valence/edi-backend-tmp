using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.KeyVault
{
    public class SqlDbSecretKeys
    {
        public string SecretNameUserId { get; set; }
        public string SecretNameUserPw { get; set; }
        public string SecretNameDbName { get; set; }
        public string SecretNameDbServer { get; set; }
    }
}
