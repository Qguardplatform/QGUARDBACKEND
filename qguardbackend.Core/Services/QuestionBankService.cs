using qguardbackend.Core.Interfaces;
using qguardbackend.Data.Constants;
using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.DTOs;
using qguardbackend.Data.Entities;
using Microsoft.AspNetCore.Http;
using qguardbackend.Data.DbContext;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using qguardbackend.Data.Common;
using qguardbackend.Data.DTOs.ResponseDto;
using System.Data;
using System.Text;
using qguardbackend.Data.Enums;
using examportal.Api.ServiceExtensions;
using Microsoft.VisualBasic.FileIO;
using System.Linq;

namespace qguardbackend.Core.Services
{
    public class QuestionBankService : IQuestionBankService
    {
        private readonly AppDbContext _context;
        private readonly ILogger<InstitutionService> _logger;
        private readonly IS3Service _s3Service;
        private readonly IAuditLogService _auditLogService;
        private readonly IUserManagementService _userManagementService;

        public QuestionBankService(AppDbContext context,
            ILogger<InstitutionService> logger,
            IUserManagementService userManagementService,
            IS3Service s3Service,
            IAuditLogService auditLogService)
        {
            _context = context;
            _logger = logger;
            _s3Service = s3Service;
            _auditLogService = auditLogService;
            _userManagementService = userManagementService;
        }
        public async Task<CustomResult<string>> CreateQuestionAsync(QuestionBankCreateModel model)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            if (string.IsNullOrWhiteSpace(model.Question))
                return CustomResult<string>.ErrorOccured("Question text is required.", ResponseCodes.BadRequestErrorCode);

            if (string.IsNullOrWhiteSpace(model.DifficultLevel.ToString()))
                return CustomResult<string>.ErrorOccured("Difficult Level is required.", ResponseCodes.BadRequestErrorCode);



            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == model.CourseId);
            if (course is null)
                return CustomResult<string>.ErrorOccured("Invalid course id passed!.", ResponseCodes.BadRequestErrorCode);
            if (model.Point <= 0)
                return CustomResult<string>.ErrorOccured("Point is required!.", ResponseCodes.BadRequestErrorCode);

            string uploadedQuestionImageUrl = null;
            // Upload question image if available
            if (model.ImageUrl != null)
            {
                var uploadResult = await UploadFileAsync(model.ImageUrl);
                if (!uploadResult.Success)
                    return CustomResult<string>.ErrorOccured(uploadResult.Error, ResponseCodes.BadRequestErrorCode);

                uploadedQuestionImageUrl = uploadResult.Url;
            }

            var question = QuestionBank.Create(model.Question, model.DifficultLevel, model.IsMultipleChoice,
                model.Tags, model.CourseId.Value, InsTId.Data, model.Point);

            question.ImageUrl = uploadedQuestionImageUrl;

            // Handle Options
            if (model.Options != null && model.Options.Any())
            {
                char currentChar = 'A';
                foreach (var optionModel in model.Options)
                {
                    string optionContent = null;

                    if (optionModel.Image != null)
                    {
                        var optionUploadResult = await UploadFileAsync(optionModel.Image);
                        if (!optionUploadResult.Success)
                            return CustomResult<string>.ErrorOccured(optionUploadResult.Error, ResponseCodes.BadRequestErrorCode);

                        optionContent = optionUploadResult.Url;
                    }
                    else if (!string.IsNullOrWhiteSpace(optionModel.OptionDescription))
                    {
                        optionContent = optionModel.OptionDescription;
                    }
                    else
                    {
                        return CustomResult<string>.ErrorOccured("Each option must have either a description or an image.", ResponseCodes.BadRequestErrorCode);
                    }

                    var questionOption = new QuestionOption
                    {
                        OptionDescription = optionContent,
                        //OptionTag = optionModel.OptionTag,
                        OptionTag = currentChar.ToString().ToUpper(),
                        IsCorrectOption = optionModel.IsCorrectOption
                    };

                    question.QuestionOptions.Add(questionOption);


                    if (currentChar < 'Z')
                        currentChar++;
                }
            }

