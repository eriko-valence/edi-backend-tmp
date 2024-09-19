using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lib_edi_in_process.Services.Data.Transform
{
    public class UsbdgDataProcessorService
    {
        // 40A36BCA695F_20230313T141954Z_NoLogger_reports.tar.gz
        // 40A36BCA7463_20230323T160650Z_002200265547501820383131_reports.tar
        public static bool IsThisUsbdgGeneratedPackageName(string name)
        {
            bool result = false;
            string usbdgReportFileNamePattern = "([A-Z0-9]+)_(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)_([A-Za-z0-9]+)_reports\\.tar\\.gz";
            Regex r = new Regex(usbdgReportFileNamePattern);
            Match m = r.Match(name);
            if (m.Success)
            {
                result = true;
            }
            return result;
        }
    }
}
