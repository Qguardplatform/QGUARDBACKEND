using qguardbackend.Data.DTOs;
using Microsoft.AspNetCore.Http;
using qguardbackend.Shared.DTOs.RequestDtos;
using qguardbackend.Application.Services;
using qguardbackend.Core.Services;

namespace qguardbackend.Core.Interfaces
{
    public interface IS3Service
    {
        Task<FileUploadResponse> UploadToS3Async(IFormFile file, string fileName);
        Task<FileUploadResponse> DeleteFileFromS3Async(string filePath);



        Task<string> UploadFileStreamAsync(S3FileUploadRequestDto model);
        Task<string> UploadBase64FileAsync(S3FileUploadRequestDtoV2 model);
        Task<(string, string)> UploadFileAsyncV2(Stream fileStream, string fileName, string contentType);
        Task<string> UploadFileAsync(Stream fileStream, string fileName, string contentType, string extension);
        Task<UploadedFileDetails> UploadFileGetDetailsAsync(long size, Stream fileStream, string fileName, string contentType, string extension);
        Task<UploadedFileDetails> UploadFileGetDetailsAsync(string entity, string filetype, long size, Stream fileStream, string fileName, string contentType, string extension);


        Task<Stream?> GetFileStreamAsync(string filePath, string key);
        Task<S3uploadResponse> UploadBase64FileGetKeyAsync(S3FileUploadRequestDtoV2 model);
    }
}
