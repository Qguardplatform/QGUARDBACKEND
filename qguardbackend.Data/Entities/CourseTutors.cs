using AutoMapper;
using qguardbackend.Data.Abstracts;
using examportal.Data.Entities;

namespace qguardbackend.Data.Entities
{
    public class CourseTutors : BaseEntity
    {

        public long? CourseId { get; set; }
        public Course Course { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
        public long? TutorId { get; set; }
        public Tutor Tutor { get; set; }

    }
}
