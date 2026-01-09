using qguardbackend.Data.Common;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs.EmailDtos;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using SISService.BoilerPlate.Service.Interfaces;

namespace qguardbackend.Core.BackGroundService
{
    public class EmailNotificationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<EmailNotificationBackgroundService> _logger;

        public EmailNotificationBackgroundService(
            IServiceProvider services,
            ILogger<EmailNotificationBackgroundService> logger)
        {
            _logger = logger;
            _services = services;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Exam Notification Service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using var scope = _services.CreateScope();
                    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    var emailService = scope.ServiceProvider.GetRequiredService<IEmailService>();

                    var now = DateTime.UtcNow;

                    var exams = await db.ExamSchedules
                        .Include(e => e.DepartmentExamSchedule)
                        .Include(e => e.ExamQuestions)
                        .Where(e => e.Status.ToLower() == "published" && (e.EndDate == null || e.EndDate >= now))
                        .ToListAsync(stoppingToken);

                    foreach (var exam in exams)
                    {
                        var examStart = exam.StartDate.Date.Add(exam.StartTime.TimeOfDay);

                        var candidateIds = await db.CandidatesCurrentStates
                            .Where(c => c.LevelId == exam.LevelId && c.SemesterId == exam.SemesterId &&
                                c.SessionId == exam.AcademicSessionId && c.InstitutionId == exam.InstitutionId)
                            .Select(c => c.CandidateId)
                            .Distinct()
                            .ToListAsync(stoppingToken);

                        var candidates = await db.Candidates
                            .Include(c => c.User)
                            .Where(c => candidateIds.Contains(c.Id))
                            .ToListAsync(stoppingToken);

                        foreach (var student in candidates)
                        {
                            var getInstdetails = await db.Institutions.Where(x => x.Id == student.InstitutionId).FirstOrDefaultAsync();
                            var email = student.User?.Email;
                            if (string.IsNullOrEmpty(email)) continue;

                            // Published grading Email
                            if (!exam.PublishedMailSent && exam.IsPublished)
                            {
                                var submission = await db.CandidateExamsSubmissions
                                    .Where(x => x.ExamScheduleId == exam.Id && x.CandidateId == student.Id)
                                    .OrderByDescending(x => x.CreatedAt)
                                    .FirstOrDefaultAsync(stoppingToken);
                                if (submission is not null)
                                {
                                    var grade = Utility.GetGradeWithRemark(submission.Score);
                                
                                    await emailService.SendExamGradingEmailAsync(new CustomSendExamDto
                                    {
                                        CandidateName = $"{student.User.FirstName} {student.User.LastName}",
                                        CandidateEmail = email,
                                        ExamTitle = exam.Title,
                                        InstitutionBaseUrl = getInstdetails.HostName,
                                        Score = submission.Score.ToString(),
                                        Grade = grade
                                    });
                                }
                            }

                            // Reminder Email (1 day before)
                            if (!exam.ReminderMailSent && now >= examStart.AddDays(-1))
                            {
                                await emailService.SendExamReminderEmailAsync(new CustomSendExamDto
                                {
                                    CandidateName = $"{student.User.FirstName} {student.User.LastName}",
                                    CandidateEmail = email,
                                    ExamTitle = exam.Title,
                                    InstitutionBaseUrl = getInstdetails.HostName,
                                    //DateSubmitted = examStart.ToString("yyyy-MM-dd"),
                                    DateSubmitted = examStart.ToString("dd-MM-yyyy"),
                                    Duration = exam.ExamDurationInMinutes?.ToString() ?? "N/A",
                                    Time = examStart.ToString("HH:mm")
                                });
                            }

                            // Starting Soon Email (10 minutes before)
                            if (!exam.StartingSoonMailSent && now >= examStart.AddMinutes(-10))
                            {
                                await emailService.SendExamStartingEmailAsync(new CustomSendExamDto
                                {
                                    CandidateName = $"{student.User.FirstName} {student.User.LastName}",
                                    CandidateEmail = email,
                                    InstitutionBaseUrl = getInstdetails.HostName,
                                    ExamTitle = exam.Title
                                });
                            }
                        }

                        // update flags
                        if (exam.IsPublished) exam.PublishedMailSent = true;
                        if (now >= examStart.AddDays(-1)) exam.ReminderMailSent = true;
                        if (now >= examStart.AddMinutes(-10)) exam.StartingSoonMailSent = true;
                    }

                    await db.SaveChangesAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error in ExamNotificationService");
                }

                await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
            }
        }
    }
}