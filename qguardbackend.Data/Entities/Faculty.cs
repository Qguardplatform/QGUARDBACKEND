using qguardbackend.Data.Abstracts;

namespace qguardbackend.Data.Entities
{
    public class Faculty : BaseEntity
    {
        public static Faculty Create(String name, long institutionId)
        {
            return new Faculty()
            {
                Name = name,
                IsActive = true,
                IsDeleted = false,
                CreatedAt = DateTime.UtcNow,
                InstitutionId = institutionId
            };
        }
        public string Name { get; set; }
        public string Description { get; set; }
        public long? InstitutionId { get; set; }
        public Institution Institution { get; set; }
    }
}
