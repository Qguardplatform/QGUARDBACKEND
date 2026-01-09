using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using LS1_Backend.LS1.Shared.Helpers;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System.Text;

namespace qguardbackend.Core.Services
{
    public class ProctorService : IProctorService
    {
        public static readonly string _proctorMe = "ProctorMe";
        private readonly ILogger<ProctorService> _logger;
        private readonly AppDbContext _context;
        private readonly IAuditLogService _auditLogService;
        private readonly ISettingsService _settingsService;
        private readonly IServiceProvider _serviceProvider;
        private readonly IUserManagementService _userManagementService;

        public ProctorService(ILogger<ProctorService> logger,
            IUserManagementService userManagementService,
            IAuditLogService auditLogService,
            ISettingsService settingsService,
            IServiceProvider serviceProvider,
            AppDbContext context)
        {
            _logger = logger;
            _context = context;
            _auditLogService = auditLogService;
            _settingsService = settingsService;
            _serviceProvider = serviceProvider;
            _userManagementService = userManagementService;
        }

        private async Task<Dictionary<string, string>> GetProctorSettings(string name)
        {
            try
            {
                var settings = await _settingsService.GetConfigurationSettings(name);

                return settings;
            }
            catch (Exception ex)
            {
                throw new Exception($"{ex.Message}");
            }
        }

        private string GetSettingsValue(Dictionary<string, string> settings, string key)
        {
            var result = settings.Where(p => p.Key.ToLower() == key.ToLower()).Select(p => new { setting = p.Value }).FirstOrDefault();
            if (result == null) throw new Exception($"settings was found!");

            return result.setting.ToString();
        }

        public async Task<CustomResult<string>> ProcessProctorFlagAsync(ProctorWebhookPayload payload)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(payload.FlagId))
                    return CustomResult<string>.ErrorOccured("FlagId is required.", ResponseCodes.BadRequestErrorCode);

                if (string.IsNullOrEmpty(payload.CandidateId))
                    return CustomResult<string>.ErrorOccured("Candidate Id is required.", ResponseCodes.BadRequestErrorCode);

                if (string.IsNullOrEmpty(payload.ExamId))
                    return CustomResult<string>.ErrorOccured("ExamId Id is required.", ResponseCodes.BadRequestErrorCode);

                if (string.IsNullOrEmpty(payload.Type))
                    return CustomResult<string>.ErrorOccured("Enforcement Type is required.", ResponseCodes.BadRequestErrorCode);

                // convert exam id to int
                long examId = Helper.ToLongOrThrow(payload.ExamId, "Exam ID");

                // Check for duplicate flag
                var existingFlag = await _context.CandidateProctorLogs.FirstOrDefaultAsync(x => x.FlagId == payload.FlagId);
                if (existingFlag != null)
                {
                    _logger.LogWarning($"Duplicate flag received: {payload.FlagId}");
                    return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Flag already processed.");
                }

                var candidate = await _context.Candidates.FirstOrDefaultAsync(x => x.UserId == payload.CandidateId);
                if (candidate is null)
                {
                    return CustomResult<string>.ErrorOccured("Candidate details not found.", ResponseCodes.BadRequestErrorCode);
                }

