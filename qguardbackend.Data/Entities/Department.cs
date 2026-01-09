using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Department : BaseEntity
    {
        public static Department Create(String name, long facultyId)
        {
            return new Department()
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                FacultyId = facultyId,
            };
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? FacultyId { get; set; }
        public Faculty Faculty { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
