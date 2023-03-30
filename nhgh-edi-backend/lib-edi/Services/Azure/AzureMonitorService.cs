using Azure;
using Azure.Identity;
using Azure.Monitor.Query;
using Azure.Monitor.Query.Models;
using lib_edi.Models.Azure.Monitor.Query;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Job.EmailReport;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Xml.Linq;
using static System.Net.Mime.MediaTypeNames;

namespace lib_edi.Services.Azure
{
    public class AzureMonitorService
    {
        //private static readonly string _logAnalyticsWorkspaceId = Environment.GetEnvironmentVariable("AZURE_MONITOR_LOG_ANALYTICS_WORKSPACE_ID_OTA");
        //private static readonly string _ediLogAnalyticsWorkspaceId = Environment.GetEnvironmentVariable("AZURE_MONITOR_LOG_ANALYTICS_WORKSPACE_ID_EDI");

        //private static readonly string _otaQueryNumberOfHours = Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_OTA_APP_EVENTS");
        //private static readonly string _otaExceptionsQueryNumberOfHours = Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_OTA_APP_EXCEPTIONS");
        //private static readonly string _ediQueryNumberOfHours = Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_EDI_APP_EVENTS");
        //private static readonly string _dataImportsQueryNumberOfHours = Environment.GetEnvironmentVariable("AZURE_MONITOR_QUERY_HOURS_DATA_IMPORTS");

        //private static readonly string _ediAzureStorageUriCcdxProvider = Environment.GetEnvironmentVariable("AZURE_STORAGE_URI_EDI_CCDX_PROVIDER");

        /// <summary>
        /// Query Azure Monitor Logs service for EDI job status records
        /// </summary>
        /// <param name="job">Object that holds import job related data (e.g., job id)</param>
        /// <param name="logger">Import job logger object</param>
        /// <returns>
        /// A list of EDI job status rows
        /// </returns>
        /// <remarks>
        /// NHGH-2484 (2022.08.10 - 1322) Added method to populate a demo grid controller
        /// </remarks>
        public static async Task<List<EdiJobStatusResult>> QueryWorkspaceForEdiJobsStatus(EdiJobInfo job, ILogger log)
        {
            string logPrefix = "- [azure_monitor_svc->query_law_edi_job_status]: ";
            try
            {
                string query = @"StorageBlobLogs
                | where OperationName == 'PutBlob'
                | where Uri startswith '" + job.EdiLaw.AzureBlobUriCcdxProvider + @"'
                | where FileName_CF != ''
                | project BlobTimeStart = TimeGenerated, fileName = FileName_CF
                | join kind = leftouter(
                    AppEvents
                    | where Name == 'CCDX_PROVIDER'
                    | where Properties['pipelineEvent'] == 'SUCCEEDED'
                    | project fileName = tostring(Properties['fileName']), ProviderSuccessTime = TimeGenerated
                ) on fileName
                | join kind = leftouter(
                    AppEvents
                    | where Name == 'CCDX_CONSUMER'
                    | where Properties['pipelineEvent'] == 'SUCCEEDED'
                    | project fileName = tostring(Properties['fileName']), ConsumerSuccessTime = TimeGenerated
                ) on fileName
                | join kind = leftouter(
                    AppEvents
                    | where Name == 'ADF_TRANSFORM'
                    | where Properties['pipelineEvent'] == 'SUCCEEDED'
                    | project fileName = tostring(Properties['fileName']), TransformSuccessTime = TimeGenerated
                ) on fileName
                | join kind = leftouter(
                    ADFActivityRun
                    | where OperationName == 'Transform - Succeeded'
                    | extend parsedInput = parse_json(Input)
                    | project fileName = tostring(parsedInput.parameters.PL_P_PARAM_FILE_NAME), SQLSuccessTime = TimeGenerated
                ) on fileName
                | extend Duration = SQLSuccessTime - BlobTimeStart
                | project fileName, BlobTimeStart, ProviderSuccessTime, ConsumerSuccessTime, TransformSuccessTime, SQLSuccessTime, Duration";

                TimeSpan queryTimeRange = TimeSpan.FromHours(Convert.ToDouble(job.EdiLaw.QueryHours));
                //logger.LogInfo("Initialize a new instance of Azure.Monitor.Query.LogsQueryClient", job);
                //logger.LogInfo(" Endpoint: https://api.loganalytics.io", job);
                //logger.LogInfo(" Credential: DefaultAzureCredential", job);
                var client = new LogsQueryClient(new DefaultAzureCredential());
                //logger.LogInfo("Query Azure Monitor Logs service (EDI Job Status)", job);
                //logger.LogInfo(" Workspace ID: " + _ediLogAnalyticsWorkspaceId, job);
                //logger.LogInfo(" TimeSpan (TotalHours): " + queryTimeRange.TotalHours, job);
                //logger.LogInfo(" - Query: AppEvents", job);
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    job.EdiLaw.WorkspaceId,
                    query,
                    new QueryTimeRange(queryTimeRange));
                //logger.LogInfo("Received Query Response from Azure Monitor Logs service (EDI Job Status)", job);
                //logger.LogInfo(" Table Rows: " + response.Value.Table.Rows.Count, job);
                //logger.LogInfo("Build List of EDI job status data from Azure Monitor Query Result", job);
                List<EdiJobStatusResult> list = BuildEdiJobStatusList(response.Value.Table);
                return list;

            }
            catch (Exception e)
            {
                log.LogInformation($"{logPrefix} exception: ");
                log.LogError(e.Message);
                //logger.LogError("Exception thrown while querying the workspace from azure monitor (EDI job status)", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureMonitorService", "QueryWorkspace", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }
        }

