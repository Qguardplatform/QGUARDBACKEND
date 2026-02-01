using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Data.DTOs.RequestDto
{
    public class BlackmailReportLogsRequestDto
    {
        public string Name { get; set; }
        public string Socials { get; set; }
        public string Description { get; set; }
        public string Address { get; set; }
        public string NearestBustop { get; set; }
        public string City { get; set; }
        public string LGA { get; set; }
        public string State { get; set; }
        public string Country { get; set; }
        public List<ReportLogUploadsRequestDto> Uploads { get; set; }
    }


    public class ReportLogUploadsRequestDto
    {
        public string FileName { get; set; }
        public string FilePath { get; set; }
        [StringLength(100)]
        public string? DocumentsType { get; set; }
    }
}
