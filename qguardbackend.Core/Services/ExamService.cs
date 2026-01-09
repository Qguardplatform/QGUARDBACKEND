using DocumentFormat.OpenXml.Spreadsheet;
using qguardbackend.Core.BackGroundService;
using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Common;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DbContext;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.DTOs.EmailDtos;
using qguardbackend.Data.DTOs.RequestDto;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Entities;
using qguardbackend.Data.Enums;
using examportal.Api.ServiceExtensions;
using Firebase.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using SISService.BoilerPlate.Service.Interfaces;
using System.Data;
using System.Text;

namespace qguardbackend.Core.Services
{
    public class ExamService : IExamService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ExamService> _logger;
        private readonly IS3Service _s3Service;
        private readonly EmailNotificationChannel _emailNotificationChannel;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;
        private readonly IEmailService _emailService;
        private readonly IQuestionBankService _questionBankService;
        private readonly IServiceProvider _serviceProvider;

        public ExamService(AppDbContext context,
            ILogger<ExamService> logger,
            IEmailService emailService,
            IQuestionBankService questionBankService,
            IUserManagementService userManagementService,
            IS3Service s3Service,
            EmailNotificationChannel emailNotificationChannel,
            IAuditLogService auditLogService,
            IServiceProvider serviceProvider)
        {
            _context = context;
            _emailService = emailService;
            _questionBankService = questionBankService;
            _logger = logger;
            _s3Service = s3Service;
            _emailNotificationChannel = emailNotificationChannel;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
            _serviceProvider = serviceProvider;
        }

