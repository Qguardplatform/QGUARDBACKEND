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
    public class EnforcementModeService : IEnforcementModeService
    {

        private readonly ILogger<EnforcementModeService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public EnforcementModeService(ILogger<EnforcementModeService> logger,
            IAuditLogService auditLogService,
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<EnforcementModeModel>> Create(EnforcementModeCreateModel model, string createdBy)
        {
            try
            {
                var check = await _context.EnforcementModes.FirstOrDefaultAsync(x => x.Code.ToLower() == model.Code.ToLower());
                if (check is not null)
                {
                    return CustomResult<EnforcementModeModel>.ErrorOccured("Enforcement mode name already exist for this Institution", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = EnforcementMode.Create(model.Name, model.Code, model.IsActive);
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "EnforcementMode", $"User [{createdBy}] created a new Enforcement Mode - {model.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementModeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var query = await _context.EnforcementModes.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<string>.ErrorOccured("Enforcement Mode Not Found!", ResponseCodes.NotFoundErrorCode);
                }
                _context.Remove(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "EnforcementMode", $"User [{createdBy}] deleted an EnforcementMode - {query.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Enforcement mode successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<EnforcementModeModel>>> GetAll(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<EnforcementMode> records = _context.EnforcementModes.OrderByDescending(x => x.CreatedAt);
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
                var result = await records.Select(query => new EnforcementModeModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    IsActive= query.IsActive,
                    DateCreated = query.CreatedAt
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<EnforcementModeModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<EnforcementModeModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<EnforcementModeModel>> GetById(long id)
        {
            try
            {
                var query = await _context.EnforcementModes.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                    return CustomResult<EnforcementModeModel>.ErrorOccured("Invalid Enforcement Mode Id!", ResponseCodes.BadRequestErrorCode);

                var model = new EnforcementModeModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    Code = query.Code,
                    IsActive = query.IsActive,
                    DateCreated = query.CreatedAt,
                };
                return CustomResult<EnforcementModeModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementModeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultEnforcementMode()
        {
            if (_context.EnforcementModes.Any())
            {
                return;
            }
            List<EnforcementModeCreateModel> myList = new List<EnforcementModeCreateModel>
            {
                new EnforcementModeCreateModel { Name = "Started", Code = "STARTED", IsActive = false },
                new EnforcementModeCreateModel { Name = "End Proctoring", Code = "END_PROCTORING", IsActive = false },
                new EnforcementModeCreateModel { Name = "Set Config", Code = "SET_CONFIG", IsActive = false },
                new EnforcementModeCreateModel { Name = "Full screen change", Code = "FULLSCREEN_CHANGE", IsActive = true },
                new EnforcementModeCreateModel { Name = "Tab focus change", Code = "TAB_FOCUS_CHANGE", IsActive = true },
                new EnforcementModeCreateModel { Name = "Exit full screen", Code = "EXIT_FULLSCREEN", IsActive = true },
                new EnforcementModeCreateModel { Name = "Periodic snapshot", Code = "PERIODIC_SNAPSHOT", IsActive = true },
                new EnforcementModeCreateModel { Name = "System check started", Code = "SYSTEM_CHECK_STARTED", IsActive = false },
                new EnforcementModeCreateModel { Name = "System check completed", Code = "SYSTEM_CHECK_COMPLETED", IsActive = false },
                new EnforcementModeCreateModel { Name = "Face Absent", Code = "FACE_ABSENCE", IsActive = true },
                new EnforcementModeCreateModel { Name = "Multiple Face", Code = "MULTIPLE_FACE", IsActive = true },
                new EnforcementModeCreateModel { Name = "Face Mismatch", Code = "FACE_MISMATCH", IsActive = true },
                new EnforcementModeCreateModel { Name = "Sound Detection", Code = "SOUND_DETECTED" , IsActive = true },
                new EnforcementModeCreateModel { Name = "Tab Not Focused", Code = "TAB_NOT_FOCUS", IsActive = true }
            };
            if (myList.Any())
            {
                foreach (var items in myList)
                {
                    var settings = EnforcementMode.Create(items.Name, items.Code, items.IsActive);
                    await _context.AddAsync(settings);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<EnforcementModeModel>> Update(long id, EnforcementModeCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.EnforcementModes.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                    return CustomResult<EnforcementModeModel>.ErrorOccured("Invalid Enforcement Mode Id!", ResponseCodes.BadRequestErrorCode);

                query.Name = model.Name;
                query.Code = model.Code;
                query.IsActive = model.IsActive;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "EnforcementMode", $"User [{createdBy}] update an EnforcementMode - {query.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(query.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementModeModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}