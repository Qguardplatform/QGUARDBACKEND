using qguardbackend.Data.DTOs;
using Microsoft.AspNetCore.Http;

namespace qguardbackend.Core.Interfaces
{
    public interface IS3Service
    {
        Task<FileUploadResponse> UploadToS3Async(IFormFile file, string fileName);
        Task<FileUploadResponse> DeleteFileFromS3Async(string filePath);
    }
}
