using AutoMapper;
using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CourseLevel : BaseEntity
    {
        public long? CourseId { get; set; }
        public Course Course { get; set; }

        public long? LevelId { get; set; }
        public Level Level { get; set; }

        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }

    }
}
