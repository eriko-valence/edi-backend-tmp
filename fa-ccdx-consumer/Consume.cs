using lib_edi.Models.Azure.AppInsights;
using lib_edi.Services.Azure;
using lib_edi.Services.Ccdx;
using lib_edi.Services.Data.Transform;
using lib_edi.Services.Ems;
using lib_edi.Services.Errors;
using lib_edi.Services.Loggers;
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
        // DEV
        const string Broker = "pkc-41973.westus2.azure.confluent.cloud:9092";
        const string Topic = "dx.destination.example";

		// PROD
		//const string Broker = "pkc-41973.westus2.azure.confluent.cloud:9092";
		//const string Topic = "dx.destination.edidata";

		const string logPrefix = "- [ccdx-consumer-usbdg]:";

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
            [KafkaTrigger(Broker,
                          Topic,
                          Username = "KAFKA_TRIGGER_SASL_USERNAME",
                          Password = "KAFKA_TRIGGER_SASL_PASSWORD",
                          Protocol = BrokerProtocol.SaslSsl,
                          AuthenticationMode = BrokerAuthenticationMode.Plain,
                          ConsumerGroup = "KAFKA_GROUP_ID",
                          SslCaLocation = "cacert.pem"),
                          ] KafkaEventData<string,byte[]>[] events, ILogger log)
        {
            // NHGH-2898 20230413 1518 variables need to be global to support uploading a failed report package to the error/holding blob container
            string reportFileName = null;
            byte[] eventValue = null;
			string blobErrorContainerName = "";
			string storageAccountConnectionString = null;

			try
            {
                log.LogInformation($"{logPrefix} Received {events.Length} telemetry file(s) from cold chain data interchange (CCDX) Kafka topic");
				storageAccountConnectionString = Environment.GetEnvironmentVariable("CCDX_AZURE_STORAGE_ACCOUNT_CONNECTION_STRING");
				blobErrorContainerName = Environment.GetEnvironmentVariable("AZURE_STORAGE_BLOB_CONTAINER_NAME_ERROR");

				foreach (KafkaEventData<string, byte[]> eventData in events)
                {
					// NHGH-2898 20230413 1518 capture the event data if report package fails and needs to be uploaded to the error blob container
					eventValue = eventData.Value;

					/* Pull headers from incoming event message */
					Dictionary<string, string> headers = new Dictionary<string, string>();
                    foreach (var header in eventData.Headers)
                    {
                        headers.Add(header.Key, GetHeaderValueAsString(header));
                    }

					// NHGH-2898 20230413 1518 report name should be catpured early so a failed report package can be uploaded to the error blob container
					string ceSubject = GetKeyValueString(headers, "ce_subject");
					string ceType = GetKeyValueString(headers, "ce_type");
					reportFileName = Path.GetFileName(ceSubject);
					log.LogInformation($"{logPrefix} Start processing event attachment {reportFileName} ");

					string blobName = CcdxService.BuildRawCcdxConsumerBlobPath(ceSubject, ceType);
                    string deviceType = CcdxService.GetLoggerTypeFromCeHeader(ceType);
                    string emdType = CcdxService.GetLoggerTypeFromCeHeader(headers["ce_type"]);
                    string ceId = GetKeyValueString(headers, "ce_id");
                    string ceTime = GetKeyValueString(headers, "ce_time");

					//CcdxService.ValidateCcdxConsumerCeTypeEnvVariables(log);

					/* Only process messages that are known to this consumer */
					if (EmsService.ValidateCceDeviceType(deviceType))
                    {
                        log.LogInformation($"{logPrefix} Is '{headers["ce_type"]}' a supported cold chain file package? Yes. ");
                        log.LogInformation($"{logPrefix} Confirmed. Content is cold chain telemetry. Proceed with processing.");
                        log.LogInformation($"{logPrefix} Does this supported cold chain telemetry message have an attached file?");
                        if (headers.ContainsKey("ce_subject"))
                        {
                            log.LogInformation($"{logPrefix} Building raw ccdx raw consumer blob path.");
                            if (UsbdgDataProcessorService.IsThisUsbdgGeneratedPackageName(ceId))
                            {
                                log.LogInformation($"{logPrefix} Validate incoming blob file extension");
                                log.LogInformation($"{logPrefix} Confirmed. Attached cce telemetry file found. Proceed with processing.");
								string blobContainerName = Environment.GetEnvironmentVariable("CCDX_AZURE_STORAGE_BLOB_CONTAINER_NAME");
								CcdxService.LogCcdxConsumerStartedEventToAppInsights(reportFileName, log);
                                log.LogInformation($"{logPrefix} Build the azure storage blob path to be used for uploading the cce telemetry file");
                                blobName = CcdxService.BuildRawCcdxConsumerBlobPath(GetKeyValueString(headers, "ce_subject"), GetKeyValueString(headers, "ce_type"));
                                log.LogInformation($"{logPrefix} Preparing to upload blob {blobName} to container {blobContainerName}: ");
                                await AzureStorageBlobService.UploadBlobToContainerUsingSdk(eventData.Value, storageAccountConnectionString, blobContainerName, blobName);
                                log.LogInformation($"{logPrefix} Uploading blob {blobName} to container {blobContainerName}");
                                CcdxService.LogCcdxConsumerSuccessEventToAppInsights(reportFileName, log);

                                log.LogInformation($"{logPrefix} Debug");
                                log.LogInformation($"  ##########################################################################");
                                log.LogInformation($"  # - Package: {reportFileName}");
                                log.LogInformation($"  # - EmdType: {emdType}");
                                log.LogInformation($"  # - CEId: {ceId}");
                                log.LogInformation($"  # - CEType: {ceType}");
                                log.LogInformation($"  # - CESubject: {reportFileName}");
                                log.LogInformation($"  # - CETime: {ceTime}");
                                log.LogInformation($"  ##########################################################################");
                                log.LogInformation($"{logPrefix} Done");
                            } else
                            {
                                log.LogError($"{logPrefix} Report package {ceId} is not from a USBDG EMD");
                            }
                        }
                        else
                        {
                            log.LogError($"{logPrefix} Email report package event is missing the ce-subject header");
                        }
                    }
                    else
                    {
                        log.LogInformation($"{logPrefix} Is '{headers["ce_type"]}' a supported cold chain file package? No. ");
                    }
                }
            }
            catch (Exception e)
            {
                string errorCode = "743B";
                string errorMessage = EdiErrorsService.BuildExceptionMessageString(e, errorCode, EdiErrorsService.BuildErrorVariableArrayList());
				
				if (reportFileName != null && reportFileName != "")
				{
					if (VaroDataProcessorService.IsThisVaroGeneratedPackageName(reportFileName))
					{
						if (blobErrorContainerName != null && eventValue.Length > 0)
						{
							log.LogInformation($"{logPrefix} Upload report package {reportFileName} to error container {blobErrorContainerName}: ");
							await AzureStorageBlobService.UploadBlobToContainerUsingSdk(eventValue, storageAccountConnectionString, blobErrorContainerName, reportFileName);
						}
						CcdxService.LogCcdxConsumerErrorEventToAppInsights(reportFileName, log, e, errorCode);
						log.LogError($"{logPrefix} There was an exception while consuming report package {reportFileName} from the Kafka topic");
						log.LogError(e, errorMessage);
					}
				}
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

            log.LogDebug($"{logPrefix} ce-id; {ceId}; ce-type; {ceType}; ce-time; {ceTime}; ems-blob-name; {ceSubject}");
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
