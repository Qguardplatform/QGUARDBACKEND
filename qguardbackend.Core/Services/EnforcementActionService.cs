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
    public class EnforcementActionService : IEnforcementActionService
    {
        private readonly ILogger<EnforcementActionService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public EnforcementActionService(ILogger<EnforcementActionService> logger,
            IAuditLogService auditLogService, 
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<EnforcementActionModel>> Create(EnforcementActionCreateModel model, string createdBy)
        {
            try
            {
                var check = await _context.EnforcementActions.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower());
                if (check is not null)
                {
                    return CustomResult<EnforcementActionModel>.ErrorOccured("Enforcement action name already exist for this Institution", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = EnforcementAction.Create(model.Name);
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "EnforcementAction", $"User [{createdBy}] created a new Enforcement Action - {model.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementActionModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var query = await _context.EnforcementActions.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<string>.ErrorOccured("Enforcement Action Not Found!", ResponseCodes.NotFoundErrorCode);
                }
                _context.Remove(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "EnforcementAction", $"User [{createdBy}] deleted an EnforcementAction - {query.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Enforcement action successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<EnforcementActionModel>>> GetAll(QueryModelMini search)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<EnforcementAction> records = _context.EnforcementActions.OrderByDescending(x => x.CreatedAt);
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
                var result = await records.Select(query => new EnforcementActionModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<EnforcementActionModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<EnforcementActionModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<EnforcementActionModel>> GetById(long id)
        {
            try
            {
                var query = await _context.EnforcementActions.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                    return CustomResult<EnforcementActionModel>.ErrorOccured("Invalid Enforcement Action Id!", ResponseCodes.BadRequestErrorCode);

                var model = new EnforcementActionModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                };
                return CustomResult<EnforcementActionModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementActionModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultEnforcementAction()
        {
            if (_context.EnforcementActions.Any())
            {
                return;
            }
            List<EnforcementActionCreateModel> myList = new List<EnforcementActionCreateModel>
            {
                new EnforcementActionCreateModel { Name = "Auto Submit Exam" },
                new EnforcementActionCreateModel { Name = "Deduct Score" },
                new EnforcementActionCreateModel { Name = "Deduct Time" },
                new EnforcementActionCreateModel { Name = "Manual Review" },
            };
            if (myList.Any())
            {
                foreach (var items in myList)
                {
                    var settings = EnforcementAction.Create(items.Name);
                    await _context.AddAsync(settings);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<EnforcementActionModel>> Update(long id, EnforcementActionCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.EnforcementActions.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                    return CustomResult<EnforcementActionModel>.ErrorOccured("Invalid Enforcement Action Id!", ResponseCodes.BadRequestErrorCode);

                query.Name = model.Name;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "EnforcementAction", $"User [{createdBy}] update an EnforcementAction - {query.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(query.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<EnforcementActionModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}