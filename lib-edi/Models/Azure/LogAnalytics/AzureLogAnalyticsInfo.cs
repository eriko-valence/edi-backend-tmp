using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.LogAnalytics
{
    public class AzureLogAnalyticsInfo
    {
        public string WorkspaceId { get; set; }
        public string AzureBlobUriCcdxProvider { get; set;}
        public double QueryHours { get; set; }
    }
}
