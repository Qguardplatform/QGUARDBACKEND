using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace qguardbackend.Core.Services
{
    public class CourseService : ICourseService
    {
        private readonly ILogger<CourseService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        public CourseService(AppDbContext context,
            ILogger<CourseService> logger,
            IUserManagementService userManagementService,
            IAuditLogService auditLogService)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<CourseResponseDto>> Create(CourseRequestDto model, string createdBy)
        {
            var strategy = _context.Database.CreateExecutionStrategy();
            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                try
                {
                    var InsTId = await _userManagementService.GetTenantId();
                    if (!InsTId.IsSuccess)
                    {
                        return CustomResult<CourseResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                    }

                    var check = await _context.Courses
                                            .FirstOrDefaultAsync(x => x.CourseCode.ToLower() == model.CourseCode.ToLower()
                                                        && x.InstitutionId == InsTId.Data);
                    if (check is not null)
                    {
                        return CustomResult<CourseResponseDto>.ErrorOccured("Course Code already exist for the Institution", ResponseCodes.AlreadyExistErrorCode);
                    }
                    var newcreate = new Course
                    {
                        Name = model.Name,
                        InstitutionId = InsTId.Data,
                        CourseCode = model.CourseCode,
                        CreatedAt = DateTime.UtcNow,
                        Description = model.Description,
                        IsActive = true,
                        IsDeleted = false
                    };
                    var create = await _context.Courses.AddAsync(newcreate);
                    await _context.SaveChangesAsync();

                    if (model.TutorsIds is not null && model.TutorsIds.Count > 0)
                    {
                        foreach (var tutor in model.TutorsIds)
                        {
                            await _context.CourseTutors.AddAsync(new CourseTutors
                            {
                                CourseId = create.Entity.Id,
                                TutorId = tutor,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    if (model.LevelsIds is not null && model.LevelsIds.Count > 0)
                    {
                        foreach (var level in model.LevelsIds)
                        {
                            await _context.CourseLevels.AddAsync(new CourseLevel
                            {
                                CourseId = create.Entity.Id,
                                LevelId = level,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow

                            });
                        }
                    }
                    if (model.DepartmentIds is not null && model.DepartmentIds.Count > 0)
                    {
                        foreach (var dept in model.DepartmentIds)
                        {
                            await _context.CourseDepartments.AddAsync(new CourseDepartments
                            {
                                CourseId = create.Entity.Id,
                                DepartmentId = dept,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Course", $"User [{createdBy}] created course - {model.Name} at {DateTime.UtcNow}.");
                    var returnDto = await this.GetById(create.Entity.Id);
                    return returnDto;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<CourseResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
                }
            });
        }
        public async Task<CustomResult<string>> EnableDisableLevel(long id, string createdBy)
        {
            try
            {
                var details = await _context.Courses.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Course not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.IsActive = !details.IsActive;
                _context.Courses.Update(details);
                await _context.SaveChangesAsync();

                string message = (details.IsActive) ? "enabled" : "disabled";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Course", $"User [{createdBy}], {message} course at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Course {message} successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> Delete(long id, string createdBy)
        {
            try
            {
                var courseDetails = await _context.Courses.Where(x => x.Id == id).FirstOrDefaultAsync();

                if (courseDetails == null)
                {
                    return CustomResult<string>.Failure(CustomError.CourseNotExisting, ResponseCodes.BadRequestErrorCode);
                }
                //check if the course has been taken at all or has been mapped to any exams yet

                var checkMapping = await _context.ExamQuestions
                    .Include(x => x.QuestionBank)
                    .Where(x => x.QuestionBank.CourseId == id).FirstOrDefaultAsync();

                if (checkMapping != null)
                {
                    return CustomResult<string>.Failure(CustomError.CourseAlreadyMappedToAnExam, ResponseCodes.BadRequestErrorCode);
                }

                _context.Courses.Remove(courseDetails);
                var courseLevels = await _context.CourseLevels.Where(x => x.CourseId == id).ToListAsync();

                if (courseLevels.Count > 0)
                {
                    _context.CourseLevels.RemoveRange(courseLevels);
                }

                var courseDepartments = await _context.CourseDepartments.Where(x => x.CourseId == id).ToListAsync();

                if (courseDepartments.Count > 0)
                {
                    _context.CourseDepartments.RemoveRange(courseDepartments);
                }

                var courseTutors = await _context.CourseTutors.Where(x => x.CourseId == id).ToListAsync();

                if (courseTutors.Count > 0)
                {
                    _context.CourseTutors.RemoveRange(courseTutors);
                }
                await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Course", $"User [{createdBy}] deleted a course - {courseDetails.Name} at {DateTime.UtcNow}.");
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Course successfully deleted");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<CourseResponseDto>>> GetAll(CourseQueryModelMini search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<CourseResponseDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);
                IQueryable<Course> records = _context.Courses
                    .Include(x => x.Institution)
                    .Where(x=>x.InstitutionId == InsTId.Data)
                    ;
                if (search.IsActive.HasValue)
                {
                    records = records.Where(x => x.IsActive == search.IsActive.Value);
                }
                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x => x.Name.ToLower().Contains(searchWord)
                        || x.CourseCode.ToLower().Contains(searchWord)
                        || x.Description.ToLower().Contains(searchWord)
                        || x.Id.ToString().Contains(searchWord));
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
                var result = await records.Select(query => new CourseResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    CourseCode = query.CourseCode,
                    Description = query.Description,
                    InstitutionName = query.Institution.Name,
                    InstitutionId = query.InstitutionId,
                    IsActive = query.IsActive,
                    LevelsIds = _context.CourseLevels.Where(x => x.CourseId == query.Id).Select(x => x.LevelId.Value).ToList(),
                    TutorsIds = _context.CourseTutors.Where(x => x.CourseId == query.Id).Select(x => x.TutorId.Value).ToList(),
                    DepartmentIds = _context.CourseDepartments.Where(x => x.CourseId == query.Id).Select(x => x.DepartmentId.Value).ToList()
                }).ToListAsync();

                if (search.Level.HasValue)
                {
                    var getLevel = await _context.CourseLevels.Where(x => x.LevelId == search.Level.Value).Select(x => x.CourseId).ToListAsync();

                    result = result.Where(x => getLevel.Contains(x.Id)).ToList();
                }

                if (search.DepartmentId.HasValue)
                {
                    var getDeptId = await _context.CourseDepartments.Where(x => x.DepartmentId == search.DepartmentId.Value).Select(x => x.CourseId).ToListAsync();
                    result = result.Where(x => getDeptId.Contains(x.Id)).ToList();
                }

                if (search.TutorId.HasValue)
                {
                    var getTutors = await _context.CourseTutors.Where(x => x.TutorId == search.TutorId.Value).Select(x => x.CourseId).ToListAsync();
                    result = result.Where(x => getTutors.Contains(x.Id)).ToList();
                }

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CourseResponseDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CourseResponseDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CourseResponseDto>> GetById(long id)
        {
            try
            {
                var query = await _context.Courses.FirstOrDefaultAsync(c => c.Id == id);
                if (query == null)
                {
                    return CustomResult<CourseResponseDto>.Failure(CustomError.CourseNotExisting, ResponseCodes.BadRequestErrorCode);
                }

                var model = new CourseResponseDto
                {
                    Id = query.Id,
                    Name = query.Name,
                    CourseCode = query.CourseCode,
                    IsActive = query.IsActive,
                    Description = query.Description,
                    InstitutionId = query.InstitutionId,
                    LevelsIds = _context.CourseLevels.Where(x => x.CourseId == query.Id).Select(x => x.LevelId.Value).ToList(),
                    TutorsIds = _context.CourseTutors.Where(x => x.CourseId == query.Id).Select(x => x.TutorId.Value).ToList(),
                    DepartmentIds = _context.CourseDepartments.Where(x => x.CourseId == query.Id).Select(x => x.DepartmentId.Value).ToList()

                };
                return CustomResult<CourseResponseDto>.Success(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CourseResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CourseResponseDto>> Update(long id, CourseRequestDto model, string createdBy)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var query = await _context.Courses.FirstOrDefaultAsync(x => x.Id == id);
                    if (query == null)
                    {
                        return CustomResult<CourseResponseDto>.ErrorOccured("Invalid Course Id!", ResponseCodes.BadRequestErrorCode);
                    }

                    query.Name = model.Name;
                    query.Description = model.Description;
                    query.CourseCode = model.CourseCode;
                    query.UpdatedAt = DateTime.UtcNow;

                    _context.Courses.Update(query);

                    //remove all mapping on Course Level
                    var courseLevels = await _context.CourseLevels.Where(x => x.CourseId == id).ToListAsync();

                    if (courseLevels.Count > 0)
                    {
                        _context.CourseLevels.RemoveRange(courseLevels);
                    }

                    if (model.LevelsIds.Count > 0)
                    {
                        foreach (var level in model.LevelsIds)
                        {
                            await _context.CourseLevels.AddAsync(new CourseLevel
                            {
                                CourseId = query.Id,
                                LevelId = level,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }
                    //remove all mapping on Course Dept

                    var courseDepartments = await _context.CourseDepartments.Where(x => x.CourseId == id).ToListAsync();

                    if (courseDepartments.Count > 0)
                    {
                        _context.CourseDepartments.RemoveRange(courseDepartments);
                    }


                    if (model.DepartmentIds.Count > 0)
                    {
                        foreach (var deptId in model.DepartmentIds)
                        {
                            await _context.CourseDepartments.AddAsync(new CourseDepartments
                            {
                                CourseId = query.Id,
                                DepartmentId = deptId,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    //remove all mapping on Course tutor
                    var courseTutors = await _context.CourseTutors.Where(x => x.CourseId == id).ToListAsync();

                    if (courseTutors.Count > 0)
                    {
                        _context.CourseTutors.RemoveRange(courseTutors);
                    }

                    if (model.TutorsIds.Count > 0)
                    {
                        foreach (var tutor in model.TutorsIds)
                        {
                            await _context.CourseTutors.AddAsync(new CourseTutors
                            {
                                CourseId = query.Id,
                                TutorId = tutor,
                                CreatedAt = DateTime.UtcNow,
                                IsActive = true,
                                IsDeleted = false,
                                UpdatedAt = DateTime.UtcNow
                            });
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Course", $"User [{createdBy}] update a course - {query.Name} at {DateTime.UtcNow}.");
                    var returnDto = await this.GetById(query.Id);
                    return returnDto;
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<CourseResponseDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
                }
            });
        }
    }
}