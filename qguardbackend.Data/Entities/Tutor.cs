using qguardbackend.Data.Abstracts;
using examportal.Data.Entities;

namespace qguardbackend.Data.Entities
{
    public class Tutor : BaseEntity
    {
        public static Tutor Create(String TutorName, long InstitutionId)
        {
            return new Tutor()
            {
                TutorName = TutorName,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = InstitutionId
            };
        }
        public string TutorName { get; set; }
 
        public long? DepartmentId { get; set; }
        public Department Department { get; set; }

        public string? ApplicationUserId { get; set; }
        public ApplicationUser ApplicationUser { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
