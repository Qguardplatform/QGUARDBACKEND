using System.Net;

namespace qguardbackend.Data.DTOs
{
    public class FileUploadResponse
    {
        public string FileName { get; set; }
        public string Message { get; set; }
        public bool Success { get; set; }
    }
}
