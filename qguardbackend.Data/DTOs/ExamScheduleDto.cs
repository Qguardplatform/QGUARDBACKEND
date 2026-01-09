using qguardbackend.Data.Enums;

namespace qguardbackend.Data.DTOs
{

    public class changeExamScheduleStatus
    {
        public long Id { get; set; }
        public ExamScheduleStatusEnum status { get; set; }
    }
    public class ExamScheduleDto
    {
        public long Id { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }

        public string AcademicSession { get; set; }
        public string Semester { get; set; }
        public long CourseId { get; set; }
        public string Course { get; set; }
        public string CourseCode { get; set; }
        public List<string> Departments { get; set; }
        public string Institution { get; set; }

        public int MaxQuestions { get; set; }
        public int TotalQuestionsOnExam { get; set; }
        public int PassScore { get; set; }
        public int ExamDurationInMinutes { get; set; }

        public bool? IsDeleted { get; set; }
        public bool? IsActive { get; set; }
        public bool IsRandomized { get; set; }
        public bool EnableAiProctoring { get; set; }
        public string Level { get; set; }

        public bool InstantAnswerGrading { get; set; }
        public bool InstantResultPublishing { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public bool AllowLateStart { get; set; }
        public string Status { get; set; }
        public long AssignProctorId { get; set; }
        public List<ProctorConfigurationRuleDto> ProctorConfigurations { get; set; }
    }

    public class ProctorConfigurationRuleDto
    {
        public string Violation { get; set; }
        public string Threshold { get; set; }
        public string Action { get; set; }
        public string Value { get; set; }
    }

    public class ExamScheduleCreateDto
    {
        public string Title { get; set; }
        public string Description { get; set; }

        public long AcademicSessionId { get; set; }
        public long SemesterId { get; set; }
        public long CourseId { get; set; }
        public List<long> DepartmentIds { get; set; }

        public bool InstantAnswerGrading { get; set; }
        public bool InstantResultPublishing { get; set; }
        public int MaxQuestions { get; set; }
        public int PassScore { get; set; }
        public int ExamDurationInMinutes { get; set; }

        public bool IsRandomized { get; set; }
        public bool EnableAiProctoring { get; set; }
        public long LevelId { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime? EndDate { get; set; }
        public DateTime StartTime { get; set; }
        public DateTime? EndTime { get; set; }

        public bool AllowLateStart { get; set; }
        public ExamScheduleStatusEnum Status { get; set; }
        public long AssignProctorId { get; set; }

        public List<PenaltyRuleCreateModel> PenaltyRules { get; set; } = new();
    }

    public class PenaltyRuleCreateModel
    {
        public long EnforcementModeId { get; set; }
        public int Threshold { get; set; }

        public long EnforcementActionId { get; set; }
        public string Deduct { get; set; }
    }
}