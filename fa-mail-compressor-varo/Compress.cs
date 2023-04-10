using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
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

namespace fa_mail_compressor_varo
{
    public static class Compress
    {
        [FunctionName("compress-report")]
        public static async Task<HttpResponseMessage> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {

            try
            {
                log.LogTrace("http trigger function received a compression request.");
                
                // NHGH-2799 2023-02-09 1420 Identify the attachments to place into the tarball 
                List<int> attachmentsToCompress = new();
                string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
                dynamic data = JsonConvert.DeserializeObject(requestBody);
                dynamic attachments = data?.attachments;

                // NHGH-2815 2023-03-01 1033 Generate the Varo package name from the Varo report file name
                string outputPackageName = VaroDataProcessorService.GeneratePackageNameFromVaroReportFileName(attachments);

                if (outputPackageName != null)
                {
                    log.LogTrace("identify email attachments to be inserted into the Varo file package");
                    if (attachments != null)
                    {
                        int i = 0;
                        foreach (dynamic item in attachments)
                        {
                            string elementName = item?.Name;
                            string ext = Path.GetExtension(elementName);
                            if (ext != null)
                            {
                                if (ext.ToLower() == ".json")
                                {
                                    attachmentsToCompress.Add(i);
                                }
                            }
                            i++;
                        }
                    }

                    log.LogTrace($"  - attachments identified: {attachmentsToCompress.Count}");

                    log.LogTrace("build and compress the Varo file package");
                    using MemoryStream outputStream = new();
                    using (GZipOutputStream gzoStream = new(outputStream))
                    {
                        gzoStream.IsStreamOwner = false;
                        gzoStream.SetLevel(9);
                        using TarOutputStream tarOutputStream = new(gzoStream, null);
                        foreach (var attachment in attachmentsToCompress)
                        {
                            string name = attachments[attachment]?.Name;
                            string contentBytesBase64 = attachments[attachment]?.ContentBytes;

                            if (name != null && contentBytesBase64 != null)
                            {
                                byte[] contentBytes = Convert.FromBase64String(contentBytesBase64);
                                tarOutputStream.IsStreamOwner = false;
                                TarEntry entry = TarEntry.CreateTarEntry(name);
                                entry.Size = contentBytes.Length;
                                tarOutputStream.PutNextEntry(entry);
                                tarOutputStream.Write(contentBytes, 0, contentBytes.Length);
                                tarOutputStream.CloseEntry();
                            }
                        }
                        tarOutputStream.Close();
                    }

                    outputStream.Flush();
                    outputStream.Position = 0;

                    log.LogTrace("returned base64 encoded string of compressed Varo file package");
                    string compressedOutputBase64String = Convert.ToBase64String(outputStream.ToArray());
                    var returnObject = new { name = outputPackageName, content = compressedOutputBase64String };

                    HttpResponseMessage httpResponseMessage;
                    httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.OK,
                        Content = new StringContent(JsonConvert.SerializeObject(returnObject, Formatting.Indented), Encoding.UTF8, "application/json")
                    };

                    log.LogTrace("sent successful http response");
                    return httpResponseMessage;
                } else
                {
                    log.LogError("unable to build a Varo package file name");
                    HttpResponseMessage httpResponseMessage = new HttpResponseMessage()
                    {
                        StatusCode = System.Net.HttpStatusCode.InternalServerError
                    };
                    return httpResponseMessage;
                }
            }
            catch (Exception e)
            {
                log.LogError("Something went wrong while compressing the Varo package file name");
                log.LogError("Exception: " + e.Message);
                HttpResponseMessage httpResponseMessage = new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.InternalServerError
                };
                return httpResponseMessage;
            }

        }
    }
}
