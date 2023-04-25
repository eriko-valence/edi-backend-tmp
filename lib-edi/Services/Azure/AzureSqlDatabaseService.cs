using lib_edi.Models.Azure.Monitor.Query;
using lib_edi.Models.Azure.Sql.Connection;
using lib_edi.Models.Azure.Sql.Query;
using lib_edi.Models.Edi.Data.Import;
using lib_edi.Models.Edi.Job;
using lib_edi.Models.Edi.Job.EmailReport;
using lib_edi.Models.SendGrid;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Services.Azure
{
    public class AzureSqlDatabaseService
    {
        //private static readonly AzureKeyVaultService _azureKeyVaultHelper = new AzureKeyVaultService();
        //private static string _databaseConnectionString = null;
        public AzureSqlDatabaseService()
        {

        }

        public static string BuildConnectionString(string appName, AzureSqlDbConnectInfo db)
        {
            string conStr = $"Server={db.Server};Database={db.Name};User ID ={db.UserId}@{db.Server};Password={db.Password};Trusted_Connection=False;Encrypt=True;";

            SqlConnectionStringBuilder builder = new(conStr)
            {
                ApplicationName = appName
            };

            return builder.ConnectionString;
        }

        /// <summary>
        /// Loads a list of EDI job status data records into an Azure SQL database. Each record contains 
        /// high level information of a job.
        /// </summary>
        /// <param name="job">Importer tool job metadata object</param>
        /// <param name="list">list of EDI job status data records</param>
        /// <param name="logger">Importer tool customized logger object</param>
        /// <returns>
        /// A dictionary of import job results
        /// </returns>
        /// <remarks>
        /// NHGH-2490 (2022.08.15 - 1825) Added method
        /// </remarks>
        public static async Task<EdiMaintJobStats> InsertEdiJobStatusEvents(EdiJobInfo job, List<EdiJobStatusResult> list)
        {
            EdiMaintJobStats jobStats = new();

            //logger.LogInfo("insert edi job status events into database", job);
            //logger.LogInfo(" - records to load: " + list.Count, job);

            jobStats.Queried = list.Count;
            int totalAdded = 0;
            int totalErrors = 0;
            int totalExcluded = 0;

            if (list != null)
            {
                using (SqlConnection conn = new(job.EdiDb.ConnectionString))
                {
                    //logger.LogInfo("open database connection", job);
                    conn.Open();
                    //logger.LogInfo("create sql command", job);
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "telemetry.uspCreateEdiJobStatusEvent";
                        cmd.Parameters.Add(new SqlParameter("@FilePackageName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ESER", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@JobStartTime", SqlDbType.DateTimeOffset));
						cmd.Parameters.Add(new SqlParameter("@ProviderSuccessTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@ConsumerSuccessTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@TransformSuccessTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@SQLSuccessTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@DurationSecs", SqlDbType.Int));
						cmd.Parameters.Add(new SqlParameter("@EMDType", SqlDbType.VarChar));
						cmd.Parameters.Add("@Result", SqlDbType.Int).Direction = ParameterDirection.Output;
                        //logger.LogInfo("execute sql command", job);
                        foreach (var item in list)
                        {
                            EdiJobStatusResult jobResult = (EdiJobStatusResult)item;
                            try
                            {
                                cmd.Parameters["@FilePackageName"].Value = dbnullable(jobResult.fileName);
                                if (jobResult.EmdType == Models.Enums.Emd.EmdEnum.Name.USBDG)
                                {
									cmd.Parameters["@ESER"].Value = dbnullable(GetEserFromFileName(jobResult.fileName));
								} else
                                {
                                    cmd.Parameters["@ESER"].Value = null; // nhgh-2908 2023.04.24 varo devices have no sn
								}
                                
                                cmd.Parameters["@JobStartTime"].Value = dbnullable(jobResult.JobStartTime);
                                cmd.Parameters["@ProviderSuccessTime"].Value = dbnullable(jobResult.ProviderSuccessTime);
                                cmd.Parameters["@ConsumerSuccessTime"].Value = dbnullable(jobResult.ConsumerSuccessTime);
                                cmd.Parameters["@TransformSuccessTime"].Value = dbnullable(jobResult.TransformSuccessTime);
                                cmd.Parameters["@SQLSuccessTime"].Value = dbnullable(jobResult.SQLSuccessTime);
                                cmd.Parameters["@DurationSecs"].Value = dbnullable(GetTotalSecondsFromTimeSpan(jobResult.Duration));
								cmd.Parameters["@EMDType"].Value = dbnullable(jobResult.EmdType);
								var rows = await cmd.ExecuteNonQueryAsync();
                                String resultString = cmd.Parameters["@Result"].Value.ToString();
                                var resultObject = cmd.Parameters["@Result"].Value;
                                int result = Convert.ToInt32(cmd.Parameters["@Result"].Value);
                                if (result == 1 || result == 2)
                                {
                                    totalAdded++;
                                }
                                else if (result > 2)
                                {
                                    totalErrors++;
                                }
                            }
                            catch (Exception ex)
                            {
                                totalErrors++;
                                //logger.LogError("Exception thrown while processing EDI job status events", ex, job);
                                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                            }
                        }
                    }
                    //logger.LogInfo("close database connection", job);
                    conn.Close();
                } // end connection
                //logger.LogInfo("results (edi job status) ", job);
                //logger.LogInfo("jobs ", job);
                //logger.LogInfo(" added                    : " + totalAdded, job);
                //logger.LogInfo(" skipped (duplicates)     : " + totalExcluded, job);
                //logger.LogInfo(" errors                   : " + totalErrors, job);
                jobStats.Loaded = totalAdded;
                jobStats.Skipped = totalExcluded;
                jobStats.Failed = totalErrors;

            }
            return jobStats;
        }

        /// <summary>
        /// Loads a list of EDI job pipeline event data records into an Azure SQL database. Each record contains 
        /// information about an event in the pipeline. For example, when a specific pipeline stage started and 
        /// stopped, the result of that stage, and errors (if a failure occured within the stage).
        /// </summary>
        /// <param name="job">Importer tool job metadata object</param>
        /// <param name="list">list of EDI pipeline event data records</param>
        /// <param name="logger">Importer tool customized logger object</param>
        /// <returns>
        /// A dictionary of import job results
        /// </returns>
        /// <remarks>
        /// NHGH-2490 (2022.08.15 - 1825) Added method
        /// </remarks>
        public static async Task<EdiMaintJobStats> InsertEdiPipelineEvents(EdiJobInfo job, List<EdiPipelineEventResult> list)
        {
            EdiMaintJobStats jobStats = new();
            //logger.LogInfo("insert edi job pipeline events into database", job);
            //logger.LogInfo(" - records to load: " + list.Count, job);
            jobStats.Queried = list.Count;

            int totalAdded = 0;
            int totalErrors = 0;
            int totalExcluded = 0;

            if (list != null)
            {
                using (SqlConnection conn = new(job.EdiDb.ConnectionString))
                {
                    //logger.LogInfo("open database connection", job);
                    conn.Open();
                    //logger.LogInfo("create sql command", job);
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "telemetry.uspCreateEdiPipelineEvent";
                        cmd.Parameters.Add(new SqlParameter("@EventTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@FilePackageName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ESER", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@PipelineEvent", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@PipelineStage", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@PipelineFailureReason", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@PipelineFailureType", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@DataLoggerType", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ExceptionMessage", SqlDbType.VarChar));
                        cmd.Parameters.Add("@Result", SqlDbType.Int).Direction = ParameterDirection.Output;

                        //logger.LogInfo("execute sql command", job);
                        foreach (var item in list)
                        {
                            EdiPipelineEventResult jobResult = (EdiPipelineEventResult)item;
                            try
                            {
                                cmd.Parameters["@EventTime"].Value = dbnullable(jobResult.EventTime);
                                cmd.Parameters["@FilePackageName"].Value = dbnullable(jobResult.FileName);
                                cmd.Parameters["@ESER"].Value = dbnullable(GetEserFromFileName(jobResult.FileName));
                                cmd.Parameters["@PipelineEvent"].Value = dbnullable(jobResult.PipelineEvent);
                                cmd.Parameters["@PipelineStage"].Value = dbnullable(jobResult.PipelineStage);
                                cmd.Parameters["@PipelineFailureReason"].Value = dbnullable(jobResult.PipelineFailureReason);
                                cmd.Parameters["@PipelineFailureType"].Value = dbnullable(jobResult.PipelineFailureType);
                                cmd.Parameters["@DataLoggerType"].Value = dbnullable(jobResult.DataLoggerType);
                                cmd.Parameters["@ExceptionMessage"].Value = dbnullable(jobResult.ExceptionMessage);
                                var rows = await cmd.ExecuteNonQueryAsync();
                                String resultString = cmd.Parameters["@Result"].Value.ToString();
                                var resultObject = cmd.Parameters["@Result"].Value;
                                int result = Convert.ToInt32(cmd.Parameters["@Result"].Value);
                                if (result == 1)
                                {
                                    totalAdded++;
                                }
                                else if (result == 2)
                                {
                                    totalExcluded++;
                                }
                                else if (result > 2)
                                {
                                    totalErrors++;
                                }
                            }
                            catch (Exception ex)
                            {
                                totalErrors++;
                                //logger.LogError("Exception thrown while processing EDI jobs pipeline data", ex, job);
                                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                            }
                        }
                    }

                    //logger.LogInfo("close database connection", job);
                    conn.Close();
                } // end connection
                //logger.LogInfo("results (edi job pipeline) ", job);
                //logger.LogInfo("jobs ", job);
                //logger.LogInfo(" added                    : " + totalAdded, job);
                //logger.LogInfo(" skipped (duplicates)     : " + totalExcluded, job);
                //logger.LogInfo(" errors                   : " + totalErrors, job);
                jobStats.Loaded = totalAdded;
                jobStats.Skipped = totalExcluded;
                jobStats.Failed = totalErrors;
            }
            return jobStats;
        }

        /// <summary>
        /// Loads a list of EDI job pipeline event data records into an Azure SQL database. Each record contains 
        /// information about an event in the pipeline. For example, when a specific pipeline stage started and 
        /// stopped, the result of that stage, and errors (if a failure occured within the stage).
        /// </summary>
        /// <param name="job">Importer tool job metadata object</param>
        /// <param name="list">list of EDI pipeline event data records</param>
        /// <param name="logger">Importer tool customized logger object</param>
        /// <returns>
        /// A dictionary of import job results
        /// </returns>
        /// <remarks>
        /// NHGH-2490 (2022.08.15 - 1825) Added method
        /// </remarks>
        public static async Task<EdiMaintJobStats> InsertEdiAdfActivityEvents(EdiJobInfo job, List<EdiAdfActivityResult> list)
        {
            EdiMaintJobStats jobStats = new();
            //logger.LogInfo("load edi adf pipeline activity events into database ", job);
            //logger.LogInfo(" - records to load: " + list.Count, job);
            jobStats.Queried = list.Count;

            int totalAdded = 0;
            int totalErrors = 0;
            int totalExcluded = 0;

            if (list != null)
            {
                using (SqlConnection conn = new(job.EdiDb.ConnectionString))
                {
                    //logger.LogInfo("open database connection", job);
                    conn.Open();
                    //logger.LogInfo("create sql command", job);
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "[telemetry].[uspCreateEdiAdfPipelineEvent]";
                        cmd.Parameters.Add(new SqlParameter("@EventTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@FilePackageName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@Status", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ActivityName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ActivityType", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@PipelineName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ErrorCode", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@ErrorMessage", SqlDbType.VarChar));
                        cmd.Parameters.Add("@Result", SqlDbType.Int).Direction = ParameterDirection.Output;

                        //logger.LogInfo("execute sql command", job);
                        foreach (var item in list)
                        {
                            EdiAdfActivityResult jobResult = (EdiAdfActivityResult)item;
                            try
                            {
                                cmd.Parameters["@EventTime"].Value = dbnullable(jobResult.EventTime);
                                cmd.Parameters["@FilePackageName"].Value = dbnullable(jobResult.PackageName);
                                cmd.Parameters["@Status"].Value = dbnullable(GetEserFromFileName(jobResult.Status));
                                cmd.Parameters["@ActivityName"].Value = dbnullable(jobResult.ActivityName);
                                cmd.Parameters["@ActivityType"].Value = dbnullable(jobResult.ActivityType);
                                cmd.Parameters["@PipelineName"].Value = dbnullable(jobResult.PipelineName);
                                cmd.Parameters["@ErrorCode"].Value = dbnullable(jobResult.ErrorCode);
                                cmd.Parameters["@ErrorMessage"].Value = dbnullable(jobResult.ErrorMessage);
                                var rows = await cmd.ExecuteNonQueryAsync();
                                String resultString = cmd.Parameters["@Result"].Value.ToString();
                                var resultObject = cmd.Parameters["@Result"].Value;
                                int result = Convert.ToInt32(cmd.Parameters["@Result"].Value);
                                if (result == 1)
                                {
                                    totalAdded++;
                                }
                                else if (result == 2)
                                {
                                    totalExcluded++;
                                }
                                else if (result > 2)
                                {
                                    totalErrors++;
                                }
                            }
                            catch (Exception ex)
                            {
                                totalErrors++;
                                //logger.LogError("Exception thrown while processing EDI ADF pipeline activity data", ex, job);
                                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                            }
                        }
                    }

                    //logger.LogInfo("close database connection", job);
                    conn.Close();
                } // end connection
                //logger.LogInfo("results (edi adf activity)", job);
                //logger.LogInfo("jobs ", job);
                //logger.LogInfo(" added                    : " + totalAdded, job);
                //logger.LogInfo(" skipped (duplicates)     : " + totalExcluded, job);
                //logger.LogInfo(" errors                   : " + totalErrors, job);
                jobStats.Loaded = totalAdded;
                jobStats.Skipped = totalExcluded;
                jobStats.Failed = totalErrors;
            }

            return jobStats;
        }

        /// <summary>
        /// Loads a list of EDI Azure function trace data records into an Azure SQL database. Each record contains 
        /// lower level debug info about an pipeline package. For example, the error message the CCDX provider
		/// logged if there was an issue loading the file package into CCDX. 
        /// </summary>
        /// <param name="job">Importer tool job metadata object</param>
        /// <param name="list">list of EDI Azure function trace data records</param>
        /// <param name="logger">Importer tool customized logger object</param>
        /// <returns>
        /// A dictionary of import job results
        /// </returns>
        /// <remarks>
        /// NHGH-2511 (2022.09.01 - 1340) Added method
        /// </remarks>
        public static async Task<EdiMaintJobStats> InsertEdiAzureFunctionTraceRecords(EdiJobInfo job, List<AzureFunctionTraceResult> list)
        {
            //Dictionary<string, string> jobStats = new();
            EdiMaintJobStats jobStats = new();
            //logger.LogInfo("insert azure function trace events into sql db", job);
            //logger.LogInfo(" - records to load: " + list.Count, job);
            //jobStats.Add("total_edi_events_queried", list.Count.ToString());
            jobStats.Queried = list.Count;

            int totalAdded = 0;
            int totalErrors = 0;
            int totalExcluded = 0;

            if (list != null)
            {
                try
                {
                    using (SqlConnection conn = new(job.EdiDb.ConnectionString))
                    {
                        //logger.LogInfo("open database connection", job);
                        conn.Open();

                        //logger.LogInfo("create sql command", job);
                        using (SqlCommand cmd = conn.CreateCommand())
                        {
                            cmd.CommandType = CommandType.StoredProcedure;
                            cmd.CommandText = "[telemetry].[uspCreateEdiFunctionTrace]";
                            cmd.Parameters.Add(new SqlParameter("@EventTime", SqlDbType.DateTimeOffset));
                            cmd.Parameters.Add(new SqlParameter("@FilePackageName", SqlDbType.VarChar));
                            cmd.Parameters.Add(new SqlParameter("@OperationName", SqlDbType.VarChar));
                            cmd.Parameters.Add(new SqlParameter("@SeverityLevel", SqlDbType.TinyInt));
                            cmd.Parameters.Add(new SqlParameter("@LogMessage", SqlDbType.VarChar));
                            cmd.Parameters.Add(new SqlParameter("@LogMessageMd5", SqlDbType.VarChar));
                            cmd.Parameters.Add("@Result", SqlDbType.Int).Direction = ParameterDirection.Output;

                            //logger.LogInfo("execute sql command", job);
                            foreach (var item in list)
                            {
                                AzureFunctionTraceResult jobResult = (AzureFunctionTraceResult)item;
                                try
                                {
                                    if (jobResult.FilePackageName != null)
                                    {
                                        cmd.Parameters["@EventTime"].Value = dbnullable(jobResult.EventTime);
                                        cmd.Parameters["@FilePackageName"].Value = dbnullable(jobResult.FilePackageName);
                                        cmd.Parameters["@OperationName"].Value = dbnullable(jobResult.OperationName);
                                        cmd.Parameters["@SeverityLevel"].Value = dbnullable(jobResult.SeverityLevel);
                                        cmd.Parameters["@LogMessage"].Value = dbnullable(jobResult.LogMessage);
                                        cmd.Parameters["@LogMessageMd5"].Value = jobResult.LogMessageMd5;
                                        var rows = await cmd.ExecuteNonQueryAsync();
                                        String resultString = cmd.Parameters["@Result"].Value.ToString();
                                        var resultObject = cmd.Parameters["@Result"].Value;
                                        int result = Convert.ToInt32(cmd.Parameters["@Result"].Value);
                                        if (result == 1)
                                        {
                                            totalAdded++;
                                        }
                                        else if (result == 2)
                                        {
                                            totalExcluded++;
                                        }
                                        else if (result > 2)
                                        {
                                            totalErrors++;
                                        }
                                    }
                                }
                                catch (Exception ex)
                                {
                                    totalErrors++;
                                    //logger.LogError("exception thrown while processing sql command", ex, job);
                                    //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                                    //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                                }
                            }
                        }

                        //logger.LogInfo("close database connection", job);
                        conn.Close();
                    } // end connection
                      //logger.LogInfo("results (edi adf azure function)", job);
                      //logger.LogInfo("jobs ", job);
                      //logger.LogInfo(" added                    : " + totalAdded, job);
                      //logger.LogInfo(" skipped (duplicates)     : " + totalExcluded, job);
                      //logger.LogInfo(" errors                   : " + totalErrors, job);

                    jobStats.Loaded = totalAdded;
                    jobStats.Skipped = totalExcluded;
                    jobStats.Failed = totalErrors;
                } catch (Exception)
                {
                    throw;
                }
            }
            return jobStats;
        }

        /// <summary>
        /// Loads a list of data importer job results into an Azure SQL database. Each result contains
		/// summary information of an import job that ran. Imported jobs load data from EDI, OTA,
		/// MetaFridge, and this data importer. 
        /// </summary>
        /// <param name="job">Importer tool job metadata object</param>
        /// <param name="list">list of data importer job results</param>
        /// <param name="logger">Importer tool customized logger object</param>
        /// <returns>
        /// A dictionary of import job results
        /// </returns>
        /// <remarks>
        /// NHGH-2506 (2022.08.13 - 0750) Added method
        /// </remarks>
        public static async Task<EdiMaintJobStats> InsertEdiDataImporterJobResults(EdiJobInfo job, List<DataImporterAppEvent> list)
        {
            //Dictionary<string, string> jobStats = new();
            EdiMaintJobStats jobStats = new();
            //logger.LogInfo("insert data importer job results into sql db", job);
            //logger.LogInfo(" - records to load: " + list.Count, job);
            jobStats.Queried = list.Count;

            int totalAdded = 0;
            int totalErrors = 0;
            int totalExcluded = 0;

            if (list != null)
            {
                using (SqlConnection conn = new(job.EdiDb.ConnectionString))
                {
                    //logger.LogInfo("open database connection", job);
                    conn.Open();

                    //logger.LogInfo("create sql command", job);
                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "[telemetry].[uspCreateEdiMaintEvent]";
                        cmd.Parameters.Add(new SqlParameter("@EventTime", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@EventsLoaded", SqlDbType.Int));
                        cmd.Parameters.Add(new SqlParameter("@EventsQueried", SqlDbType.Int));
                        cmd.Parameters.Add(new SqlParameter("@EventsFailed", SqlDbType.Int));
                        cmd.Parameters.Add(new SqlParameter("@EventsExcluded", SqlDbType.Int));
                        cmd.Parameters.Add(new SqlParameter("@JobName", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@JobStatus", SqlDbType.VarChar));
                        cmd.Parameters.Add(new SqlParameter("@JobException", SqlDbType.VarChar));
                        cmd.Parameters.Add("@Result", SqlDbType.Int).Direction = ParameterDirection.Output;

                        //logger.LogInfo("execute sql command", job);
                        foreach (var item in list)
                        {
                            DataImporterAppEvent jobResult = (DataImporterAppEvent)item;
                            try
                            {
                                cmd.Parameters["@EventTime"].Value = dbnullable(jobResult.EventTime);
                                cmd.Parameters["@EventsLoaded"].Value = dbnullable(jobResult.EventsLoaded);
                                cmd.Parameters["@EventsQueried"].Value = dbnullable(jobResult.EventsQueried);
                                cmd.Parameters["@EventsFailed"].Value = dbnullable(jobResult.EventsFailed);
                                cmd.Parameters["@EventsExcluded"].Value = dbnullable(jobResult.EventsExcluded);
                                cmd.Parameters["@JobName"].Value = dbnullable(jobResult.JobName);
                                cmd.Parameters["@JobStatus"].Value = dbnullable(jobResult.JobStatus);
                                cmd.Parameters["@JobException"].Value = dbnullable(jobResult.JobException);
                                var rows = await cmd.ExecuteNonQueryAsync();
                                String resultString = cmd.Parameters["@Result"].Value.ToString();
                                var resultObject = cmd.Parameters["@Result"].Value;
                                int result = Convert.ToInt32(cmd.Parameters["@Result"].Value);
                                if (result == 1)
                                {
                                    totalAdded++;
                                }
                                else if (result == 2)
                                {
                                    totalExcluded++;
                                }
                                else if (result > 2)
                                {
                                    totalErrors++;
                                }
                            }
                            catch (Exception ex)
                            {
                                totalErrors++;
                                //logger.LogError("exception thrown while processing sql command", ex, job);
                                //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                                //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                            }
                        }
                    }

                    //logger.LogInfo("close database connection", job);
                    conn.Close();
                } // end connection
                //logger.LogInfo("results (ota importer events)", job);
                //logger.LogInfo("jobs ", job);
                //logger.LogInfo(" added                    : " + totalAdded, job);
                //logger.LogInfo(" skipped (duplicates)     : " + totalExcluded, job);
                //logger.LogInfo(" errors                   : " + totalErrors, job);

                jobStats.Loaded = totalAdded;
                jobStats.Skipped = totalExcluded;
                jobStats.Failed = totalErrors;

            }

            return jobStats;
        }

        public static async Task<List<FailedEdiJob>> GetFailedEdiJobsFromLast24Hours(EdiJobInfo job)
        {
            List<FailedEdiJob> failedEdiJobs = new List<FailedEdiJob>();
            using (SqlConnection conn = new(job.EdiDb.ConnectionString))
            {
                    conn.Open();

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        cmd.CommandType = CommandType.StoredProcedure;
                        cmd.CommandText = "[telemetry].[getFailedEdiFilePackages]";
                        cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTimeOffset));
                        cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTimeOffset));

                    try
                        {
                        
                        cmd.Parameters["@StartDate"].Value = job.EdiEmailReportParameters.StartDate;
                        cmd.Parameters["@EndDate"].Value = job.EdiEmailReportParameters.EndDate;

                        using var reader = await cmd.ExecuteReaderAsync();
                        DataTable dt = new();
                        dt.Load(reader);

                        for (int i = 0; i < dt.Rows.Count; i++)
                        {
                            FailedEdiJob failedJob = new FailedEdiJob();
                            failedJob.FilePackageName = dt.Rows[i]["FilePackageName"].ToString();
                            failedJob.ESER = dt.Rows[i]["ESER"].ToString();
                            failedJob.BlobTimeStart = dt.Rows[i]["BlobTimeStart"] == DBNull.Value ? null : (DateTime?)dt.Rows[i]["BlobTimeStart"];
                            failedJob.ProviderSuccessTime = dt.Rows[i]["ProviderSuccessTime"] == DBNull.Value ? null : (DateTime?)dt.Rows[i]["ProviderSuccessTime"];
                            failedJob.ConsumerSuccessTime = dt.Rows[i]["ConsumerSuccessTime"] == DBNull.Value ? null : (DateTime?)dt.Rows[i]["ConsumerSuccessTime"];
                            failedJob.TransformSuccessTime = dt.Rows[i]["TransformSuccessTime"] == DBNull.Value ? null : (DateTime?)dt.Rows[i]["TransformSuccessTime"];
                            failedJob.SQLSuccessTime = dt.Rows[i]["SQLSuccessTime"] == DBNull.Value ? null : (DateTime?)dt.Rows[i]["SQLSuccessTime"];
                            failedJob.DataLoggerType = dt.Rows[i]["DataLoggerType"].ToString();
                            failedJob.PipelineFailureLocation = GetEdiPipelineFailureLocation(failedJob);
                            failedEdiJobs.Add(failedJob);
                        }
                    }
                        catch (Exception)
                        {
                            throw;
                        }
                    }

                    conn.Close();
                } // end connection
            return failedEdiJobs;
        }

        public static async Task<OverallEdiRunStat> GetOverallEdiJobRunStats(EdiJobInfo job)
        {
            OverallEdiRunStat overallEdiRunStat = new OverallEdiRunStat();
            using (SqlConnection conn = new(job.EdiDb.ConnectionString))
            {
                conn.Open();

                using (SqlCommand cmd = conn.CreateCommand())
                {
                    cmd.CommandType = CommandType.StoredProcedure;
                    cmd.CommandText = "[telemetry].[getEdiFilePackagesOverallStats]";
                    cmd.Parameters.Add(new SqlParameter("@StartDate", SqlDbType.DateTimeOffset));
                    cmd.Parameters.Add(new SqlParameter("@EndDate", SqlDbType.DateTimeOffset));

                    try
                    {
                        cmd.Parameters["@StartDate"].Value = job.EdiEmailReportParameters.StartDate;
                        cmd.Parameters["@EndDate"].Value = job.EdiEmailReportParameters.EndDate;

                        using var reader = await cmd.ExecuteReaderAsync();
                        DataTable dt = new();
                        dt.Load(reader);

                        if (dt.Rows.Count > 0)
                        {
                            int failedConsumer = Int32.Parse(dt.Rows[0]["FailedConsumer"].ToString());
                            int failedProvider = Int32.Parse(dt.Rows[0]["FailedProvider"].ToString());
                            int failedTransform = Int32.Parse(dt.Rows[0]["FailedTransform"].ToString());
                            int failedSqlLoad = Int32.Parse(dt.Rows[0]["FailedSqlLoad"].ToString());
                            int succesfulJobs = Int32.Parse(dt.Rows[0]["SuccessfulJobs"].ToString());

                            int totalFailedJobs = (failedConsumer + failedProvider + failedTransform + failedSqlLoad);
                            int totalJobs = (succesfulJobs + totalFailedJobs);

                            overallEdiRunStat.FailedConsumer = failedConsumer;
                            overallEdiRunStat.FailedProvider = failedProvider;
                            overallEdiRunStat.FailedTransform = failedTransform;
                            overallEdiRunStat.FailedSqlLoad = failedSqlLoad;
                            overallEdiRunStat.SuccessfulJobs = succesfulJobs;
                            overallEdiRunStat.TotalJobs = totalJobs;
                            overallEdiRunStat.TotalFailedJobs = totalFailedJobs;
                        }
                    }
                    catch (Exception)
                    {
                        throw;
                    }
                }

                conn.Close();
            } // end connection
            return overallEdiRunStat;
        }

        /*
        public static async Task<OtaCfd50ImportJobStatus> GetExportedMetafridgeRecords(OtaImportJob job)
        {
            //logger.LogInfo("Checking Azure SQL database connection string", job);
            if (job != null)
            {
                //logger.LogInfo("Retrieve connection string from Azure Key Vault", job);
                //try
                //	{
                //logger.LogInfo("Initialize new instance of System.Data.SqlClient.SqlConnection", job);
                using (SqlConnection conn = new(job.MfoxDb.ConnectionString))
                {
                    //logger.LogInfo("Open database connection", job);
                    conn.Open();

                    using (SqlCommand cmd = conn.CreateCommand())
                    {
                        //cmd.CommandType = System.Data.CommandType.StoredProcedure;
                        cmd.CommandType = CommandType.Text;
                        cmd.CommandText = "select * from [ota].[FridgeDataExport]";
                        //logger.LogInfo("Retrieving exported records from the database", job);
                        try
                        {
                            using (var reader = await cmd.ExecuteReaderAsync())
                            {
                                DataTable dt = new DataTable();
                                dt.Load(reader);
                                //jobResults.table = dt;
                                //jobResults.status.Add("fridges_exported_from_mfox", dt.Rows.Count.ToString());
                                //jobResults.status.Queried = dt.Rows.Count;
                            }
                        }
                        catch (Exception ex)
                        {
                            //logger.LogError("Exception thrown retrieving exported records from the database", ex, job);
                            //Dictionary<string, string> customProps = AzureAppInsightsService.BuildOtaExceptionPropertiesObject("AzureSqlDatabaseService", "DataMapping", ex);
                            //AzureAppInsightsService.LogEvent(OtaJobImportEventEnum.Name.OTA_IMPORT_EXCEPTION.ToString(), customProps);
                            throw;
                        }
                    }

                    //logger.LogInfo("Close Database Connection", job);
                    conn.Close();
                } // end connection
            }
            else
            {
                //logger.LogInfo("Database credentials missing", job);
            }

            return jobResults;
        }
        */

        private static string nulltostring(object Value)
        {
            return Value == null ? "" : Value.ToString();
        }

        private static object dbnullable(object Value)
        {
            return (Value is null or (object)"") ? DBNull.Value : Value;
        }

        private static string GetEserFromFileName(string fileName)
        {
            if (fileName != null)
            {
                string[] fileNameParts = fileName.Split('_');
                return fileNameParts[0];
            }
            else
            {
                return null;
            }
        }

        private static double? GetTotalSecondsFromTimeSpan(TimeSpan? timeSpan)
        {
            if (timeSpan != null)
            {
                return ((TimeSpan)timeSpan).TotalSeconds;
            }
            else
            {
                return null;
            }
        }

        public static String GetFirstIpAddressFromString(String ipList)
        {
            string[] ipAddresses = ipList.Split(','); // split string of ip adressses into array
            string[] ipAddress = ipAddresses[0].Split(':'); // grab first ip address and remove any existing port
            return ipAddress[0]; // return ip address without port
        }

        public static string GetEdiPipelineFailureLocation(FailedEdiJob job)
        {
            string result = null;
            if (job.BlobTimeStart == null)
            {
                result = "Blob";
            } else if (job.BlobTimeStart == null)
            {
                result = "Blob";
            } else if (job.ProviderSuccessTime == null)
            {
                result = "Provider";
            } else if (job.ConsumerSuccessTime == null)
            {
                result = "Consumer";
            } else if (job.TransformSuccessTime == null)
            {
                result = "Transform";
            } else if (job.SQLSuccessTime == null)
            {
                result = "Sql";
            }

            return result;

        }
    }
}
