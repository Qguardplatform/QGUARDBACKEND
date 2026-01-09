namespace qguardbackend.Data.DTOs.RequestDto
{
    public class ExamSubmissionRequestDto
    {
        public string CandidateUserId { get; set; }
        public long ExamScheduleId { get; set; }
        public string TotalTimeSpent { get; set; }
        public DateTime ExamDate { get; set; }
        public List<long> QuestionIds { get; set; }
        public List<ExamSubmissionAnswersRequestDto> ExamSubmissionAnswers { get; set; }
    }

    public class ExamSubmissionAnswersRequestDto
    {
        public long? QuestionBankId { get; set; }
        public long? SelectedQuestionOptionId { get; set; }
    }

    public class ExamSubmissionAnswersResponseDto
    {
        public long? CandidateId { get; set; }
        public long? ExamScheduleId { get; set; }
        public string? Title { get; set; }
        public long? NoOfQuestions { get; set; }
        public long? NoOfQuestionsAttempted { get; set; }
        public long? NoOfCorrectAnswers { get; set; }
        public long? NoOfWrongAnswers { get; set; }
        public bool? InstantResultPublish{ get; set; }
        public decimal TotalScoreOnTheExam { get; set; }
        public decimal Score { get; set; }
    }

    public class StudentExamsDto
    {
        public long Id { get; set; }
        public long CandidateId { get; set; }
        public string CandidateName { get; set; }
        public long ExamScheduleId { get; set; }
        public string ExamTitle { get; set; }
        public string Institution { get; set; }
        public string Semester { get; set; }
        public string Session { get; set; }
        public string Course { get; set; }
        public string CourseCode { get; set; }
        public string Timestamp { get; set; }
    }

    public class StudentExamDoneFilterModel : QueryModelMini
    {
        public string ExamTitle { get; set; }
        public string Course { get; set; }
    }

    public class StudentExamDetailsDto
    {
        public CandidateListDto Candidate { get; set; }
        public long ExamScheduleId { get; set; }
        public ExamTakenDetailsDto ExamDetails { get; set; }
    }
    public class ExamTakenDetailsDto
    {
        public string ExamTitle { get; set; }
        public string Semester { get; set; }
        public string Session { get; set; }
        public string Course { get; set; }
        public string TimeOfSubmission { get; set; }
        public string ExamDuration { get; set; }
        public string SubmissionStatus { get; set; }
    }

    public class StudentExamsForExportDto
    {
        public string MatricNumber { get; set; }
        public string CandidateName { get; set; }
        public string ExamTitle { get; set; }
        public string Institution { get; set; }
        public string Semester { get; set; }
        public string Session { get; set; }
        public string Course { get; set; }
        public string CourseCode { get; set; }
        public decimal Score { get; set; }
        public string Remark { get; set; }
        public string Timestamp { get; set; }
    }
    public class ExportStudentExamsDto
    {
        public string Base64File { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
    }
}