                var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == examId);
                if (examSchedule is null)
                {
                    return CustomResult<string>.ErrorOccured("Exam schedule details not found.", ResponseCodes.BadRequestErrorCode);
                }

                EnforcementMode enforcementMode = null;

                enforcementMode = await _context.EnforcementModes.FirstOrDefaultAsync(x => x.Code.ToLower() == payload.Type.ToLower());
                if (enforcementMode is null)
                {
                    // have default enforcement mode if not found
                    enforcementMode = await _context.EnforcementModes.FirstOrDefaultAsync(x => x.Code == "TAB_NOT_FOCUS");
                }

                var proctorActivity = new CandidateProctorLog
                {
                    FlagId = payload.FlagId,
                    CandidateId = candidate.Id,
                    ExamScheduleId = examSchedule.Id,
                    EnforcementModeId = enforcementMode.Id,
                    MediaUrl = string.IsNullOrEmpty(payload.MediaUrl) ? "" : payload.MediaUrl,
                    Description = string.IsNullOrEmpty(payload.Description) ? "" : payload.Description,
                    Domain = string.IsNullOrEmpty(payload.Domain) ? "" : payload.Domain,
                    Event = string.IsNullOrEmpty(payload.Event) ? "" : payload.Event,
                    Status = ProctorStatus.Pending.ToString(),
                    InstitutionId = candidate.InstitutionId.Value,
                    CreatedAt = DateTime.UtcNow,
                    IsActive = true,
                    IsDeleted = false
                };

                await _context.CandidateProctorLogs.AddAsync(proctorActivity);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Proctor flag processed successfully: {payload.FlagId}");

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Proctor flag processed successfully.");
            }
            catch (ArgumentException ex)
            {
                return CustomResult<string>.ErrorOccured($"Invalid Exam id \n {ex.Message}", ResponseCodes.BadRequestErrorCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing proctor webhook");
                return CustomResult<string>.ErrorOccured("An error occurred while processing the webhook.", ResponseCodes.OperationError);
            }
        }

        public async Task<CustomResult<string>> CreateSessionAsync(ExamSessionRequest model)
        {
            try
            {
                if (model.CandidateId <= 0)
                    return CustomResult<string>.ErrorOccured("Candidate Id is required.", ResponseCodes.BadRequestErrorCode);

                if (model.ExamId <= 0)
                    return CustomResult<string>.ErrorOccured("ExamId is required.", ResponseCodes.BadRequestErrorCode);

                var candidate = await _context.Candidates.FirstOrDefaultAsync(x => x.Id == model.CandidateId);
                if(candidate is null)
                {
                    return CustomResult<string>.ErrorOccured("Candidate details not found.", ResponseCodes.BadRequestErrorCode);
                }

                var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == model.ExamId);
                if(examSchedule is null)
                {
                    return CustomResult<string>.ErrorOccured("Exam schedule details not found.", ResponseCodes.BadRequestErrorCode);
                }

                var settings = await this.GetProctorSettings(_proctorMe);
                var apiKey = this.GetSettingsValue(settings, "ApiKey");
                var BaseApiUrl = this.GetSettingsValue(settings, "BaseURL");

                var payload = JObject.FromObject(new
                {
                    userId = candidate.UserId,
                    examId = examSchedule.Id,
                    duration = examSchedule.ExamDurationInMinutes
                });

                IDictionary<string, string> customHeader = new Dictionary<string, string>
                {
                    { "x-transaction-id", "None" }
                };

                HttpContent newPayload = new StringContent(payload.ToString(), Encoding.UTF8, "application/json");

                var httpService = _serviceProvider.GetRequiredService<IHttpService>();
                var response = httpService.Post(BaseApiUrl + "/session", newPayload, customHeader, apiKey, "clientRequest").Result;

                if (response.IsSuccessStatusCode)
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    return CustomResult<string>.Success(result, ResponseCodes.SuccessCode);
                }
                else
                {
                    var result = response.Content.ReadAsStringAsync().Result;

                    return CustomResult<string>.Success($"Failed. {response.StatusCode}-{result}", ResponseCodes.UnableToGetToken);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error generating session token");
                return CustomResult<string>.ErrorOccured("An error occurred while generating session token.", ResponseCodes.OperationError);
            }
        }

        public async Task<CustomResult<ProctoringDashboardResponse>> GetProctoringDashboardAsync(QueryModelMini query)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);

                var tenant = await _userManagementService.GetTenantId();
                if (!tenant.IsSuccess)
                    return CustomResult<ProctoringDashboardResponse>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                IQueryable<CandidateProctorLog> records = _context.CandidateProctorLogs
                                                .Include(x => x.ExamSchedule)
                                                .Include(x => x.Candidate).ThenInclude(u => u.User)
                                                .Include(x => x.EnforcementMode)
                                                .Where(x => x.InstitutionId == tenant.Data)
                                                .OrderByDescending(x => x.CreatedAt);

                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= query.StartDate && x.CreatedAt <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var searchWord = query.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.ExamSchedule.Title.ToLower().Contains(searchWord) ||
                        (x.Candidate.User.FirstName + " " + x.Candidate.User.LastName).ToLower().Contains(searchWord) ||
                        x.Candidate.User.Email.ToLower().Contains(searchWord) ||
                        x.Candidate.RegistrationNumber.ToLower().Contains(searchWord) ||
                        x.Candidate.MatricNumber.ToLower().Contains(searchWord));
                }

                // ====== SUMMARY CARDS ======
                var totalFlags = await records.CountAsync();
                var pendingReview = await records.CountAsync(x => x.Status == ProctorStatus.Pending.ToString());
                var reviewed = await records.CountAsync(x => x.Status == ProctorStatus.Reviewed.ToString());
                var totalCandidates = await records.Select(x => x.CandidateId).Distinct().CountAsync();

                var summary = new ProctoringSummaryDto
                {
                    TotalFlags = totalFlags,
                    PendingReview = pendingReview,
                    ReviewEvents = reviewed,
                    TotalCandidates = totalCandidates
                };

                // ====== GROUP BY EXAM ======
                var groupedQuery = records
                    .GroupBy(x => new
                    {
                        x.ExamScheduleId,
                        x.ExamSchedule.Title,
                        x.ExamSchedule.CreatedAt
                    })
                    .Select(g => new ProctoringExamListDto
                    {
                        ExamScheduleId = g.Key.ExamScheduleId,
                        ExamTitle = g.Key.Title,
                        CandidateCount = g.Select(x => x.CandidateId).Distinct().Count(),
                        TotalFlags = g.Count(),
                        ExamDate = g.Key.CreatedAt,
                        Status = g.Any(x => x.Status == ProctorStatus.Pending.ToString()) ?
                                  ProctorStatus.Pending.ToString() :
                                  ProctorStatus.Reviewed.ToString()
                    });

                // ====== APPLY SORTING ======
                if (query.Sorting?.ToLower() == "asc")
                {
                    groupedQuery = groupedQuery.OrderBy(x => x.ExamDate);
                }
                else
                {
                    groupedQuery = groupedQuery.OrderByDescending(x => x.ExamDate);
                }

                var list = await groupedQuery.ToListAsync();
                var paginatedList = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? list.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : list.NoPaginate(0, 0);

                var response = new ProctoringDashboardResponse
                {
                    Summary = summary,
                    ExamList = paginatedList
                };

                return CustomResult<ProctoringDashboardResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ProctoringDashboardResponse>.ErrorOccured(
                    ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ProctoringExamDetailResponse>> GetProctoringExamDetailsAsync(long examScheduleId, QueryModelMini query)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);

                IQueryable<CandidateProctorLog> logs = _context.CandidateProctorLogs
                                                .Include(x => x.Candidate).ThenInclude(u => u.User)
                                                .Include(x => x.ExamSchedule)
                                                .Include(x => x.EnforcementMode)
                                                .Where(x => x.ExamScheduleId == examScheduleId)
                                                .OrderByDescending(x => x.CreatedAt);

                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    logs = logs.Where(x => x.CreatedAt >= query.StartDate && x.CreatedAt <= endDate);
                }

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var searchWord = query.SearchWord.Trim().ToLower();
                    logs = logs.Where(x =>
                        (x.Candidate.User.FirstName + " " + x.Candidate.User.LastName).ToLower().Contains(searchWord) ||
                        x.Candidate.User.Email.ToLower().Contains(searchWord) ||
                        x.Candidate.RegistrationNumber.ToLower().Contains(searchWord) ||
                        x.Candidate.MatricNumber.ToLower().Contains(searchWord));
                }

                // ======= SUMMARY =======
                var totalFlags = await logs.CountAsync();
                var pendingReview = await logs.CountAsync(x => x.Status == ProctorStatus.Pending.ToString());
                var reviewedFlags = await logs.CountAsync(x => x.Status == ProctorStatus.Reviewed.ToString());
                var totalCandidates = await logs.Select(x => x.CandidateId).Distinct().CountAsync();

                var summary = new ProctoringSummaryDto
                {
                    TotalFlags = totalFlags,
                    PendingReview = pendingReview,
                    ReviewEvents = reviewedFlags,
                    TotalCandidates = totalCandidates
                };

                // ======= GROUP BY CANDIDATE =======
                var groupedCandidates = logs
                    .GroupBy(x => new
                    {
                        x.CandidateId,
                        FirstName = x.Candidate.User.FirstName,
                        LastName = x.Candidate.User.LastName,
                        Email = x.Candidate.User.Email
                    })
                    .Select(g => new CandidateProctoringDetailDto
                    {
                        CandidateId = g.Key.CandidateId,
                        CandidateName = g.Key.FirstName + " " + g.Key.LastName,
                        CandidateEmail = g.Key.Email,
                        TotalFlags = g.Count(),
                        ExamScheduleId = examScheduleId,
                        PendingFlags = g.Count(x => x.Status == ProctorStatus.Pending.ToString()),
                        ReviewedFlags = g.Count(x => x.Status == ProctorStatus.Reviewed.ToString()),
                        Status = g.Count(x => x.Status == ProctorStatus.Pending.ToString()) == 0
                                    ? (g.Any(x => x.Status == ProctorStatus.Reviewed.ToString())
                                        ? ProctorStatus.Pending.ToString()
                                        : "Completed")
                                    : ProctorStatus.Pending.ToString()

                    });

                // ======= SORTING =======
                if (query.Sorting?.ToLower() == "asc")
                {
                    groupedCandidates = groupedCandidates.OrderBy(x => x.CandidateName);
                }
                else
                {
                    groupedCandidates = groupedCandidates.OrderByDescending(x => x.TotalFlags);
                }

                var list = await groupedCandidates.ToListAsync();
                var paginatedList = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? list.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : list.NoPaginate(0, 0);

                var response = new ProctoringExamDetailResponse
                {
                    Summary = summary,
                    Candidates = paginatedList
                };

                return CustomResult<ProctoringExamDetailResponse>.Success(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ProctoringExamDetailResponse>.ErrorOccured(
                    ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<CandidateProctorDetailsResponse>> GetCandidateProctorDetailsAsync(long candidateId, long examScheduleId)
        {
            try
            {
                // Fetch Proctor Configurations (Penalties)
                var proctorConfigs = await _context.ProctorConfigurations
                    .Where(p => p.ExamScheduleId == examScheduleId)
                    .ToListAsync();

                // Load Logs
                var logs = await _context.CandidateProctorLogs
                    .Include(x => x.Candidate).ThenInclude(u => u.User)
                    .Include(x => x.EnforcementMode)
                    .Where(x => x.CandidateId == candidateId && x.ExamScheduleId == examScheduleId)
                    .OrderByDescending(x => x.CreatedAt)
                    .ToListAsync();

                if (!logs.Any()) return null;

                // === SUMMARY ===
                var summary = new CandidateProctorSummaryDto
                {
                    TotalFlags = logs.Count,
                    PendingReview = logs.Count(x => x.Status == ProctorStatus.Pending.ToString()),
                    Penalised = logs.Count(x => x.Status == ProctorStatus.Reviewed.ToString()),
                    Dismissed = logs.Count(x => x.Status == ProctorStatus.Dismissed.ToString())
                };

                // === VIOLATIONS LIST ===
                var violationList = logs.Select(log =>
                {
                    // Get penalty for this enforcement type (Face absence, tab switch etc.)
                    string penalty = proctorConfigs
                        .FirstOrDefault(p => p.ParameterName == log.EnforcementMode.Name)
                        ?.Value ?? "None";

                    return new CandidateViolationDto
                    {
                        LogId = log.Id,
                        FlagType = log.EnforcementMode?.Name?.ToUpper() ?? "UNKNOWN",
                        CreatedAt = log.CreatedAt,
                        Description = log.Description,
                        Penalty = penalty,
                        Status = log.Status
                    };
                }).ToList();

                // === CANDIDATE NAME ===
                var candidateName = $"{logs.First().Candidate.User.FirstName} {logs.First().Candidate.User.LastName}";

                var returnDto = new CandidateProctorDetailsResponse
                {
                    CandidateName = candidateName,
                    Summary = summary,
                    Violations = violationList
                };
                return CustomResult<CandidateProctorDetailsResponse>.Success(returnDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<CandidateProctorDetailsResponse>.ErrorOccured(
                    ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> ProcessProctorActionAsync(long candidateId, long examScheduleId, ProctorActionType action)
        {
            using var transaction = await _context.Database.BeginTransactionAsync();

            try
            {
                // GET all logs belonging to this Candidate + Exam
                var logs = await _context.CandidateProctorLogs
                    .Where(x => x.CandidateId == candidateId && x.ExamScheduleId == examScheduleId)
                    .ToListAsync();

                if (!logs.Any())
                    return CustomResult<string>.ErrorOccured("No proctor logs found for this candidate.", ResponseCodes.BadRequestErrorCode);

                // Decide Action
                if (action == ProctorActionType.Dismiss)
                {
                    foreach (var log in logs)
                    {
                        log.Status = ProctorStatus.Dismissed.ToString();
                        log.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }
                else if (action == ProctorActionType.ApplyPenalty)
                {
                    foreach (var log in logs)
                    {
                        log.Status = ProctorStatus.Reviewed.ToString();
                        log.UpdatedAt = DateTime.UtcNow;
                    }

                    // Apply Penalty using ProctorConfiguration (deduction rules)
                    var penaltyConfig = await _context.ProctorConfigurations
                        .Where(x => x.ExamScheduleId == examScheduleId && x.ParameterName.ToLower() == "deduct score")
                        .FirstOrDefaultAsync();

                    if (penaltyConfig == null)
                        return CustomResult<string>.ErrorOccured("Penalty configuration not found.", ResponseCodes.BadRequestErrorCode);

                    if (!decimal.TryParse(penaltyConfig.Value, out decimal penaltyValue))
                        return CustomResult<string>.ErrorOccured("Invalid penalty configuration value.", ResponseCodes.BadRequestErrorCode);

                    // Fetch submission to deduct score
                    var submission = await _context.CandidateExamsSubmissions
                        .Where(x => x.CandidateId == candidateId && x.ExamScheduleId == examScheduleId)
                        .ToListAsync();

                    if (!submission.Any())
                        return CustomResult<string>.ErrorOccured("Candidate exam submission not found.", ResponseCodes.BadRequestErrorCode);

                    foreach (var sub in submission)
                    {
                        sub.Score = Math.Max(0, sub.Score - penaltyValue);
                        sub.UpdatedAt = DateTime.UtcNow;
                    }
                    await _context.SaveChangesAsync();
                }

                // CHECK IF THERE ARE ANY "Pending" LEFT FOR THIS EXAM
                int pendingCount = await _context.CandidateProctorLogs
                    .Where(x => x.ExamScheduleId == examScheduleId && x.Status == ProctorStatus.Pending.ToString())
                    .CountAsync();

                if (pendingCount == 0)
                {
                    // Convert ALL logs to Reviewed if no pending left
                    var allLogs = await _context.CandidateProctorLogs
                        .Where(x => x.ExamScheduleId == examScheduleId)
                        .ToListAsync();

                    foreach (var log in allLogs)
                    {
                        if (log.Status != ProctorStatus.Dismissed.ToString())
                        {
                            log.Status = ProctorStatus.Reviewed.ToString();
                            log.UpdatedAt = DateTime.UtcNow;
                        }
                    }

                    await _context.SaveChangesAsync();
                }

                await transaction.CommitAsync();

                return CustomResult<string>.Success("Action completed successfully.");
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return CustomResult<string>.ErrorOccured("Failed: " + ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}