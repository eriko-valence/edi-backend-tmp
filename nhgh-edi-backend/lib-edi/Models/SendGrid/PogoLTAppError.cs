using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class PogoLTAppError
    {
        public string TimeStamp { get; set; }
        public string ErrorName { get; set; }
        public string ErrorType { get; set; }
        public string QueryEmail { get; set; }
        public string OrchestrationID { get; set; }
    }
}