        /// <summary>
        /// Query Azure Monitor Logs service for EDI job status records
        /// </summary>
        /// <param name="job">Object that holds import job related data (e.g., job id)</param>
        /// <param name="logger">Import job logger object</param>
        /// <returns>
        /// A list of EDI job status rows
        /// </returns>
        /// <remarks>
        /// NHGH-2484 (2022.08.10 - 1322) Added method to populate a demo grid controller
        /// </remarks>
        public static async Task<List<EdiPipelineEventResult>> QueryWorkspaceForEdiPipelineEvents(EdiJobInfo job)
        {
            try
            {
                string query = @"AppEvents 
                | extend
                  fileName = Properties.fileName, 
                  pipelineEvent = Properties.pipelineEvent, 
                  pipelineStage = Properties.pipelineStage, 
                  pipelineFailureReason = Properties.pipelineFailureReason, 
                  pipelineFailureType = Properties.pipelineFailureType, 
                  dataLoggerType = Properties.dataLoggerType,
                  exceptionMessage = Properties.errorMessage
                | where isnotnull(fileName) and isnotnull(pipelineEvent) and isnotnull(pipelineStage)
                | project
                  TimeGenerated, fileName, pipelineEvent, pipelineStage, pipelineFailureReason, 
                  pipelineFailureType, dataLoggerType, exceptionMessage";

                TimeSpan queryTimeRange = TimeSpan.FromHours(Convert.ToDouble(job.EdiLaw.QueryHours));
                //logger.LogInfo("Initialize a new instance of Azure.Monitor.Query.LogsQueryClient", job);
                //logger.LogInfo(" Endpoint: https://api.loganalytics.io", job);
                //logger.LogInfo(" Credential: DefaultAzureCredential", job);
                var client = new LogsQueryClient(new DefaultAzureCredential());
                //logger.LogInfo("Query Azure Monitor Logs service (EDI jobs)", job);
                //logger.LogInfo(" Workspace ID: " + _ediLogAnalyticsWorkspaceId, job);
                //logger.LogInfo(" TimeSpan (TotalHours): " + queryTimeRange.TotalHours, job);
                //logger.LogInfo(" - Query: AppEvents", job);
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    job.EdiLaw.WorkspaceId,
                    query,
                    new QueryTimeRange(queryTimeRange));
                //logger.LogInfo("Received Query Response from Azure Monitor Logs service", job);
                //logger.LogInfo(" Table Rows: " + response.Value.Table.Rows.Count, job);
                //logger.LogInfo("Build List of EDI Job Data from Azure Monitor Query Result", job);
                List<EdiPipelineEventResult> list = BuildEdiPipelineEventList(response.Value.Table);
                return list;

            }
            catch (Exception e)
            {
                //logger.LogError("Exception thrown while querying the workspace from azure monitor (EDI jobs)", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureMonitorService", "QueryWorkspace", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }
        }

