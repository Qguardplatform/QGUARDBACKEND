using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.ResponseDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using examportal.Api.ServiceExtensions;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using static Microsoft.EntityFrameworkCore.DbLoggerCategory;

namespace qguardbackend.Core.Services
{
    public class TutorsService : ITutorsService
    {
        private readonly ILogger<TutorsService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;

        public TutorsService(ILogger<TutorsService> logger,
            IUserManagementService userManagementService,
            IAuditLogService auditLogService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }
    
        public async Task<CustomResult<PaginatedResult<InstitutionTutorsResponseDto>>> GetAll(QueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<InstitutionTutorsResponseDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Tutor> records = _context.Tutors;

                if (InsTId.Data >0 )
                {
                    records = records.Where(x => x.InstitutionId == InsTId.Data);
                }

                if (search.IsActive.HasValue)
                {
                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.TutorName.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.TutorName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new InstitutionTutorsResponseDto
                {
                    Id = query.Id,
                    Name = query.TutorName,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                    
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<InstitutionTutorsResponseDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<InstitutionTutorsResponseDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<InstitutionTutorsResponseDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Tutors.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<InstitutionTutorsResponseDto>.ErrorOccured("Invalid Tutor Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new InstitutionTutorsResponseDto
                {
                    Id = query.Id,
                    Name = query.TutorName,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                };
                return CustomResult<InstitutionTutorsResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<InstitutionTutorsResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }    

        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Tutors.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Program not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Tutors.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Tutor", $"User [{createdBy}], {message} tutor at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Tutor {message}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
