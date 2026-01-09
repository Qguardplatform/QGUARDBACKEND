using qguardbackend.Data.Abstracts;
using System.ComponentModel.DataAnnotations.Schema;

namespace qguardbackend.Data.Entities
{
    public class CandidatesCurrentState : BaseEntity
    {
        public long CandidateId { get; set; }
        [ForeignKey(nameof(CandidateId))]
        public Candidate Candidate { get; set; }
     
        public long LevelId { get; set; }
        public Level Level { get; set; }
        public long SessionId { get; set; }
        public Session Session { get; set; }
        public long SemesterId { get; set; }
        public Semester Semester { get; set; }
        public bool IsCurrent { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}