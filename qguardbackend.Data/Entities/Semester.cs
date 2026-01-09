using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Semester : BaseEntity
    {
        public static Semester Create(String name, long InstitutionId)
        {
            return new Semester()
            {
                SemesterName = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = InstitutionId,
            };
        }
        public string SemesterName { get; set; }
        public string SemesterDescription { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