        /// <summary>
        /// Query Azure Monitor Logs service for EDF ADF pipeliene activity data
        /// </summary>
        /// <param name="job">Object that holds import job related data (e.g., job id)</param>
        /// <param name="logger">Import job logger object</param>
        /// <returns>
        /// A list of EDI ADF pipeline activity data rows
        /// </returns>
        /// <remarks>
        /// NHGH-2501 (2022.08.19 - 1545) Added method with primary purpose to gain access to ADF console error messages
        /// </remarks>
        public static async Task<List<EdiAdfActivityResult>> QueryWorkspaceForEdiAdfActivityData(EdiJobInfo job)
        {
            try
            {
                string query = @"ADFActivityRun
					| extend d=parse_json(Input)
					| extend PackageName=tostring(d[""parameters""][""PL_P_PARAM_FILE_NAME""])
					| where ActivityType in~ ('ExecutePipeline')
					| project Start, PackageName, Status, ActivityName, ActivityType, PipelineName, ErrorCode, ErrorMessage";

                TimeSpan queryTimeRange = TimeSpan.FromHours(Convert.ToDouble(job.EdiLaw.QueryHours));
                //logger.LogInfo("Initialize a new instance of Azure.Monitor.Query.LogsQueryClient", job);
                //logger.LogInfo(" Endpoint: https://api.loganalytics.io", job);
                //logger.LogInfo(" Credential: DefaultAzureCredential", job);
                var client = new LogsQueryClient(new DefaultAzureCredential());
                //logger.LogInfo("Query Azure Monitor Logs service (EDI ADF activities)", job);
                //logger.LogInfo(" Workspace ID: " + _ediLogAnalyticsWorkspaceId, job);
                //logger.LogInfo(" TimeSpan (TotalHours): " + queryTimeRange.TotalHours, job);
                //logger.LogInfo(" - Query: AppEvents", job);
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    job.EdiLaw.WorkspaceId,
                    query,
                    new QueryTimeRange(queryTimeRange));
                //logger.LogInfo("Received Query Response from Azure Monitor Logs service", job);
                //logger.LogInfo(" Table Rows: " + response.Value.Table.Rows.Count, job);
                //logger.LogInfo("Build List of EDI Job Data from Azure Monitor Query Result", job);
                List<EdiAdfActivityResult> list = BuildEdiAdfActivityList(response.Value.Table);
                return list;

            }
            catch (Exception e)
            {
                //logger.LogError("Exception thrown while querying the workspace from azure monitor (EDI ADF activities)", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureMonitorService", "QueryWorkspace", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }
        }

        /// <summary>
        /// Query Azure Monitor Logs service for EDF Azure Function trace events data
        /// </summary>
        /// <param name="job">Object that holds import job related data (e.g., job id)</param>
        /// <param name="logger">Import job logger object</param>
        /// <returns>
        /// A list of EDF Azure Function trace events data rows
        /// </returns>
        /// <remarks>
        /// NHGH-2511 (2022.09.01 - 1315) Added method. Primary scope is to collect additional information (specifically errors)
		///   that can be displayed on the EDI dashboard file package monitoring page. Some file packages have been failing
		///   with no explanation as to why. This trace data helps fill in the gaps. 
        /// </remarks>
        public static async Task<List<AzureFunctionTraceResult>> QueryWorkspaceForAzureFunctionTraceData(EdiJobInfo job)
        {
            try
            {
                string query = @"AppTraces | project TimeGenerated, Message, OperationName, SeverityLevel";

                TimeSpan queryTimeRange = TimeSpan.FromHours(Convert.ToDouble(job.EdiLaw.QueryHours));
                //logger.LogInfo("Initialize a new instance of Azure.Monitor.Query.LogsQueryClient", job);
                //logger.LogInfo(" Endpoint: https://api.loganalytics.io", job);
                //logger.LogInfo(" Credential: DefaultAzureCredential", job);
                var client = new LogsQueryClient(new DefaultAzureCredential());
                //logger.LogInfo("Query Azure Monitor Logs service (EDI Azure function traces)", job);
                //logger.LogInfo(" Workspace ID: " + _ediLogAnalyticsWorkspaceId, job);
                //logger.LogInfo(" TimeSpan (TotalHours): " + queryTimeRange.TotalHours, job);
                //logger.LogInfo(" - Query: AppEvents", job);
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    job.EdiLaw.WorkspaceId,
                    query,
                    new QueryTimeRange(queryTimeRange));
                //logger.LogInfo("Received Query Response from Azure Monitor Logs service", job);
                //logger.LogInfo(" Table Rows: " + response.Value.Table.Rows.Count, job);
                //logger.LogInfo("Build List of EDI Job Data from Azure Monitor Query Result", job);
                List<AzureFunctionTraceResult> list = BuildEdiAzureFunctionTraceList(response.Value.Table);
                return list;

            }
            catch (Exception e)
            {
                //logger.LogError("Exception thrown while querying the workspace from azure monitor (EDI Azure function traces)", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureMonitorService", "QueryWorkspace", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }
        }

