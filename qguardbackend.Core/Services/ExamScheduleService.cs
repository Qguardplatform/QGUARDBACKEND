using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using examportal.Api.ServiceExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using SISService.BoilerPlate.Service.Interfaces;
using System.Collections.Concurrent;

namespace qguardbackend.Core.Services
{
    public class ExamScheduleService : IExamScheduleService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExamScheduleService> _logger;
        private readonly IS3Service _s3Service;
        private readonly IExamService _examService;
        private readonly IUserManagementService _userManagementService;
        private readonly IAuditLogService _auditLogService;
        private readonly IServiceProvider _serviceProvider;
        public ExamScheduleService(AppDbContext context,
                IAuditLogService auditLogService,
                IUserManagementService userManagementService,
                ILogger<ExamScheduleService> logger, IExamService examService,
                IServiceProvider serviceProvider,
                IS3Service s3Service)
        {
            _context = context;
            _logger = logger;
            _s3Service = s3Service;
            _examService = examService;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
            _serviceProvider = serviceProvider;
        }

        public async Task<CustomResult<ExamScheduleDto>> CreateAsync(ExamScheduleCreateDto model, string createdBy)
        {
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<ExamScheduleDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                try
                {
                    if (string.IsNullOrWhiteSpace(model.Title))
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Title text is required.", ResponseCodes.BadRequestErrorCode);

                    if (string.IsNullOrWhiteSpace(model.Description))
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Description is required.", ResponseCodes.BadRequestErrorCode);

                    var institution = await _context.Institutions.FirstOrDefaultAsync(x => x.Id == InsTId.Data);
                    if (institution is null)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid institution id passed!.", ResponseCodes.BadRequestErrorCode);

                    var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == model.CourseId);
                    if (course is null)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid course id passed!.", ResponseCodes.BadRequestErrorCode);

                    var session = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == model.AcademicSessionId);
                    if (session is null)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid session id passed!.", ResponseCodes.BadRequestErrorCode);