            await _context.QuestionBanks.AddAsync(question);
            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question created successfully.");
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

        public async Task<CustomResult<string>> UpdateQuestionAsync(long id, QuestionBankUpdateModel model)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var question = await _context.QuestionBanks
                                    .Include(q => q.QuestionOptions)
                                    .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
                return CustomResult<string>.ErrorOccured("Question not found.", ResponseCodes.NotFoundErrorCode);


            var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == model.CourseId);
            if (course is null)
                return CustomResult<string>.ErrorOccured("Invalid course id passed!.", ResponseCodes.BadRequestErrorCode);
            if (model.Point <= 0)
                return CustomResult<string>.ErrorOccured("Point is required!.", ResponseCodes.BadRequestErrorCode);

            // Update basic properties
            question.Question = model.Question;
            question.DifficultLevel = model.DifficultLevel.ToString();
            question.Tags = model.Tags;
            question.Points = model.Point;
            question.CourseId = model.CourseId;
            question.InstitutionId = InsTId.Data;
            question.IsMultipleChoice = model.IsMultipleChoice;

            // Remove existing options

            //check if this question has an option that has been used in any submission (as correct or wrong optionID),retain the options and remove the ones that are not yet sed in the submission

            //check if there is an option on this exam that has been selected earlier already
            var OptionsIds = question.QuestionOptions.Select(x => x.Id).ToList();
            var PreviousSubmissions = await _context.CandidateExamsSubmissions
                .Where(x => x.QuestionBankId == model.QuestionBankId)
                .Select(x => x.SelectedQuestionOptionId)
                .ToListAsync();
            if (PreviousSubmissions.Count > 0 && OptionsIds.Any(a => PreviousSubmissions.Contains(a)))
            //if (PreviousSubmissions.Count > 0 && OptionsIds.Intersect(PreviousSubmissions).Any())
            {

                _context.QuestionBanks.Update(question);
                await _context.SaveChangesAsync();
                return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Exam Question Edited, Cannot Edit Question options again, the option has been previously selected as an answer by a candidate, and already used for results computation, kindly create a new Exam Question");
            }


            _context.QuestionOptions.RemoveRange(question.QuestionOptions);
            question.QuestionOptions.Clear();

            // Add new options
            if (model.Options != null && model.Options.Any())
            {
                char currentChar = 'A';
                foreach (var optionModel in model.Options)
                {
                    string optionContent;

                    if (optionModel.Image != null)
                    {
                        var uploadOptionResult = await UploadFileAsync(optionModel.Image);
                        if (!uploadOptionResult.Success)
                            return CustomResult<string>.ErrorOccured(uploadOptionResult.Error, ResponseCodes.BadRequestErrorCode);

                        optionContent = uploadOptionResult.Url;
                    }
                    else if (!string.IsNullOrWhiteSpace(optionModel.OptionDescription))
                    {
                        optionContent = optionModel.OptionDescription;
                    }
                    else
                    {
                        return CustomResult<string>.ErrorOccured("Each option must have either a description or an image.", ResponseCodes.BadRequestErrorCode);
                    }

                    question.QuestionOptions.Add(new QuestionOption
                    {
                        OptionDescription = optionContent,
                        OptionTag = currentChar.ToString().ToUpper(),
                        IsCorrectOption = optionModel.IsCorrectOption
                    });


                    if (currentChar < 'Z')
                        currentChar++;
                }
            }

            _context.QuestionBanks.Update(question);
            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question updated successfully.");
        }

        public async Task<CustomResult<string>> UpdateQuestionOptionAsync(QuestionOptionUpdateModel model)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }

            var question = await _context.QuestionOptions
                                    .FirstOrDefaultAsync(q => q.Id == model.QuestionOptionId);

            if (question == null)
                return CustomResult<string>.ErrorOccured("Question not found.", ResponseCodes.NotFoundErrorCode);

            // Update basic properties
            question.OptionDescription = string.IsNullOrWhiteSpace(model.UpdatedOptionContext) ? question.OptionDescription : model.UpdatedOptionContext;
            _context.QuestionOptions.Update(question);

            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question updated successfully.");
        }

        public async Task<CustomResult<string>> AddQuestionOptionAsync(AddQuestionOptionModel model)
        {
            var InsTId = await _userManagementService.GetTenantId();
            if (!InsTId.IsSuccess)
            {
                return CustomResult<string>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
            }
            char Tag = 'A';
            var getQuestionOptions = await _context.QuestionOptions.Where(x => x.QuestionBankId == model.QuestionBankId)
                .OrderBy(x => x.OptionTag).LastOrDefaultAsync();
            if (getQuestionOptions is not null)
            {
                Tag = Convert.ToChar(getQuestionOptions.OptionTag);
                if (Tag < 'Z')
                    Tag++;
            }

            var newQuestionOption = new QuestionOption
            {
                OptionTag = Tag.ToString().ToUpper(),
                IsDeleted = false,
                CreatedAt = DateTime.Now,
                QuestionBankId = model.QuestionBankId,
                IsActive = true,
                OptionDescription = model.NewOptionContext,
                IsCorrectOption = model.IsCorrectOption
            };

            await _context.QuestionOptions.AddAsync(newQuestionOption);
            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question option added successfully.");
        }

        public async Task<CustomResult<string>> ToggleQuestionStatusAsync(long id)
        {
            var question = await _context.QuestionBanks.FirstOrDefaultAsync(q => q.Id == id && !q.IsDeleted);

            if (question == null)
                return CustomResult<string>.ErrorOccured("Question not found.", ResponseCodes.NotFoundErrorCode);

            question.IsActive = !question.IsActive;

            _context.QuestionBanks.Update(question);
            await _context.SaveChangesAsync();

            var status = question.IsActive ? "activated" : "deactivated";
            return CustomResult<string>.Success(ResponseCodes.SuccessCode, $"Question {status} successfully.");
        }

        public async Task<CustomResult<string>> DeleteQuestionAsync(long id)
        {
            var question = await _context.QuestionBanks
                            .Include(q => q.QuestionOptions)
                            .FirstOrDefaultAsync(q => q.Id == id);

            if (question == null)
            {
                return CustomResult<string>.ErrorOccured(
                    "Question not found.",
                    ResponseCodes.NotFoundErrorCode
                );
            }

            //check if the question already belonged to a published exams schedule
            bool isUsedInPublishedExam = await _context.ExamQuestions
                                        .Include(eq => eq.ExamSchedule)
                                        .AnyAsync(eq =>
                                            eq.QuestionBankId == id &&
                                            eq.ExamSchedule.Status == ExamScheduleStatusEnum.PUBLISHED.GetEnumText()
                                        );
            if (isUsedInPublishedExam)
            {
                return CustomResult<string>.ErrorOccured(
                    "Question cannot be deleted because it is already mapped to a published exam schedule.",
                    ResponseCodes.BadRequestErrorCode
                );
            }

            if (question.QuestionOptions?.Any() == true)
                _context.QuestionOptions.RemoveRange(question.QuestionOptions);

            _context.QuestionBanks.Remove(question);

            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(ResponseCodes.SuccessCode, "Question and its options deleted successfully.");
        }

        public async Task<CustomResult<PaginatedResult<QuestionBankModel>>> GetAllQuestionsAsync(QuestionBankQueryModelMini query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                    return CustomResult<PaginatedResult<QuestionBankModel>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);

                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<QuestionBank> records = _context.QuestionBanks
                    .Include(q => q.Course)
                    .Include(q => q.Institution)
                    .Include(q => q.QuestionOptions)
                    .Where(q => q.InstitutionId == InsTId.Data);

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(q =>
                        q.Question.ToLower().Contains(word) ||
                        q.Tags.ToLower().Contains(word) ||
                        q.Course.Name.ToLower().Contains(word) ||
                        q.Institution.Name.ToLower().Contains(word) ||
                        q.QuestionOptions.Any(x => x.OptionDescription.ToLower().Contains(word)));
                }

                if (!string.IsNullOrWhiteSpace(query.Tag))
                {
                    records = records.Where(x => x.Tags.ToLower() == query.Tag.ToLower());
                }
                if (!string.IsNullOrWhiteSpace(query.DifficultLevel))
                {
                    records = records.Where(x => x.DifficultLevel.ToLower() == query.DifficultLevel.ToLower());
                }
                if (query.ExamType.HasValue)
                {
                    records = records.Where(x => x.IsMultipleChoice == query.ExamType);
                }
                if (query.CourseId > 0)
                {
                    records = records.Where(x => x.CourseId == query.CourseId);
                }
                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= query.StartDate && x.CreatedAt <= endDate);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(x => x.Question)
                    : records.OrderByDescending(x => x.CreatedAt);

                var result = await records.Select(q => new QuestionBankModel
                {
                    Id = q.Id,
                    Question = q.Question,
                    DifficultLevel = q.DifficultLevel,
                    Tags = q.Tags,
                    IsMultipleChoice = (q.IsMultipleChoice) ? "Multiple options" : "Subjective",
                    ImageUrl = q.ImageUrl,
                    Point = q.Points,
                    DateCreated = q.CreatedAt,
                    Course = q.Course == null ? null : new CourseModel
                    {
                        Id = q.Course.Id,
                        Name = q.Course.Name,
                        CourseCode = q.Course.CourseCode,
                        Description = q.Course.Description
                    },
                    Institution = q.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = q.Institution.Id,
                        Name = q.Institution.Name,
                        Code = q.Institution.Code
                    },
                    Options = q.QuestionOptions.Select(opt => new QuestionOptionModel
                    {
                        Id = opt.Id,
                        OptionTag = opt.OptionTag,
                        OptionDescription = opt.OptionDescription,
                        IsCorrectOption = opt.IsCorrectOption
                    }).ToList()
                }).ToListAsync();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? result.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : result.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<QuestionBankModel>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<QuestionBankModel>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<QuestionBankModel>> GetQuestionByAsyncId(long id)
        {
            try
            {
                var question = await _context.QuestionBanks
                    .Include(q => q.Course)
                    .Include(q => q.Institution)
                    .Include(q => q.QuestionOptions)
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (question == null)
                    return CustomResult<QuestionBankModel>.ErrorOccured("Question not found.", ResponseCodes.NotFoundErrorCode);

                var response = new QuestionBankModel
                {
                    Id = id,
                    Question = question.Question,
                    DifficultLevel = question.DifficultLevel,
                    Tags = question.Tags,
                    IsMultipleChoice = (question.IsMultipleChoice) ? "Multiple options" : "Subjective",
                    ImageUrl = question.ImageUrl,
                    Point = question.Points,
                    DateCreated = question.CreatedAt,
                    Course = question.Course == null ? null : new CourseModel
                    {
                        Id = question.Course.Id,
                        Name = question.Course.Name,
                        CourseCode = question.Course.CourseCode,
                        Description = question.Course.Description
                    },
                    Institution = question.Institution == null ? null : new InstitutionResponseDto
                    {
                        Id = question.Institution.Id,
                        Name = question.Institution.Name,
                        Code = question.Institution.Code
                    },
                    Options = question.QuestionOptions?.Select(o => new QuestionOptionModel
                    {
                        Id = o.Id,
                        OptionTag = o.OptionTag,
                        OptionDescription = o.OptionDescription,
                        IsCorrectOption = o.IsCorrectOption
                    }).ToList() ?? new List<QuestionOptionModel>()
                };

                return CustomResult<QuestionBankModel>.Success(response, ResponseCodes.SuccessCode);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<QuestionBankModel>.ErrorOccured("An error occurred while fetching the question.", ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<PaginatedResult<ExamQuestionBankListDto>>> GetAllQuestionBanksAsync(QuestionBankQueryModelMini query)
        {
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<PaginatedResult<ExamQuestionBankListDto>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                var endDate = Utility.AddTimeSpan(query.EndDate ?? DateTime.Now);
                IQueryable<QuestionBank> records = _context.QuestionBanks
                    .Include(q => q.Course)
                    .Include(q => q.Institution)
                    .Include(q => q.QuestionOptions)
                    .Where(q => q.InstitutionId == InsTId.Data);

                if (!string.IsNullOrWhiteSpace(query.SearchWord))
                {
                    var word = query.SearchWord.Trim().ToLower();
                    records = records.Where(q =>
                        q.Question.ToLower().Contains(word) ||
                        q.Tags.ToLower().Contains(word) ||
                        q.Course.Name.ToLower().Contains(word) ||
                        q.Institution.Name.ToLower().Contains(word) ||
                        q.QuestionOptions.Any(x => x.OptionDescription.ToLower().Contains(word)));
                }

                if (query.StartDate.HasValue && query.EndDate.HasValue)
                {
                    records = records.Where(x => x.CreatedAt >= query.StartDate && x.CreatedAt <= endDate);
                }
                if (!string.IsNullOrWhiteSpace(query.Tag))
                {
                    records = records.Where(x => x.Tags.ToLower() == query.Tag.ToLower());
                }
                if (query.CourseId > 0)
                {
                    records = records.Where(x => x.CourseId == query.CourseId);
                }
                if (!string.IsNullOrWhiteSpace(query.DifficultLevel))
                {
                    records = records.Where(x => x.DifficultLevel.ToLower() == query.DifficultLevel.ToLower());
                }
                if (query.ExamType.HasValue)
                {
                    records = records.Where(x => x.IsMultipleChoice == query.ExamType);
                }

                records = query.Sorting?.ToLower() == "asc"
                    ? records.OrderBy(x => x.Question)
                    : records.OrderByDescending(x => x.CreatedAt);

                var result = await records.Select(q => new QuestionBankMiniModel
                {
                    Id = q.Id,
                    Question = q.Question,
                    DifficultLevel = q.DifficultLevel,
                    Tags = q.Tags,
                    ImageUrl = q.ImageUrl,
                    Point = q.Points,
                    CourseId = q.Course.Id,
                    CourseName = q.Course.Name,
                    CreatedAt = q.CreatedAt,
                    Type = q.IsMultipleChoice ? "Multiple options" : "Subjective"
                }).ToListAsync();

                //group by tags, CourseId, CourseName

                var Groupings = result.GroupBy(x => new { x.CourseId, x.CourseName, x.Tags })
                    .Select(g => new ExamQuestionBankListDto
                    {
                        CourseId = g.Key.CourseId,
                        CourseName = g.Key.CourseName,
                        Tags = g.Key.Tags,
                        QuestionIds = g.Select(x => x.Id).ToList(),
                        QuestionType = g.Select(x => x.Type).Distinct().ToList(),
                        DifficultLevel = g.Select(x => x.DifficultLevel).Distinct().ToList(),
                        NoOfQuestions = g.Select(x => x.Question).ToList().Count(),
                        LastModified = g.Select(x => x.CreatedAt).Max(),
                    }).ToList();

                var paginated = query.PageNumber.HasValue && query.PageSize.HasValue
                    ? Groupings.ToPageList(query.PageNumber.Value, query.PageSize.Value)
                    : Groupings.NoPaginate(0, 0);

                return CustomResult<PaginatedResult<ExamQuestionBankListDto>>.Success(paginated);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<PaginatedResult<ExamQuestionBankListDto>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        public async Task<CustomResult<string>> DeleteMultipleQuestionsAsync(List<long> questionIds)
        {
            if (questionIds == null || !questionIds.Any())
            {
                return CustomResult<string>.ErrorOccured(
                    "No question IDs provided.",
                    ResponseCodes.BadRequestErrorCode
                );
            }

            // Pull all questions with options
            var questions = await _context.QuestionBanks
                                .Include(q => q.QuestionOptions)
                                .Where(q => questionIds.Contains(q.Id))
                                .ToListAsync();

            if (!questions.Any())
            {
                return CustomResult<string>.ErrorOccured(
                    $"No questions found for the provided IDs.",
                    ResponseCodes.NotFoundErrorCode
                );
            }

            // Check if any question is linked to a published exam
            var publishedLinks = await _context.ExamQuestions
                        .Include(eq => eq.ExamSchedule)
                        .Where(eq =>
                            questionIds.Contains(eq.QuestionBankId.Value) &&
                            eq.ExamSchedule.Status == ExamScheduleStatusEnum.PUBLISHED.GetEnumText()
                        )
                        .Select(eq => eq.QuestionBankId)
                        .Distinct()
                        .ToListAsync();

            if (publishedLinks.Any())
            {
                return CustomResult<string>.ErrorOccured(
                    $"Cannot delete {publishedLinks.Count} question(s) because they are mapped to a published exam schedule.",
                    ResponseCodes.BadRequestErrorCode
                );
            }

            // Remove options first
            var allOptions = questions
                .Where(q => q.QuestionOptions != null && q.QuestionOptions.Any())
                .SelectMany(q => q.QuestionOptions)
                .ToList();

            if (allOptions.Any())
                _context.QuestionOptions.RemoveRange(allOptions);

            // Remove questions
            _context.QuestionBanks.RemoveRange(questions);

            await _context.SaveChangesAsync();

            return CustomResult<string>.Success(
                ResponseCodes.SuccessCode,
                $"{questions.Count} question(s) and their options deleted successfully."
            );
        }

        public async Task<CustomResult<List<MigrationErrorVM>>> UploadQuestionAsync(QuestionBankUploadModel model)
        {
            var supportedTypes = new[] { "csv" };
            var errorList = new List<MigrationErrorVM>();
            try
            {
                var InsTId = await _userManagementService.GetTenantId();
                if (!InsTId.IsSuccess)
                {
                    return CustomResult<List<MigrationErrorVM>>.Failure(CustomError.InvalidTenant, ResponseCodes.InvalidTenant);
                }

                if (string.IsNullOrWhiteSpace(model.Tags))
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Tag text is required.", ResponseCodes.BadRequestErrorCode);

                if (string.IsNullOrWhiteSpace(model.DifficultLevel.ToString()))
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Difficult Level is required.", ResponseCodes.BadRequestErrorCode);

                var course = await _context.Courses.FirstOrDefaultAsync(x => x.Id == model.CourseId);
                if (course is null)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Invalid course id passed!.", ResponseCodes.BadRequestErrorCode);

                var file = model.File;

                if (file == null || file.Length == 0)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("No content in the uploaded file", ResponseCodes.BadRequestErrorCode);

                var fileExt = Path.GetExtension(file.FileName).Substring(1);
                if (!supportedTypes.Contains(fileExt))
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Only .csv file extension is allowed!", ResponseCodes.BadRequestErrorCode);

                var tempFilePath = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.csv");
                await using (var writer = new StreamWriter(tempFilePath, false, Encoding.UTF8))
                {
                    using var reader = new StreamReader(file.OpenReadStream(), Encoding.UTF8, true);
                    string content = await reader.ReadToEndAsync();
                    await writer.WriteAsync(content);
                }

                DataTable dt = await this.ValidateCSVUpload(tempFilePath);

                var linesCount = File.ReadLines(tempFilePath).Count();
                if (linesCount <= 1)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("CSV must contain a header and at least one data row.", ResponseCodes.BadRequestErrorCode);

                if (linesCount > 101)
                    return CustomResult<List<MigrationErrorVM>>.ErrorOccured("Rows should not be more than 100 per sheet. Kindly reduce the records into multiple sheets.", ResponseCodes.BadRequestErrorCode);

                int savedItemCount = 0;
                var tenantId = InsTId.Data;

                await using var transaction = await _context.Database.BeginTransactionAsync();

                using var parser = new TextFieldParser(tempFilePath, Encoding.UTF8)
                {
                    TextFieldType = FieldType.Delimited,
                    HasFieldsEnclosedInQuotes = true
                };
                parser.SetDelimiters(",");

                int lineNumber = 0;

                while (!parser.EndOfData)
                {
                    var columns = parser.ReadFields();
                    lineNumber++;

                    if (lineNumber == 1)
                        continue; // skip header

                    try
                    {
                        if (columns == null || columns.Length < 4)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                ErrorMessage = "Each row must have at least 4 columns (Question, Options, CorrectOption, Point)."
                            });
                            continue;
                        }

                        var questionText = columns[0]?.Trim();
                        var optionsRaw = columns[1]?.Trim();
                        var correctOption = columns[2]?.Trim();
                        var pointStr = columns[3]?.Trim();

                        if (string.IsNullOrWhiteSpace(questionText) ||
                            string.IsNullOrWhiteSpace(optionsRaw) ||
                            string.IsNullOrWhiteSpace(correctOption) ||
                            !int.TryParse(pointStr, out int point) || point <= 0)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                ErrorMessage = "Invalid data. Ensure Question, Options, CorrectOption, and Point are valid.",
                                RecordIdentifier = questionText
                            });
                            continue;
                        }

                        // Duplicate check
                        bool questionExists = await _context.QuestionBanks
                            .AnyAsync(q => q.InstitutionId == tenantId &&
                                           q.Question.ToLower().Trim() == questionText.ToLower().Trim() &&
                                           q.CourseId == model.CourseId);

                        if (questionExists)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                RecordIdentifier = questionText,
                                ErrorMessage = $"Duplicate question found: \"{questionText}\" already exists in this course."
                            });
                            continue;
                        }

                        var options = optionsRaw.Split(';').Select(o => o.Trim()).Where(o => !string.IsNullOrWhiteSpace(o)).ToList();

                        if (options.Count == 0)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                ErrorMessage = "At least one valid option is required.",
                                RecordIdentifier = questionText
                            });
                            continue;
                        }

                        if (options.Count > 26)
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                ErrorMessage = "Maximum 26 options allowed (A-Z).",
                                RecordIdentifier = questionText
                            });
                            continue;
                        }

                        // Verify correctOption tag is valid (A–Z)
                        var validTags = options.Select((_, i) => ((char)('A' + i)).ToString()).ToList();
                        if (!validTags.Contains(correctOption, StringComparer.OrdinalIgnoreCase))
                        {
                            errorList.Add(new MigrationErrorVM
                            {
                                RowNum = lineNumber,
                                ErrorMessage = $"Correct option '{correctOption}' not found among provided options.",
                                RecordIdentifier = questionText
                            });
                            continue;
                        }

                        // Create question
                        var question = QuestionBank.Create(
                            questionText,
                            model.DifficultLevel,
                            model.IsMultipleChoice,
                            model.Tags,
                            model.CourseId,
                            tenantId,
                            point
                        );

                        char tag = 'A';
                        foreach (var opt in options)
                        {
                            question.QuestionOptions.Add(new QuestionOption
                            {
                                OptionDescription = opt,
                                OptionTag = tag.ToString(),
                                IsCorrectOption = correctOption.Equals(tag.ToString(), StringComparison.OrdinalIgnoreCase)
                            });
                            tag++;
                        }

                        await _context.QuestionBanks.AddAsync(question);
                        savedItemCount++;
                    }
                    catch (Exception ex)
                    {
                        errorList.Add(new MigrationErrorVM
                        {
                            RowNum = lineNumber,
                            ErrorMessage = "Unexpected error while processing row.",
                            AdditionalMessage = ex.Message
                        });
                    }
                }

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                if (errorList.Count > 0)
                {
                    return CustomResult<List<MigrationErrorVM>>.Errors(
                        errorList,
                        $"Upload completed with some errors. {savedItemCount} questions saved, {errorList.Count} failed.",
                        ResponseCodes.BadRequestErrorCode,
                        errorList
                    );
                }

                return CustomResult<List<MigrationErrorVM>>.Success(
                    errorList,
                    $"{savedItemCount} questions uploaded successfully."
                );
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, ex.Message);
                return CustomResult<List<MigrationErrorVM>>.ErrorOccured(ex.Message, ResponseCodes.SystemExceptionErrorCode);
            }
        }

        private Task<DataTable> ValidateCSVUpload(string path)
        {
            var dt = new DataTable();
            using (StreamReader sr = new StreamReader(path, Encoding.UTF8, true))
            {
                string[] headers = sr.ReadLine().Split(',');

                var head = new QuestionUploadFormatModel
                {
                    Question = headers[0],
                    Options = headers[1],
                    CorrectOption = headers[2],
                    Points = headers[3]
                };

                if (head.Question.ToLower() != "question" && head.Options.ToLower() != "options" && head.CorrectOption.ToLower() != "correctoption"
                    && head.Points.ToLower() != "point")
                {
                    throw new Exception("Result (CSV) header format is not valid!");
                }

                foreach (string header in headers)
                {
                    dt.Columns.Add(header);
                }

                while (!sr.EndOfStream)
                {
                    string[] rows = sr.ReadLine().Split(',');
                    if (rows.Length > 1)
                    {
                        DataRow dr = dt.NewRow();
                        for (int i = 0; i < headers.Length; i++)
                        {
                            dr[i] = rows[i].Trim();
                        }
                        dt.Rows.Add(dr);
                    }
                }
            }
            return Task.FromResult(dt);
        }

        public byte[] GenerateSampleCsv(out string fileName, out string contentType)
        {
            var csv = new StringBuilder();

            csv.AppendLine("Question,Options,CorrectOption,Point");
            csv.AppendLine("What is the capital of France?,\"Paris;London;Berlin;Madrid\",A,5");
            csv.AppendLine("Which planet is known as the Red Planet?,\"Earth;Mars;Jupiter;Saturn\",B,4");
            csv.AppendLine("What is 2 + 2?,\"3;4;5;6\",B,2");
            csv.AppendLine("Who wrote 'Hamlet'?," +
                           "\"William Shakespeare;Leo Tolstoy;Mark Twain;Jane Austen\",A,5");
            csv.AppendLine("What color is a ripe banana?,\"Red;Green;Yellow;Blue\",C,1");
            csv.AppendLine("Which gas do plants absorb?,\"Oxygen;Hydrogen;Carbon Dioxide;Nitrogen\",C,3");
            csv.AppendLine("Who is known as the father of computers?,\"Charles Babbage;Alan Turing;Isaac Newton;Bill Gates\",A,5");
            csv.AppendLine("Which organ pumps blood?,\"Liver;Heart;Lungs;Brain\",B,2");
            csv.AppendLine("What is the boiling point of water (°C)?,\"90;100;110;120\",B,3");
            csv.AppendLine("What is the largest ocean?,\"Atlantic;Indian;Arctic;Pacific\",D,4");

            contentType = "text/csv";
            fileName = "sample_questions.csv";
            return Encoding.UTF8.GetBytes(csv.ToString());
        }

        public async Task<CustomResult<string>> UpdateExamBankQuestionAsync(long examscheduleId, QuestionBankUpdateModel model)
        {
            long courseId = 0;
            if (!model.CourseId.HasValue)
            {
                var getScheduleDetails = await _context.ExamSchedules.FirstOrDefaultAsync(x => x.Id == examscheduleId);
                courseId = getScheduleDetails.CourseId;
            }

            model.CourseId = courseId;
            var resp = await UpdateQuestionAsync(model.QuestionBankId, model);

            return resp;
        }
    }
}