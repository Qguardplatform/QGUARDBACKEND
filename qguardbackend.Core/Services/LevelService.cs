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
    public class LevelService : ILevelService
    {
        private readonly ILogger<LevelService> _logger;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        private readonly AppDbContext _context;

        public LevelService(ILogger<LevelService> logger,
            IAuditLogService auditLogService,
            IUserManagementService userManagementService,
        AppDbContext context)
        {
            _logger = logger;
            _auditLogService = auditLogService;
            _context = context;
            _userManagementService = userManagementService;
        }
        public async Task<CustomResult<LevelModel>> Create(LevelCreateModel model, string createdBy)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<LevelModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var check = await _context.Levels.FirstOrDefaultAsync(x => x.LevelName.ToLower() 
                == model.LevelName.ToLower()
                && x.InstitutionId == InsTId.Data
                );
                if (check is not null)
                {
                    return CustomResult<LevelModel>.ErrorOccured("Level name already exist", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = Level.Create(model.LevelName, model.Description, InsTId.Data);
                create.IsActive = true;
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Course", $"User [{createdBy}] created a new level - {model.LevelName} at {DateTime.UtcNow}.");

                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<LevelModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Levels.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Level not found!", ResponseCodes.NotFoundErrorCode);
                }
                _context.Remove(type);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Level", $"User [{createdBy}] deleted a level - {type.LevelName} at {DateTime.UtcNow}.");

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Level successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<LevelModel>>> GetAll(QueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<LevelModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Level> records = _context.Levels.Include(x => x.Institution)
                                    .Where(x=> x.InstitutionId == InsTId.Data)
                    ;
                if (search.IsActive.HasValue)
                {

                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.LevelName.ToLower().Contains(searchWord) ||
                        x.Description.ToLower().Contains(searchWord) ||
                        x.Institution.Name.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.LevelName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new LevelModel
                {
                    Id = query.Id,
                    LevelName = query.LevelName,
                    Description = query.Description,
                    Institution = query.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.Institution.Id,
                        Name = query.Institution.Name,
                        Code = query.Institution.Code
                    },
                    DateCreated = query.CreatedAt, 
                    IsActive = query.IsActive
                }).OrderByDescending(x => x.Id).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<LevelModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<LevelModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<LevelModel>> GetById(long id)
        {
            try
            {
                var query = await _context.Levels.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<LevelModel>.ErrorOccured("Invalid Level Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new LevelModel
                {
                    Id = query.Id,
                    LevelName = query.LevelName,
                    Description = query.Description,
                    IsActive = query.IsActive,
                    Institution = query.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = query.Institution.Id,
                        Name = query.Institution.Name,
                        Code = query.Institution.Code
                    },
                    DateCreated = query.CreatedAt
                };
                return CustomResult<LevelModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<LevelModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<LevelModel>> Update(long id, LevelCreateModel model, string createdBy)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<LevelModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var query = await _context.Levels.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<LevelModel>.ErrorOccured("Invalid Department Id!", ResponseCodes.BadRequestErrorCode);
                }
                var institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == InsTId.Data);

                if (institution is null)
                {
                    return CustomResult<LevelModel>.ErrorOccured("Institution does not exists", ResponseCodes.NotFoundErrorCode);
                }

                query.LevelName = model.LevelName;
                query.Description = model.Description;
                query.InstitutionId = InsTId.Data;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Level", $"User [{createdBy}] updated a level to - {query.LevelName} at {DateTime.UtcNow}.");

                return await this.GetById(query.Id);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<LevelModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Levels.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Level not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Levels.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Level", $"User [{createdBy}], {message} level at {DateTime.UtcNow}.");
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