                    var semester = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == model.SemesterId);
                    if (semester is null)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid semester id passed!.", ResponseCodes.BadRequestErrorCode);

                    var level = await _context.Levels.FirstOrDefaultAsync(x => x.Id == model.LevelId);
                    if (level is null)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid level id passed!.", ResponseCodes.BadRequestErrorCode);

                    if (model.PassScore <= 0)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Passing score is required!.", ResponseCodes.BadRequestErrorCode);

                    if (model.MaxQuestions <= 0)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Maximum number of question is required!.", ResponseCodes.BadRequestErrorCode);

                    if (model.ExamDurationInMinutes <= 0)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Exam duration is required!.", ResponseCodes.BadRequestErrorCode);

                    // Date and Time Validations
                    var today = DateTime.Today;
                    if (model.StartDate.Date < today)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Start date must be today or later.", ResponseCodes.BadRequestErrorCode);

                    if (model.EndDate.HasValue && model.EndDate.Value.Date < model.StartDate.Date)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("End date cannot be earlier than start date.", ResponseCodes.BadRequestErrorCode);

                    if (model.EndTime.HasValue && model.StartTime >= model.EndTime.Value && model.StartDate.Date == model.EndDate?.Date)
                        return CustomResult<ExamScheduleDto>.ErrorOccured("Start time must be before end time on the same day.", ResponseCodes.BadRequestErrorCode);

                    Tutor tutor = null;
                    if (model.AssignProctorId > 0)
                    {
                        tutor = await _context.Tutors.FirstOrDefaultAsync(x => x.Id == model.AssignProctorId);
                        if (tutor == null)
                            return CustomResult<ExamScheduleDto>.ErrorOccured("Invalid Assign Proctor Id.", ResponseCodes.BadRequestErrorCode);
                    }

                    var schedule = new ExamSchedule
                    {
                        Title = model.Title,
                        Description = model.Description,
                        AcademicSessionId = model.AcademicSessionId,
                        SemesterId = model.SemesterId,
                        CourseId = model.CourseId,
                        InstitutionId = InsTId.Data,
                        MaxQuestions = model.MaxQuestions,
                        PassScore = model.PassScore,
                        ExamDurationInMinutes = model.ExamDurationInMinutes,
                        IsRandomized = model.IsRandomized,
                        EnableAiProctoring = model.EnableAiProctoring,
                        LevelId = model.LevelId,
                        StartDate = Utility.ToUtc(model.StartDate).Value,
                        EndDate = Utility.ToUtc(model.EndDate),
                        StartTime = Utility.ToUtc(model.StartTime).Value,
                        EndTime = Utility.ToUtc(model.EndTime),
                        AllowLateStart = model.AllowLateStart,
                        Status = model.Status.GetEnumText(),
                        CreatedAt = DateTime.UtcNow,
                        InstantAnswerGrading = model.InstantAnswerGrading,
                        InstantResultPublishing = model.InstantResultPublishing,
                        IsPublished = model.InstantResultPublishing,
                        //AssignProctor = tutor?.Id,
                        AssignProctorId = tutor?.Id
                    };

                    var examScheduleDetails = _context.ExamSchedules.Add(schedule).Entity;
                    var saved = await _context.SaveChangesAsync();

                    //add back the departments
                    if (model.DepartmentIds != null && model.DepartmentIds.Count > 0)
                    {
                        foreach (var departmentId in model.DepartmentIds)
                        {
                            var department = await _context.Departments.FirstOrDefaultAsync(x => x.Id == departmentId);
                            if (department != null)
                            {
                                var departmentExamSchedule = new DepartmentExamSchedule
                                {
                                    DepartmentId = departmentId,
                                    ExamScheduleId = examScheduleDetails.Id
                                };
                                await _context.DepartmentExamSchedules.AddAsync(departmentExamSchedule);
                            }
                        }
                    }
                    var updated = await _context.SaveChangesAsync();

                    // Save AI Proctoring Penalty Rules if enabled
                    if (model.EnableAiProctoring && model.PenaltyRules.Any())
                    {
                        var savePenaltyRulesResult = await SavePenaltyRulesAsync(model.PenaltyRules, examScheduleDetails.Id);
                        if (!savePenaltyRulesResult.IsSuccess)
                            return CustomResult<ExamScheduleDto>.ErrorOccured(savePenaltyRulesResult.Message, ResponseCodes.BadRequestErrorCode);
                    }

                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Exam Schedule", $"User [{createdBy}] created an exam schedule at {DateTime.UtcNow}.");

                    var dto = new ExamScheduleDto
                    {
                        Id = examScheduleDetails.Id,
                        Title = examScheduleDetails.Title,
                        Description = examScheduleDetails.Description,
                        AcademicSession = examScheduleDetails.AcademicSession.Name,
                        Semester = examScheduleDetails.Semester.SemesterName,
                        Course = examScheduleDetails.Course.Name,
                        CourseCode = examScheduleDetails.Course.CourseCode,
                        Institution = examScheduleDetails.Institution.Name,
                        MaxQuestions = examScheduleDetails.MaxQuestions.Value,
                        PassScore = examScheduleDetails.PassScore.Value,
                        ExamDurationInMinutes = examScheduleDetails.ExamDurationInMinutes.Value,
                        IsRandomized = examScheduleDetails.IsRandomized,
                        EnableAiProctoring = examScheduleDetails.EnableAiProctoring,
                        Level = examScheduleDetails.Level.LevelName,
                        StartDate = Utility.ToLocal(examScheduleDetails.StartDate).Value,
                        EndDate = Utility.ToLocal(examScheduleDetails.EndDate),
                        StartTime = Utility.ToLocal(examScheduleDetails.StartTime).Value,
                        EndTime = Utility.ToLocal(examScheduleDetails.EndTime),
                        AllowLateStart = examScheduleDetails.AllowLateStart,
                        Status = examScheduleDetails.Status,
                        InstantResultPublishing = examScheduleDetails.InstantResultPublishing,
                        InstantAnswerGrading = examScheduleDetails.InstantAnswerGrading,
                        Departments = _context.DepartmentExamSchedules
                                     .Where(x => x.ExamScheduleId == examScheduleDetails.Id)
                                     .Select(x => x.Department.Name).ToList()
                    };
                    return saved > 0
                         ? CustomResult<ExamScheduleDto>.Success(dto, "Exam schedule created successfully")
                         : CustomResult<ExamScheduleDto>.ErrorOccured("Failed to create exam schedule.", ResponseCodes.BadRequestErrorCode);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, ex.Message);
                    return CustomResult<ExamScheduleDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
                }
            });
        }

        private async Task<CustomResult<bool>> SavePenaltyRulesAsync(List<PenaltyRuleCreateModel> rules, long examScheduleId)
        {
            if (rules == null || !rules.Any())
                return CustomResult<bool>.Success(true, ResponseCodes.SuccessCode);

            var errors = new List<string>();
            var configurations = new List<ProctorConfiguration>();

            foreach (var rule in rules)
            {
                // Fetch Enforcement Mode Name
                var enforcementMode = await _context.EnforcementModes
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == rule.EnforcementModeId);

                if (enforcementMode == null)
                {
                    errors.Add($"Invalid EnforcementModeId ({rule.EnforcementModeId}) provided.");
                }
                else
                {
                    // Save Mode Threshold as a key–value pair
                    configurations.Add(ProctorConfiguration.Create(
                        enforcementMode.Name, rule.Threshold.ToString(), examScheduleId
                    ));
                }

                // Fetch Enforcement Action Name
                var enforcementAction = await _context.EnforcementActions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(x => x.Id == rule.EnforcementActionId);

                if (enforcementAction == null)
                {
                    errors.Add($"Invalid EnforcementActionId ({rule.EnforcementActionId}) provided.");
                }
                else
                {
                    // Save Action Deduct as a key–value pair
                    configurations.Add(ProctorConfiguration.Create(
                        enforcementAction.Name, rule.Deduct, examScheduleId
                    ));
                }
            }

            if (configurations.Any())
            {
                await _context.ProctorConfigurations.AddRangeAsync(configurations);
                await _context.SaveChangesAsync();
            }

            // Prepare response
            if (errors.Any())
            {
                var message = "Some rules could not be validated: " + string.Join(" | ", errors);
                return CustomResult<bool>.ErrorOccured(message, ResponseCodes.BadRequestErrorCode);
            }

            return CustomResult<bool>.Success(true, ResponseCodes.SuccessCode);
        }

        public async Task<CustomResult<string>> DeleteAsync(long id, string createdBy)
        {
            var existing = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == id);
            if (existing == null)
                return CustomResult<string>.ErrorOccured("Exam schedule not found.", ResponseCodes.BadRequestErrorCode);
            existing.IsDeleted = true;
            existing.IsActive = false;
            //_context.ExamSchedules.Remove(existing);
            _context.ExamSchedules.Update(existing);
            var deleted = await _context.SaveChangesAsync();
            await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Exam schedule", $"User [{createdBy}] deleted an exam schedule - {existing.Description} at {DateTime.UtcNow}.");
            return deleted > 0
                ? CustomResult<string>.Success(ResponseCodes.SuccessCode, "Exam schedule deleted/disable.")
                : CustomResult<string>.ErrorOccured("Failed to delete exam schedule.", ResponseCodes.BadRequestErrorCode);
        }

        public async Task<CustomResult<PaginatedResult<ExamScheduleDto>>> GetAllAsync(ExamSchedulenFilterModel query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<ExamScheduleDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<ExamSchedule> records = _context.ExamSchedules
                                    .Include(x => x.AcademicSession)
                                    .Include(x => x.Semester)
                                    .Include(x => x.Course)
                                    .Include(x => x.Institution)
                                    .Include(x => x.Level)
                                    .Include(x => x.ProctorConfigurations)
                                    .Where(x => x.InstitutionId == InsTId.Data
                                    && !x.IsDeleted
                                    );

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(q =>
                        q.Title.ToLower().Contains(word) ||
                        q.Description.ToLower().Contains(word) ||
                        q.Course.Name.ToLower().Contains(word) ||
                        q.Institution.Name.ToLower().Contains(word) ||
                        q.Semester.SemesterName.ToLower().Contains(word) ||
                        q.AcademicSession.Name.ToLower().Contains(word) ||
                        q.Level.LevelName.ToLower().Contains(word));
                }

                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= query.StartDate && x.CreatedAt <= endDate);
                }
                if (query.SessionId.HasValue)
                {
                    records = records.Where(x => x.AcademicSessionId == query.SessionId.Value);
                }
                if (query.SemesterId.HasValue)
                {
                    records = records.Where(x => x.SemesterId == query.SemesterId.Value);
                }
                if (query.LevelId.HasValue)
                {
                    records = records.Where(x => x.LevelId == query.LevelId.Value);
                }
                if (query.Status.HasValue)
                {
                    records = records.Where(x => x.Status == query.Status.Value.GetEnumText());
                }
                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(x => x.Id)
                    : records.OrderByDescending(x => x.CreatedAt);

                var result = await records.Select(entity => new ExamScheduleDto
                {
                    Id = entity.Id,
                    Title = entity.Title,
                    IsActive = entity.IsActive,
                    IsDeleted = entity.IsDeleted,
                    Description = entity.Description,
                    AcademicSession = entity.AcademicSession.Name,
                    Semester = entity.Semester.SemesterName,
                    Course = entity.Course.Name,
                    CourseCode = entity.Course.CourseCode,
                    Institution = entity.Institution.Name,
                    MaxQuestions = entity.MaxQuestions.Value,
                    PassScore = entity.PassScore.Value,
                    ExamDurationInMinutes = entity.ExamDurationInMinutes.Value,
                    IsRandomized = entity.IsRandomized,
                    EnableAiProctoring = entity.EnableAiProctoring,
                    Level = entity.Level.LevelName,
                    StartDate = Utility.ToLocal(entity.StartDate).Value,
                    EndDate = Utility.ToLocal(entity.EndDate),
                    StartTime = Utility.ToLocal(entity.StartTime).Value,
                    EndTime = Utility.ToLocal(entity.EndTime),
                    AllowLateStart = entity.AllowLateStart,
                    TotalQuestionsOnExam = _context.ExamQuestions
                        .Count(x => x.ExamScheduleId == entity.Id && !x.IsDeleted),
                    Status = entity.Status,
                    InstantResultPublishing = entity.InstantResultPublishing,
                    InstantAnswerGrading = entity.InstantAnswerGrading,
                    Departments = _context.DepartmentExamSchedules
                        .Where(x => x.ExamScheduleId == entity.Id)
                        .Select(x => x.Department.Name).ToList(),
                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ExamScheduleDto>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ExamScheduleDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<ExamScheduleDto>>> GetAllExamScheduledByTimingAsync(ExamSchedulenDashboardFilterModel query, string candidateUserId)
        {

            var result = new ConcurrentBag<ExamScheduleDto>();
            try
            {
                var tenantIdResult = await _userManagementService.GetTenantId();
                if (!tenantIdResult.IsSuccess)
                    return CustomResult<PaginatedResult<ExamScheduleDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var userData = await _userManagementService.GetUserDetailsByIdAsync(candidateUserId);
                if (userData.Data == null)
                {
                    _logger.LogError($"Candidate with user Id {candidateUserId} does not exist");
                    return CustomResult<PaginatedResult<ExamScheduleDto>>.Failure(CustomError.UnableToRetrieveUserProfile, ResponseCodes.NotFoundErrorCode);
                }

                var now = DateTime.UtcNow;

                var candidate = await _context.Candidates
                    .FirstOrDefaultAsync(x => x.UserId == candidateUserId && x.InstitutionId == tenantIdResult.Data);

                if (candidate == null)
                {
                    return CustomResult<PaginatedResult<ExamScheduleDto>>.ErrorOccured("Candidate not found", ResponseCodes.NotFoundErrorCode);
                }

                var currentState = await _context.CandidatesCurrentStates
                                .FirstOrDefaultAsync(x => x.CandidateId == candidate.Id && x.IsCurrent);

                if (currentState == null)
                {
                    _logger.LogError("Candidate does not have any active session, semester and level on the system");
                    return CustomResult<PaginatedResult<ExamScheduleDto>>.Failure(CustomError.UnableToRetrieveUserProfile, ResponseCodes.NotFoundErrorCode);
                }

                IQueryable<ExamSchedule> records = _context.ExamSchedules
                    .Include(x => x.AcademicSession)
                    .Include(x => x.Semester)
                    .Include(x => x.Course)
                    .Include(x => x.Institution)
                    .Include(x => x.Level)
                    .Where(x => x.InstitutionId == tenantIdResult.Data
                        && x.LevelId == candidate.LevelId
                        && x.SemesterId == candidate.SemesterId
                        && x.AcademicSessionId == candidate.SessionId);

                // Apply timing filters correctly
                if (query.Timing == ExamTimingStatusEnum.UPCOMING)
                {
                    records = records.Where(x => x.StartDate > now);
                    //records = records.Where(x =>
                    //            x.StartDate.Add(x.StartTime.TimeOfDay) > now);
                }
                else if (query.Timing == ExamTimingStatusEnum.PAST)
                {
                    records = records.Where(x => x.EndDate < now);
                    //records = records.Where(x =>
                    //            x.EndDate.HasValue &&
                    //            x.EndDate.Value.Add(x.EndTime.Value.TimeOfDay) < now);
                }
                else if (query.Timing == ExamTimingStatusEnum.ONGOING)
                {
                    records = records
                                .AsEnumerable()
                                .Where(x => x.StartDate <= now &&
                                    (x.EndDate.Value.TimeOfDay == TimeSpan.Zero
                                        ? Utility.EndOfDay(x.EndDate.Value)
                                        : x.EndDate) >= now)
                                .AsQueryable();
                }

                var collections = records
                            .OrderByDescending(x => x.Id)
                            .Select(entity => new
                            {
                                Entity = entity,
                                Departments = _context.DepartmentExamSchedules
                                    .Where(d => d.ExamScheduleId == entity.Id)
                                    .Select(d => d.Department.Name)
                                    .ToList()
                            })
                            .ToList();

                Parallel.ForEach(collections, item =>
                {
                    var search = new ExamScheduleDto
                    {
                        Id = item.Entity.Id,
                        Title = item.Entity.Title,
                        Description = item.Entity.Description,
                        AcademicSession = item.Entity.AcademicSession.Name,
                        Semester = item.Entity.Semester.SemesterName,
                        Course = item.Entity.Course.Name,
                        CourseCode = item.Entity.Course.CourseCode,
                        Institution = item.Entity.Institution.Name,
                        MaxQuestions = item.Entity.MaxQuestions ?? 0,
                        PassScore = item.Entity.PassScore ?? 0,
                        ExamDurationInMinutes = item.Entity.ExamDurationInMinutes ?? 0,
                        IsRandomized = item.Entity.IsRandomized,
                        EnableAiProctoring = item.Entity.EnableAiProctoring,
                        Level = item.Entity.Level.LevelName,
                        StartDate = Utility.ToLocal(item.Entity.StartDate).Value,
                        EndDate = Utility.ToLocal(item.Entity.EndDate),
                        StartTime = Utility.ToLocal(item.Entity.StartTime).Value,
                        EndTime = Utility.ToLocal(item.Entity.EndTime),
                        AllowLateStart = item.Entity.AllowLateStart,
                        Status = item.Entity.Status,
                        InstantResultPublishing = item.Entity.InstantResultPublishing,
                        InstantAnswerGrading = item.Entity.InstantAnswerGrading,
                        Departments = item.Departments
                    };
                    result.Add(search);
                });

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.OrderByDescending(x => x.Id).ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.OrderByDescending(x => x.Id).NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ExamScheduleDto>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ExamScheduleDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamScheduleDto>> GetByIdAsync(long id)
        {
            var entity = await _context.ExamSchedules
                                    .Include(x => x.AcademicSession)
                                    .Include(x => x.Semester)
                                    .Include(x => x.Course)
                                    .Include(x => x.Institution)
                                    .Include(x => x.Level)
                                    .Include(x => x.ProctorConfigurations)
                                    .FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null)
                return CustomResult<ExamScheduleDto>.ErrorOccured("Exam schedule not found.", ResponseCodes.BadRequestErrorCode);

            List<string> actionNames = await _context.EnforcementActions.Select(x => x.Name).ToListAsync();

            var proctorRules = new List<ProctorConfigurationRuleDto>();

            var configList = entity.ProctorConfigurations
                    .OrderBy(x => x.Id)
                    .ToList();
            if (configList.Any())
            {
                for (int i = 0; i < configList.Count; i++)
                {
                    var item = configList[i];

                    // It's a violation if not in actionNames
                    if (!actionNames.Contains(item.ParameterName))
                    {
                        // Find the action immediately after this violation
                        var next = configList
                            .Skip(i + 1)
                            .FirstOrDefault(x => actionNames.Contains(x.ParameterName));

                        if (next != null)
                        {
                            proctorRules.Add(new ProctorConfigurationRuleDto
                            {
                                Violation = item.ParameterName,
                                Threshold = item.Value,
                                Action = next.ParameterName,
                                Value = next.Value
                            });
                        }
                    }
                }
            }

            var dto = new ExamScheduleDto
            {
                Id = entity.Id,
                Title = entity.Title,
                Description = entity.Description,
                AcademicSession = entity.AcademicSession.Name,
                Semester = entity.Semester.SemesterName,
                CourseId = entity.Course.Id,
                Course = entity.Course.Name,
                CourseCode = entity.Course.CourseCode,
                Institution = entity.Institution.Name,
                MaxQuestions = entity.MaxQuestions.Value,
                PassScore = entity.PassScore.Value,
                ExamDurationInMinutes = entity.ExamDurationInMinutes.Value,
                IsRandomized = entity.IsRandomized,
                EnableAiProctoring = entity.EnableAiProctoring,
                Level = entity.Level.LevelName,
                StartDate = Utility.ToLocal(entity.StartDate).Value,
                EndDate = Utility.ToLocal(entity.EndDate),
                StartTime = Utility.ToLocal(entity.StartTime).Value,
                EndTime = Utility.ToLocal(entity.EndTime),
                AllowLateStart = entity.AllowLateStart,
                Status = entity.Status,
                InstantResultPublishing = entity.InstantResultPublishing,
                InstantAnswerGrading = entity.InstantAnswerGrading,
                AssignProctorId = (entity.AssignProctorId.HasValue) ? entity.AssignProctorId.Value : 0,
                ProctorConfigurations = proctorRules,
                Departments = _context.DepartmentExamSchedules
                        .Where(x => x.ExamScheduleId == entity.Id)
                        .Select(x => x.Department.Name).ToList()
            };
            return CustomResult<ExamScheduleDto>.Success(dto);
        }

        public async Task<CustomResult<string>> UpdateAsync(long id, ExamScheduleCreateDto model, string createdBy)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var existing = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == id);
                if (existing == null)
                    return CustomResult<string>.ErrorOccured("Exam schedule not found.", ResponseCodes.BadRequestErrorCode);

                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                if (string.IsNullOrWhiteSpace(model.Title))
                    return CustomResult<string>.ErrorOccured("Title text is required.", ResponseCodes.BadRequestErrorCode);

                if (string.IsNullOrWhiteSpace(model.Description))
                    return CustomResult<string>.ErrorOccured("Description is required.", ResponseCodes.BadRequestErrorCode);

                var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == model.CourseId);
                if (course is null)
                    return CustomResult<string>.ErrorOccured("Invalid course id passed!.", ResponseCodes.BadRequestErrorCode);

                var session = await _context.Sessions.FirstOrDefaultAsync(x => x.Id == model.AcademicSessionId);
                if (session is null)
                    return CustomResult<string>.ErrorOccured("Invalid session id passed!.", ResponseCodes.BadRequestErrorCode);

                var semester = await _context.Semesters.FirstOrDefaultAsync(x => x.Id == model.SemesterId);
                if (semester is null)
                    return CustomResult<string>.ErrorOccured("Invalid semester id passed!.", ResponseCodes.BadRequestErrorCode);

                var level = await _context.Levels.FirstOrDefaultAsync(x => x.Id == model.LevelId);
                if (level is null)
                    return CustomResult<string>.ErrorOccured("Invalid level id passed!.", ResponseCodes.BadRequestErrorCode);

                if (model.PassScore <= 0)
                    return CustomResult<string>.ErrorOccured("Passing score is required!.", ResponseCodes.BadRequestErrorCode);

                if (model.MaxQuestions <= 0)
                    return CustomResult<string>.ErrorOccured("Maximum number of question is required!.", ResponseCodes.BadRequestErrorCode);

                if (model.ExamDurationInMinutes <= 0)
                    return CustomResult<string>.ErrorOccured("Exam duration is required!.", ResponseCodes.BadRequestErrorCode);

                // Date and Time Validations
                var today = DateTime.Today;
                if (model.StartDate.Date < today)
                    return CustomResult<string>.ErrorOccured("Start date must be today or later.", ResponseCodes.BadRequestErrorCode);

                if (model.EndDate.HasValue && model.EndDate.Value.Date < model.StartDate.Date)
                    return CustomResult<string>.ErrorOccured("End date cannot be earlier than start date.", ResponseCodes.BadRequestErrorCode);

                if (model.EndTime.HasValue && model.StartTime >= model.EndTime.Value && model.StartDate.Date == model.EndDate?.Date)
                    return CustomResult<string>.ErrorOccured("Start time must be before end time on the same day.", ResponseCodes.BadRequestErrorCode);

                existing.Title = model.Title;
                existing.Description = model.Description;
                existing.AcademicSessionId = model.AcademicSessionId;
                existing.SemesterId = model.SemesterId;
                existing.CourseId = model.CourseId;
                existing.MaxQuestions = model.MaxQuestions;
                existing.PassScore = model.PassScore;
                existing.ExamDurationInMinutes = model.ExamDurationInMinutes;
                existing.IsRandomized = model.IsRandomized;
                existing.EnableAiProctoring = model.EnableAiProctoring;
                existing.LevelId = model.LevelId;
                existing.StartDate = Utility.ToLocal(model.StartDate).Value;
                existing.EndDate = Utility.ToLocal(model.EndDate);
                existing.StartTime = Utility.ToLocal(model.StartTime).Value;
                existing.EndTime = Utility.ToLocal(model.EndTime);
                existing.AllowLateStart = model.AllowLateStart;
                existing.Status = model.Status.GetEnumText();
                existing.InstantAnswerGrading = model.InstantAnswerGrading;
                existing.IsDeleted = model.Status.GetEnumText() == ExamScheduleStatusEnum.PUBLISHED.GetEnumText() ? false : existing.IsDeleted;
                existing.InstantResultPublishing = model.InstantResultPublishing;
                existing.UpdatedAt = DateTime.UtcNow;

                if (model.EnableAiProctoring && model.PenaltyRules.Any())
                {
                    var oldConfig = await _context.ProctorConfigurations.Where(x => x.ExamScheduleId == id).ToListAsync();
                    if (oldConfig.Count > 0)
                    {
                        _context.ProctorConfigurations.RemoveRange(oldConfig);

                        var savePenaltyRulesResult = await SavePenaltyRulesAsync(model.PenaltyRules, id);
                        if (!savePenaltyRulesResult.IsSuccess)
                        {
                            await transaction.RollbackAsync();
                            return CustomResult<string>.ErrorOccured(savePenaltyRulesResult.Message, ResponseCodes.BadRequestErrorCode);
                        }
                    }
                    else
                    {
                        var savePenaltyRulesResult = await SavePenaltyRulesAsync(model.PenaltyRules, id);
                        if (!savePenaltyRulesResult.IsSuccess)
                        {
                            await transaction.RollbackAsync();
                            return CustomResult<string>.ErrorOccured(savePenaltyRulesResult.Message, ResponseCodes.BadRequestErrorCode);
                        }
                    }
                }

                var removedDepartments = await _context.DepartmentExamSchedules
                    .Where(x => x.ExamScheduleId == id)
                    .ToListAsync();
                if (removedDepartments != null)
                {
                    _context.DepartmentExamSchedules.RemoveRange(removedDepartments);
                }

                //add back the departments
                if (model.DepartmentIds != null && model.DepartmentIds.Count > 0)
                {
                    foreach (var departmentId in model.DepartmentIds)
                    {
                        var department = await _context.Departments.FirstOrDefaultAsync(x => x.Id == departmentId);
                        if (department != null)
                        {
                            var departmentExamSchedule = new DepartmentExamSchedule
                            {
                                DepartmentId = departmentId,
                                ExamScheduleId = id
                            };
                            await _context.DepartmentExamSchedules.AddAsync(departmentExamSchedule);
                        }
                    }
                }
                var updated = await _context.SaveChangesAsync();
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Exam schedule", $"User [{createdBy}] update an exam schedule at {DateTime.UtcNow}.");

                _logger.LogInformation($"The Exam schedule has been updated with status: {model.Status.GetEnumText()}, exam schedule updated title: {model.Title}");
                await transaction.CommitAsync();
                return updated > 0
                    ? CustomResult<string>.Success(ResponseCodes.SuccessCode, "Exam schedule updated.")
                    : CustomResult<string>.ErrorOccured("Failed to update exam schedule.", ResponseCodes.BadRequestErrorCode);
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> ChangeExamScheduleStatus(long id, ExamScheduleStatusEnum status, string createdBy)
        {
            try
            {
                var details = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == id);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Exam schedule not found!", ResponseCodes.NotFoundErrorCode);
                }
                details.Status = status.GetEnumText();
                _context.ExamSchedules.Update(details);
                await _context.SaveChangesAsync();

                string message = $"Exam schedule status changed to {status.GetEnumText()}";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Exam schedule", $"User [{createdBy}], {message} exam schedule at {DateTime.UtcNow}.");

                if (details.Status.ToLower() == "published")
                {
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            using var scope = _serviceProvider.CreateScope();
                            var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                            var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                            var candidateIds = await db.CandidatesCurrentStates
                                                    .Where(c => c.LevelId == details.LevelId &&
                                                                c.SemesterId == details.SemesterId &&
                                                                c.SessionId == details.AcademicSessionId &&
                                                                c.InstitutionId == details.InstitutionId)
                                                    .Select(c => c.CandidateId)
                                                    .Distinct()
                                                    .ToListAsync();

                            var candidates = await db.Candidates
                                                .Include(c => c.User)
                                                .Where(c => candidateIds.Contains(c.Id))
                                                .ToListAsync();

                            foreach (var student in candidates)
                            {
                                var email = student.User?.Email;
                                if (string.IsNullOrWhiteSpace(email)) continue;

                                await emailService.SendExamPublishedEmailAsync(new CustomSendExamDto
                                {
                                    CandidateName = $"{student.User.FirstName} {student.User.LastName}",
                                    CandidateEmail = email,
                                    ExamTitle = details.Title,
                                    Date = details.StartDate.ToString("dd-MM-yyyy"),
                                    Time = details.StartTime.ToString("HH:mm")
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error sending exam published emails");
                        }
                    });
                }
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
