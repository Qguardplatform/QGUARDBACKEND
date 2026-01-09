using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CandidateExamViewAndAttempts : BaseEntity
    {
       

        public long CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }

        public long NoOfViewTimes { get; set; } 
       

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}