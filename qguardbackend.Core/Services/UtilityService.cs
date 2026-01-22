using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using qguardbackend.Application.Interfaces;
using qguardbackend.Core.Interfaces;
using qguardbackend.Core.Services;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Shared.DTOs.RequestDtos;

namespace qguardbackend.Application.Services;

public class UtilityService : IUtilityService
{
    private readonly AppDbContext _context;
    public readonly IHttpService _httpService;
    //private readonly IEmailService _emailService;
    private readonly ILogger<UtilityService> _logger;
    private readonly IS3Service _s3Service;
    //private readonly MiddlewareServiceOptions _middlewareServiceOptions;

    public UtilityService(AppDbContext context, ILogger<UtilityService> logger,
        IS3Service s3Service,
        IHttpService httpService//,
        //IOptions<MiddlewareServiceOptions> middlewareServiceOptions
        //, IEmailService emailService
        )
    {
        _logger = logger;
        _context = context;
        _httpService = httpService;
        //_emailService = emailService;
        //_middlewareServiceOptions = middlewareServiceOptions.Value;
        _s3Service = s3Service;
    }




    public async Task<CustomResult<List<UploadedFileDetails>>> UploadMultipleFilesToS3(string entity, string filetype, IFormFileCollection request)
    {
        try
        {
            var res = new List<UploadedFileDetails>();
            foreach (var item in request)
            {
                var details = new UploadFileRequestDataDto
                {
                    FileContentType = item.ContentType,
                    FileExtension = Path.GetExtension(item.FileName),
                    Filename = Path.GetFileNameWithoutExtension(item.FileName),
                    FileStream = item.OpenReadStream()

                };
                var size = item.Length;
                //upload file 

                var upload = await _s3Service.UploadFileGetDetailsAsync(entity, filetype, size,details.FileStream, details.Filename,
                    details.FileContentType, details.FileExtension);
                if (upload == null)
                    continue;
                res.Add(upload);

            }

            if (res.Count < 1)
                return CustomResult<List<UploadedFileDetails>>.Failure(CustomError.OperationError, ResponseCodes.OperationError);

            return CustomResult<List<UploadedFileDetails>>.Success(res, ResponseMessages.SuccessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name,
                nameof(UploadMultipleFilesToS3));
            return CustomResult<List<UploadedFileDetails>>.Failure(CustomError.OperationError, ResponseCodes.OperationError);
        }
    }


    public async Task<CustomResult<string>> UploadFileToS3(UploadFileRequestDataDto request)
    {
        try
        {
            var upload = await _s3Service.UploadFileAsync(request.FileStream, request.Filename,
                request.FileContentType, request.FileExtension);

            if (string.IsNullOrEmpty(upload))
                return CustomResult<string>.Failure(CustomError.OperationError, ResponseCodes.OperationError);

            return CustomResult<string>.Success(upload, ResponseMessages.SuccessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name,
                nameof(UploadFileToS3));
            return CustomResult<string>.Failure(CustomError.OperationError, ResponseCodes.OperationError);
        }
    }

    //public async Task<CustomResult<List<SettingsDto>>> GetSettings()
    //{
    //    try
    //    {
    //        var settings = await _context.Settings
    //            .Select(d => new SettingsDto
    //            {
    //                CustomerSupportEmail = d.CustomerSupportEmail,
    //                CustomerSupportTelephone = d.CustomerSupportTelephone,
    //                Id = d.Id,
    //                OrganizationEmail = d.OrganizationEmail,
    //                OrganizationName = d.OrganizationName,
    //                OrganizationTelephone = d.OrganizationTelephone,
    //                VATvalue = d.VATvalue
    //            })
    //            .ToListAsync();

    //        return settings == null ?
    //            CustomResult<List<SettingsDto>>.Success(null!, ResponseMessages.ProductNotFound) :
    //            CustomResult<List<SettingsDto>>.Success(settings);
    //    }
    //    catch (Exception ex)
    //    {
    //        _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name, nameof(GetSettings));
    //        throw;
    //    }
    //}

    public async Task<string> UploadFileStreamAsync(S3FileUploadRequestDto model)
    {
        try
        {
            return await _s3Service.UploadFileStreamAsync(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name, nameof(UploadFileStreamAsync));
            throw;
        }
    }

