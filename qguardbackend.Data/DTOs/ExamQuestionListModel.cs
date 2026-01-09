using qguardbackend.Data.DTOs.Results;
using qguardbackend.Data.Enums;
using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace qguardbackend.Data.DTOs
{
    public class ExamQuestionListModel
    {
        public long Id { get; set; }
        public int Questions { get; set; }
        public string Level { get; set; }

        public CourseModel Course { get; set; }
        public DateTime ExamDate { get; set; }
        public string Status { get; set; }
        public DateTime DateCreated { get; set; }
    }

    public class ExamQuestionListDto
    {
        public long ExamScheduleId { get; set; }
        public string ExamScheduleTitle { get; set; }
        public string Instruction { get; set; }
        public string DifficultyLevel { get; set; }
        public string QuestionTag { get; set; }
        public DateTime LastModified { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool MakeQuestionsAppearRandom { get; set; }
        public long QuestionId { get; set; }
        public string Question { get; set; }
        public List<OptionForExamModelNoAnswer> options { get; set; }
    }

    public class ExamQuestionFilterModel : QueryModelMini
    {
        public long SemesterId { get; set; }
        public long SessionId { get; set; }
    }

    public class SingleExamQuestionCreateModel
    {
        public long ExamScheduleId { get; set; }
        public string? Instruction { get; set; }
        public bool? MakeQuestionsAppearRandom { get; set; } = true;
        public ExamQuestionMultDto QuestionDetails { get; set; }
    }
    public class SingleExamQuestionCreateResponseModel
    {
        public long ExamBankId { get; set; }
        public List<string> errors { get; set; }
        public bool IsCreatedSuccessfully { get; set; }
    }
    public class MapQuestionsToExamScheduleRequestDto
    {
        public long ExamScheduleId { get; set; }
        public string Instruction { get; set; }
        public bool MakeQuestionsAppearRandom { get; set; }
        public List<long> QuestionBankId { get; set; }

    }

    public class ExamQuestionCreateModel
    {
        public long ExamScheduleId { get; set; }
        public string Instruction { get; set; }
        public bool MakeQuestionsAppearRandom { get; set; }
        public ExamQuestionMultModel MultipleQuestions { get; set; }
    }


    public class ExamQuestionMultModel
    {
        //public long CourseId { get; set; }
        //public long InstitutionId { get; set; }
        public List<ExamQuestionMultDto> Questions { get; set; }
    }

    public class ExamQuestionMultDto
    {
        [Required(ErrorMessage = "Question is required!")]
        public string Question { get; set; }
        public DifficultLevel DifficultLevel { get; set; }
        [Required(ErrorMessage = "Question Tag is required!")]
        public string Tags { get; set; }
        public bool IsMultipleChoice { get; set; }
        public IFormFile? ImageUrl { get; set; }
        public int Point { get; set; }
        public List<OptionCreateModel> Options { get; set; }
    }

    public class ExamQuestionCreateDto
    {
        public long ExamScheduleId { get; set; }
        public string Instruction { get; set; }
        public bool MakeQuestionsAppearRandom { get; set; }
        public List<long> QuestionIds { get; set; }
    }

    public class CandidateExamScheduleModel
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public string Department { get; set; }
        public string Level { get; set; }
        public int Questions { get; set; }
        public int NoOfAttempts { get; set; }
        public bool IsResitEnabled { get; set; }
        public long ExamDurationInMinutes { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }
        public bool? IsDeleted { get; set; }
        public bool? IsActive { get; set; }
    }

    public class ExamInstructionModel
    {
        public long Id { get; set; }
        public string Instruction { get; set; }
        public string CourseCode { get; set; }
        public string CourseTitle { get; set; }
        public int TotalQuestions { get; set; }
        public bool? isProctorEnabled { get; set; }
        public int ExamDurationInMinutes { get; set; }
        public List<ProctorConfigurationRuleDto> ProctorConfigurations { get; set; }
    }

    public class ExamCreateModel
    {
        public int ExamDurationInMinutes { get; set; }
        public string Instruction { get; set; }
        public int TotalQuestions { get; set; }
        public PaginatedResult<QuestionsListModel> Questions { get; set; }
    }

    public class ExamCreateModelForCandidate
    {
        public string ExamDurationInMinutes { get; set; }
        public string Instruction { get; set; }
        public int TotalQuestions { get; set; }
        public decimal TotalQuestionsScore { get; set; }
        public decimal TotalScore { get; set; }
        public PaginatedResult<QuestionsListModelForCandidate> Questions { get; set; }
    }



    public class ExamCreateModelNoCorrectoption
    {
        public long NoOfView { get; set; }
        public int ExamDurationInMinutes { get; set; }
        public string Instruction { get; set; }
        public int TotalQuestions { get; set; }
        public int NoOfAttempts { get; set; }
        public bool IsResitEnabled { get; set; }
        public PaginatedResult<QuestionsListModelNoCorrectOption> Questions { get; set; }
    }
    public class viewCountUpdateResponse
    {
        public long NoOfView { get; set; }
        public long CandidateId { get; set; }
        public long ExamScheduleId { get; set; }

    }
    public class QuestionsListModel
    {
        //public string DifficultyLevel { get; set; }
        //public string QuestionTag { get; set; }
        //public DateTime LastModified { get; set; }
        //public DateTime CreatedAt { get; set; }
        public long QuestionId { get; set; }
        public string Question { get; set; }
        public List<OptionForExamModel> Options { get; set; }
    }
    public class QuestionsListModelForCandidate
    {

        public long QuestionId { get; set; }
        public long OptionIdSelected { get; set; }
        public int PassMarkOnExamSchedule { get; set; }
        public decimal Score { get; set; }
        public string Question { get; set; }
        public bool IsMultiChoice { get; set; }
        public List<OptionForExamModelForCandidate> Options { get; set; }
    }
    public class OptionForExamModelForCandidate
    {
        public long Id { get; set; }
        public string OptionTag { get; set; }
        public string OptionDescription { get; set; }
        public bool IsCorrectOption { get; set; }
    }


    public class QuestionsListModelNoCorrectOption
    {
        //public string DifficultyLevel { get; set; }
        public string QuestionTag { get; set; }
        public bool IsMultiChoice { get; set; }
        //public DateTime LastModified { get; set; }
        //public DateTime CreatedAt { get; set; }
        public long QuestionId { get; set; }
        public string DifficultLevel { get; set; }
        public string Question { get; set; }
        public long Point { get; set; }
        public List<OptionForExamModelNoCorrectionOption> Options { get; set; }
    }


    public class CandidateExamListModel
    {
        public long ExamScheduleId { get; set; }
        public string Course { get; set; }
        public string Code { get; set; }
        public decimal TotalScoreOnTheExam { get; set; }
        public decimal Score { get; set; }
        public long TotalQuestions { get; set; }
        public string TimeSpent { get; set; }
        public bool? IsResultPublished { get; set; }
        public DateTime ExamDate { get; set; }
    }

    public class ExamSubmissionDetailModel
    {
        public string Course { get; set; }
        public string Instruction { get; set; }
        public string TimeSpent { get; set; }
        public decimal Score { get; set; }
        public PaginatedResult<QuestionsDetailsModel> Questions { get; set; }
    }

    public class QuestionsDetailsModel
    {
        public long QuestionId { get; set; }
        public string Question { get; set; }
        public long SelectedOptionId { get; set; }
        public bool IsCorrectOption { get; set; }
        public DateTime SubmissionDateAndTime { get; set; }
        public string TimeSpent { get; set; }
        public decimal Score { get; set; }
        public List<OptionForExamModel> Options { get; set; }
    }

    public class ScheduleExamQuestionFilterModel : QueryModelMini
    {
        public long SemesterId { get; set; }
        public long SessionId { get; set; }
        //public long FacultyId { get; set; }
        //public long DepartmentId { get; set; }
        public long LevelId { get; set; }
    }

    public class DepartmeentFacultyScheduleExamQuestionFilterModel : QueryModelMini
    {
        public long SemesterId { get; set; }
        public long SessionId { get; set; }
        public long FacultyId { get; set; }
        public long DepartmentId { get; set; }
        public long LevelId { get; set; }
    }

    public class ExamRecordsByDepartmentFilterModel : QueryModelMini
    {
        public bool? Passed { get; set; }
        public bool? WhereResit { get; set; }
        public long SemesterId { get; set; }
        public long SessionId { get; set; }
        public long LevelId { get; set; }
    }

    public class DeptAndFacultyScheduleExamQuestionListModel
    {
        public long Id { get; set; }

        public string ExaminationTitle { get; set; }
        public string Description { get; set; }
        public string Session { get; set; }
        public string Semester { get; set; }
        public string Faculty { get; set; }
        public string Department { get; set; }
        //public List<DepartmentDto> Departments { get; set; }
        public string Level { get; set; }
        public string Status { get; set; }
        public bool IsResultPublished { get; set; }
        public DateTime ExamDate { get; set; }
    }
    public class ScheduleExamQuestionListModel
    {
        public long Id { get; set; }
        public string ExaminationTitle { get; set; }
        public string Description { get; set; }
        public string Session { get; set; }
        public string Semester { get; set; }
        //public string Faculty { get; set; }
        //public string Department { get; set; }
        public List<DepartmentDto> Departments { get; set; }
        public string Level { get; set; }
        public string Status { get; set; }
        public bool IsResultPublish { get; set; }
        public long noOfSubmission { get; set; }
        public DateTime ExamDate { get; set; }
    }

    public class SingleExamResultListModel
    {
        public long Id { get; set; }

        public List<DepartmentDto> Department { get; set; }
        public string Level { get; set; }
        public string ExaminationTitle { get; set; }
        public DateTime ExamDate { get; set; }
        public long TotalStudents { get; set; }
        public decimal AverageScore { get; set; }
        public int MaxQuestions { get; set; }
        public int PassScore { get; set; }
        public decimal HighestScore { get; set; }
        public PaginatedResult<CandidateExamResultListModel> BulkRecords { get; set; }
    }

    public class CandidateExamResultListModel
    {
        public string CandidateName { get; set; }
        public long CandidateId { get; set; }
        public string CandidateUserId { get; set; }
        public string MatricNumber { get; set; }
        public string Department { get; set; }
        public string Remark { get; set; }
        public decimal Score { get; set; }
        public bool? IsResitEnabled { get; set; }
        public string? ExamPublishedStatus { get; set; }
        public bool? ResultPublished { get; set; }
        public DateTime Date { get; set; }
    }

    public class NotificationTask
    {
        public long ExamScheduleId { get; set; }
        public string ExamTitle { get; set; } = default!;
        public long SessionId { get; set; }
        public long InstitutionId { get; set; }
        public long SemesterId { get; set; }
        public long LevelId { get; set; }
        public int DurationInMinutes { get; set; }
        public string Time { get; set; }
        public string Date { get; set; }
        public string Type { get; set; } = "Published"; // Published, StartingSoon, Reminder
    }
}
