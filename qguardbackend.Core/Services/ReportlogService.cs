using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace qguardbackend.Core.Services
{
    public class ReportlogService : IReportlogService
    {

        private readonly ILogger<ReportlogService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public ReportlogService(ILogger<ReportlogService> logger,
            IAuditLogService auditLogService,
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }



        private DateTime AddTimeSpan(DateTime date)
        {
            var tm = TimeSpan.Parse("23:59:59");
            DateTime endDate = date + tm;
            return endDate;
        }


        public async Task<CustomResult<ReportLogResponseDto>> Create(
       ReportLogsRequestDto model,
       string createdBy)
        {
            try
            {
                var reportLog = new ReportLogs
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

                await _context.ReportLogs.AddAsync(reportLog);
                await _context.SaveChangesAsync();

                var response = new ReportLogResponseDto
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

                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Create,
                    "ReportLogs",
                    $"User [{createdBy}] created report log [{reportLog.Name}] at {DateTime.UtcNow}.");

                return CustomResult<ReportLogResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ReportLogResponseDto>.ErrorOccured(
                    ex.Message,
                    ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ReportLogResponseDto>> Update(
    long id,
    ReportLogsRequestDto model,
    string createdBy)
        {
            try
            {
                var reportLog = await _context.ReportLogs
                    .FirstOrDefaultAsync(x => x.Id == id);

                if (reportLog == null)
                {
                    return CustomResult<ReportLogResponseDto>.ErrorOccured(
                        $"Report log with id [{id}] not found.","99");
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

                var response = new ReportLogResponseDto
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

                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Edit,
                    "ReportLogs",
                    $"User [{createdBy}] updated report log [{reportLog.Name}] at {DateTime.UtcNow}.");

                return CustomResult<ReportLogResponseDto>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ReportLogResponseDto>.ErrorOccured(
                    ex.Message,
                    ResponseCodes.SystemExceptionErrorCode);
            }
        }


        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            {
                try
                {
                    var audit = await _context.ReportLogs.FirstOrDefaultAsync(x => x.Id == id);
                    if (audit is null)
                    {
                        return CustomResult<string>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                    }

                    _context.ReportLogs.Remove(audit);
                    await _context.SaveChangesAsync();

                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Report successfully deleted");
                }
                catch (Exception ex)
                {
                    return CustomResult<string>.ErrorOccured($"{ex.Message}", ResponseCodes.SystemExceptionErrorCode);
                }
            }
        }
        public async Task<CustomResult<PaginatedResult<ReportLogResponseDto>>> GetAll(QueryModelMini filter)
        {
            try
            {
                IQueryable<ReportLogs> records = _context.ReportLogs
                                            //.Include(x => x.Institution)
                                            .OrderByDescending(x => x.CreatedAt);

                var end = AddTimeSpan(filter.EndDate ?? DateTime.Now);

                //if (!string.IsNullOrEmpty(filter.CreatedBy))
                //{
                //    records = records.Where(x => x.UserId.ToLower() == filter.CreatedBy.ToLower());
                //}

                //if (!string.IsNullOrEmpty(filter.Action))
                //{
                //    records = records.Where(x => x.Action.ToLower() == filter.Action.ToLower());
                //}
                //if (!string.IsNullOrEmpty(filter.EventType))
                //{
                //    if (Enum.TryParse(filter.EventType, true, out AuditActionType actionType))
                //    {
                //        records = records.Where(x => x.EventType == (int)actionType);
                //    }
                //}

                if (!string.IsNullOrWhiteSpace(filter.SearchWord))
                {
                    var searchWord = filter.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.Address.ToLower().Contains(searchWord) ||
                        x.State.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
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

                var resultData = await records.Select(record => new ReportLogResponseDto
                {

                }).ToListAsync();

                // Handle pagination
                var responseData = filter.PageNumber.HasValue && filter.PageSize.HasValue
                    ? resultData.ToPageList(filter.PageNumber.Value, filter.PageSize.Value)
                    : resultData.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ReportLogResponseDto>>.Success(responseData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                throw new Exception(ex.Message);
            }


        }

        public async Task<CustomResult<ReportLogResponseDto>> GetById(long id)
        {
            try
            {
                var record = await _context.ReportLogs
                    .FirstOrDefaultAsync(x => x.Id == id);
                if (record == null)
                {
                    return CustomResult<ReportLogResponseDto>.ErrorOccured("Record not found", ResponseCodes.NotFoundErrorCode);
                }

                var model = new ReportLogResponseDto
                {
                    Id = record.Id,
                    Name = record.Name,
                    LGA = record.LGA,
                    State = record.State,
                    Address = record.Address,


                };
                return CustomResult<ReportLogResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ReportLogResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
