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

namespace lib_edi.Services.SendGrid
{
    public class SendGridService
    {
        /// <summary>
        /// Sends job monitor email report 
        /// </summary>
        /// <returns>
        /// true if the email was successfully sent; otherwise, false.
        /// </returns>
        public async static Task<bool> SendJobMonitorEmailReport(List<JobMonitorResult> listJMR, List<PogoLTAppError> listErrors, DailyStatusEmailReportSendGridSettings settings, ILogger log)
        {
            try
            {
                log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: retrieving environment variables");

                if (settings != null)
                {
                    var apiKey = settings.ApiKey;
                    var templateId = settings.TemplateID;
                    var fromEmailAddress = settings.FromEmailAddress;
                    string emailReceipients = settings.EmailReceipients;
                    string emailSubjectLine = settings.EmailSubjectLine;

                    log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: initializing sendgrid client and message");
                    var client = new SendGridClient(apiKey);
                    var msg = new SendGridMessage();
                    msg.SetFrom(new EmailAddress(fromEmailAddress, "Pogo LT"));
                    msg.SetTemplateId(templateId);

                    log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: building email receipient list");
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

                    log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: add dynamic template data to email");
                    var dynamicTemplateData = new SendGridTemplateJobMonitorData
                    {
                        TotalJobsRun = listJMR.Count,
                        TotalErrors = listErrors.Count,
                        Subject = emailSubjectLine,
                        Results = listJMR,
                        Errors = listErrors
                    };
                    string stringJsonDynamicTemplateData = JsonConvert.SerializeObject(dynamicTemplateData);
                    //log.LogInformation(stringJsonDynamicTemplateData);
                    msg.SetTemplateData(dynamicTemplateData);

                    log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: make a request to send an email through Twilio SendGrid");
                    var response = await client.SendEmailAsync(msg);
                    if (response.StatusCode == HttpStatusCode.Accepted)
                    {
                        log.LogInformation("  - [email_message_service->send_job_monitor_email_report]: request has been accepted for further processing");
                        return true;
                    }
                    else
                    {
                        log.LogError("  - [email_message_service->send_job_monitor_email_report]: request was not accepted for further processing");
                        return false;
                    }
                }
                else
                {
                    log.LogError("  - [email_message_service->send_job_monitor_email_report]: missign required settings");
                    return false;
                }

            }
            catch (Exception e)
            {
                log.LogError($" - [email_message_service->send_job_monitor_email_report]: an exception was thrown: {e.Message}");
                return false;
            }

        }
    }
}
