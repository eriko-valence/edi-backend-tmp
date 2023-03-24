using lib_edi.Models.SendGrid;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Azure
{
    public class AzureTableStorageService
    {


        /// <summary>
        /// Gets a list of dynamic table entity objects older than X hours
        /// </summary>
        /// <param name="connectionString">Azure storage account connection string where the table entities to be deleted are located</param>
        /// <param name="tableName">Azure table storage table name</param>
        /// <param name="hours">Number of hours threshold. Blobs older than this hour value will be deleted </param>
        /// <param name="excludePartitionKey">Boolean flag that controls whether partition key is included in logging </param>
        /// <returns>
        /// Enumerator for the list of dynamic table entity objects
        /// </returns>
        public static IEnumerable<DynamicTableEntity> RetrieveTableEntities(string connectionString, string tableName, int days)
        {
            CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);
            CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
            CloudTable ct = cloudTableClient.GetTableReference(tableName);

            /* ###########################################################################
                # OPTION #1: Get table entities older than X hours
                ########################################################################### 
            */
            TableQuery<DynamicTableEntity> queryReportEmails = BuildTableQueryWithTimestampFiltering(days);
            return ct.ExecuteQuery(queryReportEmails);

            /* ###########################################################################
                # OPTION #2: Get table entities older than X hours
                ########################################################################### 
            */
            //var queryReportEmails = BuildTableQueryWithTimestampFiltering(hours, new string[] { "report_email" });
            //foreach (var entity in ct.ExecuteQuery(queryReportEmails)) { var emails = (entity.Properties["report_email"]);}

            /* ###########################################################################
                # OPTION #3: Get table entities older than X hours
                ########################################################################### 
            */
            //var linqQueryReportEmails = BuildTableLinqQueryWithTimestampFiltering(ct, hours);
            //var resultsLinqQueryReportEmails = linqQueryReportEmails.ToList(); // Execute the query

        }

        /// <summary>
        /// Gets a list of dynamic table entity objects older than X hours 
        /// </summary>
        /// <param name="connectionString">Azure storage account connection string where the table entities to be deleted are located</param>
        /// <param name="tableName">Azure table storage table name</param>
        /// <param name="days">Email addresses from table entities newer than {days} will be returned </param>
        /// <param name="excludePartitionKey">Boolean flag that controls whether partition key is included in logging </param>
        /// <returns>
        /// A list of Pogo LT email report email addresses as strings.
        /// </returns>
        public static List<string> GetListOfPogoLTReportEmails(string connectionString, string tableName, int days, bool verboseLogging, ILogger log)
        {

            List<string> list = new List<string>();
            IEnumerable<DynamicTableEntity> results = AzureTableStorageService.RetrieveTableEntities(connectionString, tableName, days);
            foreach (var entity in results)
            {
                list.Add(entity.Properties["report_email"].StringValue);
                if (verboseLogging)
                {
                    log.LogInformation($"report_email: {entity.Properties["report_email"].StringValue}  ---> timestamp: {entity.Timestamp}");
                }
            }
            return list.Distinct().ToList();
        }


        /// <summary>
        /// Creates a Microsoft Azure table query object using timestamp filtering 
        /// </summary>
        /// <param name="days">Number of days threshold. Table entities older than this days value will be returned </param>
        /// <returns>
        /// Microsoft Azure table query object
        /// </returns>
        public static TableQuery<DynamicTableEntity> BuildTableQueryWithTimestampFiltering(int days)
        {
            TableQuery<DynamicTableEntity> rangeQuery = new TableQuery<DynamicTableEntity>()
            .Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThanOrEqual, DateTimeOffset.Now.AddDays(days).Date));
            return rangeQuery;
        }

        /// <summary>
        /// Creates a Microsoft Azure table query object using timestamp filtering 
        /// </summary>
        /// <param name="hours">Number of hours threshold. Blobs older than this hour value will be deleted </param>
        /// <param name="properties">List of property names to include in the query result</param>
        /// <returns>
        /// Microsoft Azure table query object
        /// </returns>
        public static TableQuery<DynamicTableEntity> BuildTableQueryWithTimestampFiltering(int hours, string[] properties)
        {
            TableQuery<DynamicTableEntity> rangeQuery = new TableQuery<DynamicTableEntity>().Select(properties)
            .Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.LessThan
            , DateTime.Now.AddHours(hours)));
            return rangeQuery;
        }

        /// <summary>
        /// Creates a query object using LINQ for timestamp filtering
        /// </summary>
        /// <param name="ct">Microsoft Azure table</param>
        /// <param name="hours">Number of hours threshold. Blobs older than this hour value will be deleted </param>
        /// <returns>
        /// A LINQ query object
        /// </returns>
        public static IQueryable<DynamicTableEntity> BuildTableLinqQueryWithTimestampFiltering(CloudTable ct, int hours)
        {
            return ct.CreateQuery<DynamicTableEntity>()
                                .Where(d => d.Timestamp <= DateTime.UtcNow.AddHours(hours));
        }

        /// <summary>
        /// Gets a list of Pogo LT job results from Azure Table Storage that are X hours or newer old
        /// </summary>
        /// <param name="connectionString">Azure storage account connection string where the table entities to be deleted are located</param>
        /// <param name="tableJobStatusProgress">Azure table storage table name</param>
        /// <param name="tableJobStatus">Azure table storage table name</param>
        /// <param name="hours">Number of hours threshold. Blobs older than this hour value will be deleted </param>
        /// <param name="excludePartitionKey">Boolean flag that controls whether partition key is included in logging </param>
        /// <param name="log">Microsoft logging object </param>
        /// <returns>
        /// list of Pogo LT job result objects
        /// </returns>
        public static async Task<List<JobMonitorResult>> GetPogoLTJobResults(string connectionString, string tableJobStatusProgress, string tableJobStatus, int hours, bool excludePartitionKey, ILogger log)
        {

            List<JobMonitorResult> listJMR = new List<JobMonitorResult>();

            try
            {
                log.LogInformation("  - [azure_storage_service->get_table_entities]: building dynamic (azure storage table) entity query");
                TableQuery<DynamicTableEntity> rangeQuery = new TableQuery<DynamicTableEntity>()
                .Where(TableQuery.GenerateFilterConditionForDate("Timestamp", QueryComparisons.GreaterThan
                , DateTime.Now.AddHours(hours)));

                log.LogInformation("  - [azure_storage_service->get_table_entities]: initializing cloud storage account");
                CloudStorageAccount storageAccount = CloudStorageAccount.Parse(connectionString);

                log.LogInformation("  - [azure_storage_service->get_table_entities]: initializing cloud table references");
                CloudTableClient cloudTableClient = storageAccount.CreateCloudTableClient();
                CloudTable ctJobStatusProgress = cloudTableClient.GetTableReference(tableJobStatusProgress);
                CloudTable ctJobStatus = cloudTableClient.GetTableReference(tableJobStatus);

                log.LogInformation("  - [azure_storage_service->get_table_entities]: executing job status progress (azure storage) table query");
                var queryResult = ctJobStatusProgress.ExecuteQuery(rangeQuery);

                log.LogInformation("  - [azure_storage_service->get_table_entities]: build sendgrid dynamic template dto");
                foreach (DynamicTableEntity e in queryResult)
                {
                    JobMonitorResult jmr2 = new JobMonitorResult();
                    jmr2.QueryEmail = e.PartitionKey;
                    jmr2.Timestamp = e.Timestamp.ToString();
                    TableOperation to = TableOperation.Retrieve<DynamicTableEntity>(e.PartitionKey, e.RowKey);
                    TableResult trJobStatus = await ctJobStatus.ExecuteAsync(to);
                    var tr1 = (DynamicTableEntity)trJobStatus.Result;
                    jmr2.JobState = RetrieveDynamicProperty(tr1.Properties, "job_state");
                    jmr2.JobStatus = RetrieveDynamicProperty(tr1.Properties, "job_status");
                    jmr2.TotalMessages = RetrieveDynamicProperty(e.Properties, "tot_msgs");
                    log.LogInformation($"  - [azure_storage_service->get_table_entities]: entity retrieved => rowkey: {e.RowKey}, timestamp: {e.Timestamp}, totalmessage: {jmr2.TotalMessages}");
                    listJMR.Add(jmr2);
                }
                log.LogInformation("  - [azure_storage_service->get_table_entities]: build sendgrid dynamic template dto");
            }
            catch (Exception e)
            {
                log.LogError($"- [azure_storage_service->get_table_entities]: exception: {e.Message}");
            }
            log.LogInformation("  - [azure_storage_service->get_table_entities]: finished building sendgrid dynamic template dto");
            return listJMR;
        }

        /// <summary>
        /// Retrieves language information related a specified language code
        /// </summary>
        /// <param name="ct">Represents a Microsoft Azure table</param>
        /// <param name="language">Language tag (e.g., "en")</param>
        /// <returns>
        /// list of Pogo LT job result objects
        /// </returns>
        /*
        public static async Task<JobLanguage> GetLanguageCodes(CloudTable ct, string language)
        {
            JobLanguage jobLanguage = new JobLanguage();
            try
            {
                if (language != null)
                {
                    string tableName = Environment.GetEnvironmentVariable("AZURE_STORAGE_TABLE_NAME_LANGUAGE_CODES");
                    if (tableName != null)
                    {
                        TableOperation to = TableOperation.Retrieve<LanguageCode>(language, language);
                        TableResult tr = await ct.ExecuteAsync(to);

                        if (tr.HttpStatusCode == 200)
                        {
                            LanguageCode languageCodeEntity = (LanguageCode)tr.Result;
                            if (languageCodeEntity != null)
                            {
                                jobLanguage.LanguageID = languageCodeEntity.LanguageID;
                                jobLanguage.LanguageTag = languageCodeEntity.LanguageTag;
                                jobLanguage.SendgridTemplateID = languageCodeEntity.SendgridTemplateID;
                                jobLanguage.ExcelLanguageFile = languageCodeEntity.ExcelLanguageFile;
                                jobLanguage.LanguageCultureName = languageCodeEntity.LanguageCultureName;
                            }
                        }
                    }
                }

            }
            catch (Exception) { }

            return jobLanguage;
        }
        */

        /// <summary>
        /// Retrieves a Azure Table Storage entity property value using the property name
        /// </summary>
        /// <param name="props">A dictionary object of </param>
        /// <param name="name">Name of zure Table Storage entity property name</param>
        /// <returns>
        /// A string value of the Azure Table Storage entity property value
        /// </returns>
        public static string RetrieveDynamicProperty(IDictionary<string, EntityProperty> props, string name)
        {
            string value = "unknown";

            if (props != null)
            {
                if (props.ContainsKey(name))
                {
                    EntityProperty entityTypeProperty;
                    if (props.TryGetValue(name, out entityTypeProperty))
                    {
                        value = entityTypeProperty.ToString();
                    }
                }
            }

            return value;

        }

    }
}
