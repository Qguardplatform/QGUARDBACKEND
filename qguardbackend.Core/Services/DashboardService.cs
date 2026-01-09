using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using Microsoft.EntityFrameworkCore;

namespace qguardbackend.Core.Services
{
    public class DashboardService : IDashboardService
    {
        private readonly AppDbContext _context;
        private readonly IUserManagementService _userManagementService;

        public DashboardService(AppDbContext context,
            IUserManagementService userManagementService)
        {
            _context = context;
            _userManagementService = userManagementService;
        }

        public async Task<CustomResult<DashboardResponse>> GetInstitutionAdminDashboardAsync(DashboardFilterModel filter)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<DashboardResponse>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                if (filter == null || filter.Range == FilterRange.Unspecified)
                {
                    filter = new DashboardFilterModel { Range = FilterRange.Last7Days };
                }

                var (startDate, endDate) = GetDateRange(filter);

                // User Breakdown
                var totalTutors = await _context.Tutors.CountAsync(x => x.InstitutionId == InsTId.Data);
                var totalProctors = await CountUsersByRoleAsync(RoleType.PROCTOR.ToString(), InsTId.Data);
                var totalExamOfficers = await CountUsersByRoleAsync(RoleType.EXAMINER.ToString(), InsTId.Data);
                var totalAdmins = await CountUsersByRoleAsync(RoleType.INSTITUTIONADMIN.ToString(), InsTId.Data);
                var totalCandidates = await CountUsersByRoleAsync(RoleType.CANDIDATE.ToString(), InsTId.Data);

                var newUsersCount = await _context.Users.CountAsync(u => u.CreatedAt >= startDate && u.CreatedAt <= endDate && u.InstitutionId == InsTId.Data);
                var totalUsers = await _context.Users.CountAsync(u => u.InstitutionId == InsTId.Data);
                var activeUsers = await _context.Users.CountAsync(u => u.IsActive && u.InstitutionId == InsTId.Data);

                var userBreakdown = new UserBreakdownDto
                {
                    UserCounts = new Dictionary<string, int>
                    {
                        { "Candidates", totalCandidates },
                        { "Tutors", totalTutors },
                        { "Proctors", totalProctors },
                        { "Exam Officers", totalExamOfficers },
                        { "Institution Admins", totalAdmins }
                    },
                    TotalUsers = totalUsers,
                    ActiveUsers = activeUsers,
                    NewUsers = newUsersCount
                };

                // Examination Status
                var exams = await _context.ExamSchedules
                    .Include(e => e.ExamQuestions)
                    .Include(e => e.DepartmentExamSchedule)
                    .Include(x => x.Course)
                    .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate && e.InstitutionId == InsTId.Data)
                    .ToListAsync();

                var examStatus = new ExamStatusDto
                {
                    Scheduled = exams.Count(e => e.Status == "Scheduled"),
                    Ongoing = exams.Count(e => e.Status == "Ongoing"),
                    Completed = exams.Count(e => e.Status == "Completed"),
                    RecentExaminations = (await Task.WhenAll(exams.OrderByDescending(e => e.StartDate).Take(5)
                    .Select(async e => new RecentExamDto
                    {
                        Title = e.Course.CourseCode,
                        ScheduledDate = e.StartDate,
                        //StudentCount = await GetStudentCountForExamAsync(e)
                        StudentCount = GetStudentCountForExamAsync(e)
                    }).ToList())).ToList()
                };

                // Question Bank
                var allQuestions = await _context.QuestionBanks.Where(x => x.InstitutionId == InsTId.Data).ToListAsync();
                var questionBank = new QuestionBankDto
                {
                    TotalQuestions = allQuestions.Count,
                    NewQuestions = allQuestions.Count(q => q.CreatedAt >= startDate && q.CreatedAt <= endDate)
                };

                // Results Status
                var publishedResults = await _context.CandidateExamsSubmissions.Include(x => x.ExamSchedule).ThenInclude(x => x.Course).Where(r => r.InstitutionId == InsTId.Data && r.IsResultPublished.Value).ToListAsync(); // Published
                var unpublishedResults = await _context.CandidateExamsSubmissions.Include(x => x.ExamSchedule).ThenInclude(x => x.Course).Where(r => r.InstitutionId == InsTId.Data && !r.IsResultPublished.Value).ToListAsync(); // Unpublished

