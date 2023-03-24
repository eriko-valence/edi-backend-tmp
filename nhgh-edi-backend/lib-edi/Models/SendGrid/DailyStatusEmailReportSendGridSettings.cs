using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.SendGrid
{
    public class DailyStatusEmailReportSendGridSettings
    {
        public string ApiKey { get; set; }
        public string TemplateID { get; set; }
        public string FromEmailAddress { get; set; }
        public string EmailReceipients { get; set; }
        public string EmailSubjectLine { get; set; }
    }
}
