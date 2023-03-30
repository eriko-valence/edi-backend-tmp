using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.Sql.Connection
{
    public class AzureSqlDbConnectInfo
    {
        public string Name { get; set; }
        public string Server { get; set; }
        public string UserId { get; set; }
        public string Password { get; set; }
        public string ConnectionString { get; set; }
    }
}
