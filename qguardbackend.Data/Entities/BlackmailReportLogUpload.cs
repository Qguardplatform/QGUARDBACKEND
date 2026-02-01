
using qguardbackend.Data.Abstracts;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace qguardbackend.Data.Entities
{
    public class BlackmailReportLogUpload : BaseEntity
    {
        public long BlackmailReportLogId { get; set; }
        public string? UploadType { get; set; }
        public string? UploadName { get; set; }
        public string? Extenstion { get; set; }

        [StringLength(500)]
        public string? FilePath { get; set; }

        public long? FileSize { get; set; }

        [StringLength(100)]
        public string? MimeType { get; set; }

        public string? FileKey { get; set; }
        public string? UploadNote { get; set; }

        [ForeignKey("BlackmailReportLogId")]
        public BlackmailReportLog BlackmailReportLog { get; set; }
    }
}