        /// <summary>
        /// Query Azure Monitor Logs service for data importer job result events
        /// </summary>
        /// <param name="job">Object that holds import job related data (e.g., job id)</param>
        /// <param name="logger">Import job logger object</param>
        /// <returns>
        /// A list of data importer job result events
        /// </returns>
        /// <remarks>
        /// NHGH-2506 (2022.08.13 - 0750) Added method
        /// </remarks>
        public static async Task<List<DataImporterAppEvent>> QueryWorkspaceForEdiMaintEvents(EdiJobInfo job)
        {
            try
            {
                string query = @"AppEvents 
                    | where Name in~ (""EDI_MAINT"") 
                    | extend
                        eventsLoaded = Properties.loaded, 
                        eventsQueried = Properties.queried, 
                        eventsFailed = Properties.failed, 
                        eventsExcluded = Properties.excluded, 
                        jobStatus = Properties.job_status, 
                        jobName = Properties.job_name,
                        jobException = Properties.job_exception_message
                    | project TimeGenerated, eventsLoaded, eventsQueried, eventsFailed, 
                      eventsExcluded, jobName, jobStatus, jobException";

                TimeSpan queryTimeRange = TimeSpan.FromHours(Convert.ToDouble(job.EdiLaw.QueryHours));
                //logger.LogInfo("Initialize a new instance of Azure.Monitor.Query.LogsQueryClient", job);
                //logger.LogInfo(" Endpoint: https://api.loganalytics.io", job);
                //logger.LogInfo(" Credential: DefaultAzureCredential", job);
                var client = new LogsQueryClient(new DefaultAzureCredential());
                //logger.LogInfo("Query Azure Monitor Logs service (OTA Importer Telemetry Importer)", job);
                //logger.LogInfo(" Workspace ID: " + _logAnalyticsWorkspaceId, job);
                //logger.LogInfo(" TimeSpan (TotalHours): " + queryTimeRange.TotalHours, job);
                //logger.LogInfo(" - Query: AppEvents", job);
                Response<LogsQueryResult> response = await client.QueryWorkspaceAsync(
                    job.EdiLaw.WorkspaceId,
                    query,
                    new QueryTimeRange(queryTimeRange));
                //logger.LogInfo("Received Query Response from Azure Monitor Logs service (OTA Importer Telemetry Importer)", job);
                //logger.LogInfo(" Table Rows: " + response.Value.Table.Rows.Count, job);
                //logger.LogInfo("Build List of data importer job telemetry entries from Azure Monitor Query Result", job);
                List<DataImporterAppEvent> list = BuildOtaTelemetryEventsList(response.Value.Table);
                return list;

            }
            catch (Exception)
            {
                //logger.LogError("Exception thrown while querying the workspace from azure monitor (EDI job status)", e, job);
                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureMonitorService", "QueryWorkspace", e);
                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                throw;
            }
        }

        /// <summary>
        /// Loads data from an Azure Monitor Query Logs Table into a List
        /// </summary>
        /// <param name="table">Kendo Mvc UI Data Source Request</param>
        /// <returns>
        /// A list of EDI job status rows
        /// </returns>
        /// <remarks>
        /// NHGH-2484 (2022.08.10 - 1322) Added method to populate a demo grid controller
        /// </remarks>
        private static List<EdiJobStatusResult> BuildEdiJobStatusList(LogsTable table)
        {
            List<EdiJobStatusResult> list = new();
            if (table != null)
            {
                foreach (var row in table.Rows)
                {
                    EdiJobStatusResult ediJobStatus = new EdiJobStatusResult();
                    ediJobStatus.fileName = (string?)row[0];
                    ediJobStatus.BlobTimeStart = row[1] != null ? ((DateTimeOffset)row[1]).DateTime : null;
                    ediJobStatus.ProviderSuccessTime = row[2] != null ? ((DateTimeOffset)row[2]).DateTime : null;
                    ediJobStatus.ConsumerSuccessTime = row[3] != null ? ((DateTimeOffset)row[3]).DateTime : null;
                    ediJobStatus.TransformSuccessTime = row[4] != null ? ((DateTimeOffset)row[4]).DateTime : null;
                    ediJobStatus.SQLSuccessTime = row[5] != null ? ((DateTimeOffset)row[5]).DateTime : null;
                    ediJobStatus.Duration = row[6] != null ? ((TimeSpan)row[6]) : null;

                    // NHGH-2508 The LAW query is currently returning duplicate records in its 
                    // result set. Longer term solution is to fix the LAW query. For the time
                    // being, will just filter out the duplicates using the file package name. 
                    int index = list.FindIndex(x => x.fileName == ediJobStatus.fileName);
                    if (index == -1) { list.Add(ediJobStatus); }
                }
            }
            return list;
        }

