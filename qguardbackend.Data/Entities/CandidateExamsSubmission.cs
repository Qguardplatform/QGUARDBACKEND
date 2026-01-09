using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CandidateExamsSubmission : BaseEntity
    {
        public long CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }

        public long? QuestionBankId { get; set; }
        public QuestionBank QuestionBank { get; set; }

        public long? CorrectQuestionOptionId { get; set; }
        public long? SelectedQuestionOptionId { get; set; }
        //public QuestionOption QuestionOption { get; set; }

        public bool? IsCorrectOption { get; set; }
        public bool? IsGraded { get; set; }
        public bool? IsResultPublished { get; set; }
        public bool? Allowresit { get; set; }

        public DateTime SubmissionDateAndTime { get; set; }
        public string TotalTimeSpent { get; set; } 
        public string AnswerSelected { get; set; }
        public string CorrectAnswer { get; set; }
        public string Remark { get; set; }
        public decimal Score { get; set; }
        public long? ResultId { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}