        public async Task<CustomResult<ExamSubmissionAnswersResponseDto>> SubmitExaminationAnswersAsync(ExamSubmissionRequestDto request, string createdBy)
        {
            var errors = new List<string>();
            using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<ExamSubmissionAnswersResponseDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var candidate = await _context.Candidates
                    .Include(x => x.User)
                    .Include(x => x.Institution)
                    .FirstOrDefaultAsync(x => x.UserId == request.CandidateUserId);

                if (candidate == null)
                {
                    return CustomResult<ExamSubmissionAnswersResponseDto>.ErrorOccured("Candidate not found.", ResponseCodes.NotFoundErrorCode);
                }

                // Check for earlier submission
                var alreadySubmitted = await _context.CandidateExamsSubmissions
                    .OrderBy(x => x.Id)
                    .LastOrDefaultAsync(x => x.CandidateId == candidate.Id
                        && x.ExamScheduleId == request.ExamScheduleId && !x.Allowresit.Value);

                if (alreadySubmitted is not null)
                {
                    return CustomResult<ExamSubmissionAnswersResponseDto>.ErrorOccured(
                        "Candidate has already submitted this exam. Contact exam administrator to request a re-sit.",
                        ResponseCodes.AlreadyExistErrorCode);
                }

                // Check exam schedule
                var examSchedule = await _context.ExamSchedules
                    .Include(x => x.Course)
                    .FirstOrDefaultAsync(x => x.Id == request.ExamScheduleId);

                if (examSchedule == null)
                {
                    return CustomResult<ExamSubmissionAnswersResponseDto>.ErrorOccured("Exam not found.", ResponseCodes.NotFoundErrorCode);
                }

                var submissionsToSave = new List<CandidateExamsSubmission>();
                long correctAnswers = 0;
                long wrongAnswers = 0;
                long totalPoints = 0;

                if (!request.ExamSubmissionAnswers.Any())
                {
                    foreach (var item in request.QuestionIds)
                    {
                        var question = await _context.QuestionBanks
                            .Include(x => x.QuestionOptions)
                            .FirstOrDefaultAsync(x => x.Id == item);

                        if (question == null)
                        {
                            errors.Add($"QuestionBank with id - {item} not found.");
                            continue;
                        }

                        var recordToSave = new CandidateExamsSubmission
                        {
                            TotalTimeSpent = request.TotalTimeSpent,
                            CandidateId = candidate.Id,
                            ExamScheduleId = request.ExamScheduleId,
                            QuestionBankId = item,
                            CorrectQuestionOptionId = null,
                            IsCorrectOption = null,
                            SelectedQuestionOptionId = null,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            InstitutionId = InsTId.Data,
                            SubmissionDateAndTime = request.ExamDate,
                            IsGraded = true,
                            IsActive = true,
                            Remark = "Failed",
                            IsResultPublished = examSchedule.InstantResultPublishing,
                            CorrectAnswer = null,
                            Allowresit = false,
                            AnswerSelected = null,
                            Score = 0
                        };
                        submissionsToSave.Add(recordToSave);
                        totalPoints += question.Points;
                    }

                    if (!submissionsToSave.Any())
                    {
                        return CustomResult<ExamSubmissionAnswersResponseDto>.ErrorOccured("No valid submissions for this exam", ResponseCodes.OperationError);
                    }



                    decimal totalScore = submissionsToSave.Sum(x => x.Score);
                    long totalQuestions = request.QuestionIds.Count;
                    long attempted = 0;
                    string remark = "Failed";

                    var resultRecord = BuildCandidateExamResult(
                        examSchedule.Id,
                        candidate.Id,
                        totalScore,
                        totalQuestions,
                        attempted,
                        remark,
                        request.TotalTimeSpent,
                        request.ExamDate,
                        false
                    );

                    resultRecord.TotalPoints = totalPoints;

                    var resultDetails = await _context.CandidateExamResults.AddAsync(resultRecord);
                    await _context.SaveChangesAsync();


                    var submissionsToStore = new List<CandidateExamsSubmission>();
                    //map the Result Id so as to know which result has the specific answer submissions
                    foreach (var submission in submissionsToSave)
                    {
                        submission.ResultId = resultDetails.Entity.Id;
                        submissionsToStore.Add(submission);
                    }

                    //await _context.CandidateExamsSubmissions.AddRangeAsync(submissionsToSave);
                    await _context.CandidateExamsSubmissions.AddRangeAsync(submissionsToStore);
                    await _context.SaveChangesAsync();

                    await transaction.CommitAsync();

                    await _auditLogService.AddToAudit(
                        (int)AuditActionType.Create,
                        "Examination",
                        $"User [{createdBy}] submitted an examination at {DateTime.UtcNow}."
                    );

                    // Send email in background
                    //_ = Task.Run(async () =>
                    //{
                    await _emailService.SendExamSubmissionEmailAsync(new CustomSendExamDto
                    {
                        CandidateName = $"{candidate.User.FirstName} {candidate.User.LastName}",
                        ExamTitle = examSchedule.Title,
                        CandidateEmail = candidate.User.Email,
                        //DateSubmitted = Utility.GetFormattedDate(DateTime.Today),
                        DateSubmitted = DateTime.UtcNow.ToString("dd-MM-yyyy")
                    });
                    //});

                    return CustomResult<ExamSubmissionAnswersResponseDto>.Success(
                        new ExamSubmissionAnswersResponseDto
                        {
                            NoOfQuestions = totalQuestions,
                            NoOfCorrectAnswers = 0,
                            NoOfWrongAnswers = 0,
                            NoOfQuestionsAttempted = 0,
                            CandidateId = candidate.Id,
                            ExamScheduleId = examSchedule.Id,
                            Title = examSchedule.Title,
                            InstantResultPublish = examSchedule.InstantResultPublishing,
                            Score = totalScore,
                            TotalScoreOnTheExam = totalPoints
                        },
                        ResponseMessages.SuccessMessage
                    );
                }
                else // means the student attempt the exam
                {
                    foreach (var submission in request.ExamSubmissionAnswers)
                    {
                        if (!submission.QuestionBankId.HasValue)
                        {
                            errors.Add($"QuestionBankId is missing for one of the submitted answers.");
                            continue;
                        }

                        // Load question & options
                        var question = await _context.QuestionBanks
                            .Include(x => x.QuestionOptions)
                            .FirstOrDefaultAsync(x => x.Id == submission.QuestionBankId &&
                                          x.CourseId == examSchedule.CourseId &&
                                          x.InstitutionId == InsTId.Data);

                        if (question == null)
                        {
                            errors.Add($"QuestionBank {submission.QuestionBankId} not found.");
                            continue;
                        }

                        var options = question.QuestionOptions.ToList();
                        if (options == null || !options.Any())
                        {
                            var msg = $"No options found for QuestionBankId {submission.QuestionBankId}.";
                            errors.Add(msg);
                            _logger.LogWarning(msg);
                            continue;
                        }

                        var correctOption = options.FirstOrDefault(o => o.IsCorrectOption == true);
                        if (correctOption == null)
                        {
                            var msg = $"No correct option configured for QuestionBankId {submission.QuestionBankId}.";
                            errors.Add(msg);
                            continue;
                        }

                        bool isCorrect = submission.SelectedQuestionOptionId.HasValue &&
                                         correctOption?.Id == submission.SelectedQuestionOptionId.Value;

                        if (isCorrect) correctAnswers++;
                        else wrongAnswers++;

                        submissionsToSave.Add(new CandidateExamsSubmission
                        {
                            TotalTimeSpent = request.TotalTimeSpent,
                            CandidateId = candidate.Id,
                            ExamScheduleId = examSchedule.Id,
                            QuestionBankId = submission.QuestionBankId.Value,
                            CorrectQuestionOptionId = correctOption?.Id,
                            IsCorrectOption = isCorrect,
                            SelectedQuestionOptionId = submission.SelectedQuestionOptionId,
                            CreatedAt = DateTime.UtcNow,
                            UpdatedAt = DateTime.UtcNow,
                            InstitutionId = InsTId.Data,
                            SubmissionDateAndTime = request.ExamDate,
                            IsGraded = true,
                            IsActive = true,
                            Remark = isCorrect ? "Passed" : "Failed",
                            IsResultPublished = examSchedule.InstantResultPublishing,
                            CorrectAnswer = correctOption?.OptionDescription,
                            Allowresit = false,
                            AnswerSelected = options.FirstOrDefault(o => o.Id == submission.SelectedQuestionOptionId)?.OptionDescription,
                            Score = isCorrect ? question.Points : 0
                        });
                    }

                    await _context.CandidateExamsSubmissions.AddRangeAsync(submissionsToSave);
                    await _context.SaveChangesAsync();

                    decimal finalScore = submissionsToSave.Sum(x => x.Score);
                    long totalQ = request.QuestionIds.Count;
                    long attemptedQ = submissionsToSave.Count(x => x.QuestionBankId != null);
                    string finalRemark = GetRemark(finalScore, examSchedule.PassScore ?? 0);
                    //get total points
                    var totalPoint = await _context.QuestionBanks
                            .Where(q => request.QuestionIds.Contains(q.Id))
                            .SumAsync(q => (int?)q.Points) ?? 0;

                    var candidateResult = BuildCandidateExamResult(
                        examSchedule.Id,
                        candidate.Id,
                        finalScore,
                        totalQ,
                        attemptedQ,
                        finalRemark,
                        request.TotalTimeSpent,
                        request.ExamDate,
                        false
                    );
                    candidateResult.TotalPoints = totalPoint;

                    _context.CandidateExamResults.Add(candidateResult);
                    await _context.SaveChangesAsync();

                    await _auditLogService.AddToAudit(
                        (int)AuditActionType.Create,
                        "Examination",
                        $"User [{createdBy}] submitted an examination at {DateTime.UtcNow}."
                    );

                    await transaction.CommitAsync();

                    // Send email in background
                    _ = Task.Run(async () =>
                    {
                        await _emailService.SendExamSubmissionEmailAsync(new CustomSendExamDto
                        {
                            CandidateName = $"{candidate.User.FirstName} {candidate.User.LastName}",
                            ExamTitle = examSchedule.Title,
                            CandidateEmail = candidate.User.Email,
                            //DateSubmitted = Utility.GetFormattedDate(DateTime.Today)
                            DateSubmitted = DateTime.UtcNow.ToString("dd-MM-yyyy")
                        });
                    });

                    return CustomResult<ExamSubmissionAnswersResponseDto>.Success(
                        new ExamSubmissionAnswersResponseDto
                        {
                            NoOfQuestions = totalQ,
                            NoOfCorrectAnswers = correctAnswers,
                            NoOfWrongAnswers = wrongAnswers,
                            NoOfQuestionsAttempted = attemptedQ,
                            CandidateId = candidate.Id,
                            ExamScheduleId = examSchedule.Id,
                            Title = examSchedule.Title,
                            InstantResultPublish = examSchedule.InstantResultPublishing,
                            Score = finalScore,
                            TotalScoreOnTheExam = totalPoint
                        },
                        errors.Any()
                            ? $"Completed with issues: {string.Join("; ", errors)}"
                            : ResponseMessages.SuccessMessage
                    );
                }
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ExamService).Name,
                    nameof(SubmitExaminationAnswersAsync));

                return CustomResult<ExamSubmissionAnswersResponseDto>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
            }
        }

        private CandidateExamResult BuildCandidateExamResult(long examScheduleId, long candidateId, decimal totalScore, long totalQuestions, long noOfQuestionsAttempted, string remark, string totalTimeSpent, DateTime submissionDateAndTime, bool resit)
        {
            return CandidateExamResult.Create(
                examScheduleId,
                candidateId,
                totalScore,
                totalQuestions,
                noOfQuestionsAttempted,
                remark,
                totalTimeSpent,
                submissionDateAndTime,
                resit
            );
        }

        private string GetRemark(decimal score, decimal passMark)
        {
            return score >= passMark ? "Passed" : "Failed";
        }

        public async Task<CustomResult<string>> MapQuestionsToExamSchedule(MapQuestionsToExamScheduleRequestDto model, string createdBy)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var errorList = new List<string>();
                    if (string.IsNullOrWhiteSpace(model.Instruction))
                        return CustomResult<string>.ErrorOccured("Instruction text is required.", ResponseCodes.BadRequestErrorCode);

                    var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == model.ExamScheduleId);
                    if (examSchedule is null)
                        return CustomResult<string>.ErrorOccured("Invalid exam schedule id passed!.", ResponseCodes.BadRequestErrorCode);
                    examSchedule.Instruction = model.Instruction;
                    _context.ExamSchedules.Update(examSchedule);

                    int counter = 0;

                    foreach (var questionBankId in model.QuestionBankId)
                    {
                        //check if the question has been mapped to the exam before now before remapping it again

                        var IsQuestionMappedToExamEarlier = await _context.ExamQuestions
                        .FirstOrDefaultAsync(x => x.ExamScheduleId == model.ExamScheduleId
                        && x.QuestionBankId == questionBankId);
                        if (IsQuestionMappedToExamEarlier is null)
                        {
                            var createExam = ExamQuestion.Create(model.Instruction,
                                model.ExamScheduleId, model.MakeQuestionsAppearRandom,
                                questionBankId);
                            createExam.InstitutionId = InsTId.Data;

                            _context.ExamQuestions.Add(createExam);
                            await _context.ExamQuestions.AddAsync(createExam);

                            counter++;
                        }
                    }

                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] added QuestionIds {string.Join("|", model.QuestionBankId)} to exam schedule - {examSchedule.Description} at {DateTime.UtcNow}.");

                    return CustomResult<string>.Success($"{counter} question(s) added to exam successfully.", ResponseCodes.SuccessCode);

                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ExamService).Name,
                                                nameof(SubmitExaminationAnswersAsync));
                    return CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }

        public async Task<CustomResult<string>> CreateQuestionForExamAsync(ExamQuestionCreateModel model, string createdBy)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var errorList = new List<string>();
                    if (string.IsNullOrWhiteSpace(model.Instruction))
                        return CustomResult<string>.ErrorOccured("Instruction text is required.", ResponseCodes.BadRequestErrorCode);

                    var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == model.ExamScheduleId);
                    if (examSchedule is null)
                        return CustomResult<string>.ErrorOccured("Invalid exam schedule id passed!.", ResponseCodes.BadRequestErrorCode);
                    examSchedule.Instruction = model.Instruction;
                    _context.ExamSchedules.Update(examSchedule);

                    var createdQuestions = await this.CreateQuestionAsync(examSchedule.CourseId,
                        model.MultipleQuestions);
                    if (createdQuestions.Count() > 0)
                    {
                        foreach (var questionBankId in createdQuestions)
                        {
                            //check if the question has been mapped to the exam before now before remapping it again

                            var IsQuestionMappedToExamEarlier = await _context.ExamQuestions
                            .FirstOrDefaultAsync(x => x.ExamScheduleId == model.ExamScheduleId
                            && x.QuestionBankId == questionBankId);
                            if (IsQuestionMappedToExamEarlier is null)
                            {

                                var createExam = ExamQuestion.Create(model.Instruction,
                                    model.ExamScheduleId, model.MakeQuestionsAppearRandom,
                                    questionBankId);
                                createExam.InstitutionId = InsTId.Data;
                                _context.ExamQuestions.Add(createExam);
                                await _context.ExamQuestions.AddAsync(createExam);

                            }

                        }
                        await _context.SaveChangesAsync();
                        await transaction.CommitAsync();
                        await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] added Question to exam schedule - {examSchedule.Description} at {DateTime.UtcNow}.");

                        return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question added to exam successfully.");
                    }
                    return CustomResult<string>.ErrorOccured("Unable to create question.", ResponseCodes.BadRequestErrorCode);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ExamService).Name,
                                                nameof(SubmitExaminationAnswersAsync));
                    return CustomResult<string>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }

        public async Task<SingleExamQuestionCreateResponseModel> CreateSingleQuestionAsync(long courseId, ExamQuestionMultDto item)
        {
            var resp = new SingleExamQuestionCreateResponseModel();
            var errorList = new List<string>();
            List<long> ids = new List<long>();
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                throw new Exception("Invalid institution id passed!.");
            }


            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == courseId);
            if (course is null)
            {
                throw new Exception("Invalid course id passed!.");
            }


            if (string.IsNullOrWhiteSpace(item.Question))
            {
                errorList.Add($"Question text is required");
                //continue;
                resp.errors = errorList;
                resp.IsCreatedSuccessfully = false;
                return resp;
            }
            if (string.IsNullOrWhiteSpace(item.DifficultLevel.ToString()))
            {
                errorList.Add($"Difficult text is required");
                //continue;
                resp.errors = errorList;
                resp.IsCreatedSuccessfully = false;
                return resp;
            }

            if (item.Point <= 0)
            {
                errorList.Add($"Point is required!");
                //continue;
                resp.errors = errorList;
                resp.IsCreatedSuccessfully = false;
                return resp;
            }

            string uploadedQuestionImageUrl = null;
            // Upload question image if available
            if (item.ImageUrl != null)
            {
                var uploadResult = await UploadFileAsync(item.ImageUrl);
                if (!uploadResult.Success)
                {
                    errorList.Add($"{uploadResult.Error} for question - {item.Question}");
                    //continue;
                    resp.errors = errorList;
                    resp.IsCreatedSuccessfully = false;
                    return resp;
                }
                uploadedQuestionImageUrl = uploadResult.Url;
            }

            var question = QuestionBank.Create(item.Question, item.DifficultLevel, item.IsMultipleChoice,
                item.Tags, courseId, InsTId.Data, item.Point);
            var qb = await _context.QuestionBanks.AddAsync(question);
            await _context.SaveChangesAsync();
            question.ImageUrl = uploadedQuestionImageUrl;

            // Handle Options
            if (item.Options != null && item.Options.Any())
            {
                char currentChar = 'A';
                foreach (var optionModel in item.Options)
                {
                    string optionContent = null;
                    string optionImg = null;

                    if (optionModel.Image != null)
                    {
                        var optionUploadResult = await UploadFileAsync(optionModel.Image);
                        if (!optionUploadResult.Success)
                        {
                            errorList.Add($"{optionUploadResult.Error} for question option {item.Question}");
                            continue;
                        }
                        optionImg = optionUploadResult.Url;
                    }
                    else if (!string.IsNullOrWhiteSpace(optionModel.OptionDescription))
                    {
                        optionContent = optionModel.OptionDescription;
                    }
                    else
                    {
                        errorList.Add($"Each option must have either a description or an image.");
                        continue;
                    }

                    var questionOption = new QuestionOption
                    {
                        OptionDescription = optionModel.OptionDescription,
                        OptionTag = currentChar.ToString().ToUpper(),
                        //OptionTag = optionModel.OptionTag,
                        IsCorrectOption = optionModel.IsCorrectOption,
                        CreatedAt = DateTime.UtcNow,
                        ImageUrl = optionImg,
                        IsActive = true,
                        QuestionBankId = qb.Entity.Id,
                    };
                    question.QuestionOptions.Add(questionOption);

                    if (currentChar < 'Z')
                        currentChar++;
                }
            }

            ids.Add(question.Id);

            var rec = await _context.SaveChangesAsync();
            resp.errors = errorList;
            resp.ExamBankId = qb.Entity.Id;
            resp.IsCreatedSuccessfully = true;
            return resp;
        }

        public async Task<List<long>> CreateQuestionAsync(long courseId, ExamQuestionMultModel model)
        {
            var errorList = new List<string>();
            List<long> ids = new List<long>();
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                throw new Exception("Invalid institution id passed!.");
            }


            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == courseId);
            if (course is null)
            {
                throw new Exception("Invalid course id passed!.");
            }

            foreach (var item in model.Questions)
            {
                if (string.IsNullOrWhiteSpace(item.Question))
                {
                    errorList.Add($"Question text is required");
                    continue;
                }
                if (string.IsNullOrWhiteSpace(item.DifficultLevel.ToString()))
                {
                    errorList.Add($"Difficult text is required");
                    continue;
                }

                if (item.Point <= 0)
                {
                    errorList.Add($"Point is required!");
                    continue;
                }

                string uploadedQuestionImageUrl = null;
                // Upload question image if available
                if (item.ImageUrl != null)
                {
                    var uploadResult = await UploadFileAsync(item.ImageUrl);
                    if (!uploadResult.Success)
                    {
                        errorList.Add($"{uploadResult.Error} for question - {item.Question}");
                        continue;
                    }
                    uploadedQuestionImageUrl = uploadResult.Url;
                }

                var question = QuestionBank.Create(item.Question, item.DifficultLevel, item.IsMultipleChoice,
                    item.Tags, courseId, InsTId.Data, item.Point);
                var qb = await _context.QuestionBanks.AddAsync(question);
                await _context.SaveChangesAsync();
                question.ImageUrl = uploadedQuestionImageUrl;

                // Handle Options
                if (item.Options != null && item.Options.Any())
                {
                    char currentChar = 'A';
                    foreach (var optionModel in item.Options)
                    {
                        string optionContent = null;
                        string optionImg = null;

                        if (optionModel.Image != null)
                        {
                            var optionUploadResult = await UploadFileAsync(optionModel.Image);
                            if (!optionUploadResult.Success)
                            {
                                errorList.Add($"{optionUploadResult.Error} for question option {item.Question}");
                                continue;
                            }
                            optionImg = optionUploadResult.Url;
                        }
                        else if (!string.IsNullOrWhiteSpace(optionModel.OptionDescription))
                        {
                            optionContent = optionModel.OptionDescription;
                        }
                        else
                        {
                            errorList.Add($"Each option must have either a description or an image.");
                            continue;
                        }

                        var questionOption = new QuestionOption
                        {
                            OptionDescription = optionModel.OptionDescription,
                            OptionTag = currentChar.ToString().ToUpper(),
                            //OptionTag = optionModel.OptionTag,
                            IsCorrectOption = optionModel.IsCorrectOption,
                            CreatedAt = DateTime.UtcNow,
                            ImageUrl = optionImg,
                            IsActive = true,
                            QuestionBankId = qb.Entity.Id,
                        };
                        question.QuestionOptions.Add(questionOption);

                        if (currentChar < 'Z')
                            currentChar++;
                    }
                }

                ids.Add(question.Id);
            }
            await _context.SaveChangesAsync();
            return ids;
        }

        private async Task<(bool Success, string Url, string Error)> UploadFileAsync(IFormFile file, string fileName = null)
        {
            var supportedTypes = new[] { "jpg", "jpeg", "png", "svg" };
            const int maxFileSize = 2_000_000; // 2MB

            if (file.Length > maxFileSize)
                return (false, null, "File is too large, 2MB max!");

            var fileExt = Path.GetExtension(file.FileName).TrimStart('.').ToLower();
            if (!supportedTypes.Contains(fileExt))
                return (false, null, "Only SVG/PNG/JPEG/JPG files are allowed!");

            fileName ??= $"{DateTime.UtcNow:ddssmm}_{file.FileName}";
            var result = await _s3Service.UploadToS3Async(file, fileName);

            return result.Success ? (true, result.FileName, null) : (false, null, "Failed to upload file, something went wrong.");
        }

        public async Task<CustomResult<string>> DeleteAsync(long id, string createdBy)
        {
            var question = await _context.ExamQuestions.FirstOrDefaultAsync(q => q.QuestionBankId == id);
            if (question == null)
                return CustomResult<string>.ErrorOccured("Exam question not found.", ResponseCodes.NotFoundErrorCode);

            _context.ExamQuestions.Remove(question);
            await _context.SaveChangesAsync();
            await _auditLogService.AddToAudit((int)AuditActionType.Delete, "Exam", $"User [{createdBy}] deleted an exam question with instruction - {question.Institution} at {DateTime.UtcNow}.");

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Exam question deleted successfully.");
        }

        public async Task<CustomResult<PaginatedResult<ExamQuestionListModel>>> GetAllAsync(ExamQuestionFilterModel query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<ExamQuestionListModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<ExamSchedule> records = _context.ExamSchedules
                    .Include(e => e.Course)
                    .Include(e => e.Level)
                    .Include(e => e.ExamQuestions).ThenInclude(x => x.QuestionBank)
                    .Where(e => !e.IsDeleted && e.InstitutionId == InsTId.Data);


                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(e =>
                        e.Course.Name.ToLower().Contains(word) ||
                        e.Course.CourseCode.ToLower().Contains(word));
                }
                if (query.SessionId > 0)
                {
                    records = records.Where(e => e.AcademicSessionId == query.SessionId);
                }
                if (query.SemesterId > 0)
                {
                    records = records.Where(e => e.SemesterId == query.SemesterId);
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(e => e.Course.Name)
                    : records.OrderByDescending(e => e.CreatedAt);

                var result = await records.Select(e => new ExamQuestionListModel
                {
                    Id = e.Id,
                    Questions = (e.ExamQuestions == null) ? 0 : e.ExamQuestions.Count,
                    Level = e.Level == null ? null : e.Level.LevelName,
                    ExamDate = e.StartDate,
                    Status = e.Status,
                    DateCreated = e.CreatedAt,
                    Course = new CourseModel
                    {
                        Id = e.Course.Id,
                        Name = e.Course.Name,
                        CourseCode = e.Course.CourseCode
                    }
                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ExamQuestionListModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ExamQuestionListModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamQuestionListModel>> GetByIdAsync(long id)
        {
            try
            {
                var query = await _context.ExamSchedules
                    .Include(e => e.Course)
                    .Include(e => e.Level)
                    .Include(e => e.ExamQuestions)
                    .FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

                if (query == null)
                    return CustomResult<ExamQuestionListModel>.ErrorOccured("Question not found.", ResponseCodes.NotFoundErrorCode);

                var response = new ExamQuestionListModel
                {
                    Id = query.Id,
                    Questions = (query.ExamQuestions == null) ? 0 : query.ExamQuestions.Count,
                    Level = query.Level == null ? null : query.Level.LevelName,
                    ExamDate = query.StartDate,
                    Status = query.Status,
                    DateCreated = query.CreatedAt,
                    Course = new CourseModel
                    {
                        Id = query.Course.Id,
                        Name = query.Course.Name,
                        CourseCode = query.Course.CourseCode
                    }
                };

                return CustomResult<ExamQuestionListModel>.Success(response, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamQuestionListModel>.ErrorOccured("An error occurred while fetching the exam question.", ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamQuestionCreateDto>> UpdateAsync(long id, ExamQuestionCreateDto model, string createdBy)
        {
            var errorList = new List<string>();
            var exam = await _context.ExamQuestions.FirstOrDefaultAsync(q => q.Id == id);

            if (exam == null)
                return CustomResult<ExamQuestionCreateDto>.ErrorOccured("Exam question not found.", ResponseCodes.NotFoundErrorCode);

            if (string.IsNullOrWhiteSpace(model.Instruction))
                return CustomResult<ExamQuestionCreateDto>.ErrorOccured("Instruction text is required.", ResponseCodes.BadRequestErrorCode);

            var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == model.ExamScheduleId);
            if (examSchedule is null)
                return CustomResult<ExamQuestionCreateDto>.ErrorOccured("Invalid exam schedule id passed!.", ResponseCodes.BadRequestErrorCode);

            if (model.QuestionIds != null && model.QuestionIds.Count > 0)
            {
                var ExamQuestionId = model.QuestionIds[0];
                foreach (var questionId in model.QuestionIds)
                {
                    var oldQuestions = await _context.ExamQuestions.Where(x => x.Id == questionId).ToListAsync();
                    if (oldQuestions.Count > 0)
                    {
                        //check if the examination has been taken by any student at all before removing it from the schedule
                        await RemoveTheQuestionsOnTheExamSchedule(oldQuestions);
                        foreach (var QuestionId in model.QuestionIds)
                        {
                            //get details of selected priviledges
                            var questionBank = await _context.QuestionBanks.FirstOrDefaultAsync(p => p.Id == QuestionId);
                            if (questionBank == null)
                            {
                                errorList.Add($"Invalid Question Id - {QuestionId}");
                                continue;
                            }
                            var examQuestion = new ExamQuestion
                            {
                                ExamScheduleId = model.ExamScheduleId,
                                Instruction = model.Instruction,
                                MakeQuestionsAppearRandom = model.MakeQuestionsAppearRandom,
                                QuestionBankId = QuestionId,
                                IsActive = true,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false,
                            };
                            _context.ExamQuestions.Add(examQuestion);
                        }
                    }
                    else
                    {
                        foreach (var QuestionId in model.QuestionIds)
                        {

                            var questionBank = await _context.QuestionBanks.FirstOrDefaultAsync(p => p.Id == QuestionId);
                            if (questionBank == null)
                            {
                                errorList.Add($"Invalid Question Id - {QuestionId}");
                                continue;
                            }
                            var examQuestion = new ExamQuestion
                            {
                                ExamScheduleId = model.ExamScheduleId,
                                Instruction = model.Instruction,
                                MakeQuestionsAppearRandom = model.MakeQuestionsAppearRandom,
                                QuestionBankId = QuestionId,
                                IsActive = true,
                                UpdatedAt = DateTime.UtcNow,
                                CreatedAt = DateTime.UtcNow,
                                IsDeleted = false,
                            };
                            _context.ExamQuestions.Update(examQuestion);
                        }
                    }
                }
            }

            await _context.SaveChangesAsync();

            if (errorList.Count > 0)
            {
                JsonConvert.SerializeObject(errorList);
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] added Question added to exam at {DateTime.UtcNow}. but has this error list {JsonConvert.SerializeObject(errorList)}");
            }
            else
            {
                await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] added Question added to exam at {DateTime.UtcNow}.");
            }

            return CustomResult<ExamQuestionCreateDto>.Success(model, "Question added to exam successfully.", errorList);
        }

        private async Task RemoveTheQuestionsOnTheExamSchedule(List<ExamQuestion> oldQuestions)
        {
            _context.ExamQuestions.RemoveRange(oldQuestions);
            await _context.SaveChangesAsync();

        }

        public async Task<CustomResult<PaginatedResult<CandidateExamScheduleModel>>> CandidatePulishedExamSchedule(string candidateId, QueryModelMini query)
        {
            try
            {
                var user = await _context.Candidates.FirstOrDefaultAsync(x => x.UserId == candidateId);
                if (user == null)
                    return CustomResult<PaginatedResult<CandidateExamScheduleModel>>.ErrorOccured("Candidate not found.", ResponseCodes.NotFoundErrorCode);

                //get the candidate
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<ExamSchedule> records = _context.ExamSchedules
                    .Include(e => e.Course)
                    .Include(e => e.Level)
                    .Include(e => e.DepartmentExamSchedule)
                    .Include(e => e.ExamQuestions)
                    .Where(e => e.LevelId == user.LevelId
                    && e.Status == ExamScheduleStatusEnum.PUBLISHED.GetEnumText()
                    && !e.IsDeleted);

                //get the department exam schedule
                var deptExams = await _context.DepartmentExamSchedules
                    .Include(e => e.ExamSchedule)
                    .Where(e => e.DepartmentId == user.DepartmentId && !e.IsDeleted)
                    .Select(e => e.ExamScheduleId).ToListAsync();

                //filter out the exam schedules from the department exam schedule
                records = records.Where(e => deptExams.Contains(e.Id));

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(e =>
                        e.Course.Name.ToLower().Contains(word) ||
                        e.Level.LevelName.ToLower().Contains(word) ||
                        e.Course.CourseCode.ToLower().Contains(word));
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(e => e.Course.Name)
                    : records.OrderByDescending(e => e.CreatedAt);

                var result = await records.Select(e => new CandidateExamScheduleModel
                {
                    Id = e.Id,
                    Title = e.Title,
                    IsDeleted = e.IsDeleted,
                    IsActive = e.IsActive,
                    NoOfAttempts =
                    _context.CandidateExamsSubmissions
                    .Any(x => x.CandidateId == user.Id && x.ExamScheduleId == e.Id
                    && x.Allowresit == false
                    ) ? 1 : 0,
                    IsResitEnabled = _context.CandidateExamsSubmissions
                    .Any(x => x.CandidateId == user.Id && x.ExamScheduleId == e.Id
                    && x.Allowresit == true
                    ),

                    //_context.CandidateExamsSubmissions
                    //.Where(x => x.CandidateId == user.Id && x.ExamScheduleId == e.Id
                    ////&& !x.Allowresit.Value
                    ////&& x.IsResultPublished.Value && x.IsGraded.Value
                    //).Count(),
                    Description = string.IsNullOrEmpty(e.Description) ? "N/A" : e.Description,
                    Questions = (e.ExamQuestions == null) ? 0 : e.ExamQuestions.Count,
                    //Department = e.Department == null ? null : e.Department.Name,
                    Department = _context.Departments.FirstOrDefault(x => x.Id == user.DepartmentId).Name,
                    Level = e.Level == null ? null : e.Level.LevelName,
                    StartDate = Utility.ToLocal(e.StartDate).Value,
                    EndDate = Utility.ToLocal(e.EndDate),
                    StartTime = Utility.ToLocal(e.StartTime).Value,
                    EndTime = Utility.ToLocal(e.EndTime),
                    ExamDurationInMinutes = e.ExamDurationInMinutes.Value
                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateExamScheduleModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateExamScheduleModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamInstructionModel>> GetExamInstruction(long examScheduleId)
        {
            try
            {
                var exam = await _context.ExamSchedules
                                    .Include(x => x.ExamQuestions)
                                    .Include(x => x.Course)
                                    .Include(x => x.ProctorConfigurations)
                                    .FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (exam == null)
                    return CustomResult<ExamInstructionModel>.ErrorOccured("Exam details not found.", ResponseCodes.NotFoundErrorCode);

                List<string> actionNames = await _context.EnforcementActions.Select(x => x.Name).ToListAsync();

                var proctorRules = new List<ProctorConfigurationRuleDto>();

                var configList = exam.ProctorConfigurations
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

                var returnDto = new ExamInstructionModel
                {
                    Id = exam.Id,
                    isProctorEnabled = exam.EnableAiProctoring,
                    CourseCode = exam.Course.CourseCode,
                    CourseTitle = exam.Course.Name,
                    Instruction = exam.Instruction,
                    ExamDurationInMinutes = exam.ExamDurationInMinutes.Value,
                    TotalQuestions = exam.ExamQuestions == null ? 0 : exam.ExamQuestions.Count,
                    ProctorConfigurations = proctorRules
                };
                return CustomResult<ExamInstructionModel>.Success(returnDto, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamInstructionModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamCreateModel>> QuestionsInExamSchedule(long examScheduleId, int? PageNumber, int? PageSize)
        {
            try
            {
                var exam = await _context.ExamSchedules
                                    .Include(x => x.ExamQuestions)
                                    .FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (exam == null)
                    return CustomResult<ExamCreateModel>.ErrorOccured("Exam details not found.", ResponseCodes.NotFoundErrorCode);

                var returnDto = new ExamCreateModel
                {
                    ExamDurationInMinutes = exam.ExamDurationInMinutes.Value,
                    Instruction = exam.Instruction,
                    TotalQuestions = exam.ExamQuestions == null ? 0 : exam.ExamQuestions.Count,
                    Questions = await GetQuestionsAndOptions(examScheduleId, exam.IsRandomized, PageNumber, PageSize)
                };
                return CustomResult<ExamCreateModel>.Success(returnDto, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamCreateModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamCreateModelForCandidate>> QuestionsInExamScheduleForACandidate(string candidateUserId, long examScheduleId, int? PageNumber, int? PageSize)
        {
            try
            {
                var user = await _context.Candidates.FirstOrDefaultAsync(x => x.UserId == candidateUserId);
                if (user == null)
                    return CustomResult<ExamCreateModelForCandidate>.ErrorOccured("Candidate not found.", ResponseCodes.NotFoundErrorCode);

                var exam = await _context.ExamSchedules
                                    .Include(x => x.ExamQuestions)
                                    .FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (exam == null)
                    return CustomResult<ExamCreateModelForCandidate>.ErrorOccured("Exam details not found.", ResponseCodes.NotFoundErrorCode);

                var questionInSchedule = exam.ExamQuestions.Select(x => x.QuestionBankId);

                var studentTestRecord = await _context.CandidateExamResults
                                        .Where(x => x.ExamScheduleId == examScheduleId
                                        && x.CandidateId == user.Id
                                        && !x.AllowResit
                                        )
                                        .FirstOrDefaultAsync();
                if (studentTestRecord == null)
                    return CustomResult<ExamCreateModelForCandidate>.ErrorOccured("No result found for this student.", ResponseCodes.NotFoundErrorCode);

                var returnDto = new ExamCreateModelForCandidate
                {
                    ExamDurationInMinutes = studentTestRecord.TotalTimeSpent,

                    Instruction = exam.Instruction,

                    TotalScore = studentTestRecord.Score,

                    TotalQuestions = exam.ExamQuestions?.Count ?? 0,

                    TotalQuestionsScore = studentTestRecord.TotalPoints,

                    Questions = await GetQuestionsAndOptionsForACandidate(user.Id, examScheduleId, exam.IsRandomized, PageNumber, PageSize)
                };
                return CustomResult<ExamCreateModelForCandidate>.Success(returnDto, ResponseMessages.SuccessMessage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamCreateModelForCandidate>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }


        public async Task<CustomResult<viewCountUpdateResponse>> UpdateCandidateViewCount(long examScheduleId, long candidateId)
        {
            try
            {

                //var loggedInUser = await _userManagementService.GetLoggedInUser();
                var instData = await _userManagementService.GetTenantId();

                var exam = await _context.ExamSchedules
                           .Include(x => x.ExamQuestions)
                           .FirstOrDefaultAsync(x => x.Id == examScheduleId && x.InstitutionId == instData.Data);
                if (exam == null)
                    return CustomResult<viewCountUpdateResponse>.ErrorOccured("Exam details not found.", ResponseCodes.NotFoundErrorCode);


                long NoOfView = 0;

                var checkIfViewed = await _context.CandidateExamViewAndAttempts
               .FirstOrDefaultAsync(x => x.CandidateId == candidateId && x.ExamScheduleId == examScheduleId);
                if (checkIfViewed is null)
                {
                    //Add the candidate to this table getCandidateDetail
                    var toAddAsViewed = new CandidateExamViewAndAttempts
                    {
                        CandidateId = candidateId,
                        InstitutionId = instData.Data,
                        IsActive = true,
                        IsDeleted = false,
                        ExamScheduleId = examScheduleId,
                        CreatedAt = DateTime.Now,
                        NoOfViewTimes = 1
                    };

                    var viewData = await _context.CandidateExamViewAndAttempts.AddAsync(toAddAsViewed);
                    await _context.SaveChangesAsync();

                }
                else
                {
                    //update the view count
                    NoOfView = checkIfViewed.NoOfViewTimes + 1;
                    checkIfViewed.NoOfViewTimes = NoOfView;

                    _context.CandidateExamViewAndAttempts.Update(checkIfViewed);
                    await _context.SaveChangesAsync();



                }


                var returnDto = new viewCountUpdateResponse
                {
                    NoOfView = NoOfView,
                    CandidateId = candidateId,
                    ExamScheduleId = examScheduleId
                };
                return CustomResult<viewCountUpdateResponse>.Success(returnDto, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<viewCountUpdateResponse>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }


        public async Task<CustomResult<ExamCreateModelNoCorrectoption>> GetQuestionsAndOptionsNoCorrectOption(long examScheduleId, int? PageNumber, int? PageSize)
        {
            try
            {
                var loggedInUser = await _userManagementService.GetLoggedInUser();
                var instData = await _userManagementService.GetTenantId();

                var getCandidateDetail = await _context.Candidates.FirstOrDefaultAsync(x => x.UserId ==
                loggedInUser.Data.Id);

                long NoOfView = 0;
                int NoOfAttempt = 0;
                bool IsResitEnabled = false;

                //check if candidate exist
                if (getCandidateDetail is not null)
                {
                    var checkIfViewed = await _context.CandidateExamViewAndAttempts
                   .FirstOrDefaultAsync(x => x.CandidateId == getCandidateDetail.Id && x.ExamScheduleId == examScheduleId);
                    if (checkIfViewed is not null)
                    {
                        NoOfView = checkIfViewed.NoOfViewTimes;

                    }

                    NoOfAttempt =
_context.CandidateExamsSubmissions
.Any(x => x.CandidateId == getCandidateDetail.Id && x.ExamScheduleId == examScheduleId
&& x.Allowresit == false
) ? 1 : 0;
                    IsResitEnabled = _context.CandidateExamsSubmissions
      .Any(x => x.CandidateId == getCandidateDetail.Id && x.ExamScheduleId == examScheduleId
      && x.Allowresit == true
      );

                }

                var exam = await _context.ExamSchedules
                                    .Include(x => x.ExamQuestions)
                                    .FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (exam == null)
                    return CustomResult<ExamCreateModelNoCorrectoption>.ErrorOccured("Exam details not found.", ResponseCodes.NotFoundErrorCode);



                var returnDto = new ExamCreateModelNoCorrectoption
                {
                    NoOfView = NoOfView,
                    NoOfAttempts = NoOfAttempt,
                    IsResitEnabled = IsResitEnabled,
                    ExamDurationInMinutes = exam.ExamDurationInMinutes.Value,
                    Instruction = exam.Instruction,
                    TotalQuestions = exam.ExamQuestions == null ? 0 : exam.ExamQuestions.Count,
                    Questions = await GetQuestionsAndOptionsNoCorrectOption(examScheduleId, exam.IsRandomized, PageNumber, PageSize)
                };
                return CustomResult<ExamCreateModelNoCorrectoption>.Success(returnDto, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamCreateModelNoCorrectoption>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task<PaginatedResult<QuestionsListModelNoCorrectOption>> GetQuestionsAndOptionsNoCorrectOption(long examScheduleId, bool isRandomized, int? PageNumber, int? PageSize)
        {
            var questions = await _context.ExamQuestions
                    .Include(x => x.QuestionBank).ThenInclude(c => c.QuestionOptions)
                    .Where(x => x.ExamScheduleId == examScheduleId)
                    .Select(x => new QuestionsListModelNoCorrectOption
                    {

                        QuestionId = x.QuestionBankId.Value,
                        Point = x.QuestionBank.Points,
                        QuestionTag = x.QuestionBank.Tags,
                        IsMultiChoice = x.QuestionBank.IsMultipleChoice,
                        DifficultLevel = x.QuestionBank.DifficultLevel,
                        Question = string.IsNullOrEmpty(x.QuestionBank.ImageUrl) ? x.QuestionBank.Question : x.QuestionBank.ImageUrl,
                        Options = x.QuestionBank == null ? null : x.QuestionBank.QuestionOptions.Select(o =>
                        new OptionForExamModelNoCorrectionOption
                        {
                            OptionTag = o.OptionTag,
                            OptionDescription = string.IsNullOrEmpty(o.ImageUrl) ? o.OptionDescription : o.ImageUrl,
                            Id = o.Id
                        }).ToList()
                    }).ToListAsync();

            if (isRandomized)
            {
                var random = new Random();
                questions = questions.OrderBy(x => random.Next()).ToList();
            }
            else
            {
                questions = questions.OrderBy(x => x.QuestionId).ToList();
            }
            return questions.ToPageList(PageNumber ?? 1, PageSize ?? 10);
        }

        private async Task<PaginatedResult<QuestionsListModel>> GetQuestionsAndOptions(long examScheduleId, bool isRandomized, int? PageNumber, int? PageSize)
        {
            var questions = await _context.ExamQuestions
                    .Include(x => x.QuestionBank).ThenInclude(c => c.QuestionOptions)
                    .Where(x => x.ExamScheduleId == examScheduleId)
                    .Select(x => new QuestionsListModel
                    {

                        QuestionId = x.QuestionBankId.Value,
                        Question = string.IsNullOrEmpty(x.QuestionBank.ImageUrl) ? x.QuestionBank.Question : x.QuestionBank.ImageUrl,
                        Options = x.QuestionBank == null ? null : x.QuestionBank.QuestionOptions.Select(o =>
                        new OptionForExamModel
                        {
                            OptionTag = o.OptionTag,
                            OptionDescription = string.IsNullOrEmpty(o.ImageUrl) ? o.OptionDescription : o.ImageUrl,
                            IsCorrectOption = o.IsCorrectOption,
                            Id = o.Id
                        }).ToList()
                    }).ToListAsync();

            if (isRandomized)
            {
                var random = new Random();
                questions = questions.OrderBy(x => random.Next()).ToList();
            }
            else
            {
                questions = questions.OrderBy(x => x.QuestionId).ToList();
            }
            return questions.ToPageList(PageNumber ?? 1, PageSize ?? 10);
        }

        private async Task<PaginatedResult<QuestionsListModelForCandidate>> GetQuestionsAndOptionsForACandidate(long candidateId, long examScheduleId, bool isRandomized, int? PageNumber, int? PageSize)
        {
            var page = PageNumber.GetValueOrDefault(1);
            var size = PageSize.GetValueOrDefault(10);

            var questions = await _context.ExamQuestions
                            .Include(x => x.ExamSchedule)
                            .Include(x => x.QuestionBank).ThenInclude(c => c.QuestionOptions)
                            .Where(x => x.ExamScheduleId == examScheduleId)
                            .ToListAsync();

            var submissions = await _context.CandidateExamsSubmissions
                            .Where(b => b.CandidateId == candidateId
                            &&
                            //!b.IsDeleted
                            !b.Allowresit.Value
                            && b.ExamScheduleId == examScheduleId)
                            .ToListAsync();

            var questionModels = questions.Select(x =>
            {
                var sub = submissions.FirstOrDefault(b => b.QuestionBankId == x.QuestionBankId);

                return new QuestionsListModelForCandidate
                {
                    QuestionId = x.QuestionBankId ?? 0,

                    IsMultiChoice = x.QuestionBank?.IsMultipleChoice ?? false,

                    PassMarkOnExamSchedule = x.ExamSchedule?.PassScore ?? 0,

                    OptionIdSelected = sub?.SelectedQuestionOptionId ?? 0,

                    Score = sub?.Score ?? 0,

                    Question = string.IsNullOrEmpty(x.QuestionBank?.ImageUrl)
                        ? x.QuestionBank?.Question
                        : x.QuestionBank?.ImageUrl,

                    Options = x.QuestionBank?.QuestionOptions?.Select(o => new OptionForExamModelForCandidate
                    {
                        OptionTag = o.OptionTag,
                        OptionDescription = string.IsNullOrEmpty(o.ImageUrl) ? o.OptionDescription : o.ImageUrl,
                        IsCorrectOption = o.IsCorrectOption,
                        Id = o.Id
                    }).ToList()
                };
            }).ToList();

            if (isRandomized)
            {
                var rnd = new Random();
                questionModels = questionModels.OrderBy(_ => rnd.Next()).ToList();
            }
            else
            {
                questionModels = questionModels.OrderBy(x => x.QuestionId).ToList();
            }

            return questionModels.ToPageList(page, size);
        }

        public async Task<CustomResult<PaginatedResult<CandidateExamListModel>>> CandidateResultList(string candidateId, ExamQuestionFilterModel query)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<CandidateExamResult> records = _context.CandidateExamResults
                    .Include(e => e.ExamSchedule).ThenInclude(e => e.Course)
                    .Include(e => e.Candidate).ThenInclude(e => e.User)
                    //.Where(e => e.Candidate.UserId == candidateId && e.ExamSchedule.InstantResultPublishing);
                    .Where(e => e.Candidate.UserId == candidateId && e.ExamSchedule.IsPublished);

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(e =>
                        e.ExamSchedule.Course.Name.ToLower().Contains(word) ||
                        e.ExamSchedule.Course.CourseCode.ToLower().Contains(word));
                }
                if (query.SessionId > 0)
                {
                    records = records.Where(e => e.ExamSchedule.AcademicSessionId == query.SessionId);
                }
                if (query.SemesterId > 0)
                {
                    records = records.Where(e => e.ExamSchedule.SemesterId == query.SemesterId);
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(e => e.ExamSchedule.Course.Name)
                    : records.OrderByDescending(e => e.CreatedAt);

                records = records.OrderByDescending(e => e.ExamSchedule.StartDate);

                var result = await records.Select(e => new CandidateExamListModel
                {
                    ExamScheduleId = e.ExamScheduleId,
                    IsResultPublished = e.ExamSchedule.IsPublished,
                    Course = e.ExamSchedule.Course.Name,
                    Code = e.ExamSchedule.Course.CourseCode,
                    Score = e.Score,
                    TotalScoreOnTheExam = e.TotalPoints,
                    TotalQuestions = e.TotalQuestions,
                    TimeSpent = e.TotalTimeSpent,
                    ExamDate = e.CreatedAt
                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.OrderByDescending(x => x.ExamDate).ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.OrderByDescending(x => x.ExamDate).NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateExamListModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateExamListModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExamSubmissionDetailModel>> GetCandidateExaminationDetails(long examScheduleId, long candidateId, int? PageNumber, int? PageSize)
        {
            try
            {
                var record = await _context.ExamSchedules
                        .Include(e => e.Course)
                        .FirstOrDefaultAsync(e => e.Id == examScheduleId);
                if (record == null)
                {
                    return CustomResult<ExamSubmissionDetailModel>.ErrorOccured("Exam details not found", ResponseCodes.NotFoundErrorCode);
                }

                var questionsAndOptions = await GetAllQuestionsWithOptionsAsync(examScheduleId, candidateId, PageNumber, PageSize);

                var result = new ExamSubmissionDetailModel
                {
                    Instruction = record.Instruction,
                    Course = (record.Course == null) ? "N/A" : record.Course.Name,
                    Questions = (questionsAndOptions.Items.Any()) ? questionsAndOptions : null,
                    Score = (questionsAndOptions.Items.Any()) ? questionsAndOptions.Items.First().Score : 0,
                    TimeSpent = (questionsAndOptions.Items.Any()) ? questionsAndOptions.Items.First().TimeSpent : "n/a"
                };
                return CustomResult<ExamSubmissionDetailModel>.Success(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExamSubmissionDetailModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task<PaginatedResult<QuestionsDetailsModel>> GetAllQuestionsWithOptionsAsync(long examScheduleId, long candidateId, int? PageNumber, int? PageSize)
        {
            var InsTId = await _userManagementService.GetTenantId();


            IQueryable<CandidateExamsSubmission> records = _context.CandidateExamsSubmissions
                        .Include(e => e.Candidate)
                        .Include(e => e.ExamSchedule).ThenInclude(x => x.Course)
                        .Include(e => e.QuestionBank).ThenInclude(q => q.QuestionOptions)
                //.Include(e => e.QuestionOption)
                .Where(e => e.ExamScheduleId == examScheduleId && e.CandidateId == candidateId
                && e.InstitutionId == InsTId.Data);

            var result = await records.Select(x => new QuestionsDetailsModel
            {
                QuestionId = x.QuestionBankId.Value,
                Question = string.IsNullOrEmpty(x.QuestionBank.ImageUrl) ? x.QuestionBank.Question : x.QuestionBank.ImageUrl,
                Options = x.QuestionBank == null ? null : x.QuestionBank.QuestionOptions.Select(o => new OptionForExamModel
                {
                    OptionTag = o.OptionTag,
                    OptionDescription = string.IsNullOrEmpty(o.ImageUrl) ? o.OptionDescription : o.ImageUrl,
                    IsCorrectOption = o.IsCorrectOption
                }).ToList(),
                SelectedOptionId = x.SelectedQuestionOptionId.Value,
                IsCorrectOption = x.IsCorrectOption ?? false,
                SubmissionDateAndTime = x.SubmissionDateAndTime,
                TimeSpent = x.TotalTimeSpent,
                Score = x.Score
            }).ToListAsync();

            var paginated = PageNumber.HasValue && PageSize.HasValue
                    ? result.ToPageList(PageNumber.Value, PageSize.Value)
                    : result.NoPaginate(0, 0);

            return paginated;
        }

        public async Task<CustomResult<PaginatedResult<DeptAndFacultyScheduleExamQuestionListModel>>> AllDepartmentResultSubmissionExam(DepartmeentFacultyScheduleExamQuestionFilterModel query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<DeptAndFacultyScheduleExamQuestionListModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                var records = _context.DepartmentExamSchedules
                     .Include(x => x.Department).ThenInclude(x => x.Faculty)
                     .Include(x => x.ExamSchedule).ThenInclude(x => x.Course)
                     .Include(x => x.ExamSchedule).ThenInclude(x => x.Institution)
                     .Include(x => x.ExamSchedule).ThenInclude(x => x.Level)
                     .Include(x => x.ExamSchedule).ThenInclude(x => x.Semester)
                     .Include(x => x.ExamSchedule).ThenInclude(x => x.AcademicSession)
                     .Where(x => x.Department.InstitutionId == InsTId.Data)
                     .AsQueryable();

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(e =>
                        e.ExamSchedule.Course.Name.ToLower().Contains(word) ||
                        e.ExamSchedule.Institution.Name.ToLower().Contains(word) ||
                        e.Department.Name.ToLower().Contains(word) ||
                        e.ExamSchedule.Semester.SemesterName.ToLower().Contains(word) ||
                        e.ExamSchedule.AcademicSession.Name.ToLower().Contains(word) ||
                        e.ExamSchedule.Level.LevelName.ToLower().Contains(word) ||
                        e.ExamSchedule.Course.CourseCode.ToLower().Contains(word));
                }
                if (query.SessionId > 0)
                {
                    records = records.Where(e => e.ExamSchedule.AcademicSessionId == query.SessionId);
                }
                if (query.SemesterId > 0)
                {
                    records = records.Where(e => e.ExamSchedule.SemesterId == query.SemesterId);
                }
                if (query.DepartmentId > 0)
                {
                    records = records.Where(e => e.Department.Id == query.DepartmentId);
                }
                if (query.LevelId > 0)
                {
                    records = records.Where(e => e.ExamSchedule.LevelId == query.LevelId);
                }
                if (query.FacultyId > 0)
                {
                    records = records.Where(e => e.Department.FacultyId == query.FacultyId);
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(e => e.ExamSchedule.Course.Name)
                    : records.OrderByDescending(e => e.CreatedAt);

                var result = await records.Select(e => new DeptAndFacultyScheduleExamQuestionListModel
                {
                    Id = e.ExamSchedule.Id,
                    ExaminationTitle = e.ExamSchedule.Title,
                    Department = e.Department == null ? "n/a" : e.Department.Name,
                    Faculty = e.Department.Faculty == null ? "n/a" : e.Department.Faculty.Name,
                    Level = e.ExamSchedule.Level == null ? "n/a" : e.ExamSchedule.Level.LevelName,
                    Session = e.ExamSchedule.AcademicSession == null ? "n/a" : e.ExamSchedule.AcademicSession.Name,
                    Semester = e.ExamSchedule.Semester == null ? "n/a" : e.ExamSchedule.Semester.SemesterName,
                    Status = e.ExamSchedule.Status,
                    IsResultPublished = e.ExamSchedule.IsPublished,
                    ExamDate = e.ExamSchedule.StartDate

                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<DeptAndFacultyScheduleExamQuestionListModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<DeptAndFacultyScheduleExamQuestionListModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
        public async Task<CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>>
            AllResultSubmissionExam(ScheduleExamQuestionFilterModel query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<ExamSchedule> records = _context.ExamSchedules
                    .Include(e => e.Course)
                    .Include(e => e.Institution)
                    .Include(e => e.Level)
                    .Include(e => e.Semester)
                    .Include(e => e.AcademicSession)
                    .Where(x => x.InstitutionId == InsTId.Data);

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(e =>
                        e.Course.Name.ToLower().Contains(word) ||
                        e.Institution.Name.ToLower().Contains(word) ||
                        e.Semester.SemesterName.ToLower().Contains(word) ||
                        e.AcademicSession.Name.ToLower().Contains(word) ||
                        e.Level.LevelName.ToLower().Contains(word) ||
                        e.Course.CourseCode.ToLower().Contains(word));
                }
                if (query.SessionId > 0)
                {
                    records = records.Where(e => e.AcademicSessionId == query.SessionId);
                }
                if (query.SemesterId > 0)
                {
                    records = records.Where(e => e.SemesterId == query.SemesterId);
                }

                if (query.LevelId > 0)
                {
                    records = records.Where(e => e.LevelId == query.LevelId);
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(e => e.Course.Name)
                    : records.OrderByDescending(e => e.CreatedAt);

                var result = await records.Select(e => new ScheduleExamQuestionListModel
                {
                    Id = e.Id,
                    ExaminationTitle = e.Title,
                    Departments = _context.DepartmentExamSchedules.Include(x => x.Department)
                    .Where(x => x.ExamScheduleId == e.Id).Select(d => new DepartmentDto
                    {
                        DateCreated = d.Department.CreatedAt,
                        Name = d.Department.Name,
                        Id = d.Department.Id

                    }).ToList(),
                    Level = e.Level == null ? "n/a" : e.Level.LevelName,
                    Session = e.AcademicSession == null ? "n/a" : e.AcademicSession.Name,
                    Semester = e.Semester == null ? "n/a" : e.Semester.SemesterName,
                    Status = e.Status,
                    ExamDate = e.StartDate,
                    Description = e.Description,
                    IsResultPublish = e.IsPublished,
                    noOfSubmission =
                    _context.CandidateExamsSubmissions.Where(x => x.ExamScheduleId == e.Id).Select(x => x.CandidateId).Distinct().Count()

                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ScheduleExamQuestionListModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<SingleExamResultListModel>> SingleExamRecordsByCourse(long examScheduleId, ExamRecordsByDepartmentFilterModel search)
        {
            try
            {
                var tenantResult = await _userManagementService.GetTenantId();
                if (!tenantResult.IsSuccess)
                    return CustomResult<SingleExamResultListModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var institutionId = tenantResult.Data;

                var examSchedule = await _context.ExamSchedules
                    .Include(x => x.Level)
                    .Where(e => e.Id == examScheduleId && e.InstitutionId == institutionId)
                    .Select(e => new
                    {
                        e.Id,
                        e.Title,
                        e.StartDate,
                        e.MaxQuestions,
                        e.PassScore,
                        LevelName = e.Level != null ? e.Level.LevelName : "n/a"
                    })
                    .FirstOrDefaultAsync();

                if (examSchedule == null)
                    return CustomResult<SingleExamResultListModel>.ErrorOccured("Exam details not found", ResponseCodes.NotFoundErrorCode);

                var departments = await _context.DepartmentExamSchedules
                                .Include(e => e.Department)
                                .Where(e => e.ExamScheduleId == examScheduleId)
                                .Select(d => new DepartmentDto
                                {
                                    Id = d.Department.Id,
                                    Name = d.Department.Name,
                                    DateCreated = d.Department.CreatedAt
                                })
                                .ToListAsync();

                var records = await this.ExamRecordsByDepartment(examScheduleId, institutionId, search);

                var stats = await _context.CandidateExamResults
                                    .Where(x => x.ExamScheduleId == examScheduleId
                                    && !x.AllowResit
                                    )
                                    .GroupBy(x => x.ExamScheduleId)
                                    .Select(g => new
                                    {
                                        Average = g.Average(x => x.Score),
                                        Highest = g.Max(x => x.Score),
                                        Total = g.Select(x => x.CandidateId).Distinct().Count()
                                    })
                                    .FirstOrDefaultAsync();

                var result = new SingleExamResultListModel
                {
                    Id = examSchedule.Id,
                    ExaminationTitle = examSchedule.Title,
                    Department = departments,
                    Level = examSchedule.LevelName,
                    ExamDate = examSchedule.StartDate,
                    MaxQuestions = examSchedule.MaxQuestions ?? 0,
                    PassScore = examSchedule.PassScore ?? 0,
                    AverageScore = stats?.Average ?? 0,
                    HighestScore = stats?.Highest ?? 0,
                    TotalStudents = stats?.Total ?? 0,
                    BulkRecords = records.Data
                };
                return CustomResult<SingleExamResultListModel>.Success(result, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<SingleExamResultListModel>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task<CustomResult<PaginatedResult<CandidateExamResultListModel>>> ExamRecordsByDepartment(long examScheduleId, long institutionId, ExamRecordsByDepartmentFilterModel query)
        {
            try
            {
                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);

                var baseQuery = _context.CandidateExamResults
                                .AsNoTracking()
                                .Where(e => e.ExamScheduleId == examScheduleId);

                if (query.WhereResit.HasValue)
                {
                    baseQuery = baseQuery.Where(x => x.AllowResit == query.WhereResit);
                }
                else
                {
                    baseQuery = baseQuery.Where(x => !x.AllowResit);

                }

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    baseQuery = baseQuery.Where(e =>
                        e.Candidate.MatricNumber.ToLower().Contains(word) ||
                        e.Candidate.User.FirstName.ToLower().Contains(word) ||
                        e.Candidate.User.LastName.ToLower().Contains(word) ||
                        e.ExamSchedule.Course.CourseCode.ToLower().Contains(word));
                }

                if (query.SessionId > 0)
                    baseQuery = baseQuery.Where(e => e.ExamSchedule.AcademicSessionId == query.SessionId);

                if (query.SemesterId > 0)
                    baseQuery = baseQuery.Where(e => e.ExamSchedule.SemesterId == query.SemesterId);

                if (query.LevelId > 0)
                    baseQuery = baseQuery.Where(e => e.ExamSchedule.LevelId == query.LevelId);

                if (query.StartDate.HasValue && query.EndDate.HasValue)
                    baseQuery = baseQuery.Where(e => e.CreatedAt >= query.StartDate && e.CreatedAt <= endDate);

                if (query.Passed.HasValue)
                {
                    var passMark = await _context.ExamSchedules
                        .Where(x => x.Id == examScheduleId)
                        .Select(x => x.PassScore)
                        .FirstOrDefaultAsync();

                    baseQuery = query.Passed.Value
                        ? baseQuery.Where(x => x.Score >= passMark)
                        : baseQuery.Where(x => x.Score < passMark);
                }

                baseQuery = query.Sorting?.ToLower() == "asc"
                    ? baseQuery.OrderBy(e => e.ExamSchedule.Course.Name)
                    : baseQuery.OrderByDescending(e => e.CreatedAt);

                var result = baseQuery.Select(x => new CandidateExamResultListModel
                {
                    MatricNumber = x.Candidate.MatricNumber,
                    CandidateId = x.CandidateId,
                    CandidateUserId = x.Candidate.UserId,
                    CandidateName = x.Candidate.User == null
                                        ? "n/a"
                                        : x.Candidate.User.LastName + " " + x.Candidate.User.FirstName,
                    IsResitEnabled = x.AllowResit,
                    Department = x.Candidate.Department == null ? "n/a" : x.Candidate.Department.Name,
                    Score = x.Score,
                    Remark = string.IsNullOrEmpty(x.Remark) ? "n/a" : x.Remark,
                    ExamPublishedStatus = x.ExamSchedule.Status,
                    ResultPublished = x.ExamSchedule.IsPublished,
                    Date = x.CreatedAt
                });

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<CandidateExamResultListModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<CandidateExamResultListModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> AllowMultipleCandidatesExamResit(AllowMultipleCandidatesExamResitRequestDto request, string updatedBy)
        {
            try
            {

                foreach (var candidateId in request.CandidatesIds)
                {
                    await AllowCandidateExamResit(request.ExamScheduleId, candidateId, "");
                }

                string message = $"the {request.CandidatesIds.Count} candiates has been marked for Exam resit- action by User [{""}] at {DateTime.UtcNow}.";
                await _auditLogService.AddToAudit((int)AuditActionType.Edit, "Exam resit activates", message);

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> AllowCandidateExamResit(long examScheduleId, long candidateId, string createdBy)
        {
            try
            {
                var details = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (details == null)
                    return CustomResult<string>.ErrorOccured("Exam details not found", ResponseCodes.NotFoundErrorCode);

                var candidate = await _context.Candidates
                            .Include(y => y.User)
                            .FirstOrDefaultAsync(x => x.Id == candidateId);
                if (candidate == null)
                    return CustomResult<string>.ErrorOccured("Candidate not found", ResponseCodes.NotFoundErrorCode);

                var submissions = await _context.CandidateExamsSubmissions
                        .Where(x => x.ExamScheduleId == examScheduleId && x.CandidateId == candidateId)
                        .ToListAsync();

                if (!submissions.Any())
                    return CustomResult<string>.ErrorOccured("No submission found for candidate", ResponseCodes.NotFoundErrorCode);

                foreach (var sub in submissions)
                {
                    sub.Allowresit = true;    // Always true
                    sub.IsDeleted = false;    // Keep active
                    sub.UpdatedAt = DateTime.UtcNow;
                }

                _context.CandidateExamsSubmissions.UpdateRange(submissions);
                await _context.SaveChangesAsync();

                // Now update result summary (with incremental resit)
                await UpdateReSitOnResult(examScheduleId, candidateId);

                string message =
                        $"Candidate {candidate.User.FirstName} {candidate.User.LastName} " +
                        $"has been approved for another resit for exam: {details.Title}.";

                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Edit,
                    "Exam Resit Approved",
                    $"{message} Action by {createdBy} at {DateTime.UtcNow}"
                );

                return CustomResult<string>.Success(ResponseCodes.SuccessCode, message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<string>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private async Task UpdateReSitOnResult(long examScheduleId, long candidateId)
        {
            var exam = await _context.CandidateExamResults
                .Where(x => x.ExamScheduleId == examScheduleId && x.CandidateId == candidateId).ToListAsync();

            if (exam.Any())
            {
                foreach (var item in exam)
                {
                    item.AllowResit = true; // Always allow

                    _context.CandidateExamResults.Update(item);
                    await _context.SaveChangesAsync();
                }

            };
        }

        public async Task<CustomResult<string>> PublishResult(long examScheduleId, string createdBy)
        {
            try
            {
                var details = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == examScheduleId);
                if (details == null)
                {
                    return CustomResult<string>.ErrorOccured("Exam details not found", ResponseCodes.NotFoundErrorCode);
                }
                var isPublished = !details.IsPublished == true ? true : false;
                details.IsPublished = !details.IsPublished;
                details.UpdatedAt = DateTime.UtcNow;
                _context.ExamSchedules.Update(details);
                await _context.SaveChangesAsync();

                // publish/unpublish results
                var submissions = await _context.CandidateExamsSubmissions
                                    .Where(x => x.ExamScheduleId == examScheduleId)
                                    .ToListAsync();

                foreach (var s in submissions)
                {
                    s.IsResultPublished = !details.IsPublished;
                    s.UpdatedAt = DateTime.UtcNow;
                }
                _context.CandidateExamsSubmissions.UpdateRange(submissions);
                await _context.SaveChangesAsync();

                string message = (details.IsPublished)
                            ? "Exam Result published"
                            : "Exam Result unpublished";

                await _auditLogService.AddToAudit(
                    (int)AuditActionType.Edit,
                    "Exam Schedule",
                    $"{message} by User [{createdBy}] at {DateTime.UtcNow}."
                );

                if (isPublished)
                //if (details.IsPublished)
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

        public async Task<CustomResult<QuestionBankModel>> CreateSingleQuestionForExamAsync(SingleExamQuestionCreateModel model, string createdBy)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<QuestionBankModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var resp = new QuestionBankModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var errorList = new List<string>();
                    if (string.IsNullOrWhiteSpace(model.Instruction))
                        return CustomResult<QuestionBankModel>.ErrorOccured("Instruction text is required.", ResponseCodes.BadRequestErrorCode);

                    var examSchedule = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == model.ExamScheduleId);
                    if (examSchedule is null)
                        return CustomResult<QuestionBankModel>.ErrorOccured("Invalid exam schedule id passed!.", ResponseCodes.BadRequestErrorCode);
                    examSchedule.Instruction = model.Instruction;
                    _context.ExamSchedules.Update(examSchedule);

                    var createdQuestions = await this.CreateSingleQuestionAsync(examSchedule.CourseId,
                        model.QuestionDetails);


                    //check if the question has been mapped to the exam before now before remapping it again

                    var IsQuestionMappedToExamEarlier = await _context.ExamQuestions
                    .FirstOrDefaultAsync(x => x.ExamScheduleId == model.ExamScheduleId
                    && x.QuestionBankId == createdQuestions.ExamBankId);
                    if (IsQuestionMappedToExamEarlier is null)
                    {
                        var createExam = ExamQuestion.Create(model.Instruction,
                        model.ExamScheduleId, model.MakeQuestionsAppearRandom.Value,
                        createdQuestions.ExamBankId);
                        createExam.InstitutionId = InsTId.Data;

                        _context.ExamQuestions.Add(createExam);
                        await _context.ExamQuestions.AddAsync(createExam);

                        await _context.SaveChangesAsync();

                    }

                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] added a single question to an exam schedule - {examSchedule.Description} at {DateTime.UtcNow}.");

                    //get the details of the exams
                    if (createdQuestions.IsCreatedSuccessfully)
                    {
                        var exams = await _questionBankService.GetQuestionByAsyncId(createdQuestions.ExamBankId);
                        return CustomResult<QuestionBankModel>.Success(exams.Data, ResponseCodes.SuccessCode);

                    }
                    return CustomResult<QuestionBankModel>.Failure(CustomError.OperationError, ResponseCodes.SuccessCode);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ExamService).Name,
                                                nameof(SubmitExaminationAnswersAsync));
                    return CustomResult<QuestionBankModel>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }

        public async Task<CustomResult<QuestionBankModel>> RemoveMappedSingleQuestionFromExamAsync(long examscheduleId, long questionbankId, string createdBy)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<QuestionBankModel>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }
            //check if the questionbankId has been attempted and submission has been made earlier on the question bank Id
            var checkifSubmittedEarlier = await _context.CandidateExamsSubmissions
                .FirstOrDefaultAsync(x => x.QuestionBankId == questionbankId && x.ExamScheduleId == examscheduleId);
            if (checkifSubmittedEarlier is not null)
            {
                return CustomResult<QuestionBankModel>.Failure(CustomError.QuestionHasBeenAttemptedAlready,
                            ResponseCodes.OperationError);
            }

            //check if the exam schedule start date and time has end 
            var isStartedAlredy = await _context.ExamSchedules
                .FirstOrDefaultAsync(x => x.Id == examscheduleId && x.StartDate >= DateTime.UtcNow);
            if (isStartedAlredy is not null)
            {
                return CustomResult<QuestionBankModel>.Failure(CustomError.ExamStartTimeIsPastOrOngoing,
                         ResponseCodes.OperationError);

            }
            var resp = new QuestionBankModel();
            var strategy = _context.Database.CreateExecutionStrategy();

            return await strategy.ExecuteAsync(async () =>
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    var errorList = new List<string>();
                    //check if the question has been mapped to the exam before now before remapping it again

                    var IsQuestionMappedToExamEarlier = await _context.ExamQuestions
                    .Include(x => x.ExamSchedule)
                    .Include(x => x.QuestionBank)
                    .FirstOrDefaultAsync(x => x.ExamScheduleId == examscheduleId
                    && x.QuestionBankId == questionbankId);
                    if (IsQuestionMappedToExamEarlier is not null)
                    {
                        _context.ExamQuestions.Remove(IsQuestionMappedToExamEarlier);
                    }
                    else
                    {
                        return CustomResult<QuestionBankModel>.Failure(CustomError.QuestionNoMappedToExamSchedule,
                            ResponseCodes.OperationError);

                    }
                    var saved = await _context.SaveChangesAsync();

                    await transaction.CommitAsync();
                    await _auditLogService.AddToAudit((int)AuditActionType.Create, "Question", $"User [{createdBy}] unmapped a single exam question with Id {IsQuestionMappedToExamEarlier.Id} and Questionbank Id: {IsQuestionMappedToExamEarlier.QuestionBank.Id} from an exam schedule - {IsQuestionMappedToExamEarlier.ExamSchedule.Description} at {DateTime.UtcNow}.");

                    //get the details of the exams
                    if (saved > 0)
                    {
                        var exams = await _questionBankService.GetQuestionByAsyncId(questionbankId);
                        return CustomResult<QuestionBankModel>.Success(exams.Data, ResponseCodes.SuccessCode);

                    }
                    return CustomResult<QuestionBankModel>.Failure(CustomError.OperationError, ResponseCodes.SuccessCode);
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();
                    _logger.LogError(ex, "An error occurred in {Repo}.{MethodName}", typeof(ExamService).Name,
                                                nameof(SubmitExaminationAnswersAsync));
                    return CustomResult<QuestionBankModel>.Failure(CustomError.SystemExceptionError, ResponseCodes.OperationError);
                }
            });
        }

        public async Task<CustomResult<PaginatedResult<StudentExamsDto>>> CandidateExamsResultList(long candidateId, StudentExamDoneFilterModel search)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<StudentExamsDto>>.Failure(
                        CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(search.EndDate ?? DateTime.Now);

                IQueryable<CandidateExamResult> records = _context.CandidateExamResults
                    .Include(x => x.Candidate).ThenInclude(x => x.User)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.AcademicSession)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Semester)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Course)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Institution)
                    .Where(x => x.ExamSchedule.InstitutionId == InsTId.Data && x.CandidateId == candidateId);

                if (!string.IsNullOrWhiteSpace(search.SearchWord))
                {
                    var searchWord = search.SearchWord.Trim().ToLower();
                    records = records.Where(x =>
                        x.ExamSchedule.Title.ToLower().Contains(searchWord) ||
                        x.ExamSchedule.Course.Name.ToLower().Contains(searchWord) ||
                        x.ExamSchedule.Semester.SemesterName.ToLower().Contains(searchWord) ||
                        x.ExamSchedule.AcademicSession.Name.ToLower().Contains(searchWord) ||
                        x.Candidate.User.Email.ToLower().Contains(searchWord) ||
                        x.Candidate.MatricNumber.ToLower().Contains(searchWord) ||
                        x.Candidate.PhoneNumber.ToLower().Contains(searchWord) ||
                        x.Candidate.User.FirstName.ToLower().Contains(searchWord) ||
                        x.Candidate.User.LastName.ToLower().Contains(searchWord));
                }

                if (!string.IsNullOrEmpty(search.ExamTitle))
                {
                    records = records.Where(x => x.ExamSchedule.Title.ToLower().Contains(search.ExamTitle.ToLower()));
                }

                if (!string.IsNullOrEmpty(search.Course))
                {
                    records = records.Where(x => x.ExamSchedule.Course.Name.ToLower() == search.Course.ToLower());
                }

                if (search.StartDate.HasValue && search.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= search.StartDate && x.CreatedAt <= endDate);
                }

                if (search.Sorting?.ToLower() == "asc")
                {
                    records = records.OrderBy(x => x.Candidate.User.FirstName);
                }
                else
                {
                    records = records.OrderByDescending(x => x.CreatedAt);
                }
                var result = await records.Select(x => new StudentExamsDto
                {
                    Id = x.Id,
                    CandidateId = x.CandidateId,
                    ExamScheduleId = x.ExamScheduleId,
                    CandidateName = x.Candidate.User == null ? "n/a" : x.Candidate.User.LastName + " " + x.Candidate.User.FirstName,
                    ExamTitle = x.ExamSchedule == null ? "n/a" : x.ExamSchedule.Title,
                    Course = x.ExamSchedule.Course == null ? "n/a" : x.ExamSchedule.Course.Name,
                    CourseCode = x.ExamSchedule.Course == null ? "n/a" : x.ExamSchedule.Course.CourseCode,
                    Session = x.ExamSchedule.AcademicSession == null ? "n/a" : x.ExamSchedule.AcademicSession.Name,
                    Semester = x.ExamSchedule.Semester == null ? "n/a" : x.ExamSchedule.Semester.SemesterName,
                    Institution = x.ExamSchedule.Institution == null ? "n/a" : x.ExamSchedule.Institution.Name,
                    Timestamp = x.CreatedAt.ToString("dd MMM yyyy HH:mm tt")
                }).ToListAsync();

                var paginatedResult = search.PageNumber.HasValue && search.PageSize.HasValue
                    ? result.OrderByDescending(x => x.Id).ToPageList(search.PageNumber.Value, search.PageSize.Value)
                    : result.OrderByDescending(x => x.Id).NoPaginate(0, 0);

                return CustomResult<PaginatedResult<StudentExamsDto>>.Success(paginatedResult);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<StudentExamsDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<StudentExamDetailsDto>> CandidateExamsResultDetailsByExamSchedule(long candidateId, long examScheduleId)
        {
            try
            {
                if (candidateId <= 0 || examScheduleId <= 0)
                {
                    return CustomResult<StudentExamDetailsDto>.ErrorOccured("Invalid candidate or exam schedule Id", ResponseCodes.BadRequestErrorCode);
                }

                var query = await _context.CandidateExamResults
                    .Include(x => x.Candidate).ThenInclude(x => x.User)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.AcademicSession)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Semester)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Course)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Institution)
                    .FirstOrDefaultAsync(x => x.ExamScheduleId == examScheduleId && x.CandidateId == candidateId);
                if (query == null)
                {
                    return CustomResult<StudentExamDetailsDto>.ErrorOccured("No exam details found for the candidate", ResponseCodes.NotFoundErrorCode);
                }

                var returnDto = new StudentExamDetailsDto
                {
                    ExamScheduleId = query.ExamScheduleId,
                    Candidate = query.Candidate.User == null ? null : new CandidateListDto
                    {
                        Email = query.Candidate.User.Email,
                        FullName = query.Candidate.User.LastName + " " + query.Candidate.User.FirstName,
                        Id = query.Candidate.Id,
                        PhoneNumber = query.Candidate.User.PhoneNumber,
                        MatricNumber = query.Candidate.MatricNumber
                    },
                    ExamDetails = query.ExamSchedule == null ? null : new ExamTakenDetailsDto
                    {
                        Course = query.ExamSchedule.Course == null ? "n/a" : query.ExamSchedule.Course.Name,
                        ExamDuration = $"{query.ExamSchedule.ExamDurationInMinutes} minutes",
                        ExamTitle = query.ExamSchedule.Title,
                        Semester = query.ExamSchedule.Semester.SemesterName,
                        Session = query.ExamSchedule.AcademicSession.Name,
                        SubmissionStatus = "Successful",
                        TimeOfSubmission = query.CreatedAt.ToString("dd MMM yyyy HH:mm tt"),
                    }
                };

                return CustomResult<StudentExamDetailsDto>.Success(returnDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<StudentExamDetailsDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<ExportStudentExamsDto>> ExportCandidateExams(long examScheduleId)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<ExportStudentExamsDto>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                IQueryable<CandidateExamResult> records = _context.CandidateExamResults
                    .Include(x => x.Candidate).ThenInclude(x => x.User)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.AcademicSession)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Semester)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Course)
                    .Include(x => x.ExamSchedule).ThenInclude(x => x.Institution)
                    .Where(x => x.ExamSchedule.InstitutionId == InsTId.Data && x.ExamScheduleId == examScheduleId && !x.AllowResit)
                    .OrderByDescending(x => x.Id);

                var result = records.Select(x => new StudentExamsForExportDto
                {
                    MatricNumber = x.Candidate.MatricNumber,
                    CandidateName = x.Candidate.User == null ? "n/a" : x.Candidate.User.LastName + " " + x.Candidate.User.FirstName,
                    ExamTitle = x.ExamSchedule == null ? "n/a" : x.ExamSchedule.Title,
                    Course = x.ExamSchedule.Course == null ? "n/a" : x.ExamSchedule.Course.Name,
                    CourseCode = x.ExamSchedule.Course == null ? "n/a" : x.ExamSchedule.Course.CourseCode,
                    Session = x.ExamSchedule.AcademicSession == null ? "n/a" : x.ExamSchedule.AcademicSession.Name,
                    Semester = x.ExamSchedule.Semester == null ? "n/a" : x.ExamSchedule.Semester.SemesterName,
                    Institution = x.ExamSchedule.Institution == null ? "n/a" : x.ExamSchedule.Institution.Name,
                    Score = x.Score,
                    Remark = string.IsNullOrEmpty(x.Remark) ? "n/a" : x.Remark,
                    Timestamp = x.CreatedAt.ToString("dd MMM yyyy HH:mm tt")
                }).AsQueryable();

                if (!result.Any())
                {
                    return CustomResult<ExportStudentExamsDto>.ErrorOccured("No record found", ResponseCodes.NotFoundErrorCode);
                }

                var sb = new StringBuilder();
                sb.AppendLine("MatricNumber,CandidateName,ExamTitle,Course,CourseCode,Session,Semester,Institution,Score,Remark,Timestamp");

                foreach (var item in result)
                {
                    sb.AppendLine($"{item.MatricNumber},{item.CandidateName},{item.ExamTitle},{item.Course},{item.CourseCode},{item.Session},{item.Semester},{item.Institution},{item.Score},{item.Remark},{item.Timestamp}");
                }

                var csvString = sb.ToString();
                var csvBytes = Encoding.UTF8.GetBytes(csvString);
                var base64Csv = Convert.ToBase64String(csvBytes);

                var exportDto = new ExportStudentExamsDto
                {
                    Base64File = base64Csv,
                    FileName = $"ExamExport_{DateTime.Now:ddMMyyyyHHmmss}.csv",
                    ContentType = "text/csv"
                };

                return CustomResult<ExportStudentExamsDto>.Success(exportDto);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<ExportStudentExamsDto>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }
    }
}