using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CandidateExamResult : BaseEntity
    {
        public static CandidateExamResult Create(long examScheduleId, long candidateId, decimal score, long totalQuestion,
            long noOfQuestionsAttempted, String remark, String totalTimeSpent, DateTime submissionDateAndTime, bool reSit)
        {
            return new CandidateExamResult()
            {
                ExamScheduleId = examScheduleId,
                CandidateId = candidateId,
                Score = score,
                TotalQuestions = totalQuestion,
                NoOfQuestionsAttempted = noOfQuestionsAttempted,
                Remark = remark,
                TotalTimeSpent = totalTimeSpent,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = submissionDateAndTime,
                AllowResit = reSit
            };
        }
        public long CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }

        public decimal Score { get; set; }
        public decimal TotalPoints { get; set; }
        public long TotalQuestions { get; set; }
        public string TotalTimeSpent { get; set; }
        public long NoOfQuestionsAttempted { get; set; }
        public string Remark { get; set; }
        public bool AllowResit { get; set; }
    }
}