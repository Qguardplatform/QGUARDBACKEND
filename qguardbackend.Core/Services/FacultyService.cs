using DocumentFormat.OpenXml.Drawing.Charts;
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
    public class FacultyService : IFacultyService
    {
        private readonly ILogger<FacultyService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;

        public FacultyService(ILogger<FacultyService> logger,
            IAuditLogService auditLogService,
            IUserManagementService userManagementService,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }
        public async Task<CustomResult<FacultyDto>> Create(FalcultyCreateModel model, string createdBy)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var tenantIdResult = await _userManagementService.GetTenantId();
                    if (!tenantIdResult.IsSuccess)
                        return CustomResult<FacultyDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                    var check = await _context.Faculties.FirstOrDefaultAsync(x => x.Name.ToLower() == model.Name.ToLower()
                    && x.InstitutionId == model.InstitutionId
                    );
                    if (check is not null)
                    {
                        return CustomResult<FacultyDto>.ErrorOccured("Faculty name already exist for the Institution", ResponseCodes.AlreadyExistErrorCode);
                    }
                    var newcreate = new Faculty
                    {
                        Name = model.Name,
                        InstitutionId = model.InstitutionId,
                        IsActive = true,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        IsDeleted = false,
                    };
                    var create = await _context.Faculties.AddAsync(newcreate);
                    await _context.SaveChangesAsync();

                    //create the Faculty programs
                    if (model.ProgramIds.Count > 0)
                    {
                        foreach (var program in model.ProgramIds)
                        {
                            var facultyProgram = FacultyProgram.Create(create.Entity.Id, program.ProgramId, tenantIdResult.Data);
                            await _context.FacultyPrograms.AddAsync(facultyProgram);
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Faculty", $"User [{createdBy}] created faculty - {model.Name} at {DateTime.UtcNow}.");
                    var returnDto = await this.GetById(create.Entity.Id);
                    return returnDto;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<FacultyDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
                }
            });
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var type = await _context.Faculties.FirstOrDefaultAsync(x => x.Id == id);
                if (type == null)
                {
                    return CustomResult<string>.ErrorOccured("Faculty not found!", ResponseCodes.NotFoundErrorCode);
                }
                type.IsActive = false;
                _context.Update(type);
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Faculty", $"User [{createdBy}] deleted a faculty - {type.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Faculty successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<FacultyDto>>> GetAll(FacultyQueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<FacultyDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Faculty> records = _context.Faculties
                    .Where(x => x.InstitutionId.Value == InsTId.Data);

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


                var result = new List<FacultyDto>();

                result = await records.Select(query => new FacultyDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                    FacultyPrograms = _context.FacultyPrograms
                                       .Include(x => x.Program)
                                       .Where(x => x.FacultyId == query.Id).Select(a => new ProgramModel
                                       {

                                           Id = a.Program.Id,
                                           Name = a.Program.Name,
                                           IsActive = a.IsActive,
                                           ProgramType = a.Program.ProgramType

                                       }).ToList()
                }).ToListAsync();

                if (search.ProgramId.HasValue)
                {
                    result = result.Where(x => x.FacultyPrograms.Any(a => a.Id == search.ProgramId)).ToList();
                    //result = result.Where(x => x.FacultyPrograms.Any(a => a.ProgramType == search.ProgramId)).ToList();
                }

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<FacultyDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<FacultyDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<FacultyDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Faculties.FirstOrDefaultAsync(c => c.Id == id);

                //get all programs
                var prog = await _context.FacultyPrograms.Where(x => x.FacultyId == id).ToListAsync();
                var programDetail = await _context.FacultyPrograms
                     .Include(x => x.Program)
                     .Where(x => x.FacultyId == id).ToListAsync();

                var facultyProgram = new List<ProgramModel>();
                foreach (var program in programDetail)
                {
                    var programModel = new ProgramModel
                    {
                        Id = program.Program.Id,
                        Name = program.Program.Name,
                        IsActive = program.IsActive,
                    };
                    facultyProgram.Add(programModel);
                }

                if (query == null)
                {
                    return CustomResult<FacultyDto>.ErrorOccured("Invalid Faculty Id!", ResponseCodes.BadRequestErrorCode);
                }
                var model = new FacultyDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    DateCreated = query.CreatedAt,
                    IsActive = query.IsActive,
                    FacultyPrograms = facultyProgram
                };
                return CustomResult<FacultyDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<FacultyDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        private async Task PerformErase(long id)
        {
            var facultyProgram = await _context.FacultyPrograms.Where(x => x.FacultyId == id).ToListAsync();
            if (facultyProgram.Any())
            {
                foreach (var delete in facultyProgram)
                {
                    _context.FacultyPrograms.Remove(delete);
                }

                //_context.FacultyPrograms.RemoveRange(facultyProgram);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<CustomResult<List<MigrationErrorVM>>> Update(long id, FalcultyCreateModel model, string createdBy)
        {
            var errorList = new List<MigrationErrorVM>();
            try
            {
                var tenantIdResult = await _userManagementService.GetTenantId();
                if (!tenantIdResult.IsSuccess)
                    return CustomResult<List<MigrationErrorVM>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var query = await _context.Faculties.FirstOrDefaultAsync(x => x.Id == id);
                if (query == null)
                {
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Invalid Faculty Id!", ResponseCodes.BadRequestErrorCode);
                }

                query.Name = model.Name;
                query.UpdatedAt = DateTime.UtcNow;

                _context.Update(query);

                if (model.ProgramIds.Count > 0)
                {
                    var oldFacultyProgram = await _context.FacultyPrograms.Where(x => x.FacultyId == id).ToListAsync();
                    if (oldFacultyProgram.Count > 0)
                    {
                        await PerformErase(id);


                        foreach (var item in model.ProgramIds)
                        {
                            var program = await _context.Programs.FirstOrDefaultAsync(p => p.Id == item.ProgramId);
                            if (program == null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = 1,
                                    ErrorMessage = $"Invalid program id - {item.ProgramId}"
                                });
                                continue;
                            }
                            var facultyProgram = FacultyProgram.Create(id, program.Id, tenantIdResult.Data);
                            _context.FacultyPrograms.Add(facultyProgram);
                            await _context.SaveChangesAsync();
                        }
                    }
                    else
                    {
                        foreach (var item in model.ProgramIds)
                        {
                            var program = await _context.Programs.FirstOrDefaultAsync(p => p.Id == item.ProgramId);
                            if (program == null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = 1,
                                    ErrorMessage = $"Invalid program id - {item.ProgramId}"
                                });
                                continue;
                            }
                            var checkOnFacultyProgram = await _context.FacultyPrograms.FirstOrDefaultAsync(c => c.FacultyId == id && c.ProgramId == item.ProgramId);
                            if (checkOnFacultyProgram != null)
                            {
                                errorList.Add(new MigrationErrorVM
                                {
                                    RowNum = 1,
                                    ErrorMessage = $"Program with Id - ({item.ProgramId}) already exist for this faculty!"
                                });
                                continue;
                            }
                            var facultyProgram = FacultyProgram.Create(id, program.Id, tenantIdResult.Data);
                            _context.FacultyPrograms.Add(facultyProgram);
                            await _context.SaveChangesAsync();
                        }
                    }
                }
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Faculty", $"User [{createdBy}] update a faculty - {query.Name} at {DateTime.UtcNow}.");

                if (errorList.Count > 0)
                {
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured(errorList, $"An error occurred while trying to update faculty", ResponseCodes.BadRequestErrorCode);
                }
                else
                {
                    return CustomResult<List<MigrationErrorVM>>.Success(errorList, $"Faculty updated successfully");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<List<MigrationErrorVM>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Faculties.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Faculty not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Faculties.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Faculty", $"User [{createdBy}], {message} faculty at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Faculty {message} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}
