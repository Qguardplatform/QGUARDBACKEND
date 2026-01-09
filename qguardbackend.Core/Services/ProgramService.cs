using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using examportal.Api.ServiceExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace qguardbackend.Core.Services
{
    public class ProgramService : IProgramService
    {
        private readonly ILogger<ProgramService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService  _userManagementService;

        public ProgramService(ILogger<ProgramService> logger,
            IAuditLogService auditLogService,
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _userManagementService = userManagementService;
            _auditLogService = auditLogService;
        }
        public async Task<CustomResult<ProgramModel>> Create(ProgramCreateModel model, string createdBy)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<ProgramModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var check = await _context.Programs.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower()
                && x.InstitutionId == InsTId.Data);
                if (check is not null)
                {
                    return CustomResult<ProgramModel>.ErrorOccured("Program name already exist for the Institution", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = Program.Create(model.Name, InsTId.Data);
                create.DurationInMonths = model.DurationInMonths;
                create.noOfSemesters = model.noOfSemesters;
                create.IsActive = true;
                create.ProgramType = model.ProgramType.GetEnumText();
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Program", $"User [{createdBy}] created a new program - {model.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ProgramModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Programs.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Program not found!", ResponseCodes.NotFoundErrorCode);
                }
                type.IsActive = false;
                _context.Update(type);
                await _context.SaveChangesAsync();

                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Program", $"User [{createdBy}] deleted a program - {type.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Program successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<ProgramModel>>> GetAll(GetProgramQueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<ProgramModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Program> records = _context.Programs
                    .Where(x=>x.InstitutionId == InsTId.Data);

                if (search.IsActive.HasValue)
                {

                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (search.ProgramType.HasValue)
                {

                    records = records.Where(x => x.ProgramType.ToLower()
                    == search.ProgramType.Value.GetEnumText());
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
                var result = await records.Select(query => new ProgramModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    DurationInMonths = query.DurationInMonths,
                    ProgramType = query.ProgramType,
                    noOfSemesters = query.noOfSemesters,
                    IsActive = query.IsActive
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ProgramModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ProgramModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ProgramModel>> GetById(long id)
        {
            try
            {
                var query = await _context.Programs.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<ProgramModel>.ErrorOccured("Invalid Program Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new ProgramModel
                {
                    Id = query.Id,
                    Name = query.Name,
                    DurationInMonths = query.DurationInMonths,
                    ProgramType = query.ProgramType,
                    noOfSemesters = query.noOfSemesters,
                    IsActive = query.IsActive,
                    DateCreated = query.CreatedAt
                };
                return CustomResult<ProgramModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ProgramModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Update(long id, ProgramCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.Programs.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<string>.ErrorOccured("Invalid Program Id!", ResponseCodes.BadRequestErrorCode);
                }

                query.Name = model.Name;
                query.ProgramType = model.ProgramType.GetEnumText();
                query.noOfSemesters = model.noOfSemesters;
                query.DurationInMonths = model.DurationInMonths;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Programs.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Program", $"User [{createdBy}] update a program - {query.Name} at {DateTime.UtcNow}.");
                //return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Program updated successfully");
                return CustomResult<string>.Success($"Program updated successfully", $"Program updated successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Programs.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Program not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Programs.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Program", $"User [{createdBy}], {message} a program at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Program {message} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