    public async Task<string> UploadBase64FileAsync(S3FileUploadRequestDtoV2 model)
    {
        try
        {
            return await _s3Service.UploadBase64FileAsync(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name, nameof(UploadBase64FileAsync));
            throw;
        }
    }

    public async Task<S3uploadResponse> UploadBase64FileGetKeyAsync(S3FileUploadRequestDtoV2 model)
    {
        try
        {
            return await _s3Service.UploadBase64FileGetKeyAsync(model);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name, nameof(UploadBase64FileAsync));
            throw;
        }
    }

    public string ExtractBase64Data(string dataUrl)
    {
        if (string.IsNullOrEmpty(dataUrl))
            return string.Empty;

        // Check if it's a data URL (starts with "data:")
        if (dataUrl.StartsWith("data:"))
        {
            int commaIndex = dataUrl.IndexOf(',');
            if (commaIndex >= 0 && commaIndex < dataUrl.Length - 1)
            {
                return dataUrl.Substring(commaIndex + 1);
            }
        }

        return dataUrl; // Return as-is if not a data URL
    }

    public async Task<Stream?> DownloadFileAsync(string filepath, string key)
    {
        try
        {
            return await _s3Service.GetFileStreamAsync(filepath, key);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(UtilityService).Name, nameof(UploadBase64FileAsync));
            throw;
        }
    }


    public async Task<CustomResult<List<UploadedFileDetails>>> UploadMultipleFilesToS3WithNotes(
    string entity,
    string filetype,
    IFormFileCollection files,
    List<string> notes)
    {
        try
        {
            if (files == null || files.Count == 0)
                return CustomResult<List<UploadedFileDetails>>.Failure(
                    CustomError.OperationError,
                    ResponseCodes.OperationError);

            if (notes != null && notes.Count != files.Count)
                return CustomResult<List<UploadedFileDetails>>.Failure(
                    CustomError.OperationError,
                    ResponseCodes.OperationError);

            var res = new List<UploadedFileDetails>();

            for (int i = 0; i < files.Count; i++)
            {
                var item = files[i];
                var note = notes?[i] ?? string.Empty;

                var details = new UploadFileRequestDataDto
                {
                    FileContentType = item.ContentType,
                    FileExtension = Path.GetExtension(item.FileName),
                    Filename = Path.GetFileNameWithoutExtension(item.FileName),
                    FileStream = item.OpenReadStream()
                };

                var size = item.Length;

                // Upload file 
                var upload = await _s3Service.UploadFileGetDetailsAsync(
                    entity,
                    filetype,
                    size,
                    details.FileStream,
                    details.Filename,
                    details.FileContentType,
                    details.FileExtension);

                if (upload == null)
                    continue;

                // Create result with note
                var resultWithNote = new UploadedFileDetails
                {
                    // Copy all properties from upload
                    UploadedFileURL = upload.UploadedFileURL,
                    fileName = upload.fileName,
                    FileSize = upload.FileSize,
                    Category = upload.Category,
                    // Add any other properties from UploadedFileDetails

                    // Add the note
                    Note = note
                };

                res.Add(resultWithNote);
            }

            if (res.Count < 1)
                return CustomResult<List<UploadedFileDetails>>.Failure(
                    CustomError.OperationError,
                    ResponseCodes.OperationError);

            return CustomResult<List<UploadedFileDetails>>.Success(
                res,
                ResponseMessages.SuccessMessage);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}",
                typeof(UtilityService).Name,
                nameof(UploadMultipleFilesToS3WithNotes));
            return CustomResult<List<UploadedFileDetails>>.Failure(
                CustomError.OperationError,
                ResponseCodes.OperationError);
        }
    }

  
}

public class UploadedFileDetails
{
    public string fileName { get; set; }
    public string UploadedFileURL { get; set; }
    public string UploadedKey { get; set; }
    public long FileSize { get; set; }
    public string Category { get; set; }
    public string FolderPath { get; set; }
    public string Note { get; set; }
}

public class UploadFileRequestDto
{
    public string Filename { get; set; }
    public IFormFile File { get; set; }
}

public class UploadFileRequestDataDto
{
    public string Filename { get; set; }
    public string FileContentType { get; set; }
    public string FileExtension { get; set; }
    public Stream FileStream { get; set; }
}

