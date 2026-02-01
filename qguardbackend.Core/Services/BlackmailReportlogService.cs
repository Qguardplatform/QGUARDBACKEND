using DocumentFormat.OpenXml.Bibliography;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using qguardbackend.Application.Interfaces;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using qguardbackend.Shared.DTOs.RequestDtos;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Core.Services
{
    public class BlackmailReportlogService : IBlackmailReportlogService
    {

        private readonly ILogger<BlackmailReportlogService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        private readonly IUtilityService _utilityService;
        public BlackmailReportlogService(ILogger<BlackmailReportlogService> logger,
            IAuditLogService auditLogService, IUtilityService utilityService,
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
            _utilityService = utilityService;
        }



        private DateTime AddTimeSpan(DateTime date)
        {
            var tm = TimeSpan.Parse("23:59:59");
            DateTime endDate = date + tm;
            return endDate;
        }


        public async Task<CustomResult<BlackmailReportLogResponseDto>> Create(
       BlackmailReportLogsRequestDto model,
       string createdBy)
        {
            try
            {
                var resultList = new List<ReportLogUploadResponseDto>();
                var reportLog = new BlackmailReportLog
                {
                    Name = model.Name,
                    Socials = model.Socials,
                    Description = model.Description,
                    Address = model.Address,
                    NearestBustop = model.NearestBustop,
                    City = model.City,
                    LGA = model.LGA,
                    State = model.State,
                    Country = model.Country,
                    CreatedAt = DateTime.UtcNow,
                    //CreatedBy = createdBy
                };

                var added = await _context.BlackmailReportLogs.AddAsync(reportLog);
                await _context.SaveChangesAsync();

                var response = new BlackmailReportLogResponseDto
                {
                    Id = reportLog.Id,
                    Name = reportLog.Name,
                    Socials = reportLog.Socials,
                    Description = reportLog.Description,
                    Address = reportLog.Address,
                    NearestBustop = reportLog.NearestBustop,
                    City = reportLog.City,
                    LGA = reportLog.LGA,
                    State = reportLog.State,
                    Country = reportLog.Country
                };



                foreach (var doc in model.Uploads)
                {
                    var uploadResult = await _utilityService.UploadBase64FileGetKeyAsync(new S3FileUploadRequestDtoV2
                    {
                        EntityName = "Distributor",
                        CategoryName = "Certificates",
                        FileName = $"{doc.FileName}_{Guid.NewGuid()}",
                        FileString = doc.FilePath
                    });

                    if (string.IsNullOrWhiteSpace(uploadResult.URL)) continue;

                    string cleanBase64 = ExtractBase64Data(doc.FilePath);

                    // Calculate padding characters
                    int paddingCount = 0;
                    if (cleanBase64.EndsWith("=="))
                        paddingCount = 2;
                    else if (cleanBase64.EndsWith("="))
                        paddingCount = 1;

                    // Calculate file size: (base64_length * 3/4) - padding
                    long file_Size = (cleanBase64.Length * 3L / 4L) - paddingCount;


                    //var fileBytes = Convert.FromBase64String(doc.Document);
                    //var mimeType = MimeMapping.MimeUtility.GetMimeMapping(doc.FileName);
                    //var fileSize = fileBytes.Length;

                    var document = new BlackmailReportLogUpload
                    {
                        BlackmailReportLogId = added.Entity.Id,
                        UploadName = doc.FileName,

                        UploadType = doc.DocumentsType,

                        FilePath = uploadResult.URL,
                        FileKey = uploadResult.Key,
                        //FileSize = fileSize,
                        FileSize = file_Size,
                        //MimeType = mimeType,

                    };

                    _context.BlackmailReportLogUploads.Add(document);
                    await _context.SaveChangesAsync();



                    resultList.Add(new ReportLogUploadResponseDto
                    {
                        Id = document.Id,
                        FileName = document.UploadName,
                        DocumentsType = document.UploadType,

                        FilePath = document.FilePath,
                        FileSize = document.FileSize,
                        MimeType = document.MimeType
                    });

                }

                response.ReportLogUploads = resultList;

                await _auditLogService.AddToAudit(
                (int)AuditActionType.Create,
                "ReportLogs",
                $"User [{createdBy}] created report log [{reportLog.Name}] at {DateTime.UtcNow}.");

                return CustomResult<BlackmailReportLogResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<BlackmailReportLogResponseDto>.ErrorOccured(
                    ex.Message,
                    ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public static string ExtractBase64Data(string dataUrl)
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

        public async Task<CustomResult<BlackmailReportLogResponseDto>> Update(
    long id,
    BlackmailReportLogsRequestDto model,
    string createdBy)
        {
            try
            {

                var resultList = new List<ReportLogUploadResponseDto>();
                var reportLog = await _context.BlackmailReportLogs
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (reportLog == null)
                {
                    return CustomResult<BlackmailReportLogResponseDto>.ErrorOccured(
                        $"Report log with id [{id}] not found.", "99");
                }

                // Update fields
                reportLog.Name = model.Name;
                reportLog.Socials = model.Socials;
                reportLog.Description = model.Description;
                reportLog.Address = model.Address;
                reportLog.NearestBustop = model.NearestBustop;
                reportLog.City = model.City;
                reportLog.LGA = model.LGA;
                reportLog.State = model.State;
                reportLog.Country = model.Country;

                reportLog.UpdatedAt = DateTime.UtcNow;
                //reportLog.UpdatedBy = createdBy;

                await _context.SaveChangesAsync();

                var response = new BlackmailReportLogResponseDto
                {
                    Id = reportLog.Id,
                    Name = reportLog.Name,
                    Socials = reportLog.Socials,
                    Description = reportLog.Description,
                    Address = reportLog.Address,
                    NearestBustop = reportLog.NearestBustop,
                    City = reportLog.City,
                    LGA = reportLog.LGA,
                    State = reportLog.State,
                    Country = reportLog.Country
                };



                //TODO: remove all old uploads from table and from S3

                var removeOldFiles = await _context.BlackmailReportLogUploads.Where(x => x.BlackmailReportLogId == id).ToListAsync();


                if (removeOldFiles.Any())
                {
                    foreach (var item in removeOldFiles)
                    {//remove from s3

                    }
                    _context.BlackmailReportLogUploads.RemoveRange(removeOldFiles);

                }



                foreach (var doc in model.Uploads)
                {
                    var uploadResult = await _utilityService.UploadBase64FileGetKeyAsync(new S3FileUploadRequestDtoV2
                    {
                        EntityName = "Distributor",
                        CategoryName = "Certificates",
                        FileName = $"{doc.FileName}_{Guid.NewGuid()}",
                        FileString = doc.FilePath
                    });

                    if (string.IsNullOrWhiteSpace(uploadResult.URL)) continue;

                    string cleanBase64 = ExtractBase64Data(doc.FilePath);

                    // Calculate padding characters
                    int paddingCount = 0;
                    if (cleanBase64.EndsWith("=="))
                        paddingCount = 2;
                    else if (cleanBase64.EndsWith("="))
                        paddingCount = 1;

                    // Calculate file size: (base64_length * 3/4) - padding
                    long file_Size = (cleanBase64.Length * 3L / 4L) - paddingCount;


                    //var fileBytes = Convert.FromBase64String(doc.Document);
                    //var mimeType = MimeMapping.MimeUtility.GetMimeMapping(doc.FileName);
                    //var fileSize = fileBytes.Length;

                    var document = new BlackmailReportLogUpload
                    {
                        BlackmailReportLogId = id,
                        UploadName = doc.FileName,

                        UploadType = doc.DocumentsType,

                        FilePath = uploadResult.URL,
                        FileKey = uploadResult.Key,
                        //FileSize = fileSize,
                        FileSize = file_Size,
                        //MimeType = mimeType,

                    };

                    _context.BlackmailReportLogUploads.Add(document);
                    await _context.SaveChangesAsync();



                    resultList.Add(new ReportLogUploadResponseDto
                    {
                        Id = document.Id,
                        FileName = document.UploadName,
                        DocumentsType = document.UploadType,

                        FilePath = document.FilePath,
                        FileSize = document.FileSize,
                        MimeType = document.MimeType
                    });

                }

                response.ReportLogUploads = resultList;



                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Edit,
                    "ReportLogs",
                    $"User [{createdBy}] updated report log [{reportLog.Name}] at {DateTime.UtcNow}.");

                return CustomResult<BlackmailReportLogResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<BlackmailReportLogResponseDto>.ErrorOccured(
                    ex.Message,
                    ResponseCodes.SystemExceptionErrorCode);
            }
        }


        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            {
                try
                {
                    var audit = await _context.BlackmailReportLogs.FirstOrDefaultAsync(x => x.Id == id);
                    if (audit is null)
                    {
                        return CustomResult<string>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                    }

                    _context.BlackmailReportLogs.Remove(audit);
                    await _context.SaveChangesAsync();

                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Report successfully deleted");
                }
                catch (Exception ex)
                {
                    return CustomResult<string>.ErrorOccured($"{ex.Message}", ResponseCodes.SystemExceptionErrorCode);
                }
            }
        }
        public async Task<CustomResult<PaginatedResult<BlackmailReportLogResponseDto>>> GetAll(QueryModelMini filter)
        {
            try
            {
                IQueryable<BlackmailReportLog> records = _context.BlackmailReportLogs
                                            //.Include(x => x.Institution)
                                            .OrderByDescending(x => x.CreatedAt);

                var end = AddTimeSpan(filter.EndDate ?? DateTime.Now);



                if (!string.IsNullOrWhiteSpace(filter.SearchWord))
                {
                    var searchWord = filter.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.Address.ToLower().Contains(searchWord) ||
                        x.State.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
                        x.NearestBustop.ToLower().Contains(searchWord) ||
                        x.Country.ToLower().Contains(searchWord) ||
                        x.Socials.ToLower().Contains(searchWord) ||
                        x.LGA.ToLower().Contains(searchWord) ||
                        x.Name.ToLower().Contains(searchWord));
                }

                if (filter.StartDate.HasValue && filter.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= filter.StartDate.Value && x.CreatedAt <= end);
                }


                if (filter.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.CreatedAt);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }

                var resultData = await records.Select(record => new BlackmailReportLogResponseDto
                {
                    Id = record.Id,
                    Name = record.Name,
                    Address = record.Address,
                    State = record.State,
                    Description = record.Description,
                    City = record.City,
                    Country = record.Country,
                    Socials = record.Socials,
                    LGA = record.LGA,
                    NearestBustop = record.NearestBustop,
                    ReportLogUploads = _context.BlackmailReportLogUploads
                    .Where(x => x.BlackmailReportLogId == record.Id)
                    .Select(a => new ReportLogUploadResponseDto
                    {

                        Id = a.Id,
                        DocumentsType = a.UploadType,
                        FileName = a.UploadName,
                        FilePath = a.FilePath
                    }).ToList(),
                }).ToListAsync();

                // Handle pagination
                var responseData = filter.PageNumber.HasValue && filter.PageSize.HasValue
                    ? resultData.ToPageList(filter.PageNumber.Value, filter.PageSize.Value)
                    : resultData.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<BlackmailReportLogResponseDto>>.Success(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }


        }

        public async Task<CustomResult<BlackmailReportLogResponseDto>> GetById(long id)
        {
            try
            {
                var record = await _context.BlackmailReportLogs
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (record == null)
                {
                    return CustomResult<BlackmailReportLogResponseDto>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                }

                var model = new BlackmailReportLogResponseDto
                {
                    Id = record.Id,
                    Name = record.Name,
                    Address = record.Address,
                    State = record.State,
                    Description = record.Description,
                    City = record.City,
                    Country = record.Country,
                    Socials = record.Socials,
                    LGA = record.LGA,
                    NearestBustop = record.NearestBustop,
                    ReportLogUploads = _context.BlackmailReportLogUploads
                    .Where(x => x.BlackmailReportLogId == record.Id)
                    .Select(a => new ReportLogUploadResponseDto
                    {
                        Id = a.Id,
                        DocumentsType = a.UploadType,
                        FileName = a.UploadName,
                        FilePath = a.FilePath
                    }).ToList(),

                };
                return CustomResult<BlackmailReportLogResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<BlackmailReportLogResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