                var resultStatus = new ResultStatusDto
                {
                    Published = publishedResults.Count,
                    Unpublished = unpublishedResults.Count,
                    RecentlyPublished = publishedResults
                        .OrderByDescending(r => r.CreatedAt)
                        .Take(5)
                        .Select(r => new RecentResultDto
                        {
                            CourseTitle = r.ExamSchedule.Course.CourseCode,
                            PublishedDate = FormatPublishedDate(r.UpdatedAt.Value)
                        }).ToList()
                };

                var result = new DashboardResponse
                {
                    UserBreakdown = userBreakdown,
                    ExamStatus = examStatus,
                    QuestionBank = questionBank,
                    ResultStatus = resultStatus
                };

                return CustomResult<DashboardResponse>.Success(result, ResponseMessages.SuccessMessage);

            }
            catch (Exception ex)
            {
                return CustomResult<DashboardResponse>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task<int> CountUsersByRoleAsync(string roleName, long institutionId)
        {
            var role = await _context.Roles.FirstOrDefaultAsync(r => r.Name.ToLower() == roleName.ToLower());
            if (role == null)
                return 0;

            return await _context.UserRoles.CountAsync(ur => ur.RoleId == role.Id && ur.InstitutionId == institutionId);
        }

        private (DateTime start, DateTime end) GetDateRange(DashboardFilterModel filter)
        {
            var today = DateTime.UtcNow.Date;

            return filter.Range switch
            {
                FilterRange.Today => (today, today.AddDays(1).AddTicks(-1)),
                FilterRange.Last7Days => (today.AddDays(-6), today.AddDays(1).AddTicks(-1)),
                FilterRange.ThisMonth => (new DateTime(today.Year, today.Month, 1), today.AddDays(1).AddTicks(-1)),
                FilterRange.LastMonth =>
                                (
                                    new DateTime(today.Year, today.Month, 1).AddMonths(-1),
                                    new DateTime(today.Year, today.Month, 1).AddTicks(-1)
                                ),
                FilterRange.ThisYear => (new DateTime(today.Year, 1, 1), today.AddDays(1).AddTicks(-1)),
                FilterRange.Custom when filter.StartDate.HasValue && filter.EndDate.HasValue =>
                    (filter.StartDate.Value.Date, filter.EndDate.Value.Date.AddDays(1).AddTicks(-1)),
                _ => (today.AddDays(-6), today.AddDays(1).AddTicks(-1)) // Default: Last 7 days
            };
        }

        private string FormatPublishedDate(DateTime createdAt)
        {
            var now = DateTime.UtcNow;
            var difference = now - createdAt;

            if (difference.TotalMinutes < 1)
                return "just now";
            if (difference.TotalMinutes < 60)
                return $"{(int)difference.TotalMinutes} minutes ago";
            if (difference.TotalHours < 24)
                return $"{(int)difference.TotalHours} hours ago";
            if (difference.TotalHours < 48)
                return "yesterday";

            return createdAt.ToLocalTime().ToString("hh:mm tt, dd/MM/yyyy");
        }

        private  int GetStudentCountForExamAsync(ExamSchedule exam)
        {
            var departmentIds = exam.DepartmentExamSchedule?
                .Where(d => d.DepartmentId.HasValue)
                .Select(d => d.DepartmentId.Value)
                .ToList();

            if (departmentIds == null || !departmentIds.Any())
                return 0;

            return  _context.Candidates.Count(c => departmentIds.Contains(c.DepartmentId ?? 0));
        }

        public async Task<CustomResult<ExamPerformanceDto>> GetInstitutionExamPerformanceAsync(DashboardFilterModel filter)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<ExamPerformanceDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }
            if (filter == null || filter.Range == FilterRange.Unspecified)
            {
                filter = new DashboardFilterModel { Range = FilterRange.Last7Days };
            }

            var (startDate, endDate) = GetDateRange(filter);

            // Current semester submissions
            var currentSubmissions = await _context.CandidateExamsSubmissions
                .Include(x => x.ExamSchedule)
                    .ThenInclude(x => x.Course)
                .Where(x => x.SubmissionDateAndTime >= startDate
                    && x.SubmissionDateAndTime <= endDate
                    && x.InstitutionId == InsTId.Data)
                .ToListAsync();

            var coursePerformances = currentSubmissions
                .GroupBy(x => x.ExamSchedule.Course.CourseCode)
                .Select(g => new CoursePerformanceDto
                {
                    CourseCode = g.Key,
                    AverageScore = Math.Round(g.Average(x => (double)x.Score), 1)
                })
                .OrderByDescending(c => c.AverageScore)
                .Take(10)
                .ToList();

            var overallAverageScore = currentSubmissions.Any()
                ? Math.Round(currentSubmissions.Average(x => (double)x.Score), 1)
                : 0;

            // Previous semester submissions
            var previousStart = startDate.AddMonths(-6);
            var previousEnd = startDate.AddTicks(-1);

            var previousSubmissions = await _context.CandidateExamsSubmissions
                .Where(x => x.SubmissionDateAndTime >= previousStart
                    && x.SubmissionDateAndTime <= previousEnd
                    && x.InstitutionId == InsTId.Data)
                .ToListAsync();

            var previousAverageScore = previousSubmissions.Any()
                ? previousSubmissions.Average(x => (double)x.Score)
                : 0;

            var improvementRate = previousAverageScore == 0
                ? 0
                : Math.Round(((overallAverageScore - previousAverageScore) / previousAverageScore) * 100, 1);

            var result = new ExamPerformanceDto
            {
                OverallAverageScore = overallAverageScore,
                ImprovementRate = improvementRate,
                CoursePerformances = coursePerformances
            };

            return CustomResult<ExamPerformanceDto>.Success(result, ResponseMessages.SuccessMessage);
        }

        public async Task<CustomResult<AdminDashboardResponse>> GetAdminDashboardAsync(DashboardFilterModel filter)
        {
            try
            {
                var (startDate, endDate) = GetDateRange(filter);

                var tenantResult = await _userManagementService.GetTenantId();
                if (!tenantResult.IsSuccess)
                    return CustomResult<AdminDashboardResponse>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var institutionId = tenantResult.Data;

                var totalInstitutions = await _context.Institutions.CountAsync();

                var totalTutors = await _context.Tutors.CountAsync(x => x.InstitutionId == institutionId);

                var totalUsers = await _context.Users.CountAsync(u => u.InstitutionId == institutionId);
                var activeUsers = await _context.Users
                    .Where(u => u.InstitutionId == institutionId && u.IsActive)
                    .CountAsync();

                var scheduledExams = await _context.ExamSchedules
                    .Where(e => e.InstitutionId == institutionId &&
                                e.StartDate >= startDate && e.EndDate <= endDate)
                    .ToListAsync();

                var examsTaken = scheduledExams.Count(e => e.Status == ExamScheduleStatusEnum.PUBLISHED.ToString());

                var userCategories = await (
                                from user in _context.Users
                                join userRole in _context.UserRoles on user.Id equals userRole.UserId
                                join role in _context.Roles on userRole.RoleId equals role.Id
                                where /*user.InstitutionId == institutionId &&*/
                                      user.CreatedAt >= startDate &&
                                      user.CreatedAt <= endDate
                                group new { user, role } by user.Institution.Name into g
                                select new UserCategoryDto
                                {
                                    Institution = g.Key,
                                    TotalUsers = g.Select(x => x.user.Id).Distinct().Count(),
                                    Candidates = g.Count(x => x.role.Name == RoleType.CANDIDATE.ToString()),
                                    Tutors = totalTutors,
                                    Proctors = g.Count(x => x.role.Name == RoleType.PROCTOR.ToString()),
                                    ExamOfficers = g.Count(x => x.role.Name == RoleType.EXAMINER.ToString()), 
                                    Admins = g.Count(x => x.role.Name == RoleType.INSTITUTIONADMIN.ToString())
                                }
                            ).ToListAsync();

                var examStats = await _context.ExamSchedules
                    .Where(e => e.StartDate >= startDate && e.EndDate <= endDate)
                    .GroupBy(e => e.Institution.Name)
                    .Select(g => new ExamStatisticDto
                    {                    
                        Institution = g.Key,
                        Scheduled = g.Count(e => e.Status == "Scheduled"),
                        Ongoing = g.Count(e => e.Status == "Ongoing"),
                        Completed = g.Count(e => e.Status == "Completed")
                    })
                    .ToListAsync();

                var response = new AdminDashboardResponse
                {
                    TotalInstitutions = totalInstitutions,
                    ActiveUsers = new ActiveUserSummary { Count = activeUsers, Total = totalUsers },
                    Exams = new ExamSummary { Taken = examsTaken, Scheduled = scheduledExams.Count },
                    UserCategories = userCategories,
                    ExamStatistics = examStats
                };

                return CustomResult<AdminDashboardResponse>.Success(response);
            }
            catch (Exception ex)
            {
                return CustomResult<AdminDashboardResponse>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}