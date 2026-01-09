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
    public class DepartmentService : IDepartmentService
    {
        private readonly ILogger<DepartmentService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;

        public DepartmentService(ILogger<DepartmentService> logger,
            IUserManagementService userManagementService,
        IAuditLogService auditLogService,
        AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<DepartmentDto>> Create(DepartmentCreateModel model, string createdBy)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<DepartmentDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var check = await _context.Departments.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower() && x.InstitutionId == InsTId.Data);
                if (check is not null)
                {
                    return CustomResult<DepartmentDto>.ErrorOccured("Department name already exist", ResponseCodes.AlreadyExistErrorCode);
                }
                var checkFaculty = await _context.Faculties.FirstOrDefaultAsync(x => x.Id == model.FacultyId);
                if (checkFaculty == null)
                {
                    return CustomResult<DepartmentDto>.ErrorOccured("Invalid Faculty Id!", ResponseCodes.BadRequestErrorCode);
                }

                var create = Department.Create(model.Name, model.FacultyId);
                create.IsActive = true;
                create.InstitutionId = InsTId.Data;
                create.CreatedAt = DateTime.UtcNow;

                var res = await _context.Departments.AddAsync(create);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Department", $"User [{createdBy}] created department - {model.Name} at {DateTime.UtcNow}.");

                var returnDto = new DepartmentDto
                {
                    Name = res.Entity.Name,
                    Id = res.Entity.Id,
                    IsActive = true,
                    DateCreated = DateTime.Now,
                    FacultyDetails = _context.Faculties.
                      Where(x => x.Id == model.FacultyId).Select(c => new FacultyDto
                      {
                          Name = c.Name,
                          Id = c.Id,
                          DateCreated = c.CreatedAt,
                          IsActive = c.IsActive,
                      }).FirstOrDefault()
                };
                return CustomResult<DepartmentDto>.Success(returnDto, "Department Created Successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<DepartmentDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Department not found!", ResponseCodes.NotFoundErrorCode);
                }
                type.IsActive = false;
                _context.Update(type);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Department", $"User [{createdBy}] deleted a department - {type.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Department successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<DepartmentDto>>> GetAll(GetDeptQueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<DepartmentDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Department> records = _context.Departments
                    .Include(x => x.Faculty)
                    .Where(x => x.Faculty.InstitutionId == InsTId.Data
                    || x.InstitutionId == InsTId.Data)
                    ;
                if (search.IsActive.HasValue)
                {

                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (search.FacultyId.HasValue)
                {

                    records = records.Where(x => x.FacultyId == search.FacultyId.Value);
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
                var result = await records.Select(query => new DepartmentDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                    FacultyDetails = new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                    }
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<DepartmentDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<DepartmentDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<DepartmentDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Departments
                    .Include(x => x.FacultyId)
                    .FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<DepartmentDto>.ErrorOccured("Invalid Department Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new DepartmentDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                    FacultyDetails = new FacultyDto
                    {
                        Id = query.Faculty.Id,
                        Name = query.Faculty.Name,
                    }
                };
                return CustomResult<DepartmentDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<DepartmentDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Update(long id, DepartmentCreateModel model, string createdBy)
        {
            try
            {
                var query = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<string>.ErrorOccured("Invalid Department Id!", ResponseCodes.BadRequestErrorCode);
                }
                var checkFaculty = await _context.Faculties.FirstOrDefaultAsync(x => x.Id == model.FacultyId);
                if (checkFaculty == null)
                {
                    return CustomResult<string>.ErrorOccured("Invalid Faculty Id!", ResponseCodes.BadRequestErrorCode);
                }

                query.Name = model.Name;
                query.FacultyId = model.FacultyId;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Department", $"User [{createdBy}] update a department - {query.Name} at {DateTime.UtcNow}.");
                //return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Department updated successfully");
                return CustomResult<string>.Success($"Department updated successfully", $"Department updated successfully");
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
                var details = await _context.Departments.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Department not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Departments.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Department", $"User [{createdBy}], {message} department at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Department {message} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}