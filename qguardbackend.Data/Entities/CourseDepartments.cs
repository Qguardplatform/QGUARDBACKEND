using AutoMapper;
using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class CourseDepartments : BaseEntity
    {

        public long? CourseId { get; set; }
        public Course Course { get; set; }

        public long? DepartmentId { get; set; }
        public Department Department { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
