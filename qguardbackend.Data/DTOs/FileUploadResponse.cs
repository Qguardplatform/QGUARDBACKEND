using System.ComponentModel.DataAnnotations;
using System.Net;

namespace qguardbackend.Data.DTOs
{
    public class FileUploadResponse
    {
        public string FileName { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }



    public class AWS
    {
        //[Required]
        public string? Profile { get; set; }

        //[Required]
        public string? Region { get; set; }

        //[Required]
        public string? BucketName { get; set; }
    }
}
