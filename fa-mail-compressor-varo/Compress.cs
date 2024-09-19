using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using ICSharpCode.SharpZipLib.Tar;
using ICSharpCode.SharpZipLib.GZip;
using lib_edi.Services.Ems;
using lib_edi.Services.Data.Transform;
using lib_edi.Models.Enums.Azure.AppInsights;
using lib_edi.Services.Ccdx;
using System.Net.Mail;
using lib_edi.Helpers;
using Microsoft.Azure.Functions.Worker;

namespace fa_mail_compressor_varo
{
    public static class Compress
    {
		const string logPrefix1 = "- [varo-mail-compressor]:";
        const string logPrefix2 = "  - [varo-mail-compressor]:";
        const string logPrefix3 = "    - [varo-mail-compressor]:";

        [Function("compress-report")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            string outputPackageName = null;

			try
			{
                log.LogInformation($"{logPrefix1} http trigger function received a compression request.");

                // NHGH-3474 20240912 1126 Use a dictionary to store the varo report data to compress
                Dictionary<string, byte[]> varoReportDataToCompress = new();

                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                dynamic attachments = data?.attachments;

                // NHGH-3474 20240912 1126 Retrieve the list of supported Varo file extensions
                List<string> varoUncompressedExtensions = VaroDataProcessorService.GetUncompressedVaroReportFileExtensions();
                List<string> varoCompressedExtensions = VaroDataProcessorService.GetCompressedVaroDataFileExtensions();

                // NHGH-2815 2023-03-01 1033 Generate the Varo package name from the Varo report file name
                outputPackageName = VaroDataProcessorService.GeneratePackageNameFromVaroReportFileName(attachments);

				log.LogInformation($"{logPrefix1} generate output tarball name: {outputPackageName}");

				CcdxService.LogMailCompressorStartedEventToAppInsights(outputPackageName, PipelineStageEnum.Name.MAIL_COMPRESSOR_VARO, log);

				if (outputPackageName != null)
                {
                    log.LogInformation($"{logPrefix1} locate varo report data to add to tarball");
                    if (attachments != null)
                    {
                        int i = 0;
                        foreach (dynamic item in attachments)
                        {
                            string elementName = item?.Name;
                            string ext = Path.GetExtension(elementName);
                            if (ext != null)
                            {
                                ext = ext.ToLower();
                                // NHGH-3474 20240912 1126 Add uncompressed Varo report data to tarball
                                if (varoUncompressedExtensions.Exists(item => item == ext))
                                {
                                    if (item.Name != null && item.ContentBytes != null)
                                    {
                                        log.LogInformation($"{logPrefix2} uncompressed varo file found: {item.Name}");
                                        varoReportDataToCompress.Add(elementName, (byte[])item.ContentBytes);
                                    }
                                }

                                // NHGH-3474 20240912 1126 Add compressed Varo report data to tarball
                                if (varoCompressedExtensions.Exists(item => item == ext))
                                {
                                    if (item.ContentBytes != null)
                                    {
                                        log.LogInformation($"{logPrefix2} compressed varo file found: {item.Name}");
                                        Dictionary<string, byte[]> extractedVaroDataBytes = CompressionHelper.ExtractZipArchive((byte[])item.ContentBytes);
                                        foreach (KeyValuePair<string, byte[]> extractedItem in extractedVaroDataBytes)
                                        {
                                            log.LogInformation($"{logPrefix3} archive entry: {item.Name}");
                                            varoReportDataToCompress.Add(extractedItem.Key, extractedItem.Value);
                                        }
                                    }
                                }
                            }
                            i++;
                        }
                    }

                    log.LogInformation($"{logPrefix1} add varo report data to tarball");
                    using MemoryStream outputStream = CompressionHelper.BuildTarball(varoReportDataToCompress);
                    
                    log.LogInformation($"{logPrefix1} base64 encode tarball stream");
                    string compressedOutputBase64String = Convert.ToBase64String(outputStream.ToArray());
                    var returnObject = new { name = outputPackageName, content = compressedOutputBase64String };

                    log.LogInformation($"{logPrefix1} send base64 encoded tarball in http response message");
                    HttpResponseMessage httpResponseMessage;
                    httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(JsonConvert.SerializeObject(returnObject, Formatting.Indented), Encoding.UTF8, "application/json")
                    };

                    log.LogInformation($"{logPrefix1} done");

					CcdxService.LogMailCompressorSuccessEventToAppInsights(outputPackageName, PipelineStageEnum.Name.MAIL_COMPRESSOR_VARO, log);

					return httpResponseMessage;
                } else
                {
					log.LogError($"{outputPackageName} unable to generate report package name from email attachments");
					log.LogError($"{logPrefix1} unable to build a Varo package file name");

					log.LogError($"{logPrefix1} Incoming telemetry file {outputPackageName} is not from a supported data logger");
					log.LogInformation($"{logPrefix1} Track ccdx provider unsupported logger event (app insights)");
					CcdxService.LogMailCompressorUnknownReportPackageToAppInsights(outputPackageName, PipelineStageEnum.Name.MAIL_COMPRESSOR_VARO, log);

					HttpResponseMessage httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.InternalServerError
                    };
                    return httpResponseMessage;
                }
            }
            catch (Exception e)
            {
				string errorCode = "THLB";
				log.LogError($"{logPrefix1} something went wrong while compressing the Varo package file name");
                log.LogError($"{logPrefix1} exception  : " + e.Message);
				log.LogError($"{logPrefix1} error code : " + errorCode);

				log.LogError($"{outputPackageName} An exception was thrown compressing and packaging these email attachments: {e.Message} ({errorCode})");

				CcdxService.LogMailCompressorErrorEventToAppInsights(outputPackageName, PipelineStageEnum.Name.MAIL_COMPRESSOR_VARO, log, e, errorCode);

				HttpResponseMessage httpResponseMessage = new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
                return httpResponseMessage;
            }

        }
    }
}
