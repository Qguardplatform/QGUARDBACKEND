using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace qguardbackend.Core.Services
{
    public class SessionService : ISessionService
    {
        private readonly ILogger<SessionService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public SessionService(ILogger<SessionService> logger,
                IUserManagementService userManagementService,
             IAuditLogService auditLogService,
        AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }
        public async Task<CustomResult<SessionDto>> Create(SessionCreateModel model, string createdBy)
        {
            try
            {
                var check = await _context.Sessions.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower()
                && x.InstitutionId == model.InstitutionId);


                if (check is not null)
                {
                    return CustomResult<SessionDto>.ErrorOccured("Session name already exist for the Institution", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = Session.Create(model.Name, model.InstitutionId);
                create.IsActive = true;

                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Session", $"User [{createdBy}] created a new session - {model.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SessionDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Session not found!", ResponseCodes.NotFoundErrorCode);
                }
                type.IsActive = false;
                _context.Update(type);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Session", $"User [{createdBy}] deleted a session - {type.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Session successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<SessionDto>>> GetAll(QueryModelMini search)
        {
            try
            {


                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<SessionDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Session> records = _context.Sessions.Where(x=>x.InstitutionId == InsTId.Data).AsQueryable();

                if (search.IsActive.HasValue)
                {

                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.Name);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new SessionDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<SessionDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<SessionDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<SessionDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Sessions.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<SessionDto>.ErrorOccured("Invalid Session Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new SessionDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive
                };
                return CustomResult<SessionDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SessionDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultSession()
        {
            if (_context.Sessions.Any())
            {
                return;
            }
            var getDefaultIns = await _context.Institutions.FirstOrDefaultAsync();
            List<SessionCreateModel> myList = new List<SessionCreateModel>
            {
                new SessionCreateModel { Name = "2023/2024 Session" },
                new SessionCreateModel { Name = "2024/2025 Session" }
            };
            if (myList.Any())
            {
                foreach (var items in myList)
                {
                    var settings = Session.Create(items.Name, getDefaultIns.Id);
                    await _context.AddAsync(settings);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<SessionDto>> Update(long id, SessionCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<SessionDto>.ErrorOccured("Invalid Session Id!", ResponseCodes.BadRequestErrorCode);
                }

                query.Name = model.Name;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Session", $"User [{createdBy}] update a session - {query.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(query.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SessionDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("session not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Sessions.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "session", $"User [{createdBy}], {message} session at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