        /// <summary>
        /// Loads data import job result events from an Azure Monitor Query Logs Table into a list
        /// </summary>
        /// <param name="table">Kendo Mvc UI Data Source Request</param>
        /// <returns>
        /// A list of data import job result events
        /// </returns>
        /// <remarks>
        /// NHGH-2506 (2022.08.13 - 0750) Added method
        /// </remarks>
        private static List<DataImporterAppEvent> BuildOtaTelemetryEventsList(LogsTable table)
        {
            List<DataImporterAppEvent> list = new();
            if (table != null)
            {
                foreach (var row in table.Rows)
                {
                    DataImporterAppEvent importJobResults = new();
                    importJobResults.EventTime = row[0] != null ? ((DateTimeOffset)row[0]).DateTime : null;
                    importJobResults.EventsLoaded = ConvertStringToInteger(row[1]?.ToString());
                    importJobResults.EventsQueried = ConvertStringToInteger(row[2]?.ToString());
                    importJobResults.EventsFailed = ConvertStringToInteger(row[3]?.ToString());
                    importJobResults.EventsExcluded = ConvertStringToInteger(row[4]?.ToString());
                    if (row[5] != null) { importJobResults.JobName = row[5].ToString(); }
                    if (row[6] != null) { importJobResults.JobStatus = row[6].ToString(); }
                    if (row[7] != null) { importJobResults.JobException = row[7].ToString(); }
                    // this conditional check protects against some older import job result events with null values
                    // the newer job result events should not have any null values
                    if (row[5] != null && row[6] != null && row[1] != null && row[2] != null && row[3] != null && row[4] != null)
                    {
                        list.Add(importJobResults);
                    }
                }
            }
            return list;
        }

        /// <summary>
        /// Loads EDI pieplie event data from an Azure Monitor Query Logs Table into a List
        /// </summary>
        /// <param name="table">Kendo Mvc UI Data Source Request</param>
        /// <returns>
        /// A list of EDI job status rows
        /// </returns>
        /// <remarks>
        /// NHGH-2484 (2022.08.10 - 1322) Added method to populate a demo grid controller
        /// </remarks>
        private static List<EdiPipelineEventResult> BuildEdiPipelineEventList(LogsTable table)
        {
            List<EdiPipelineEventResult> list = new();
            if (table != null)
            {
                foreach (var row in table.Rows)
                {
                    EdiPipelineEventResult ediJobStatus = new()
                    {
                        EventTime = row[0] != null ? ((DateTimeOffset)row[0]).DateTime : null,
                        FileName = row[1]?.ToString(),
                        PipelineEvent = row[2]?.ToString(),
                        PipelineStage = row[3]?.ToString(),
                        PipelineFailureReason = row[4]?.ToString(),
                        PipelineFailureType = row[5]?.ToString(),
                        DataLoggerType = row[6]?.ToString(),
                        ExceptionMessage = row[7]?.ToString()
                    };
                    list.Add(ediJobStatus);
                }
            }
            return list;
        }

        /// <summary>
        /// Loads EDI ADF activity data from an Azure Monitor Query Logs Table into a List
        /// </summary>
        /// <param name="table">Kendo Mvc UI Data Source Request</param>
        /// <returns>
        /// A list of EDI ADF activity rows
        /// </returns>
        /// <remarks>
        /// NHGH-2501 (2022.08.19 - 1550) Added method with primary purpose to gain access to ADF console error messages
        /// </remarks>
        private static List<EdiAdfActivityResult> BuildEdiAdfActivityList(LogsTable table)
        {
            List<EdiAdfActivityResult> list = new();
            if (table != null)
            {
                foreach (var row in table.Rows)
                {
                    EdiAdfActivityResult ediJobStatus = new()
                    {
                        EventTime = row[0] != null ? ((DateTimeOffset)row[0]).DateTime : null,
                        PackageName = row[1]?.ToString(),
                        Status = row[2]?.ToString(),
                        ActivityName = row[3]?.ToString(),
                        ActivityType = row[4]?.ToString(),
                        PipelineName = row[5]?.ToString(),
                        ErrorCode = row[6]?.ToString(),
                        ErrorMessage = row[7]?.ToString()
                    };
                    list.Add(ediJobStatus);
                }
            }
            return list;
        }

