using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Course : BaseEntity
    {
        public string Name { get; set; }
        public string CourseCode { get; set; }
        public string Description { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
        public ICollection<QuestionBank> Questions { get; set; }
    }
}
