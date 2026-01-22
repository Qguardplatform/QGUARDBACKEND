using Microsoft.AspNetCore.Http;
using qguardbackend.Application.Services;
using qguardbackend.Core.Services;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Shared.DTOs.RequestDtos;

namespace qguardbackend.Application.Interfaces;

public interface IUtilityService //: IAutoDependencyCore
{
    //Task<CustomResult<List<SettingsDto>>> GetSettings();
    Task<string> UploadFileStreamAsync(S3FileUploadRequestDto model);
    Task<string> UploadBase64FileAsync(S3FileUploadRequestDtoV2 model);
    Task<CustomResult<string>> UploadFileToS3(UploadFileRequestDataDto request);
    Task<Stream?> DownloadFileAsync(string filepath, string key);
    Task<S3uploadResponse> UploadBase64FileGetKeyAsync(S3FileUploadRequestDtoV2 model);
    string ExtractBase64Data(string dataUrl);
    Task<CustomResult<List<UploadedFileDetails>>> UploadMultipleFilesToS3WithNotes(
    string entity,
    string filetype,
    IFormFileCollection files,
    List<string> notes);
    Task<CustomResult<List<UploadedFileDetails>>> UploadMultipleFilesToS3(string entity, string filetype, IFormFileCollection request);
}