        /// <summary>
        /// Loads EDI Azure function trace data from an Azure Monitor Query Logs Table into a List
        /// </summary>
        /// <param name="table">Kendo Mvc UI Data Source Request</param>
        /// <returns>
        /// A list of EDI Azure function trace data rows
        /// </returns>
        /// <remarks>
        /// NHGH-2511 (2022.09.01 - 1332) Added method
        /// </remarks>
        private static List<AzureFunctionTraceResult> BuildEdiAzureFunctionTraceList(LogsTable table)
        {
            /*
			 
			TimeGenerated, Message, OperationName, SeverityLevel

			*/

            List<AzureFunctionTraceResult> list = new();
            if (table != null)
            {
                foreach (var row in table.Rows)
                {
                    AzureFunctionTraceResult azureFunctionTraceRow = new()
                    {
                        EventTime = row[0] != null ? ((DateTimeOffset)row[0]).DateTime : null,
                        FilePackageName = GetFilePackageNameFromTrace(row[1]?.ToString()),
                        OperationName = row[2]?.ToString(),
                        SeverityLevel = ConvertStringToByte(row[3]?.ToString()),
                        LogMessage = row[1]?.ToString(),
                        LogMessageMd5 = GetMd5Hash(row[1]?.ToString())
                    };
                    //System.Diagnostics.Debug.WriteLine(azureFunctionTraceRow.EventTime + " --> " + azureFunctionTraceRow.FilePackageName + " --> " + azureFunctionTraceRow.LogMessage);

                    // NHGH-2511 The LAW query is currently returning duplicate records in its 
                    // result set. Longer term solution is to fix the LAW query. For the time
                    // being, will just filter out the duplicates using the file package name. 
                    int index = list.FindIndex(x => x.EventTime == azureFunctionTraceRow.EventTime && x.LogMessageMd5 == azureFunctionTraceRow.LogMessageMd5);
                    if (index == -1)
                    {
                        if (azureFunctionTraceRow.FilePackageName != null)
                        {
                            list.Add(azureFunctionTraceRow);
                        }
                    }


                }
            }
            return list;
        }

        private static string GetMd5Hash(string s)
        {
            var md5Hash = string.Join("", MD5.Create().ComputeHash(Encoding.ASCII.GetBytes(s)).Select(s => s.ToString("x2")));
            return md5Hash;
        }

        // NHGH-2511 Extract EDI file package name from string
        static string GetFilePackageNameFromTrace(string trace)
        {
            string filePackageName = null;

            if (trace != null)
            {
                // 40A36BCA69DD_20220902T005618Z_003300474630501120363837_reports.tar.gz
                // 40A36BCA6B51_20220902T215951Z_NoLogger_reports.tar.gz
                string patternFilePackageName1 = "[A-Za-z0-9]+_[A-Za-z0-9]+_[A-Za-z0-9]+_reports\\.tar\\.gz";
                Regex r1 = new(patternFilePackageName1);
                MatchCollection codes1 = r1.Matches(trace);
                if (codes1.Count == 1)
                {
                    filePackageName = codes1[0].Value;
                }
                else if (codes1.Count == 0)
                {
                    // 40A36BCA692C_20220902T011854Z_reports.tar.gz
                    string patternFilePackageName2 = "[A-Za-z0-9]+_[A-Za-z0-9]+_reports\\.tar\\.gz";
                    Regex r2 = new(patternFilePackageName2);
                    MatchCollection codes2 = r2.Matches(trace);
                    if (codes2.Count == 1)
                    {
                        filePackageName = codes2[0].Value;
                    }
                }
            }
            return filePackageName;
        }

        private static byte? ConvertStringToByte(string s)
        {
            byte? b = null;

            if (s != null)
            {
                b = Byte.Parse(s);
            }


            return b;
        }

        private static int? ConvertStringToInteger(string s)
        {
            int? i = null;
            if (s != null)
            {
                i = Int32.Parse(s);
            }
            return i;
        }
    }
}
