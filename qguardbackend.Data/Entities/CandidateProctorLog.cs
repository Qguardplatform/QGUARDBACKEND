using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CandidateProctorLog : BaseEntity
    {
        public string FlagId { get; set; }

        public long CandidateId { get; set; }
        public Candidate Candidate { get; set; }

        public long ExamScheduleId { get; set; }
        public ExamSchedule ExamSchedule { get; set; }

        public long EnforcementModeId { get; set; } // "TabSwitch", "NoFaceDetected", "MultipleFaces"
        public EnforcementMode EnforcementMode { get; set; }

        public string MediaUrl { get; set; }
        public string Description { get; set; }
        public string Event { get; set; }
        public string Domain { get; set; }
        public string Status { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}