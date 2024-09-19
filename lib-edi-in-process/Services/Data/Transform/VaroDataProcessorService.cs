using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace lib_edi_in_process.Services.Data.Transform
{
    public class VaroDataProcessorService
    {
        public static bool IsReportPackageSupported(string incomingCcdxHeaderCeType, string incomingCcdxHeaderDxEmail)
        {
            bool dxEmailMatch = false;
            bool ceTypeMatch = false;

            string supportedCcdxHeaderDxEmail = Environment.GetEnvironmentVariable("SUPPORTED_CCDX_DX_EMAIL");
            string supportedCcdxHeaderCeType = Environment.GetEnvironmentVariable("SUPPORTED_CCDX_CE_TYPE");

            if (supportedCcdxHeaderDxEmail != null && incomingCcdxHeaderDxEmail != null)
            {
                if (supportedCcdxHeaderDxEmail.ToUpper() == incomingCcdxHeaderDxEmail.ToUpper())
                {
                    dxEmailMatch = true;
                }
            }

            if (supportedCcdxHeaderCeType != null && incomingCcdxHeaderCeType != null)
            {
                if (supportedCcdxHeaderCeType.ToUpper() == incomingCcdxHeaderCeType.ToUpper())
                {
                    ceTypeMatch = true;
                }
            }

            if (dxEmailMatch && ceTypeMatch)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public static bool IsThisVaroGeneratedPackageName(string name)
        {
            bool result = false;
            string varoReportFileNamePattern = "(\\d\\d\\d\\d\\d\\d\\d\\dT\\d\\d\\d\\d\\d\\dZ)_([a-z0-9]+)_reports\\.tar\\.gz";
            Regex r = new Regex(varoReportFileNamePattern);
            Match m = r.Match(name);
            if (m.Success)
            {
                result = true;
            }
            return result;
        }
    }
}
