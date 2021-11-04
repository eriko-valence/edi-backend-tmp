using lib_edi.Models.Azure.AppInsights;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.Errors;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Kafka;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace fa_ccdx_consumer
{
    /// <summary>
    /// A telemetry processing application consumes data from a cold chain data interchange (CCDX) Kafka topic.
    /// </summary>
    /// <remarks>
    /// "As a short overview of the process, the Data Interchange publishes telemetry data via the Apache Kafka 
    /// messaging protocol. To consume data, applications authenticate against the Data Interchange using API key 
    /// pairs, which restricts access to topics shared with the Telemetry Consumer by the Data Owner. For data 
    /// privacy, all connections to the Data Interchange are encrypted with SSL."
    ///   - source: https://www.cold-chain-data.com/integration/telemetry-consumer
    /// EDI architecture: 
    /// - "An Azure Function known as the CCDX Consumer is subscribed to the CCDX Kafka topic and receives the 
    /// original report file(s) from CCDX and stores them in another container (container_2) within the same Azure 
    /// Data Lake instance."
    /// </remarks>
    public class Consume
    {
        //const string Broker = "pkc-41973.westus2.azure.confluent.cloud:9092";
        //const string Topic = "dx.destination.edidata";

        /// <summary>
        /// Kafka triggered azure function that consumes messages from a cold chain data interchange (CCDX) Kafka topic and loads them into 
        /// an Azure storage blob container monitored by an Azure Data Factory (ADF) Equipment Monitoring System (EMS) ETL pipeline. 
        /// </summary>
        /// <param name="Broker">Kafka broker</param>
        /// <param name="Topic">Kafka topic</param>
        /// <param name="Username">Kafka SASL username</param>
        /// <param name="Password">Kafka SASL password</param>
        /// <param name="Protocol">Kafka broker protocol</param>
        /// <param name="AuthenticationMode">Kafka broker authentication mode</param>
        /// <param name="ConsumerGroup">Kafka consumer group</param>
        /// <param name="events">Kafka message data</param>
        /// <param name="log">Microsoft logging object</param>
        /// <remarks>
        /// - Kafka Extension has two modes. One is Single Mode, and the other is Batch mode. This Azure function uses batch mode 
        /// as the KafkaEventData parameter is array. 
        /// - Kafka Extension has a Kafka Listener that consumes messages from the broker. It reads messages ever SubscriberInternalInSecond.
        /// - Kafka Lister doesn’t execute functions. Instead, send its messages to the channel. The channel is a buffer between Kafka 
        /// Listener and Functions. Channel can keep the number of messages that is maxBatchSize * ExecutorChannelCapacity. 
        /// ExecutorChannelCapacity is one by default. If you increase the value, you can increase the buffer size. Function read the 
        /// messages from 1 or maxBatchSize according to the Mode, execute a function. Once the channel reaches the limit, Kafka Listener 
        /// stops consuming. ChannelFullRetryIntervalInMsis the time (ms) to retry to write channel.
        /// - Marking an offset as consumed is called committing an offset. In Kafka, we record offset commits by writing to an internal 
        /// Kafka topic called the offsets topic. A message is considered consumed only when its offset is committed to the offsets 
        /// topic. 
        /// </remarks>
        [FunctionName("ccdx-consumer")]
        public static async Task Run(
            [KafkaTrigger("%KAFKA_BROKER%",
                          "%KAFKA_TOPIC%",
                          Username = "KAFKA_TRIGGER_SASL_USERNAME",
                          Password = "KAFKA_TRIGGER_SASL_PASSWORD",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "KAFKA_GROUP_ID",
                          SslCaLocation = "cacert.pem"),
                          ] KafkaEventData<string,byte[]>[] events, ILogger log)
        {
            string reportFileName = null;
            //PipelineEvent pipelineEvent;

            try
            {
                log.LogInformation($"- [ccdx-consumer->run]: Received {events.Length} telemetry file(s) from cold chain data interchange (CCDX) Kafka topic");
                /* Validate the 'ce-type' env variables exist - they are needed for determing if the event message gets processed */
                CcdxService.ValidateCcdxConsumerCeTypeEnvVariables(log);
                foreach (KafkaEventData<string, byte[]> eventData in events)
                {
                    /* Pull headers from incoming event message */
                    Dictionary<string, string> headers = new Dictionary<string, string>();
                    foreach (var header in eventData.Headers)
                    {
                        headers.Add(header.Key, GetHeaderValueAsString(header));
                    }
                    log.LogInformation($"- [ccdx-consumer->run]: Is this a supported cold chain telemetry message? {headers["ce_type"]}");


                    
                    /* Only process messages that are known to this consumer */
                    if (CcdxService.ValidateCeTypeHeader(headers["ce_type"]))
					{
                        log.LogInformation($"- [ccdx-consumer->run]: Confirmed. Content is cold chain telemetry. Proceed with processing.");

                        log.LogInformation($"- [ccdx-consumer->run]: Building raw ccdx raw consumer blob path.");
                        string blobName = CcdxService.BuildRawCcdxConsumerBlobPath(GetKeyValueString(headers, "ce_subject"), GetKeyValueString(headers, "ce_type"));

                        string blobContainerName = "";
                        //Dictionary<string, string> customProps = null;
                        log.LogInformation($"- [ccdx-consumer->run]: Does this supported cold chain telemetry message have an attached file?");
                        if (headers.ContainsKey("ce_subject"))
                        {
                            log.LogInformation($"- [ccdx-consumer->run]: Confirmed. Attached cce telemetry file found. Proceed with processing.");
                            reportFileName = Path.GetFileName(GetKeyValueString(headers, "ce_subject"));
                            blobContainerName = Environment.GetEnvironmentVariable("CCDX_AZURE_STORAGE_BLOB_CONTAINER_NAME");
                            string storageAccountConnectionString = Environment.GetEnvironmentVariable("CCDX_AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
                            CcdxService.LogCcdxConsumerStartedEventToAppInsights(reportFileName, log);
                            log.LogInformation($"- [ccdx-consumer->run]: Build the azure storage blob path to be used for uploading the cce telemetry file");
                            blobName = CcdxService.BuildRawCcdxConsumerBlobPath(GetKeyValueString(headers, "ce_subject"), GetKeyValueString(headers, "ce_type"));
                            log.LogInformation($"- [ccdx-consumer->run]: Preparing to upload blob {blobName} to container {blobContainerName}: ");
                            log.LogInformation($"- [ccdx-consumer->run]:   ce_id: {GetKeyValueString(headers, "ce_id")} ");
                            log.LogInformation($"- [ccdx-consumer->run]:   ce_type: {GetKeyValueString(headers, "ce_type")} ");
                            log.LogInformation($"- [ccdx-consumer->run]:   ce_time: {GetKeyValueString(headers, "ce_time")} ");
                            log.LogInformation($"- [ccdx-consumer->run]:   ce_subject: {GetKeyValueString(headers, "ce_subject")} ");
                            await AzureStorageBlobService.UploadBlobToContainerUsingSdk(eventData.Value, storageAccountConnectionString, blobContainerName, blobName);
                            log.LogInformation($"- [ccdx-consumer->run]: Uploading blob {blobName} to container {blobContainerName}");
                            CcdxService.LogCcdxConsumerSuccessEventToAppInsights(reportFileName, log);
                            log.LogInformation($"- [ccdx-consumer->run]: Done");
                        }
                        else
                        {
                            log.LogInformation($"- [ccdx-consumer->run]: Failed to upload blob {blobName} to container {blobContainerName} due to missing the ce-subject heaer");
                            CcdxService.LogCcdxConsumerMissingSubjectHeaderEventToAppInsights(reportFileName, log);
                        }
                    }
                    else
                    {
                        //Filter out these telemetry messages as they are not supported by this consumer
                    }
                }
            }
            catch (Exception e)
            {
                string errorCode = "743B";
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList());
                CcdxService.LogCcdxConsumerErrorEventToAppInsights(reportFileName, log, e, errorCode);
                log.LogError("There was an exception while consuming a message from the Kafka topic");
                log.LogError(e, errorMessage);
            }
        }

        public static void WriteHeadersToLogFile(KafkaEventData<string, byte[]> eventData, ILogger log)
        {

            string ceId = "";
            string ceType = "";
            string ceTime = "";
            string ceSubject = "";


            foreach (var header in eventData.Headers)
            {
                if (header.Key.Contains("ce_id"))
                {
                    ceId = System.Text.Encoding.UTF8.GetString(header.Value);

                }
                else if (header.Key.Contains("ce_type"))
                {
                    ceType = System.Text.Encoding.UTF8.GetString(header.Value);
                }
                else if (header.Key.Contains("ce_time"))
                {
                    ceTime = System.Text.Encoding.UTF8.GetString(header.Value);
                }
                else if (header.Key.Contains("ce_subject"))
                {
                    ceSubject = System.Text.Encoding.UTF8.GetString(header.Value);
                }
            }

            log.LogDebug($"- [ccdx-consumer->run]: ce-id; {ceId}; ce-type; {ceType}; ce-time; {ceTime}; ems-blob-name; {ceSubject}");
        }

        public static String GetHeaderValueAsString(IKafkaEventDataHeader header)
        {
            return System.Text.Encoding.UTF8.GetString(header.Value);
        }

        public static String GetKeyValueString(Dictionary<string, string> dictionary, string key)
        {
            string result = "";
            if (dictionary.ContainsKey(key))
            {
                result = dictionary[key];
            }
            return result;
        }


    }
}
