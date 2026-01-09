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
    public class SemesterService : ISemesterService
    {
        private readonly ILogger<SemesterService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public SemesterService(ILogger<SemesterService> logger,
            IAuditLogService auditLogService, IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }
        public async Task<CustomResult<SemesterModel>> Create(SemesterCreateModel model, string createdBy)
        {
            try
            {
                var check = await _context.Semesters.FirstOrDefaultAsync(x => x.SemesterName.ToLower() == model.Name.ToLower()
                && x.InstitutionId == model.InstitutionId);
                if (check is not null)
                {
                    return CustomResult<SemesterModel>.ErrorOccured("Semester name already exist for the Institution", ResponseCodes.AlreadyExistErrorCode);
                }
                var create = Semester.Create(model.Name, model.InstitutionId);
                create.IsActive = true;
                await _context.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Semester", $"User [{createdBy}] created a new Semester - {model.Name} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(create.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SemesterModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Semester not found!", ResponseCodes.NotFoundErrorCode);
                }
                type.IsActive = false;
                _context.Update(type);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Semester", $"User [{createdBy}] deleted a Semester - {type.SemesterName} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Semester successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<SemesterModel>>> GetAll(QueryModelMini search)
        {
            try
            {


                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<SemesterModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

               

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Semester> records = _context.Semesters
                    .Where(
                         x => x.InstitutionId == InsTId.Data);
                if (search.IsActive.HasValue)
                {

                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.SemesterName.ToLower().Contains(searchWord));
                }
                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }
                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.SemesterName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(query => new SemesterModel
                {
                    Id = query.Id,
                    Name = query.SemesterName,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<SemesterModel>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<SemesterModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<SemesterModel>> GetById(long id)
        {
            try
            {
                var query = await _context.Semesters.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<SemesterModel>.ErrorOccured("Invalid Semester Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new SemesterModel
                {
                    Id = query.Id,
                    Name = query.SemesterName,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive
                };
                return CustomResult<SemesterModel>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SemesterModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task SeedDefaultSemester()
        {
            if (_context.Semesters.Any())
            {
                return;

            }
            var getDefaultIns = await _context.Institutions.FirstOrDefaultAsync();
            List<SemesterCreateModel> myList = new List<SemesterCreateModel>
            {
                new SemesterCreateModel { Name = "First Semester" },
                new SemesterCreateModel { Name = "Second Semester" },
                new SemesterCreateModel { Name = "Third Semester" }
            };
            if (myList.Any())
            {
                foreach (var items in myList)
                {
                    var settings = Semester.Create(items.Name, getDefaultIns.Id);
                    await _context.AddAsync(settings);
                }
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<SemesterModel>> Update(long id, SemesterCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<SemesterModel>.ErrorOccured("Invalid Semester Id!", ResponseCodes.BadRequestErrorCode);
                }

                query.SemesterName = model.Name;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Semester", $"User [{createdBy}] update a Semester - {query.SemesterName} at {DateTime.UtcNow}.");
                var returnDto = await this.GetById(query.Id);
                return returnDto;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SemesterModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Semester not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Semesters.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "Semester enabled" : "Semester disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Semester", $"User [{createdBy}], {message} at {DateTime.UtcNow}.");
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