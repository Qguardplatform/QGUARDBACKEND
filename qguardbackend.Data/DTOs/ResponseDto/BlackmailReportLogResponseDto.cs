using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.ResponseDto
{
    public class BlackmailReportLogResponseDto
    {
     
            public long Id { get; set; }
            public string Name { get; set; }
            public string Socials { get; set; }
            public string Description { get; set; }
            public string Address { get; set; }
            public string NearestBustop { get; set; }
            public string City { get; set; }
            public string LGA { get; set; }
            public string State { get; set; }
            public string Country { get; set; }
            public List<ReportLogUploadResponseDto> ReportLogUploads { get; set; }
        
    }

    public class ReportLogUploadResponseDto
    {
        public long Id { get; set; }
        public string FileName { get; set; }
        public string? DocumentsType { get; set; }

        public string? FilePath { get; set; }
        public long? FileSize { get; set; }
        public string? MimeType { get; set; }
    }

}
