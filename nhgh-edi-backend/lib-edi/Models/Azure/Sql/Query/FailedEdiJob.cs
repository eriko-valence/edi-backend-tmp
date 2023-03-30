using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace lib_edi.Models.Azure.Sql.Query
{
    public class FailedEdiJob
    {
        /*
        public FailedEdiJob(string FilePackageName_, string ESER_, DateTime? BlobTimeStart_, DateTime? ProviderSuccessTime_, DateTime? ConsumerSuccessTime_, DateTime? TransformSuccessTime_, DateTime? SQLSuccessTime_, int DurationSecs_, DateTime DateAdded_, DateTime DateUpdated_, string DataLoggerType_)
        {
            this.FilePackageName = FilePackageName_;
            this.ESER = ESER_;
            this.BlobTimeStart = BlobTimeStart_;
            this.ProviderSuccessTime = ProviderSuccessTime_;
            this.ConsumerSuccessTime = ConsumerSuccessTime_;
            this.TransformSuccessTime = TransformSuccessTime_;
            this.SQLSuccessTime = SQLSuccessTime_;
            this.DurationSecs = DurationSecs_;
            this.DateAdded = DateAdded_;
            this.DateUpdated = DateUpdated_;
            this.DataLoggerType = DataLoggerType_;
        }
        */

        public string FilePackageName { get; set; }
        public string ESER { get; set; }
        public DateTime? BlobTimeStart { get; set; }
        public DateTime? ProviderSuccessTime { get; set; }
        public DateTime? ConsumerSuccessTime { get; set; }
        public DateTime? TransformSuccessTime { get; set; }
        public DateTime? SQLSuccessTime { get; set; }
        public int DurationSecs { get; set; }
        public DateTime DateAdded { get; set; }
        public DateTime DateUpdated { get; set; }
        public string DataLoggerType { get; set; }
        public string PipelineFailureLocation { get; set; }
    }
}
