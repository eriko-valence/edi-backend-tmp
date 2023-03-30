using lib_edi.Models.SendGrid;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SendGrid;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using System.Net.Http;
using System.Net;
using lib_edi.Models.Azure.Sql.Query;
using Azure;

namespace lib_edi.Services.SendGrid
{
    public class SendGridService
    {
        /// <summary>
        /// Sends EDI job failure report email via SendGrid 
        /// </summary>
        /// <returns>
        /// true if the email was successfully sent; otherwise, false.
        /// </returns>
        public async static Task<bool> SendEdiJobFailuresEmailReport(List<FailedEdiJob> jobs, OverallEdiRunStat jobStats, SendGridConnectInfo settings, ILogger log)
        {
            string logPrefix = "  - [sendgrid_service->send_edi_job_failure_report_email]: ";

            try
            {
                log.LogInformation($"{logPrefix} start");

                List<SendGridEdiFailedJob> sendGridResultlist = new();

                foreach (FailedEdiJob job in jobs)
                {
                    SendGridEdiFailedJob sendGridJob = new SendGridEdiFailedJob();
                    sendGridJob.FilePackageName = job.FilePackageName;
                    sendGridJob.PipelineFailureLocation = job.PipelineFailureLocation;
                    sendGridJob.DataLoggerType = job.DataLoggerType;
                    sendGridJob.BlobTimeStart = job.BlobTimeStart;
                    sendGridResultlist.Add(sendGridJob);
                }

                if (settings != null)
                {
                    var apiKey = settings.ApiKey;
                    var templateId = settings.TemplateID;
                    var fromEmailAddress = settings.FromEmailAddress;
                    string emailReceipients = settings.EmailReceipients;
                    string emailSubjectLine = settings.EmailSubjectLine;

                    log.LogInformation($"{logPrefix} initializing sendgrid client and message");
                    var client = new SendGridClient(apiKey);
                    var msg = new SendGridMessage();
                    msg.SetFrom(new EmailAddress(fromEmailAddress, "EDI"));
                    msg.SetTemplateId(templateId);

                    log.LogInformation($"{logPrefix} building email receipient list");
                    string[] arrayEmailReceipients = emailReceipients.Split(',');
                    List<EmailAddress> listEmailAddresses = new List<EmailAddress>();
                    foreach (string email in arrayEmailReceipients)
                    {
                        listEmailAddresses.Add(new EmailAddress(email, null));
                    }
                    List<Personalization> toEmailList = new List<Personalization>();
                    toEmailList.Add(new Personalization
                    {
                        Tos = listEmailAddresses
                    });
                    msg.Personalizations = toEmailList;

                    log.LogInformation($"{logPrefix} add dynamic template data to email");
                    var dynamicTemplateData = new SendGridEdiFailedJobsResults
                    {
                        Subject = emailSubjectLine,
                        Results = sendGridResultlist,
                        JobStats = jobStats
                    };
                    string stringJsonDynamicTemplateData = JsonConvert.SerializeObject(dynamicTemplateData);
                    log.LogInformation($"{logPrefix} template data: ");
                    log.LogInformation($"{logPrefix} ------------------------------------ ");
                    log.LogInformation(stringJsonDynamicTemplateData);
                    log.LogInformation($"{logPrefix} ------------------------------------ ");

                    msg.SetTemplateData(dynamicTemplateData);

                    log.LogInformation($"{logPrefix} submit request to send an email through Twilio SendGrid ");
                    var response = await client.SendEmailAsync(msg);
                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        log.LogInformation($"{logPrefix} submission request accepted");
                        return true;
                    }
                    else
                    {
                        log.LogInformation($"{logPrefix} submission request not accepted with status code {response.StatusCode}");
                        return false;
                    }
                }
                else
                {
                    log.LogInformation($"{logPrefix} missign required settings");
                    return false;
                }

            }
            catch (Exception e)
            {
                log.LogInformation($"{logPrefix} an exception was thrown:");
                log.LogInformation($"{logPrefix} ------------------------------------ ");
                log.LogInformation(e.Message);
                log.LogInformation($"{logPrefix} ------------------------------------ ");
                return false;
            }

        }
    }
}
